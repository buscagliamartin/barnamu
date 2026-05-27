// <copyright file="IAuctionHouseViewPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views.AuctionHouse;

using MUnique.OpenMU.DataModel.Entities;

/// <summary>
/// Interface of a view whose implementation sends Auction House data to the client.
/// </summary>
public interface IAuctionHouseViewPlugIn : IViewPlugIn
{
    /// <summary>
    /// Shows a page of auction house listings.
    /// </summary>
    /// <param name="view">The client view id.</param>
    /// <param name="page">The current page.</param>
    /// <param name="listings">The listings.</param>
    /// <returns>The value task.</returns>
    ValueTask ShowListingsAsync(byte view, byte page, IReadOnlyList<AuctionListing> listings);

    /// <summary>
    /// Shows a status message in the Auction House window.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <returns>The value task.</returns>
    ValueTask ShowMessageAsync(string message);
}
