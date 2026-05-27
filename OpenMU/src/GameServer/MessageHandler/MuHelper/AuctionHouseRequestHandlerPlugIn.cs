// <copyright file="AuctionHouseRequestHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.MuHelper;

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlayerActions.AuctionHouse;
using MUnique.OpenMU.GameLogic.Views.AuctionHouse;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// BarnaMu: handler for the custom Auction House sub-packets (0xBF group, sub-code 0x31).
/// </summary>
[PlugIn]
[Display(Name = nameof(AuctionHouseRequestHandlerPlugIn), Description = "BarnaMu: handles custom Auction House UI requests.")]
[Guid("4AF0FBB9-3057-4D3E-9D94-A32C47F1F361")]
[BelongsToGroup(MuHelperGroupHandler.GroupKey)]
public class AuctionHouseRequestHandlerPlugIn : ISubPacketHandlerPlugIn
{
    internal const byte SubCode = 0x31;

    private const byte ViewBrowse = 0;
    private const byte ViewOwnListings = 1;
    private const byte ViewDeliveries = 2;
    private const byte ViewPayouts = 3;

    private readonly AuctionHouseService _service = new();

    /// <inheritdoc/>
    public bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public byte Key => SubCode;

    /// <inheritdoc/>
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (packet.Length < 5)
        {
            return;
        }

        var span = packet.Span;
        var operation = span[4];
        switch (operation)
        {
            case 0:
                await this.ShowBrowseAsync(player, span.Length > 5 ? span[5] : (byte)1, span.Length > 6 ? span[6] : (byte)0).ConfigureAwait(false);
                break;
            case 1:
                if (span.Length >= 12)
                {
                    await this.SellAsync(player, span[5], span[6], span[7], BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(8, 4))).ConfigureAwait(false);
                }

                break;
            case 2:
                if (span.Length >= 16)
                {
                    await this.BuyAsync(
                        player,
                        span[6],
                        span[7],
                        BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(8, 4)),
                        BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(12, 4))).ConfigureAwait(false);
                }

                break;
            case 3:
                await this.ShowPageAsync(player, ViewOwnListings, 1, null).ConfigureAwait(false);
                break;
            case 4:
                if (span.Length >= 12)
                {
                    await this.CancelAsync(player, BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(8, 4))).ConfigureAwait(false);
                }

                break;
            case 5:
                await this.ShowPageAsync(player, ViewDeliveries, 1, null).ConfigureAwait(false);
                break;
            case 6:
                if (span.Length >= 12)
                {
                    await this.ReceiveAsync(player, BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(8, 4))).ConfigureAwait(false);
                }

                break;
            case 7:
                await this.ShowPageAsync(player, ViewPayouts, 1, null).ConfigureAwait(false);
                break;
            case 8:
                if (span.Length >= 12)
                {
                    await this.ClaimAsync(player, BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(8, 4))).ConfigureAwait(false);
                }

                break;
            default:
                break;
        }
    }

    private async ValueTask ShowBrowseAsync(Player player, byte requestedPage, byte requestedFilter)
    {
        var page = Math.Max((byte)1, requestedPage);
        var currency = this.ParseCurrencyFilter(requestedFilter);
        await this.ShowPageAsync(player, ViewBrowse, page, currency).ConfigureAwait(false);
    }

    private async ValueTask SellAsync(Player player, byte slot, byte currencyCode, byte requestedJewelSlot, uint price)
    {
        if (!this.TryReadCurrency(currencyCode, requestedJewelSlot, out var currency, out var jewelBankSlot))
        {
            await this.ShowMessageAsync(player, "Auction House: invalid sell request.").ConfigureAwait(false);
            return;
        }

        var result = await this._service.CreateListingAsync(player, slot, currency, price, jewelBankSlot).ConfigureAwait(false);
        await this.ShowMessageAsync(player, result).ConfigureAwait(false);
        await this.ShowPageAsync(player, ViewOwnListings, 1, null).ConfigureAwait(false);
    }

    private async ValueTask BuyAsync(Player player, byte currencyCode, byte requestedJewelSlot, uint listingNumber, uint price)
    {
        if (!this.TryReadCurrency(currencyCode, requestedJewelSlot, out var currency, out var jewelBankSlot))
        {
            await this.ShowMessageAsync(player, "Auction House: invalid buy request.").ConfigureAwait(false);
            return;
        }

        var result = await this._service.BuyAsync(player, listingNumber, currency, price, jewelBankSlot).ConfigureAwait(false);
        await this.ShowMessageAsync(player, result).ConfigureAwait(false);
        await this.ShowPageAsync(player, ViewBrowse, 1, null).ConfigureAwait(false);
    }

    private async ValueTask CancelAsync(Player player, uint listingNumber)
    {
        var result = await this._service.CancelAsync(player, listingNumber).ConfigureAwait(false);
        await this.ShowMessageAsync(player, result).ConfigureAwait(false);
        await this.ShowPageAsync(player, ViewOwnListings, 1, null).ConfigureAwait(false);
    }

    private async ValueTask ReceiveAsync(Player player, uint listingNumber)
    {
        var result = await this._service.ReceiveAsync(player, listingNumber).ConfigureAwait(false);
        await this.ShowMessageAsync(player, result).ConfigureAwait(false);
        await this.ShowPageAsync(player, ViewDeliveries, 1, null).ConfigureAwait(false);
    }

    private async ValueTask ClaimAsync(Player player, uint listingNumber)
    {
        var result = await this._service.ClaimPayoutAsync(player, listingNumber).ConfigureAwait(false);
        await this.ShowMessageAsync(player, result).ConfigureAwait(false);
        await this.ShowPageAsync(player, ViewPayouts, 1, null).ConfigureAwait(false);
    }

    private async ValueTask ShowPageAsync(Player player, byte view, byte page, AuctionCurrency? currency)
    {
        IReadOnlyList<AuctionListing> listings = view switch
        {
            ViewOwnListings => await this._service.GetOwnListingsAsync(player).ConfigureAwait(false),
            ViewDeliveries => await this._service.GetPendingDeliveriesAsync(player).ConfigureAwait(false),
            ViewPayouts => await this._service.GetPendingPayoutsAsync(player).ConfigureAwait(false),
            _ => await this._service.GetActiveListingsAsync(player, currency, page).ConfigureAwait(false),
        };

        await player.InvokeViewPlugInAsync<IAuctionHouseViewPlugIn>(p => p.ShowListingsAsync(view, page, listings)).ConfigureAwait(false);
    }

    private async ValueTask ShowMessageAsync(Player player, string message)
    {
        await player.ShowBlueMessageAsync(message).ConfigureAwait(false);
        await player.InvokeViewPlugInAsync<IAuctionHouseViewPlugIn>(p => p.ShowMessageAsync(message)).ConfigureAwait(false);
    }

    private AuctionCurrency? ParseCurrencyFilter(byte value) => value switch
    {
        1 => AuctionCurrency.Zen,
        2 => AuctionCurrency.WCoin,
        3 => AuctionCurrency.Jewel,
        _ => null,
    };

    private bool TryReadCurrency(byte value, byte jewelSlot, out AuctionCurrency currency, out int? resolvedJewelSlot)
    {
        resolvedJewelSlot = null;
        switch (value)
        {
            case 0:
                currency = AuctionCurrency.Zen;
                return true;
            case 1:
                currency = AuctionCurrency.WCoin;
                return true;
            case 2:
                if (jewelSlot > 16)
                {
                    currency = default;
                    return false;
                }

                currency = AuctionCurrency.Jewel;
                resolvedJewelSlot = jewelSlot;
                return true;
            default:
                currency = default;
                return false;
        }
    }
}
