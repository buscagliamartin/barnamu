// <copyright file="DuelRankChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// BarnaMu: chat command which shows the player their own Duel Ladder standing
/// (reset bracket, skill tier, rating, wins, losses).
/// </summary>
[Guid("D5E10AF1-7B2F-4E1C-9D34-A1B2C3D4E5F6")]
[PlugIn]
[Display(Name = nameof(DuelRankChatCommandPlugIn), Description = "BarnaMu: shows the player's Duel Ladder rank (/duelrank).")]
public class DuelRankChatCommandPlugIn : IChatCommandPlugIn
{
    private const string Command = "/duelrank";

    /// <inheritdoc/>
    public string Key => Command;

    /// <inheritdoc/>
    public CharacterStatus MinCharacterStatusRequirement => CharacterStatus.Normal;

    /// <inheritdoc/>
    public async ValueTask HandleCommandAsync(Player player, string command)
    {
        if (player.SelectedCharacter is not { } character)
        {
            return;
        }

        var resets = player.Attributes is null ? 0 : (int)player.Attributes[Stats.Resets];
        var bracket = DuelLadderService.GetResetBracket(resets);
        var rating = character.DuelRating > 0 ? character.DuelRating : DuelLadderService.BaseRating;
        var tier = DuelLadderService.GetSkillTier(rating);

        await SendAsync(player, "=== Duel Ladder ===").ConfigureAwait(false);
        await SendAsync(player, $"Reset Bracket: T{bracket}   Skill Tier: {tier}").ConfigureAwait(false);
        await SendAsync(player, $"Rating: {rating}").ConfigureAwait(false);
        await SendAsync(player, $"Wins: {character.DuelWins}   Losses: {character.DuelLosses}").ConfigureAwait(false);
    }

    private static async ValueTask SendAsync(Player player, string message)
    {
        await player.InvokeViewPlugInAsync<IShowMessagePlugIn>(p => p.ShowMessageAsync(message, MessageType.BlueNormal)).ConfigureAwait(false);
    }
}
