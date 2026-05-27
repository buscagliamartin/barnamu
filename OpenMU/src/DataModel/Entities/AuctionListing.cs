// <copyright file="AuctionListing.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.DataModel.Entities;

/// <summary>
/// Supported auction house listing currencies.
/// </summary>
public enum AuctionCurrency
{
    /// <summary>
    /// Zen from the character inventory money.
    /// </summary>
    Zen,

    /// <summary>
    /// Account W Coin.
    /// </summary>
    WCoin,

    /// <summary>
    /// Jewel bank balance.
    /// </summary>
    Jewel,
}

/// <summary>
/// Auction listing lifecycle status.
/// </summary>
public enum AuctionListingStatus
{
    /// <summary>
    /// Listed and buyable.
    /// </summary>
    Active,

    /// <summary>
    /// Sold, with delivery and/or payout still pending.
    /// </summary>
    Sold,

    /// <summary>
    /// Cancelled and returned to the seller.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Expired and waiting for seller return.
    /// </summary>
    Expired,

    /// <summary>
    /// Sold listing where buyer delivery and seller payout are both complete.
    /// </summary>
    Completed,
}

/// <summary>
/// DB-backed auction house listing which holds a real item in escrow.
/// </summary>
[AggregateRoot]
public class AuctionListing
{
    /// <summary>
    /// Gets or sets the human-facing listing number.
    /// </summary>
    public long ListingNumber { get; set; }

    /// <summary>
    /// Gets or sets the seller account identifier.
    /// </summary>
    public Guid SellerAccountId { get; set; }

    /// <summary>
    /// Gets or sets the seller character identifier.
    /// </summary>
    public Guid SellerCharacterId { get; set; }

    /// <summary>
    /// Gets or sets the seller character name.
    /// </summary>
    [Required]
    public string SellerCharacterName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the buyer account identifier.
    /// </summary>
    public Guid? BuyerAccountId { get; set; }

    /// <summary>
    /// Gets or sets the buyer character identifier.
    /// </summary>
    public Guid? BuyerCharacterId { get; set; }

    /// <summary>
    /// Gets or sets the buyer character name.
    /// </summary>
    public string BuyerCharacterName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the escrowed item. This is the real item object, not a rebuilt copy.
    /// </summary>
    public virtual Item? EscrowItem { get; set; }

    /// <summary>
    /// Gets or sets a display snapshot of the escrowed item.
    /// </summary>
    [Required]
    public string ItemDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the item definition group.
    /// </summary>
    public byte ItemGroup { get; set; }

    /// <summary>
    /// Gets or sets the item definition number.
    /// </summary>
    public short ItemNumber { get; set; }

    /// <summary>
    /// Gets or sets the item level.
    /// </summary>
    public byte ItemLevel { get; set; }

    /// <summary>
    /// Gets or sets the price paid by the buyer.
    /// </summary>
    public long Price { get; set; }

    /// <summary>
    /// Gets or sets the sales fee removed from the seller payout.
    /// </summary>
    public long FeeAmount { get; set; }

    /// <summary>
    /// Gets or sets the amount claimable by the seller.
    /// </summary>
    public long SellerPayoutAmount { get; set; }

    /// <summary>
    /// Gets or sets the listing currency.
    /// </summary>
    public AuctionCurrency Currency { get; set; }

    /// <summary>
    /// Gets or sets the jewel bank slot when <see cref="Currency"/> is <see cref="AuctionCurrency.Jewel"/>.
    /// </summary>
    public int? JewelBankSlot { get; set; }

    /// <summary>
    /// Gets or sets the listing status.
    /// </summary>
    public AuctionListingStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the UTC expiry timestamp.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC sold timestamp.
    /// </summary>
    public DateTime? SoldAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC cancelled timestamp.
    /// </summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC buyer delivery timestamp.
    /// </summary>
    public DateTime? DeliveryClaimedAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC seller payout timestamp.
    /// </summary>
    public DateTime? SellerPayoutClaimedAt { get; set; }
}
