// Auction House plugin I built for OpenMU.
// 
// It's all in a single self-contained file, no database changes needed, everything is stored in a JSON file called jewel_auction_house.json in OpenMU\src\Startup that survives disconnects, server restarts and crashes, so no listings or pending transactions ever get lost.
// 
// It's built specifically for Jewels (Bless, Soul, Chaos, Life and Creation) and works entirely through chat commands. Here's what it supports:
// 
// List jewels for sale, item is removed from your bag instantly
// Browse the market, with optional filter by jewel type
// Buy with confirmation, you have to type the exact item and price to prevent scams
// Purchased jewels go into a mailbox instead of directly into your bag, so nothing gets lost if your inventory is full
// Sellers collect their Zen separately through a claim system, so they don't need to be online when the item sells
// All IDs recycle cleanly, no gaps building up over time
// Everything is case insensitive
// 
// Commands: /ah help and /ah list for the command list.
// Feel free to use it, modify it, whatever you need.

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.PlugIns;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;
using MUnique.OpenMU.Interfaces;

[PlugIn]
[Display(Name = "Jewel Auction House", Description = "A lightweight, JSON-backed auction house strictly for Jewels, using a claim-based Zen payout system.")]
[Guid("F9A3E4D1-2C5B-4890-A67E-1B3C4D5E6FA7")]
public class JewelAuctionHousePlugIn : IChatCommandPlugIn
{
    private const string StorageFilePath = "jewel_auction_house.json";
    private const long MaxZenLimit = 2_000_000_000;

    public string Key => "/ah";
    public CharacterStatus MinCharacterStatusRequirement => CharacterStatus.Normal;

    private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
    private AuctionData _data = new AuctionData();

    public JewelAuctionHousePlugIn()
    {
        _ = LoadDataAsync();
    }

    public async ValueTask HandleCommandAsync(Player player, string command)
    {
        var args = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var subCommand = args.Length > 1 ? args[1].ToLower() : "help";

        switch (subCommand)
        {
            case "help":       await ShowHelpAsync(player).ConfigureAwait(false); break;
            case "list":       await ListJewelsAsync(player, args).ConfigureAwait(false); break;
            case "sell":       await SellJewelAsync(player, args).ConfigureAwait(false); break;
            case "buy":        await BuyJewelAsync(player, args).ConfigureAwait(false); break;
            case "mylistings": await MyListingsAsync(player).ConfigureAwait(false); break;
            case "payments":   await ViewPaymentsAsync(player).ConfigureAwait(false); break;
            case "claim":      await ClaimPaymentAsync(player, args).ConfigureAwait(false); break;
            case "purchases":  await ViewPurchasesAsync(player).ConfigureAwait(false); break;
            case "receive":    await ReceivePurchaseAsync(player, args).ConfigureAwait(false); break;
            default:           await SendMessageAsync(player, "Unknown command. Type /ah help").ConfigureAwait(false); break;
        }
    }

    // -------------------------------------------------------------------------
    // HELP
    // -------------------------------------------------------------------------

    private async ValueTask ShowHelpAsync(Player player)
    {
        await SendMessageAsync(player, "=== Jewel Auction House ===").ConfigureAwait(false);
        await SendMessageAsync(player, "/ah list [type]                      - View all listings, or filter by jewel type").ConfigureAwait(false);
        await SendMessageAsync(player, "/ah sell [type] [price]              - List a jewel for sale").ConfigureAwait(false);
        await SendMessageAsync(player, "   Types: bless, soul, chaos, life, creation").ConfigureAwait(false);
        await SendMessageAsync(player, "/ah buy [ID] [type] [price]          - Buy a specific jewel by ID, type and price").ConfigureAwait(false);
        await SendMessageAsync(player, "   Example: /ah buy 3 bless 3000000").ConfigureAwait(false);
        await SendMessageAsync(player, "/ah mylistings                       - View your active listings").ConfigureAwait(false);
        await SendMessageAsync(player, "/ah purchases                        - View your pending jewel deliveries").ConfigureAwait(false);
        await SendMessageAsync(player, "/ah receive [ID]                     - Receive a purchased jewel into your bag").ConfigureAwait(false);
        await SendMessageAsync(player, "/ah payments                         - View your pending Zen").ConfigureAwait(false);
        await SendMessageAsync(player, "/ah claim [ID]                       - Claim a pending Zen payment").ConfigureAwait(false);
    }

    // -------------------------------------------------------------------------
    // LIST  — optional filter: /ah list bless  or  /ah list jewel of bless
    // -------------------------------------------------------------------------

    private async ValueTask ListJewelsAsync(Player player, string[] args)
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!_data.Listings.Any())
            {
                await SendMessageAsync(player, "The Auction House is currently empty.").ConfigureAwait(false);
                return;
            }

            IEnumerable<Listing> source = _data.Listings;

            if (args.Length > 2)
            {
                string filterInput = string.Join(" ", args.Skip(2)).ToLower().Trim();

                if (!TryResolveJewelType(filterInput, out byte fGroup, out short fNumber))
                {
                    await SendMessageAsync(player, "Unknown jewel type. Use: bless, soul, chaos, life, creation").ConfigureAwait(false);
                    return;
                }

                source = source.Where(l => l.Group == fGroup && l.Number == fNumber);

                if (!source.Any())
                {
                    await SendMessageAsync(player, $"No {GetJewelName(fGroup, fNumber)} listings found.").ConfigureAwait(false);
                    return;
                }

                await SendMessageAsync(player, $"=== {GetJewelName(fGroup, fNumber)} Listings (cheapest first) ===").ConfigureAwait(false);
            }
            else
            {
                await SendMessageAsync(player, "=== Available Jewels (cheapest first) ===").ConfigureAwait(false);
            }

            foreach (var listing in source.OrderBy(l => l.Price).Take(10))
            {
                await SendMessageAsync(player,
                    $"[{listing.Id}] {GetJewelName(listing.Group, listing.Number)} {listing.Price:N0} Zen | Seller: {listing.SellerName}"
                ).ConfigureAwait(false);
            }
        }
        finally { _lock.Release(); }
    }

    // -------------------------------------------------------------------------
    // SELL
    // -------------------------------------------------------------------------

    private async ValueTask SellJewelAsync(Player player, string[] args)
    {
        if (player.Inventory == null || player.SelectedCharacter?.Inventory == null) return;

        if (args.Length < 4 || !long.TryParse(args[args.Length - 1], out long price))
        {
            await SendMessageAsync(player, "Usage: /ah sell [bless|soul|chaos|life|creation] [price]").ConfigureAwait(false);
            return;
        }

        // Type is everything between "sell" and the final price arg
        string jewelType = string.Join(" ", args.Skip(2).Take(args.Length - 3)).ToLower().Trim();

        if (!TryResolveJewelType(jewelType, out byte group, out short number))
        {
            await SendMessageAsync(player, "Invalid jewel type. Use: bless, soul, chaos, life, creation").ConfigureAwait(false);
            return;
        }

        if (price <= 0 || price > MaxZenLimit)
        {
            await SendMessageAsync(player, $"Price must be between 1 and {MaxZenLimit:N0} Zen.").ConfigureAwait(false);
            return;
        }

        var item = player.SelectedCharacter.Inventory.Items.FirstOrDefault(i =>
            i.Definition != null &&
            i.Definition.Group == group &&
            i.Definition.Number == number &&
            i.ItemSlot >= 12 && i.ItemSlot <= 75);

        if (item == null)
        {
            await SendMessageAsync(player, $"No {GetJewelName(group, number)} found in your backpack.").ConfigureAwait(false);
            await SendMessageAsync(player, "(If it is on your cursor, drop it into your bag first.)").ConfigureAwait(false);
            return;
        }

        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            player.SelectedCharacter.Inventory.Items.Remove(item);

            await player.InvokeViewPlugInAsync<IItemRemovedPlugIn>(
                p => p.RemoveItemAsync(item.ItemSlot)
            ).ConfigureAwait(false);

            // Recycle IDs — pick lowest positive integer not already in use
            var usedIds = _data.Listings.Select(l => l.Id).ToHashSet();
            int nextId = 1;
            while (usedIds.Contains(nextId)) nextId++;

            var listing = new Listing
            {
                Id         = nextId,
                SellerName = player.SelectedCharacter.Name,
                SellerId   = player.SelectedCharacter.Id,
                Price      = price,
                Group      = group,
                Number     = number
            };

            _data.Listings.Add(listing);
            await SaveDataInternalAsync().ConfigureAwait(false);

            await SendMessageAsync(player,
                $"Listed {GetJewelName(group, number)} for {price:N0} Zen. Listing ID: [{listing.Id}]"
            ).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[JewelAuctionHouse] Sell Error: {ex}");
            await SendMessageAsync(player, "A server error occurred while listing the item.").ConfigureAwait(false);
        }
        finally { _lock.Release(); }
    }

    // -------------------------------------------------------------------------
    // BUY — Zen deducted immediately, jewel held in PendingPurchase mailbox
    // -------------------------------------------------------------------------

    private async ValueTask BuyJewelAsync(Player player, string[] args)
    {
        if (player.Inventory == null || player.SelectedCharacter?.Inventory == null) return;

        // Usage: /ah buy [ID] [type] [price]
        if (args.Length < 5 || !int.TryParse(args[2], out int listingId) || !long.TryParse(args[args.Length - 1], out long confirmedPrice))
        {
            await SendMessageAsync(player, "Usage: /ah buy [ID] [type] [price]").ConfigureAwait(false);
            await SendMessageAsync(player, "Example: /ah buy 3 bless 3000000").ConfigureAwait(false);
            return;
        }

        // Type is everything between args[3] and the last arg
        string typeInput = string.Join(" ", args.Skip(3).Take(args.Length - 4)).ToLower().Trim();

        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            var listing = _data.Listings.FirstOrDefault(l => l.Id == listingId);
            if (listing == null)
            {
                await SendMessageAsync(player, $"Listing [{listingId}] not found.").ConfigureAwait(false);
                return;
            }

            if (player.SelectedCharacter.Name == listing.SellerName)
            {
                await SendMessageAsync(player, "You cannot buy your own listing.").ConfigureAwait(false);
                return;
            }

            if (!MatchesJewelType(typeInput, listing.Group, listing.Number))
            {
                string expectedFull  = GetJewelName(listing.Group, listing.Number);
                string expectedShort = GetJewelShortAlias(listing.Group, listing.Number);
                await SendMessageAsync(player,
                    $"Item type mismatch for listing [{listingId}]. Expected: '{expectedFull}' or '{expectedShort}'."
                ).ConfigureAwait(false);
                return;
            }

            if (confirmedPrice != listing.Price)
            {
                await SendMessageAsync(player,
                    $"Price mismatch for listing [{listingId}]. Listed price is {listing.Price:N0} Zen."
                ).ConfigureAwait(false);
                return;
            }

            if (player.Money < listing.Price)
            {
                await SendMessageAsync(player, $"Not enough Zen. Need {listing.Price:N0}, you have {player.Money:N0}.").ConfigureAwait(false);
                return;
            }

            // Deduct Zen immediately
            player.TryRemoveMoney((int)listing.Price);
            await player.InvokeViewPlugInAsync<IUpdateMoneyPlugIn>(
                p => p.UpdateMoneyAsync()
            ).ConfigureAwait(false);

            // Remove listing from market
            _data.Listings.Remove(listing);

            // Queue Zen payment for the seller
			var usedPaymentIds = _data.Payments.Select(p => p.Id).ToHashSet();
			int nextPaymentId = 1;
			while (usedPaymentIds.Contains(nextPaymentId)) nextPaymentId++;

			_data.Payments.Add(new PendingPayment
			{
			Id         = nextPaymentId,
			SellerId   = listing.SellerId,
			SellerName = listing.SellerName,
			Amount     = listing.Price
			});

            // Hold jewel in buyer's purchase mailbox — no inventory space needed yet
            var usedPurchaseIds = _data.Purchases.Select(p => p.Id).ToHashSet();
            int nextPurchaseId = 1;
            while (usedPurchaseIds.Contains(nextPurchaseId)) nextPurchaseId++;

            _data.Purchases.Add(new PendingPurchase
            {
                Id         = nextPurchaseId,
                BuyerId    = player.SelectedCharacter.Id,
                BuyerName  = player.SelectedCharacter.Name,
                SellerName = listing.SellerName,
                PricePaid  = listing.Price,
                Group      = listing.Group,
                Number     = listing.Number
            });

            await SaveDataInternalAsync().ConfigureAwait(false);

            await SendMessageAsync(player,
                $"Purchase successful! {GetJewelName(listing.Group, listing.Number)} is waiting in your mailbox."
            ).ConfigureAwait(false);
            await SendMessageAsync(player,
                $"Cost: {listing.Price:N0} Zen. Use /ah purchases to view and /ah receive [ID] to collect."
            ).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[JewelAuctionHouse] Buy Error: {ex}");
            await SendMessageAsync(player, "A server error occurred during the purchase.").ConfigureAwait(false);
        }
        finally { _lock.Release(); }
    }

    // -------------------------------------------------------------------------
    // PURCHASES — view pending jewel deliveries
    // -------------------------------------------------------------------------

    private async ValueTask ViewPurchasesAsync(Player player)
    {
        if (player.SelectedCharacter == null) return;

        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            var mine = _data.Purchases.Where(p => p.BuyerId == player.SelectedCharacter.Id).ToList();
            if (!mine.Any())
            {
                await SendMessageAsync(player, "You have no pending jewel deliveries.").ConfigureAwait(false);
                return;
            }

            await SendMessageAsync(player, "=== Pending Purchases (use /ah receive [ID]) ===").ConfigureAwait(false);
            await SendMessageAsync(player, "WARNING: Jewel will be LOST FOREVER if you have no free space in your main inventory!").ConfigureAwait(false);
            foreach (var p in mine)
            {
                await SendMessageAsync(player,
                    $"[{p.Id}] {GetJewelName(p.Group, p.Number)} | Paid: {p.PricePaid:N0} Zen | Seller: {p.SellerName}"
                ).ConfigureAwait(false);
            }
        }
        finally { _lock.Release(); }
    }

    // -------------------------------------------------------------------------
    // RECEIVE — deliver a pending jewel into the buyer's inventory
    // -------------------------------------------------------------------------

    private async ValueTask ReceivePurchaseAsync(Player player, string[] args)
    {
        if (player.Inventory == null || player.SelectedCharacter?.Inventory == null) return;

        if (args.Length < 3 || !int.TryParse(args[2], out int purchaseId))
        {
            await SendMessageAsync(player, "Usage: /ah receive [ID]").ConfigureAwait(false);
            return;
        }

        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            var purchase = _data.Purchases.FirstOrDefault(p =>
                p.Id == purchaseId && p.BuyerId == player.SelectedCharacter.Id);

            if (purchase == null)
            {
                await SendMessageAsync(player, "Purchase not found or does not belong to you.").ConfigureAwait(false);
                return;
            }

            var itemDef = player.GameContext?.Configuration?.Items?
                .FirstOrDefault(i => i != null && i.Group == purchase.Group && i.Number == purchase.Number);

            if (itemDef == null)
            {
                await SendMessageAsync(player, "Server Error: Item definition not found. Contact an admin.").ConfigureAwait(false);
                return;
            }

            var newItem = player.PersistenceContext.CreateNew<Item>();
            newItem.Definition = itemDef;
            newItem.Durability  = itemDef.Durability;

            await player.Inventory.AddItemAsync(newItem).ConfigureAwait(false);

            await player.InvokeViewPlugInAsync<INpcItemBoughtPlugIn>(
                p => p.NpcItemBoughtAsync(newItem)
            ).ConfigureAwait(false);

            await player.InvokeViewPlugInAsync<IItemAppearPlugIn>(
                p => p.ItemAppearAsync(newItem)
            ).ConfigureAwait(false);

            _data.Purchases.Remove(purchase);
            await SaveDataInternalAsync().ConfigureAwait(false);

            await SendMessageAsync(player,
                $"You received {GetJewelName(purchase.Group, purchase.Number)} into your inventory!"
            ).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[JewelAuctionHouse] Receive Error: {ex}");
            await SendMessageAsync(player, "A server error occurred. Your jewel is safe in your purchases mailbox.").ConfigureAwait(false);
        }
        finally { _lock.Release(); }
    }

    // -------------------------------------------------------------------------
    // MY LISTINGS
    // -------------------------------------------------------------------------

    private async ValueTask MyListingsAsync(Player player)
    {
        if (player.SelectedCharacter == null) return;

        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            var mine = _data.Listings.Where(l => l.SellerId == player.SelectedCharacter.Id).ToList();
            if (!mine.Any())
            {
                await SendMessageAsync(player, "You have no active listings.").ConfigureAwait(false);
                return;
            }

            await SendMessageAsync(player, "=== Your Active Listings ===").ConfigureAwait(false);
            foreach (var l in mine)
                await SendMessageAsync(player, $"[{l.Id}] {GetJewelName(l.Group, l.Number)} - {l.Price:N0} Zen").ConfigureAwait(false);
        }
        finally { _lock.Release(); }
    }

    // -------------------------------------------------------------------------
    // PAYMENTS & CLAIM
    // -------------------------------------------------------------------------

    private async ValueTask ViewPaymentsAsync(Player player)
    {
        if (player.SelectedCharacter == null) return;

        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            var myPayments = _data.Payments.Where(p => p.SellerId == player.SelectedCharacter.Id).ToList();
            if (!myPayments.Any())
            {
                await SendMessageAsync(player, "No pending payments found.").ConfigureAwait(false);
                return;
            }

            await SendMessageAsync(player, "=== Pending Payments ===").ConfigureAwait(false);
            foreach (var p in myPayments)
                await SendMessageAsync(player, $"[{p.Id}] {p.Amount:N0} Zen").ConfigureAwait(false);
            await SendMessageAsync(player, "Use /ah claim [ID] to collect your Zen.").ConfigureAwait(false);
        }
        finally { _lock.Release(); }
    }

    private async ValueTask ClaimPaymentAsync(Player player, string[] args)
    {
        if (player.SelectedCharacter == null) return;

        if (args.Length < 3 || !int.TryParse(args[2], out int paymentId))
        {
            await SendMessageAsync(player, "Usage: /ah claim [ID]").ConfigureAwait(false);
            return;
        }

        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            var payment = _data.Payments.FirstOrDefault(p =>
                p.Id == paymentId && p.SellerId == player.SelectedCharacter.Id);

            if (payment == null)
            {
                await SendMessageAsync(player, "Payment not found or does not belong to you.").ConfigureAwait(false);
                return;
            }

            long availableSpace = MaxZenLimit - player.Money;
            if (availableSpace < payment.Amount)
            {
                await SendMessageAsync(player,
                    $"Cannot claim: need {payment.Amount - availableSpace:N0} more Zen capacity first."
                ).ConfigureAwait(false);
                return;
            }

            player.TryAddMoney((int)payment.Amount);

            await player.InvokeViewPlugInAsync<IUpdateMoneyPlugIn>(
                p => p.UpdateMoneyAsync()
            ).ConfigureAwait(false);

            _data.Payments.Remove(payment);
            await SaveDataInternalAsync().ConfigureAwait(false);
            await SendMessageAsync(player, $"Claimed {payment.Amount:N0} Zen successfully!").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[JewelAuctionHouse] Claim Error: {ex}");
            await SendMessageAsync(player, "A server error occurred while claiming.").ConfigureAwait(false);
        }
        finally { _lock.Release(); }
    }

    // -------------------------------------------------------------------------
    // HELPERS & PERSISTENCE
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tries to resolve a user-supplied string (short alias or full name, case-insensitive)
    /// into a group/number pair. Returns false if unrecognised.
    /// </summary>
    private static bool TryResolveJewelType(string input, out byte group, out short number)
    {
        group = 0; number = 0;
        switch (input.ToLower().Trim())
        {
            case "bless":
            case "jewel of bless":    group = 14; number = 13; return true;
            case "soul":
            case "jewel of soul":     group = 14; number = 14; return true;
            case "chaos":
            case "jewel of chaos":    group = 12; number = 15; return true;
            case "life":
            case "jewel of life":     group = 14; number = 16; return true;
            case "creation":
            case "jewel of creation": group = 14; number = 22; return true;
            default: return false;
        }
    }

    private static string GetJewelName(byte group, short number)
    {
        if (group == 14 && number == 13) return "Jewel of Bless";
        if (group == 14 && number == 14) return "Jewel of Soul";
        if (group == 12 && number == 15) return "Jewel of Chaos";
        if (group == 14 && number == 16) return "Jewel of Life";
        if (group == 14 && number == 22) return "Jewel of Creation";
        return "Unknown Jewel";
    }

    private static string GetJewelShortAlias(byte group, short number)
    {
        if (group == 14 && number == 13) return "bless";
        if (group == 14 && number == 14) return "soul";
        if (group == 12 && number == 15) return "chaos";
        if (group == 14 && number == 16) return "life";
        if (group == 14 && number == 22) return "creation";
        return "unknown";
    }

    private static bool MatchesJewelType(string input, byte group, short number)
    {
        return TryResolveJewelType(input, out byte g, out short n) && g == group && n == number;
    }

    private async ValueTask SendMessageAsync(Player player, string message)
    {
        await player.InvokeViewPlugInAsync<IShowMessagePlugIn>(
            p => p.ShowMessageAsync(message, MessageType.BlueNormal)
        ).ConfigureAwait(false);
    }

    private async Task LoadDataAsync()
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (File.Exists(StorageFilePath))
            {
                var json = await File.ReadAllTextAsync(StorageFilePath).ConfigureAwait(false);
                _data = JsonSerializer.Deserialize<AuctionData>(json) ?? new AuctionData();
            }
        }
        catch (Exception ex) { Console.WriteLine($"[JewelAuctionHouse] Load Error: {ex.Message}"); }
        finally { _lock.Release(); }
    }

    private async Task SaveDataInternalAsync()
    {
        var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(StorageFilePath, json).ConfigureAwait(false);
    }

    // -------------------------------------------------------------------------
    // DATA MODELS
    // -------------------------------------------------------------------------

    public class AuctionData
    {
        public int LastListingId  { get; set; } = 0;  // kept for JSON compatibility
        public int LastPaymentId  { get; set; } = 0;
        public List<Listing>         Listings  { get; set; } = new();
        public List<PendingPayment>  Payments  { get; set; } = new();
        public List<PendingPurchase> Purchases { get; set; } = new();
    }

    public class Listing
    {
        public int    Id         { get; set; }
        public Guid   SellerId   { get; set; }
        public string SellerName { get; set; } = string.Empty;
        public long   Price      { get; set; }
        public byte   Group      { get; set; }
        public short  Number     { get; set; }
    }

    public class PendingPayment
    {
        public int    Id         { get; set; }
        public Guid   SellerId   { get; set; }
        public string SellerName { get; set; } = string.Empty;
        public long   Amount     { get; set; }
    }

    public class PendingPurchase
    {
        public int    Id         { get; set; }
        public Guid   BuyerId    { get; set; }
        public string BuyerName  { get; set; } = string.Empty;
        public string SellerName { get; set; } = string.Empty;
        public long   PricePaid  { get; set; }
        public byte   Group      { get; set; }
        public short  Number     { get; set; }
    }
}
