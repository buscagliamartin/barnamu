// <copyright file="DuelLadderService.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic;

using Microsoft.Extensions.Logging;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Persistence;

/// <summary>
/// BarnaMu: skill tier brackets for the Duel Ladder (mapped from ELO rating).
/// </summary>
public enum DuelSkillTier : byte
{
    /// <summary>Below 1200 rating.</summary>
    Bronze,

    /// <summary>1200-1399.</summary>
    Silver,

    /// <summary>1400-1599.</summary>
    Gold,

    /// <summary>1600-1799.</summary>
    Platinum,

    /// <summary>1800-1999.</summary>
    Diamond,

    /// <summary>2000+.</summary>
    Master,
}

/// <summary>
/// BarnaMu: Duel Ladder service. Records ranked duel results, applies ELO updates with
/// a three-layer anti-farm guard (different account, different IP, same reset bracket),
/// and detects reset-bracket promotions (rating and W/L are reset when a character moves
/// up a bracket so they start fresh in the new tier).
/// </summary>
public static class DuelLadderService
{
    /// <summary>
    /// Starting rating for new characters and after a bracket promotion.
    /// </summary>
    public const int BaseRating = 1200;

    /// <summary>
    /// Minimum rating (rating cannot fall below this).
    /// </summary>
    public const int MinRating = 100;

    /// <summary>
    /// ELO K-factor: how much a single duel result moves the rating.
    /// </summary>
    public const int KFactor = 32;

    /// <summary>
    /// Maps a character's reset count to its competitive bracket (1-5).
    /// Tier 1: 0-5 resets, Tier 2: 6-15, Tier 3: 16-30, Tier 4: 31-50, Tier 5: 51+.
    /// </summary>
    /// <param name="resets">The character's current reset count.</param>
    /// <returns>The reset bracket id (1-5).</returns>
    public static byte GetResetBracket(int resets) => resets switch
    {
        <= 5 => 1,
        <= 15 => 2,
        <= 30 => 3,
        <= 50 => 4,
        _ => 5,
    };

    /// <summary>
    /// Maps an ELO rating to the displayed skill tier (Bronze .. Master).
    /// </summary>
    /// <param name="rating">The ELO rating.</param>
    /// <returns>The skill tier.</returns>
    public static DuelSkillTier GetSkillTier(int rating) => rating switch
    {
        < 1200 => DuelSkillTier.Bronze,
        < 1400 => DuelSkillTier.Silver,
        < 1600 => DuelSkillTier.Gold,
        < 1800 => DuelSkillTier.Platinum,
        < 2000 => DuelSkillTier.Diamond,
        _ => DuelSkillTier.Master,
    };

    /// <summary>
    /// Records a ranked duel result: updates both characters' rating + W/L when all anti-farm
    /// checks pass. Safe to call directly from the duel-finish hook; never throws.
    /// </summary>
    /// <param name="winner">The winner of the duel.</param>
    /// <param name="loser">The loser of the duel.</param>
    /// <returns>The value task.</returns>
    public static ValueTask RecordResultAsync(Player winner, Player loser)
    {
        try
        {
            RecordResultCore(winner, loser);
        }
        catch (Exception ex)
        {
            winner?.Logger?.LogError(ex, "Duel Ladder: error while recording duel result.");
        }

        return ValueTask.CompletedTask;
    }

    private static void RecordResultCore(Player winner, Player loser)
    {
        if (winner.SelectedCharacter is not { } winnerChar
            || loser.SelectedCharacter is not { } loserChar)
        {
            return;
        }

        // Initialise rating for never-dueled characters (column default is 1200 but be safe).
        if (winnerChar.DuelRating <= 0)
        {
            winnerChar.DuelRating = BaseRating;
        }

        if (loserChar.DuelRating <= 0)
        {
            loserChar.DuelRating = BaseRating;
        }

        // Detect bracket promotion for both characters BEFORE applying ELO, so a promoted
        // character starts fresh in their new bracket.
        var winnerBracket = GetResetBracket(GetResets(winner));
        var loserBracket = GetResetBracket(GetResets(loser));
        ApplyBracketPromotion(winnerChar, winnerBracket);
        ApplyBracketPromotion(loserChar, loserBracket);

        // --- Anti-farm layer 1: same-account block.
        if (winner.Account is { } winnerAccount
            && loser.Account is { } loserAccount)
        {
            var winnerAccountId = winnerAccount.GetId();
            if (winnerAccountId != Guid.Empty && winnerAccountId == loserAccount.GetId())
            {
                return;
            }
        }

        // --- Anti-farm layer 2: same-IP no rating change.
        if (winner.RemoteAddress is { } winIp
            && loser.RemoteAddress is { } loseIp
            && winIp.Equals(loseIp))
        {
            return;
        }

        // --- Anti-farm layer 3: cross-bracket duels do not affect rating.
        if (winnerBracket != loserBracket)
        {
            return;
        }

        // ELO update.
        int winnerRating = winnerChar.DuelRating;
        int loserRating = loserChar.DuelRating;
        double winnerExpected = 1.0 / (1.0 + Math.Pow(10.0, (loserRating - winnerRating) / 400.0));
        double loserExpected = 1.0 - winnerExpected;

        int winnerDelta = (int)Math.Round(KFactor * (1.0 - winnerExpected));
        int loserDelta = (int)Math.Round(KFactor * (0.0 - loserExpected));

        winnerChar.DuelRating = Math.Max(MinRating, winnerRating + winnerDelta);
        loserChar.DuelRating = Math.Max(MinRating, loserRating + loserDelta);
        winnerChar.DuelWins += 1;
        loserChar.DuelLosses += 1;
    }

    private static void ApplyBracketPromotion(Character character, byte currentBracket)
    {
        if (character.DuelResetBracket == 0)
        {
            // First ranked duel ever for this character: record the bracket without resetting.
            character.DuelResetBracket = currentBracket;
            return;
        }

        if (character.DuelResetBracket == currentBracket)
        {
            return;
        }

        // Reset count crossed into a new bracket - clean slate in the new tier.
        character.DuelRating = BaseRating;
        character.DuelWins = 0;
        character.DuelLosses = 0;
        character.DuelResetBracket = currentBracket;
    }

    private static int GetResets(Player player)
    {
        if (player.Attributes is null)
        {
            return 0;
        }

        return (int)player.Attributes[Stats.Resets];
    }
}
