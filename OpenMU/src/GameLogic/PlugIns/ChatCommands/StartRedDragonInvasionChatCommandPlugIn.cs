// <copyright file="StartRedDragonInvasionChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.PlugIns.InvasionEvents;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// BarnaMu: GM chat command which immediately triggers the Red Dragon Invasion
/// on the next periodic tick (within ~1s) instead of waiting for the scheduled time.
/// Usage: <c>/reddragonstart</c>.
/// </summary>
[Guid("8C2D5E1F-3A4B-4C5D-9E6F-7A8B9C0D1E2F")]
[PlugIn]
[Display(Name = "Start Red Dragon Invasion", Description = "BarnaMu GM command: forces the Red Dragon Invasion to begin on the next tick.")]
[ChatCommandHelp(Command, typeof(Arguments), MinimumStatus)]
public class StartRedDragonInvasionChatCommandPlugIn : ChatCommandPlugInBase<StartRedDragonInvasionChatCommandPlugIn.Arguments>
{
    private const string Command = "/reddragonstart";
    private const CharacterStatus MinimumStatus = CharacterStatus.GameMaster;

    /// <inheritdoc />
    public override string Key => Command;

    /// <inheritdoc />
    public override CharacterStatus MinCharacterStatusRequirement => MinimumStatus;

    /// <inheritdoc />
    protected override async ValueTask DoHandleCommandAsync(Player gameMaster, Arguments arguments)
    {
        // See note in StartGoldenInvasionChatCommandPlugIn — same pattern.
        var plugin = gameMaster.GameContext.PlugInManager
            .GetActivePlugInsOf<IPeriodicTaskPlugIn>()
            .OfType<RedDragonInvasionPlugIn>()
            .FirstOrDefault();

        if (plugin is null)
        {
            await gameMaster.ShowBlueMessageAsync("Red Dragon Invasion plugin no esta cargado o esta desactivado.").ConfigureAwait(false);
            return;
        }

        plugin.ForceStart();
        await gameMaster.ShowBlueMessageAsync("Red Dragon Invasion forzada — arranca en el proximo tick (~1s).").ConfigureAwait(false);
    }

    /// <summary>Arguments for the <c>/reddragonstart</c> command (none).</summary>
    public class Arguments : ArgumentsBase
    {
    }
}
