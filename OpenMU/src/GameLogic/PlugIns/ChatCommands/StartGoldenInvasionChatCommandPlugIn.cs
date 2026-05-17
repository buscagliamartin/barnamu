// <copyright file="StartGoldenInvasionChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.PlugIns.InvasionEvents;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// BarnaMu: GM chat command which immediately triggers the Golden Invasion on
/// the next periodic tick (within ~1s) instead of waiting for the scheduled time.
/// Usage: <c>/goldenstart</c>.
/// </summary>
[Guid("4B6F1A2C-3D8E-4F0A-9B1C-5D2E3F4A5B6C")]
[PlugIn]
[Display(Name = "Start Golden Invasion", Description = "BarnaMu GM command: forces the Golden Invasion to begin on the next tick.")]
[ChatCommandHelp(Command, typeof(Arguments), MinimumStatus)]
public class StartGoldenInvasionChatCommandPlugIn : ChatCommandPlugInBase<StartGoldenInvasionChatCommandPlugIn.Arguments>
{
    private const string Command = "/goldenstart";
    private const CharacterStatus MinimumStatus = CharacterStatus.GameMaster;

    /// <inheritdoc />
    public override string Key => Command;

    /// <inheritdoc />
    public override CharacterStatus MinCharacterStatusRequirement => MinimumStatus;

    /// <inheritdoc />
    protected override async ValueTask DoHandleCommandAsync(Player gameMaster, Arguments arguments)
    {
        // PlugInManager doesn't expose a "GetPlugIn<TConcrete>()" — only the plug-in
        // POINT (interface) is indexed. Find the active IPeriodicTaskPlugIn whose
        // runtime type is GoldenInvasionPlugIn.
        var plugin = gameMaster.GameContext.PlugInManager
            .GetActivePlugInsOf<IPeriodicTaskPlugIn>()
            .OfType<GoldenInvasionPlugIn>()
            .FirstOrDefault();

        if (plugin is null)
        {
            await gameMaster.ShowBlueMessageAsync("Golden Invasion plugin no esta cargado o esta desactivado.").ConfigureAwait(false);
            return;
        }

        plugin.ForceStart();
        await gameMaster.ShowBlueMessageAsync("Golden Invasion forzada — arranca en el proximo tick (~1s).").ConfigureAwait(false);
    }

    /// <summary>Arguments for the <c>/goldenstart</c> command (none).</summary>
    public class Arguments : ArgumentsBase
    {
    }
}
