// <copyright file="StartT9BossInvasionChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.PlugIns.InvasionEvents;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// BarnaMu: GM chat command which immediately triggers the T9 Boss Invasion
/// (Selupan, Erohim, Dark Elf) on the next periodic tick (within ~1s) instead
/// of waiting for the scheduled time.
/// Usage: <c>/t9start</c>.
/// </summary>
[Guid("A1B2C3D4-5E6F-4789-89AB-CDEF01234567")]
[PlugIn]
[Display(Name = "Start T9 Boss Invasion", Description = "BarnaMu GM command: forces the T9 Boss Invasion (Selupan / Erohim / Dark Elf) to begin on the next tick.")]
[ChatCommandHelp(Command, typeof(Arguments), MinimumStatus)]
public class StartT9BossInvasionChatCommandPlugIn : ChatCommandPlugInBase<StartT9BossInvasionChatCommandPlugIn.Arguments>
{
    private const string Command = "/t9start";
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
            .OfType<T9BossInvasionPlugIn>()
            .FirstOrDefault();

        if (plugin is null)
        {
            await gameMaster.ShowBlueMessageAsync("T9 Boss Invasion plugin no esta cargado o esta desactivado.").ConfigureAwait(false);
            return;
        }

        plugin.ForceStart();
        await gameMaster.ShowBlueMessageAsync("T9 Boss Invasion forzada — Selupan / Erohim / Dark Elf en el proximo tick (~1s).").ConfigureAwait(false);
    }

    /// <summary>Arguments for the <c>/t9start</c> command (none).</summary>
    public class Arguments : ArgumentsBase
    {
    }
}
