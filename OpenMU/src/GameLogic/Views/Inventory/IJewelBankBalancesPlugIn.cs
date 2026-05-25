// <copyright file="IJewelBankBalancesPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views.Inventory;

/// <summary>
/// Interface of a view whose implementation sends the current MU Helper jewel bank balances to the client.
/// </summary>
public interface IJewelBankBalancesPlugIn : IViewPlugIn
{
    /// <summary>
    /// Sends the current per-account jewel bank balances to the player's client.
    /// </summary>
    /// <returns>The value task.</returns>
    ValueTask ShowBalancesAsync();
}
