// <copyright file="PartyAutoMode.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic;

/// <summary>
/// Defines the automatic party request response mode for a player.
/// </summary>
public enum PartyAutoMode
{
    /// <summary>Normal mode: show the party request popup.</summary>
    Normal,

    /// <summary>Auto-accept all incoming party requests.</summary>
    AutoAccept,

    /// <summary>Auto-decline all incoming party requests.</summary>
    AutoDecline,
}
