// <copyright file="GensNpcPlugin.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;
using MUnique.OpenMU.Interfaces;

/// <summary>
/// Plugin to handle talking to Gens NPCs.
/// </summary>
[Guid("4A198B22-83A4-4E6D-A081-3E2A5D8B6F9A")]
[PlugIn]
public class GensNpcPlugin : IPlayerTalkToNpcPlugIn
{
    /// <inheritdoc />
    public async ValueTask PlayerTalksToNpcAsync(Player player, NonPlayerCharacter npc, NpcTalkEventArgs eventArgs)
    {
        if (npc.Definition.Number != 543 && npc.Definition.Number != 544)
        {
            return;
        }

        eventArgs.HasBeenHandled = true;
        
        string gensName = npc.Definition.Number == 543 ? "Duprian" : "Vanert";
        
        await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.TalkingNotImplementedFormat), npc.Definition.Number, npc.Definition.Designation).ConfigureAwait(false);
        
        // In the future, we could implement joining here.
        // For now, at least the NPC responds and doesn't just do nothing.
    }
}
