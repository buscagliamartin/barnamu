// <copyright file="AuctionHouseService.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.AuctionHouse;

using System.Threading;
using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.PlayerActions.CashShop;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.Persistence;

/// <summary>
/// DB-backed auction house service with real item escrow.
/// </summary>
public class AuctionHouseService
{
    /// <summary>
    /// Default listing duration.
    /// </summary>
    public static readonly TimeSpan ListingDuration = TimeSpan.FromDays(7);

    private const int MaxActiveListingsPerCharacter = 10;
    private const int SalesTaxPercent = 5;
    private const long MaxCurrencyAmount = 2_000_000_000;
    private static readonly SemaphoreSlim Lock = new(1, 1);

    /// <summary>
    /// Lists a backpack item in the auction house.
    /// </summary>
    public async ValueTask<string> CreateListingAsync(Player player, byte itemSlot, AuctionCurrency currency, long price, int? jewelBankSlot)
    {
        if (player.Account is null || player.Inventory is null || player.SelectedCharacter is null)
        {
            return "Auction House: character is not ready.";
        }

        if (!this.IsBackpackSlot(itemSlot))
        {
            return "Auction House: only backpack items can be listed.";
        }

        if (!this.IsValidPrice(currency, price, jewelBankSlot, out var priceError))
        {
            return priceError;
        }

        var item = player.Inventory.GetItem(itemSlot);
        if (item?.Definition is null)
        {
            return $"Auction House: no item found in slot {itemSlot}.";
        }

        if (item.Definition.IsBoundToCharacter)
        {
            return "Auction House: bound items cannot be listed.";
        }

        await Lock.WaitAsync().ConfigureAwait(false);
        try
        {
            var listings = (await player.PersistenceContext.GetAsync<AuctionListing>().ConfigureAwait(false)).ToList();
            await this.MarkExpiredAsync(player.PersistenceContext, listings).ConfigureAwait(false);

            var characterId = player.SelectedCharacter.GetId();
            var activeListings = listings.Count(listing =>
                listing.SellerCharacterId == characterId
                && listing.Status == AuctionListingStatus.Active);
            if (activeListings >= MaxActiveListingsPerCharacter)
            {
                return $"Auction House: maximum {MaxActiveListingsPerCharacter} active listings per character.";
            }

            item = player.Inventory.GetItem(itemSlot);
            if (item?.Definition is null)
            {
                return $"Auction House: no item found in slot {itemSlot}.";
            }

            var now = DateTime.UtcNow;
            var listing = player.PersistenceContext.CreateNew<AuctionListing>();
            listing.ListingNumber = listings.Count == 0 ? 1 : listings.Max(l => l.ListingNumber) + 1;
            listing.SellerAccountId = player.Account.GetId();
            listing.SellerCharacterId = characterId;
            listing.SellerCharacterName = player.SelectedCharacter.Name ?? string.Empty;
            listing.EscrowItem = item;
            listing.ItemDisplayName = item.ToString();
            listing.ItemGroup = item.Definition.Group;
            listing.ItemNumber = item.Definition.Number;
            listing.ItemLevel = item.Level;
            listing.Price = price;
            listing.Currency = currency;
            listing.JewelBankSlot = jewelBankSlot;
            listing.Status = AuctionListingStatus.Active;
            listing.CreatedAt = now;
            listing.ExpiresAt = now.Add(ListingDuration);
            listing.BuyerCharacterName = string.Empty;

            item.StorePrice = null;
            var oldSlot = item.ItemSlot;
            await player.Inventory.RemoveItemAsync(item).ConfigureAwait(false);
            await player.InvokeViewPlugInAsync<IItemRemovedPlugIn>(p => p.RemoveItemAsync(oldSlot)).ConfigureAwait(false);

            await player.SaveProgressAsync().ConfigureAwait(false);
            return $"Auction House: listed #{listing.ListingNumber} {listing.ItemDisplayName} for {this.FormatPrice(listing)}.";
        }
        finally
        {
            Lock.Release();
        }
    }

    /// <summary>
    /// Gets active listings for display.
    /// </summary>
    public async ValueTask<IReadOnlyList<AuctionListing>> GetActiveListingsAsync(Player player, AuctionCurrency? currency, int page)
    {
        var listings = (await player.PersistenceContext.GetAsync<AuctionListing>().ConfigureAwait(false)).ToList();
        await this.MarkExpiredAsync(player.PersistenceContext, listings).ConfigureAwait(false);
        await player.PersistenceContext.SaveChangesAsync().ConfigureAwait(false);

        return listings
            .Where(listing => listing.Status == AuctionListingStatus.Active)
            .Where(listing => currency is null || listing.Currency == currency)
            .OrderBy(listing => listing.Price)
            .ThenBy(listing => listing.ListingNumber)
            .Skip(Math.Max(page - 1, 0) * 10)
            .Take(10)
            .ToList();
    }

    /// <summary>
    /// Gets the player's active and sold listings.
    /// </summary>
    public async ValueTask<IReadOnlyList<AuctionListing>> GetOwnListingsAsync(Player player)
    {
        if (player.SelectedCharacter is null)
        {
            return Array.Empty<AuctionListing>();
        }

        var characterId = player.SelectedCharacter.GetId();
        var listings = (await player.PersistenceContext.GetAsync<AuctionListing>().ConfigureAwait(false)).ToList();
        await this.MarkExpiredAsync(player.PersistenceContext, listings).ConfigureAwait(false);
        await player.PersistenceContext.SaveChangesAsync().ConfigureAwait(false);

        return listings
            .Where(listing => listing.SellerCharacterId == characterId)
            .Where(listing => listing.Status is AuctionListingStatus.Active or AuctionListingStatus.Expired or AuctionListingStatus.Sold)
            .OrderBy(listing => listing.ListingNumber)
            .ToList();
    }

    /// <summary>
    /// Gets pending deliveries for the buyer.
    /// </summary>
    public async ValueTask<IReadOnlyList<AuctionListing>> GetPendingDeliveriesAsync(Player player)
    {
        if (player.SelectedCharacter is null)
        {
            return Array.Empty<AuctionListing>();
        }

        var characterId = player.SelectedCharacter.GetId();
        var listings = await player.PersistenceContext.GetAsync<AuctionListing>().ConfigureAwait(false);
        return listings
            .Where(listing => listing.BuyerCharacterId == characterId)
            .Where(listing => listing.Status == AuctionListingStatus.Sold && listing.DeliveryClaimedAt is null)
            .OrderBy(listing => listing.ListingNumber)
            .ToList();
    }

    /// <summary>
    /// Gets pending seller payouts.
    /// </summary>
    public async ValueTask<IReadOnlyList<AuctionListing>> GetPendingPayoutsAsync(Player player)
    {
        if (player.SelectedCharacter is null)
        {
            return Array.Empty<AuctionListing>();
        }

        var characterId = player.SelectedCharacter.GetId();
        var listings = await player.PersistenceContext.GetAsync<AuctionListing>().ConfigureAwait(false);
        return listings
            .Where(listing => listing.SellerCharacterId == characterId)
            .Where(listing => listing.Status == AuctionListingStatus.Sold && listing.SellerPayoutClaimedAt is null)
            .OrderBy(listing => listing.ListingNumber)
            .ToList();
    }

    /// <summary>
    /// Buys an active listing.
    /// </summary>
    public async ValueTask<string> BuyAsync(Player player, long listingNumber, AuctionCurrency currency, long price, int? jewelBankSlot)
    {
        if (player.Account is null || player.SelectedCharacter is null)
        {
            return "Auction House: character is not ready.";
        }

        await Lock.WaitAsync().ConfigureAwait(false);
        try
        {
            var listings = (await player.PersistenceContext.GetAsync<AuctionListing>().ConfigureAwait(false)).ToList();
            await this.MarkExpiredAsync(player.PersistenceContext, listings).ConfigureAwait(false);
            var listing = listings.FirstOrDefault(l => l.ListingNumber == listingNumber);
            if (listing is null)
            {
                return $"Auction House: listing #{listingNumber} not found.";
            }

            if (listing.Status != AuctionListingStatus.Active || listing.ExpiresAt <= DateTime.UtcNow)
            {
                return $"Auction House: listing #{listingNumber} is not active.";
            }

            if (listing.EscrowItem is null)
            {
                return $"Auction House: listing #{listingNumber} has no escrow item.";
            }

            if (listing.SellerAccountId == player.Account.GetId())
            {
                return "Auction House: you cannot buy from your own account.";
            }

            if (listing.Currency != currency || listing.Price != price || listing.JewelBankSlot != jewelBankSlot)
            {
                return $"Auction House: confirmation mismatch. Expected {this.FormatPrice(listing)}.";
            }

            var paymentResult = await this.TryDebitBuyerAsync(player, listing).ConfigureAwait(false);
            if (paymentResult is not null)
            {
                return paymentResult;
            }

            var fee = listing.Price * SalesTaxPercent / 100;
            listing.FeeAmount = fee;
            listing.SellerPayoutAmount = listing.Price - fee;
            listing.BuyerAccountId = player.Account.GetId();
            listing.BuyerCharacterId = player.SelectedCharacter.GetId();
            listing.BuyerCharacterName = player.SelectedCharacter.Name ?? string.Empty;
            listing.SoldAt = DateTime.UtcNow;
            listing.Status = AuctionListingStatus.Sold;

            await player.SaveProgressAsync().ConfigureAwait(false);
            return $"Auction House: bought #{listing.ListingNumber}. Use /ah receive {listing.ListingNumber}.";
        }
        finally
        {
            Lock.Release();
        }
    }

    /// <summary>
    /// Cancels or returns an active/expired seller listing.
    /// </summary>
    public async ValueTask<string> CancelAsync(Player player, long listingNumber)
    {
        if (player.Inventory is null || player.SelectedCharacter is null)
        {
            return "Auction House: character is not ready.";
        }

        await Lock.WaitAsync().ConfigureAwait(false);
        try
        {
            var listings = (await player.PersistenceContext.GetAsync<AuctionListing>().ConfigureAwait(false)).ToList();
            await this.MarkExpiredAsync(player.PersistenceContext, listings).ConfigureAwait(false);
            var listing = listings.FirstOrDefault(l => l.ListingNumber == listingNumber);
            if (listing is null || listing.SellerCharacterId != player.SelectedCharacter.GetId())
            {
                return $"Auction House: listing #{listingNumber} not found for this character.";
            }

            if (listing.Status is not (AuctionListingStatus.Active or AuctionListingStatus.Expired))
            {
                return $"Auction House: listing #{listingNumber} cannot be cancelled.";
            }

            var item = listing.EscrowItem;
            if (item is null)
            {
                return $"Auction House: listing #{listingNumber} has no escrow item.";
            }

            if (player.Inventory.CheckInvSpace(item) is null)
            {
                return "Auction House: not enough inventory space to return the item.";
            }

            await player.Inventory.AddItemAsync(item).ConfigureAwait(false);
            await player.InvokeViewPlugInAsync<IItemAppearPlugIn>(p => p.ItemAppearAsync(item)).ConfigureAwait(false);

            listing.EscrowItem = null;
            listing.Status = AuctionListingStatus.Cancelled;
            listing.CancelledAt = DateTime.UtcNow;

            await player.SaveProgressAsync().ConfigureAwait(false);
            return $"Auction House: listing #{listing.ListingNumber} cancelled and item returned.";
        }
        finally
        {
            Lock.Release();
        }
    }

    /// <summary>
    /// Receives a bought item.
    /// </summary>
    public async ValueTask<string> ReceiveAsync(Player player, long listingNumber)
    {
        if (player.Inventory is null || player.SelectedCharacter is null)
        {
            return "Auction House: character is not ready.";
        }

        await Lock.WaitAsync().ConfigureAwait(false);
        try
        {
            var listings = await player.PersistenceContext.GetAsync<AuctionListing>().ConfigureAwait(false);
            var listing = listings.FirstOrDefault(l => l.ListingNumber == listingNumber);
            if (listing is null || listing.BuyerCharacterId != player.SelectedCharacter.GetId())
            {
                return $"Auction House: delivery #{listingNumber} not found for this character.";
            }

            if (listing.Status != AuctionListingStatus.Sold || listing.DeliveryClaimedAt is not null)
            {
                return $"Auction House: delivery #{listingNumber} is not pending.";
            }

            var item = listing.EscrowItem;
            if (item is null)
            {
                return $"Auction House: delivery #{listingNumber} has no escrow item.";
            }

            if (player.Inventory.CheckInvSpace(item) is null)
            {
                return "Auction House: not enough inventory space to receive the item.";
            }

            await player.Inventory.AddItemAsync(item).ConfigureAwait(false);
            await player.InvokeViewPlugInAsync<IItemAppearPlugIn>(p => p.ItemAppearAsync(item)).ConfigureAwait(false);

            listing.EscrowItem = null;
            listing.DeliveryClaimedAt = DateTime.UtcNow;
            this.CompleteIfDone(listing);

            await player.SaveProgressAsync().ConfigureAwait(false);
            return $"Auction House: received #{listing.ListingNumber} {listing.ItemDisplayName}.";
        }
        finally
        {
            Lock.Release();
        }
    }

    /// <summary>
    /// Claims a sold listing payout.
    /// </summary>
    public async ValueTask<string> ClaimPayoutAsync(Player player, long listingNumber)
    {
        if (player.Account is null || player.SelectedCharacter is null)
        {
            return "Auction House: character is not ready.";
        }

        await Lock.WaitAsync().ConfigureAwait(false);
        try
        {
            var listings = await player.PersistenceContext.GetAsync<AuctionListing>().ConfigureAwait(false);
            var listing = listings.FirstOrDefault(l => l.ListingNumber == listingNumber);
            if (listing is null || listing.SellerCharacterId != player.SelectedCharacter.GetId())
            {
                return $"Auction House: payout #{listingNumber} not found for this character.";
            }

            if (listing.Status != AuctionListingStatus.Sold || listing.SellerPayoutClaimedAt is not null)
            {
                return $"Auction House: payout #{listingNumber} is not pending.";
            }

            var payoutResult = await this.TryCreditSellerAsync(player, listing).ConfigureAwait(false);
            if (payoutResult is not null)
            {
                return payoutResult;
            }

            listing.SellerPayoutClaimedAt = DateTime.UtcNow;
            this.CompleteIfDone(listing);

            await player.SaveProgressAsync().ConfigureAwait(false);
            return $"Auction House: claimed #{listing.ListingNumber} payout of {this.FormatAmount(listing.SellerPayoutAmount, listing.Currency, listing.JewelBankSlot)}.";
        }
        finally
        {
            Lock.Release();
        }
    }

    /// <summary>
    /// Formats a listing for temporary chat-command display.
    /// </summary>
    public string FormatListing(AuctionListing listing)
    {
        return $"#{listing.ListingNumber} {listing.ItemDisplayName} | {this.FormatPrice(listing)} | Seller: {listing.SellerCharacterName} | {listing.Status}";
    }

    /// <summary>
    /// Parses a currency token.
    /// </summary>
    public bool TryParseCurrency(string value, out AuctionCurrency currency)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "zen":
                currency = AuctionCurrency.Zen;
                return true;
            case "wcoin":
            case "w":
            case "wc":
                currency = AuctionCurrency.WCoin;
                return true;
            case "jewel":
            case "jewels":
            case "j":
                currency = AuctionCurrency.Jewel;
                return true;
            default:
                currency = default;
                return false;
        }
    }

    /// <summary>
    /// Tries to resolve a jewel bank slot alias.
    /// </summary>
    public bool TryResolveJewelBankSlot(string input, out int slot)
    {
        slot = input.Trim().ToLowerInvariant() switch
        {
            "bless" or "jewelofbless" => 0,
            "soul" or "jewelofsoul" => 1,
            "life" or "jeweloflife" => 2,
            "creation" or "jewelofcreation" => 3,
            "guardian" or "jewelofguardian" => 4,
            "gemstone" or "gem" => 5,
            "harmony" or "jewelofharmony" => 6,
            "chaos" or "jewelofchaos" => 7,
            "lowref" or "lowerrefine" or "lowerrefinestone" => 8,
            "highref" or "higherrefine" or "higherrefinestone" => 9,
            "bok1" or "kundun1" => 10,
            "bok2" or "kundun2" => 11,
            "bok3" or "kundun3" => 12,
            "bok4" or "kundun4" => 13,
            "bok5" or "kundun5" => 14,
            "bluechoco" or "bluechocolate" => 15,
            "pinkchoco" or "pinkchocolate" => 16,
            _ => -1,
        };

        return slot >= 0;
    }

    private async ValueTask<string?> TryDebitBuyerAsync(Player player, AuctionListing listing)
    {
        switch (listing.Currency)
        {
            case AuctionCurrency.Zen:
                if (listing.Price > int.MaxValue || player.Money < listing.Price || !player.TryRemoveMoney((int)listing.Price))
                {
                    return $"Auction House: not enough Zen. Need {listing.Price:N0}.";
                }

                await player.InvokeViewPlugInAsync<IUpdateMoneyPlugIn>(p => p.UpdateMoneyAsync()).ConfigureAwait(false);
                return null;

            case AuctionCurrency.WCoin:
                if (player.Account is null)
                {
                    return "Auction House: not enough W Coin.";
                }

                if (!WCoinService.TryApply(player.PersistenceContext, player.Account, -listing.Price, "AuctionPurchase", "AuctionHouse", player.Name, $"Listing {listing.ListingNumber}", out var debitError))
                {
                    return debitError ?? "Auction House: not enough W Coin.";
                }

                return null;

            case AuctionCurrency.Jewel:
                if (listing.JewelBankSlot is not { } jewelSlot)
                {
                    return "Auction House: listing has no jewel currency type.";
                }

                return await this.TryDebitJewelsAsync(player, jewelSlot, listing.Price).ConfigureAwait(false);

            default:
                return "Auction House: unsupported currency.";
        }
    }

    private async ValueTask<string?> TryCreditSellerAsync(Player player, AuctionListing listing)
    {
        switch (listing.Currency)
        {
            case AuctionCurrency.Zen:
                if (listing.SellerPayoutAmount > int.MaxValue || player.Money + listing.SellerPayoutAmount > int.MaxValue || !player.TryAddMoney((int)listing.SellerPayoutAmount))
                {
                    return "Auction House: not enough Zen capacity to claim payout.";
                }

                await player.InvokeViewPlugInAsync<IUpdateMoneyPlugIn>(p => p.UpdateMoneyAsync()).ConfigureAwait(false);
                return null;

            case AuctionCurrency.WCoin:
                if (player.Account is null)
                {
                    return "Auction House: W Coin payout failed.";
                }

                if (!WCoinService.TryApply(player.PersistenceContext, player.Account, listing.SellerPayoutAmount, "AuctionSale", "AuctionHouse", "AuctionHouse", $"Listing {listing.ListingNumber}", out var creditError))
                {
                    return creditError ?? "Auction House: W Coin payout failed.";
                }

                return null;

            case AuctionCurrency.Jewel:
                if (player.Account is null || listing.JewelBankSlot is not { } jewelSlot || listing.SellerPayoutAmount > int.MaxValue)
                {
                    return "Auction House: jewel payout failed.";
                }

                if (!this.TryAddJewelBankCount(player.Account, jewelSlot, (int)listing.SellerPayoutAmount))
                {
                    return "Auction House: jewel payout failed.";
                }

                await player.InvokeViewPlugInAsync<IJewelBankBalancesPlugIn>(p => p.ShowBalancesAsync()).ConfigureAwait(false);
                return null;

            default:
                return "Auction House: unsupported payout currency.";
        }
    }

    private async ValueTask<string?> TryDebitJewelsAsync(Player player, int jewelSlot, long amount)
    {
        if (player.Account is null || player.Inventory is null || amount <= 0 || amount > int.MaxValue)
        {
            return "Auction House: invalid jewel amount.";
        }

        var needed = (int)amount;
        var bankCount = this.GetJewelBankCount(player.Account, jewelSlot);
        var inventorySingles = this.CountInventorySingles(player, jewelSlot);
        if (bankCount + inventorySingles < needed)
        {
            return $"Auction House: not enough {this.GetJewelBankSlotName(jewelSlot)}. Need {needed}.";
        }

        var fromBank = Math.Min(bankCount, needed);
        if (fromBank > 0 && !this.TryAddJewelBankCount(player.Account, jewelSlot, -fromBank))
        {
            return "Auction House: jewel bank debit failed.";
        }

        needed -= fromBank;
        while (needed > 0)
        {
            var item = this.FindInventorySingle(player, jewelSlot);
            if (item is null)
            {
                return "Auction House: jewel inventory debit failed.";
            }

            var slot = item.ItemSlot;
            await player.Inventory.RemoveItemAsync(item).ConfigureAwait(false);
            await player.InvokeViewPlugInAsync<IItemRemovedPlugIn>(p => p.RemoveItemAsync(slot)).ConfigureAwait(false);
            needed--;
        }

        if (fromBank > 0)
        {
            await player.InvokeViewPlugInAsync<IJewelBankBalancesPlugIn>(p => p.ShowBalancesAsync()).ConfigureAwait(false);
        }

        return null;
    }

    private async ValueTask MarkExpiredAsync(IContext context, IEnumerable<AuctionListing> listings)
    {
        var now = DateTime.UtcNow;
        var changed = false;
        foreach (var listing in listings.Where(l => l.Status == AuctionListingStatus.Active && l.ExpiresAt <= now))
        {
            listing.Status = AuctionListingStatus.Expired;
            changed = true;
        }

        if (changed)
        {
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    private bool IsBackpackSlot(byte itemSlot)
    {
        return itemSlot >= InventoryConstants.EquippableSlotsCount
               && itemSlot < InventoryConstants.FirstStoreItemSlotIndex;
    }

    private bool IsValidPrice(AuctionCurrency currency, long price, int? jewelBankSlot, out string error)
    {
        if (price <= 0 || price > MaxCurrencyAmount)
        {
            error = $"Auction House: price must be between 1 and {MaxCurrencyAmount:N0}.";
            return false;
        }

        if (currency == AuctionCurrency.Jewel && jewelBankSlot is not (>= 0 and <= 16))
        {
            error = "Auction House: invalid jewel currency.";
            return false;
        }

        if (currency != AuctionCurrency.Jewel && jewelBankSlot is not null)
        {
            error = "Auction House: jewel currency can only be used with jewel listings.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private string FormatPrice(AuctionListing listing)
    {
        return this.FormatAmount(listing.Price, listing.Currency, listing.JewelBankSlot);
    }

    private string FormatAmount(long amount, AuctionCurrency currency, int? jewelBankSlot)
    {
        return currency switch
        {
            AuctionCurrency.Zen => $"{amount:N0} Zen",
            AuctionCurrency.WCoin => $"{amount:N0} W Coin",
            AuctionCurrency.Jewel when jewelBankSlot is { } slot => $"{amount:N0} {this.GetJewelBankSlotName(slot)}",
            _ => $"{amount:N0} unknown",
        };
    }

    private void CompleteIfDone(AuctionListing listing)
    {
        if (listing.DeliveryClaimedAt is not null && listing.SellerPayoutClaimedAt is not null)
        {
            listing.Status = AuctionListingStatus.Completed;
        }
    }

    private string GetJewelBankSlotName(int slot) => slot switch
    {
        0 => "Jewel of Bless",
        1 => "Jewel of Soul",
        2 => "Jewel of Life",
        3 => "Jewel of Creation",
        4 => "Jewel of Guardian",
        5 => "Gemstone",
        6 => "Jewel of Harmony",
        7 => "Jewel of Chaos",
        8 => "Lower refine stone",
        9 => "Higher refine stone",
        10 => "Box of Kundun +1",
        11 => "Box of Kundun +2",
        12 => "Box of Kundun +3",
        13 => "Box of Kundun +4",
        14 => "Box of Kundun +5",
        15 => "Blue Chocolate Box",
        16 => "Pink Chocolate Box",
        _ => "Unknown jewel",
    };

    private int GetJewelBankCount(Account account, int slot) => slot switch
    {
        0 => account.JewelBankBless,
        1 => account.JewelBankSoul,
        2 => account.JewelBankLife,
        3 => account.JewelBankCreation,
        4 => account.JewelBankGuardian,
        5 => account.JewelBankGemstone,
        6 => account.JewelBankHarmony,
        7 => account.JewelBankChaos,
        8 => account.JewelBankLowerRefineStone,
        9 => account.JewelBankHigherRefineStone,
        10 => account.JewelBankKundun1,
        11 => account.JewelBankKundun2,
        12 => account.JewelBankKundun3,
        13 => account.JewelBankKundun4,
        14 => account.JewelBankKundun5,
        15 => account.JewelBankChocoBlue,
        16 => account.JewelBankChocoPink,
        _ => 0,
    };

    private bool TryAddJewelBankCount(Account account, int slot, int delta)
    {
        var result = this.GetJewelBankCount(account, slot) + delta;
        if (result < 0)
        {
            return false;
        }

        switch (slot)
        {
            case 0: account.JewelBankBless = result; return true;
            case 1: account.JewelBankSoul = result; return true;
            case 2: account.JewelBankLife = result; return true;
            case 3: account.JewelBankCreation = result; return true;
            case 4: account.JewelBankGuardian = result; return true;
            case 5: account.JewelBankGemstone = result; return true;
            case 6: account.JewelBankHarmony = result; return true;
            case 7: account.JewelBankChaos = result; return true;
            case 8: account.JewelBankLowerRefineStone = result; return true;
            case 9: account.JewelBankHigherRefineStone = result; return true;
            case 10: account.JewelBankKundun1 = result; return true;
            case 11: account.JewelBankKundun2 = result; return true;
            case 12: account.JewelBankKundun3 = result; return true;
            case 13: account.JewelBankKundun4 = result; return true;
            case 14: account.JewelBankKundun5 = result; return true;
            case 15: account.JewelBankChocoBlue = result; return true;
            case 16: account.JewelBankChocoPink = result; return true;
            default: return false;
        }
    }

    private int CountInventorySingles(Player player, int slot)
    {
        return player.Inventory?.Items.Count(item => this.IsInventorySingle(item, player, slot)) ?? 0;
    }

    private Item? FindInventorySingle(Player player, int slot)
    {
        return player.Inventory?.Items.FirstOrDefault(item => this.IsInventorySingle(item, player, slot));
    }

    private bool IsInventorySingle(Item item, Player player, int slot)
    {
        if (item.Definition is null)
        {
            return false;
        }

        if (slot < 10)
        {
            var mix = player.GameContext.Configuration.JewelMixes.FirstOrDefault(m => m.Number == slot);
            return mix?.SingleJewel == item.Definition;
        }

        var (group, number, level) = this.GetBoxSlot(slot);
        return item.Definition.Group == group && item.Definition.Number == number && item.Level == level;
    }

    private (int Group, int Number, byte Level) GetBoxSlot(int slot) => slot switch
    {
        10 => (14, 11, 8),
        11 => (14, 11, 9),
        12 => (14, 11, 10),
        13 => (14, 11, 11),
        14 => (14, 11, 12),
        15 => (14, 34, 0),
        16 => (14, 32, 0),
        _ => (0, 0, 0),
    };
}
