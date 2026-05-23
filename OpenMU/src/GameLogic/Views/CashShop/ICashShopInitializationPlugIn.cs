// <copyright file="ICashShopInitializationPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views.CashShop;

/// <summary>
/// Interface of a view whose implementation initializes the in-game cash shop on the client.
/// </summary>
public interface ICashShopInitializationPlugIn : IViewPlugIn
{
    /// <summary>
    /// Sends the initial cash shop configuration to the client.
    /// </summary>
    ValueTask InitializeCashShopAsync();
}
