// <copyright file="WarpAction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions;

using System.Diagnostics.CodeAnalysis;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.Interfaces;

/// <summary>
/// Action to warp to another place.
/// </summary>
public class WarpAction
{
    /// <summary>
    /// Warps the player.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="warpInfo">The warp information.</param>
    public async ValueTask WarpToAsync(Player player, WarpInfo warpInfo)
    {
        if (this.CheckRequirements(player, warpInfo, out var errorMessage))
        {
            await player.WarpToAsync(warpInfo.Gate!).ConfigureAwait(false);
        }
        else
        {
            await player.ShowBlueMessageAsync(errorMessage).ConfigureAwait(false);
        }
    }

    private bool CheckRequirements(Player player, WarpInfo warpInfo, [MaybeNullWhen(true)] out string errorMessage)
    {
        errorMessage = null;

        var levelReq = warpInfo.LevelRequirement;
        var isVip = player.Account?.State == AccountState.Vip
            || player.Account?.State == AccountState.GameMaster
            || player.Account?.State == AccountState.GameMasterInvisible;

        if (isVip)
        {
            // BarnaMu (2026-05): tabla de niveles VIP de entrada por mapa.
            // OJO: map 37 = Kanturu Ruins (NO Vulcanus), Vulcanus = 63.
            // El switch anterior tenía 37 marcado como Vulcanus y nunca mencionaba 63,
            // así que VIPs no entraban a Vulcanus con bonus y eran castigados en Kanturu Ruins.
            levelReq = warpInfo.Gate?.Map?.Number switch
            {
                37 => 130, // Kanturu Ruins
                38 => 200, // Kanturu Relics
                80 => 160, // Karutan 1
                81 => 160, // Karutan 2
                57 => 240, // Raklion (La Cleon)
                63 => 260, // Vulcanus
                31 => 250, // Land of Trials (Erohim)
                34 => 280, // Crywolf Fortress
                41 => 300, // Barracks of Balgass
                42 => 300, // Balgass Refuge
                56 => 300, // Swamp of Calmness
                _ => levelReq,
            };
        }

        var requirement = player.SelectedCharacter?.GetEffectiveMoveLevelRequirement(levelReq);
        if (requirement > player.Attributes?[Stats.Level])
        {
            errorMessage = $"You need to be level {requirement} in order to warp";
            return false;
        }

        if (warpInfo.Gate?.Map is null)
        {
            errorMessage = "The warp target is not initialized";
            return false;
        }

        if (warpInfo.Gate.Map.TryGetRequirementError(player, out var message))
        {
            errorMessage = message;
            return false;
        }

        // Money check should be last to avoid getting zen when other checks failed
        if (!this.CheckMoneyRequirement(player, warpInfo))
        {
            errorMessage = $"You need {warpInfo.Costs} in order to warp";
            return false;
        }

        return true;
    }

    private bool CheckMoneyRequirement(Player player, WarpInfo warpInfo)
    {
        if (player.TryRemoveMoney(warpInfo.Costs))
        {
            return true;
        }

        return false;
    }
}