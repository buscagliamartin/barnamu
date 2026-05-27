// <copyright file="IDuelLadderTopPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views.Duel;

using MUnique.OpenMU.GameLogic.PlayerActions;

/// <summary>
/// BarnaMu: view plugin which sends the top-10 of a Duel Ladder reset bracket to the client
/// as a 0xBF / sub-code 0x31, op=0 packet.
/// </summary>
public interface IDuelLadderTopPlugIn : IViewPlugIn
{
    /// <summary>
    /// Sends the top-10 entries for the given reset bracket to the client.
    /// </summary>
    /// <param name="bracket">The reset bracket id (1-5) the entries belong to.</param>
    /// <param name="entries">The top entries ordered by descending rating.</param>
    /// <returns>The value task.</returns>
    ValueTask ShowTopAsync(byte bracket, IReadOnlyList<DuelLadderEntry> entries);
}
