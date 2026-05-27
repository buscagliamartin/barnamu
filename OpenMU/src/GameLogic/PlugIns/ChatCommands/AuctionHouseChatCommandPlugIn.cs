// <copyright file="AuctionHouseChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.PlayerActions.AuctionHouse;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Temporary chat command surface for the DB-backed Auction House server core.
/// </summary>
[Guid("4AF0FBB9-3057-4D3E-9D94-A32C47F1F35E")]
[PlugIn]
[Display(Name = "Auction House Chat Command", Description = "BarnaMu: DB-backed auction house command surface for testing before the custom client window.")]
public class AuctionHouseChatCommandPlugIn : IChatCommandPlugIn
{
    private readonly AuctionHouseService _service = new();

    /// <inheritdoc />
    public string Key => "/ah";

    /// <inheritdoc />
    public CharacterStatus MinCharacterStatusRequirement => CharacterStatus.Normal;

    /// <inheritdoc />
    public async ValueTask HandleCommandAsync(Player player, string command)
    {
        var args = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var subCommand = args.Length > 1 ? args[1].ToLowerInvariant() : "help";

        switch (subCommand)
        {
            case "help":
                await this.ShowHelpAsync(player).ConfigureAwait(false);
                break;
            case "list":
                await this.ListAsync(player, args).ConfigureAwait(false);
                break;
            case "sell":
                await this.SellAsync(player, args).ConfigureAwait(false);
                break;
            case "buy":
                await this.BuyAsync(player, args).ConfigureAwait(false);
                break;
            case "mine":
            case "mylistings":
                await this.MyListingsAsync(player).ConfigureAwait(false);
                break;
            case "cancel":
                await this.CancelAsync(player, args).ConfigureAwait(false);
                break;
            case "delivery":
            case "deliveries":
                await this.DeliveriesAsync(player).ConfigureAwait(false);
                break;
            case "receive":
                await this.ReceiveAsync(player, args).ConfigureAwait(false);
                break;
            case "payout":
            case "payouts":
            case "payments":
                await this.PayoutsAsync(player).ConfigureAwait(false);
                break;
            case "claim":
                await this.ClaimAsync(player, args).ConfigureAwait(false);
                break;
            default:
                await player.ShowBlueMessageAsync("Auction House: unknown command. Use /ah help.").ConfigureAwait(false);
                break;
        }
    }

    private async ValueTask ShowHelpAsync(Player player)
    {
        await player.ShowBlueMessageAsync("Auction House commands:").ConfigureAwait(false);
        await player.ShowBlueMessageAsync("/ah list [page|zen|wcoin|jewel]").ConfigureAwait(false);
        await player.ShowBlueMessageAsync("/ah sell <slot> zen <price>").ConfigureAwait(false);
        await player.ShowBlueMessageAsync("/ah sell <slot> wcoin <price>").ConfigureAwait(false);
        await player.ShowBlueMessageAsync("/ah sell <slot> jewel <type> <amount>").ConfigureAwait(false);
        await player.ShowBlueMessageAsync("/ah buy <id> zen <price> | /ah buy <id> wcoin <price> | /ah buy <id> jewel <type> <amount>").ConfigureAwait(false);
        await player.ShowBlueMessageAsync("/ah mine | /ah cancel <id> | /ah delivery | /ah receive <id> | /ah payouts | /ah claim <id>").ConfigureAwait(false);
    }

    private async ValueTask ListAsync(Player player, string[] args)
    {
        var page = 1;
        AuctionCurrency? currency = null;
        if (args.Length > 2)
        {
            if (int.TryParse(args[2], out var parsedPage))
            {
                page = Math.Max(1, parsedPage);
            }
            else if (this._service.TryParseCurrency(args[2], out var parsedCurrency))
            {
                currency = parsedCurrency;
            }
        }

        var listings = await this._service.GetActiveListingsAsync(player, currency, page).ConfigureAwait(false);
        if (listings.Count == 0)
        {
            await player.ShowBlueMessageAsync("Auction House: no active listings.").ConfigureAwait(false);
            return;
        }

        foreach (var listing in listings)
        {
            await player.ShowBlueMessageAsync(this._service.FormatListing(listing)).ConfigureAwait(false);
        }
    }

    private async ValueTask SellAsync(Player player, string[] args)
    {
        if (!this.TryParseListingRequest(args, 2, out var slot, out var currency, out var price, out var jewelBankSlot, out var error))
        {
            await player.ShowBlueMessageAsync(error).ConfigureAwait(false);
            return;
        }

        var result = await this._service.CreateListingAsync(player, slot, currency, price, jewelBankSlot).ConfigureAwait(false);
        await player.ShowBlueMessageAsync(result).ConfigureAwait(false);
    }

    private async ValueTask BuyAsync(Player player, string[] args)
    {
        if (args.Length < 5 || !long.TryParse(args[2], out var listingNumber))
        {
            await player.ShowBlueMessageAsync("Usage: /ah buy <id> zen <price> | /ah buy <id> wcoin <price> | /ah buy <id> jewel <type> <amount>").ConfigureAwait(false);
            return;
        }

        if (!this.TryParsePriceConfirmation(args, 3, out var currency, out var price, out var jewelBankSlot, out var error))
        {
            await player.ShowBlueMessageAsync(error).ConfigureAwait(false);
            return;
        }

        var result = await this._service.BuyAsync(player, listingNumber, currency, price, jewelBankSlot).ConfigureAwait(false);
        await player.ShowBlueMessageAsync(result).ConfigureAwait(false);
    }

    private async ValueTask MyListingsAsync(Player player)
    {
        var listings = await this._service.GetOwnListingsAsync(player).ConfigureAwait(false);
        if (listings.Count == 0)
        {
            await player.ShowBlueMessageAsync("Auction House: you have no listings.").ConfigureAwait(false);
            return;
        }

        foreach (var listing in listings)
        {
            await player.ShowBlueMessageAsync(this._service.FormatListing(listing)).ConfigureAwait(false);
        }
    }

    private async ValueTask CancelAsync(Player player, string[] args)
    {
        if (args.Length < 3 || !long.TryParse(args[2], out var listingNumber))
        {
            await player.ShowBlueMessageAsync("Usage: /ah cancel <id>").ConfigureAwait(false);
            return;
        }

        var result = await this._service.CancelAsync(player, listingNumber).ConfigureAwait(false);
        await player.ShowBlueMessageAsync(result).ConfigureAwait(false);
    }

    private async ValueTask DeliveriesAsync(Player player)
    {
        var deliveries = await this._service.GetPendingDeliveriesAsync(player).ConfigureAwait(false);
        if (deliveries.Count == 0)
        {
            await player.ShowBlueMessageAsync("Auction House: no pending deliveries.").ConfigureAwait(false);
            return;
        }

        foreach (var delivery in deliveries)
        {
            await player.ShowBlueMessageAsync(this._service.FormatListing(delivery)).ConfigureAwait(false);
        }
    }

    private async ValueTask ReceiveAsync(Player player, string[] args)
    {
        if (args.Length < 3 || !long.TryParse(args[2], out var listingNumber))
        {
            await player.ShowBlueMessageAsync("Usage: /ah receive <id>").ConfigureAwait(false);
            return;
        }

        var result = await this._service.ReceiveAsync(player, listingNumber).ConfigureAwait(false);
        await player.ShowBlueMessageAsync(result).ConfigureAwait(false);
    }

    private async ValueTask PayoutsAsync(Player player)
    {
        var payouts = await this._service.GetPendingPayoutsAsync(player).ConfigureAwait(false);
        if (payouts.Count == 0)
        {
            await player.ShowBlueMessageAsync("Auction House: no pending payouts.").ConfigureAwait(false);
            return;
        }

        foreach (var payout in payouts)
        {
            await player.ShowBlueMessageAsync(this._service.FormatListing(payout)).ConfigureAwait(false);
        }
    }

    private async ValueTask ClaimAsync(Player player, string[] args)
    {
        if (args.Length < 3 || !long.TryParse(args[2], out var listingNumber))
        {
            await player.ShowBlueMessageAsync("Usage: /ah claim <id>").ConfigureAwait(false);
            return;
        }

        var result = await this._service.ClaimPayoutAsync(player, listingNumber).ConfigureAwait(false);
        await player.ShowBlueMessageAsync(result).ConfigureAwait(false);
    }

    private bool TryParseListingRequest(string[] args, int startIndex, out byte slot, out AuctionCurrency currency, out long price, out int? jewelBankSlot, out string error)
    {
        slot = 0;
        if (args.Length <= startIndex || !byte.TryParse(args[startIndex], out slot))
        {
            currency = default;
            price = 0;
            jewelBankSlot = null;
            error = "Usage: /ah sell <slot> zen <price> | /ah sell <slot> wcoin <price> | /ah sell <slot> jewel <type> <amount>";
            return false;
        }

        return this.TryParsePriceConfirmation(args, startIndex + 1, out currency, out price, out jewelBankSlot, out error);
    }

    private bool TryParsePriceConfirmation(string[] args, int startIndex, out AuctionCurrency currency, out long price, out int? jewelBankSlot, out string error)
    {
        currency = default;
        price = 0;
        jewelBankSlot = null;
        error = string.Empty;

        if (args.Length <= startIndex || !this._service.TryParseCurrency(args[startIndex], out currency))
        {
            error = "Auction House: invalid currency. Use zen, wcoin, or jewel.";
            return false;
        }

        if (currency == AuctionCurrency.Jewel)
        {
            if (args.Length <= startIndex + 2 || !this._service.TryResolveJewelBankSlot(args[startIndex + 1], out var parsedJewelSlot) || !long.TryParse(args[startIndex + 2], out price))
            {
                error = "Usage: jewel <type> <amount>. Example: jewel bless 10";
                return false;
            }

            jewelBankSlot = parsedJewelSlot;
            error = string.Empty;
            return true;
        }

        if (args.Length <= startIndex + 1 || !long.TryParse(args[startIndex + 1], out price))
        {
            error = "Auction House: invalid price.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
