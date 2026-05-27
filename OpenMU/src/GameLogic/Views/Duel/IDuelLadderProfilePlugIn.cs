// <copyright file="IDuelLadderProfilePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views.Duel;

/// <summary>
/// BarnaMu: view plugin which sends the player's own Duel Ladder profile to the client
/// as a 0xBF / sub-code 0x31, op=1 packet.
/// </summary>
public interface IDuelLadderProfilePlugIn : IViewPlugIn
{
    /// <summary>
    /// Sends the player's own Duel Ladder profile to the client.
    /// </summary>
    /// <param name="bracket">The character's current reset bracket (1-5).</param>
    /// <param name="skillTier">The skill tier mapped from the character's rating (<see cref="DuelSkillTier"/> as a byte).</param>
    /// <param name="rating">The character's current ELO rating.</param>
    /// <param name="wins">The character's current-season wins.</param>
    /// <param name="losses">The character's current-season losses.</param>
    /// <param name="rankInBracket">The character's 1-based rank within their reset bracket.</param>
    /// <returns>The value task.</returns>
    ValueTask ShowProfileAsync(byte bracket, byte skillTier, int rating, int wins, int losses, ushort rankInBracket);
}
