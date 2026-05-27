// <copyright file="DuelLadderQueryAction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions;

using Microsoft.Extensions.Logging;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.Views.Duel;

// Disambiguate: 'Character' here is the entity, not the PlayerActions.Character sub-namespace.
using CharacterEntity = MUnique.OpenMU.DataModel.Entities.Character;

/// <summary>
/// BarnaMu: a single Duel Ladder leaderboard entry passed from the action layer
/// to the view layer.
/// </summary>
public sealed class DuelLadderEntry
{
    /// <summary>Gets the character name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the MU class number (from <see cref="DataModel.Configuration.CharacterClass.Number"/>).</summary>
    public byte ClassNumber { get; init; }

    /// <summary>Gets the current ELO rating.</summary>
    public int Rating { get; init; }

    /// <summary>Gets the current-season wins.</summary>
    public int Wins { get; init; }

    /// <summary>Gets the current-season losses.</summary>
    public int Losses { get; init; }
}

/// <summary>
/// BarnaMu: serves the in-game Duel Ladder window's two queries:
/// top-10 per reset bracket, and the player's own profile (with rank-in-bracket).
/// Reads via a typed Character context so configuration is not eagerly tracked.
/// </summary>
public class DuelLadderQueryAction
{
    private const int TopN = 10;
    private const byte MinBracket = 1;
    private const byte MaxBracket = 5;

    /// <summary>
    /// Loads the top-10 characters in the requested reset bracket (ordered by rating)
    /// and sends them to the client via <see cref="IDuelLadderTopPlugIn"/>.
    /// </summary>
    /// <param name="player">The requesting player.</param>
    /// <param name="bracket">The reset bracket id (1-5).</param>
    public async ValueTask QueryTopAsync(Player player, byte bracket)
    {
        if (bracket < MinBracket || bracket > MaxBracket)
        {
            return;
        }

        try
        {
            using var context = player.GameContext.PersistenceContextProvider.CreateNewTypedContext(typeof(CharacterEntity), useCache: false, player.GameContext.Configuration);
            var characters = await context.GetAsync<CharacterEntity>().ConfigureAwait(false);

            var entries = characters
                .Where(c => c.DuelResetBracket == bracket && (c.DuelWins + c.DuelLosses) > 0)
                .OrderByDescending(c => c.DuelRating)
                .ThenBy(c => c.Name)
                .Take(TopN)
                .Select(c => new DuelLadderEntry
                {
                    Name = c.Name ?? string.Empty,
                    ClassNumber = c.CharacterClass is { } cc ? (byte)cc.Number : (byte)0,
                    Rating = c.DuelRating,
                    Wins = c.DuelWins,
                    Losses = c.DuelLosses,
                })
                .ToList();

            await player.InvokeViewPlugInAsync<IDuelLadderTopPlugIn>(p => p.ShowTopAsync(bracket, entries)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            player.Logger?.LogError(ex, "Duel Ladder: failed to query top-{N} for bracket {Bracket}.", TopN, bracket);
        }
    }

    /// <summary>
    /// Loads the player's own Duel Ladder profile (bracket, skill tier, rating, W/L,
    /// rank-in-bracket) and sends it to the client via <see cref="IDuelLadderProfilePlugIn"/>.
    /// </summary>
    /// <param name="player">The requesting player.</param>
    public async ValueTask QueryProfileAsync(Player player)
    {
        if (player.SelectedCharacter is not { } selectedCharacter)
        {
            return;
        }

        try
        {
            var resets = player.Attributes is null ? 0 : (int)player.Attributes[Stats.Resets];
            var bracket = DuelLadderService.GetResetBracket(resets);
            var rating = selectedCharacter.DuelRating > 0 ? selectedCharacter.DuelRating : DuelLadderService.BaseRating;
            var skillTier = (byte)DuelLadderService.GetSkillTier(rating);

            using var context = player.GameContext.PersistenceContextProvider.CreateNewTypedContext(typeof(CharacterEntity), useCache: false, player.GameContext.Configuration);
            var characters = await context.GetAsync<CharacterEntity>().ConfigureAwait(false);

            // Count characters in the same bracket with a strictly higher rating
            // among those who actually participated this season.
            var ahead = characters.Count(c =>
                c.DuelResetBracket == bracket
                && (c.DuelWins + c.DuelLosses) > 0
                && c.DuelRating > rating);
            var rankInBracket = (ushort)Math.Min(ushort.MaxValue, ahead + 1);

            await player.InvokeViewPlugInAsync<IDuelLadderProfilePlugIn>(
                p => p.ShowProfileAsync(bracket, skillTier, rating, selectedCharacter.DuelWins, selectedCharacter.DuelLosses, rankInBracket))
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            player.Logger?.LogError(ex, "Duel Ladder: failed to query own profile.");
        }
    }
}
