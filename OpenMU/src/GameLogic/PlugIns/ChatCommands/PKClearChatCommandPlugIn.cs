// <copyright file="PKClearChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Views.Character;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A chat command plugin which clears the PK status of the calling player.
/// BarnaMu: simplificado a "solo el propio jugador" + cobra zen escalado por PK count
/// (100.000 zen por cada PK que tenga el personaje). El old behavior de pasar otro
/// personaje como target queda fuera — para limpiar PKs ajenos un GM puede usar SQL
/// directo (UPDATE data."Character" SET "PlayerKillCount"=0, "State"=0 WHERE...).
/// </summary>
[Guid("EB97A8F6-F6BD-460A-BCBE-253BF679361A")]
[PlugIn]
[Display(Name = nameof(PlugInResources.PkClearChatCommandPlugIn_Name), Description = nameof(PlugInResources.PkClearChatCommandPlugIn_Description), ResourceType = typeof(PlugInResources))]
[ChatCommandHelp(Command, typeof(Arguments), CharacterStatus.Normal)]
public class PkClearChatCommandPlugIn : ChatCommandPlugInBase<PkClearChatCommandPlugIn.Arguments>
{
    private const string Command = "/pkclear";

    /// <summary>BarnaMu: zen por unidad de PlayerKillCount.</summary>
    private const int ZenCostPerPk = 100_000;

    /// <inheritdoc />
    public override string Key => Command;

    /// <inheritdoc/>
    public override CharacterStatus MinCharacterStatusRequirement => CharacterStatus.Normal;

    /// <inheritdoc />
    protected override async ValueTask DoHandleCommandAsync(Player player, Arguments arguments)
    {
        if (player.SelectedCharacter is not { } character)
        {
            return;
        }

        if (character.State == HeroState.Normal && character.PlayerKillCount == 0)
        {
            await player.ShowBlueMessageAsync("No tenes PKs para limpiar.").ConfigureAwait(false);
            return;
        }

        // Usamos al menos 1 para el caso raro donde el State quedo "Murderer/PK" pero el
        // contador es 0; igual cobramos el costo minimo (no queremos /pkclear gratis).
        var effectivePkCount = Math.Max(1, character.PlayerKillCount);

        // Cap defensivo para evitar overflow con counts irreales (>21k PKs).
        var cost = (int)Math.Min((long)ZenCostPerPk * effectivePkCount, int.MaxValue);

        if (player.Money < cost)
        {
            await player.ShowBlueMessageAsync(
                $"No tenes zen suficiente. /pkclear cuesta {cost:N0} zen (100.000 x {effectivePkCount} PK{(effectivePkCount == 1 ? string.Empty : "s")}).")
                .ConfigureAwait(false);
            return;
        }

        player.Money -= cost;
        character.State = HeroState.Normal;
        character.StateRemainingSeconds = 0;
        character.PlayerKillCount = 0;
        await player.ForEachWorldObserverAsync<IUpdateCharacterHeroStatePlugIn>(p => p.UpdateCharacterHeroStateAsync(player), true).ConfigureAwait(false);

        await player.ShowBlueMessageAsync($"PK status limpiado. Cobrado: {cost:N0} zen.").ConfigureAwait(false);
    }

    /// <summary>
    /// Arguments for the <c>/pkclear</c> command (none — solo limpia el propio jugador).
    /// </summary>
    public class Arguments : ArgumentsBase
    {
    }
}
