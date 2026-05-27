// <copyright file="AuctionHouseViewPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.AuctionHouse;

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Views.AuctionHouse;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Sends Auction House pages to the custom client window.
/// </summary>
[PlugIn]
[Display(Name = nameof(AuctionHouseViewPlugIn), Description = "BarnaMu: sends custom Auction House UI packets to the client.")]
[Guid("4AF0FBB9-3057-4D3E-9D94-A32C47F1F360")]
public class AuctionHouseViewPlugIn : IAuctionHouseViewPlugIn
{
    private const byte Group = 0xBF;
    private const byte SubCode = 0x31;
    private const int NameLength = 48;
    private const int SellerLength = 12;
    private const int RowPacketLength = 4 + 1 + 1 + 1 + 1 + 4 + 2 + 1 + 4 + NameLength + SellerLength + 1;

    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuctionHouseViewPlugIn"/> class.
    /// </summary>
    /// <param name="player">The player.</param>
    public AuctionHouseViewPlugIn(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowListingsAsync(byte view, byte page, IReadOnlyList<AuctionListing> listings)
    {
        var connection = this._player.Connection;
        if (connection is null)
        {
            return;
        }

        var count = Math.Min(listings.Count, 10);
        await connection.SendAsync(() =>
        {
            const int length = 8;
            var span = connection.Output.GetSpan(length)[..length];
            span.Clear();
            span[0] = 0xC1;
            span[1] = length;
            span[2] = Group;
            span[3] = SubCode;
            span[4] = 0;
            span[5] = view;
            span[6] = page;
            span[7] = (byte)count;
            return length;
        }).ConfigureAwait(false);

        for (var i = 0; i < count; i++)
        {
            var listing = listings[i];
            await connection.SendAsync(() =>
            {
                var span = connection.Output.GetSpan(RowPacketLength)[..RowPacketLength];
                span.Clear();
                span[0] = 0xC1;
                span[1] = RowPacketLength;
                span[2] = Group;
                span[3] = SubCode;
                span[4] = 1;
                span[5] = view;
                span[6] = (byte)listing.Status;
                span[7] = (byte)listing.Currency;
                BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(8, 4), (uint)listing.ListingNumber);
                BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(12, 2), (ushort)((listing.ItemGroup * 512) + listing.ItemNumber));
                span[14] = listing.ItemLevel;
                BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(15, 4), (uint)Math.Min(uint.MaxValue, listing.Price));
                WriteUtf8(span.Slice(19, NameLength), listing.ItemDisplayName);
                WriteUtf8(span.Slice(19 + NameLength, SellerLength), listing.SellerCharacterName);
                span[RowPacketLength - 1] = listing.JewelBankSlot.HasValue ? (byte)listing.JewelBankSlot.Value : (byte)0xFF;
                return RowPacketLength;
            }).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async ValueTask ShowMessageAsync(string message)
    {
        var connection = this._player.Connection;
        if (connection is null)
        {
            return;
        }

        var byteCount = Math.Min(Encoding.UTF8.GetByteCount(message), 180);
        var length = 5 + byteCount + 1;
        await connection.SendAsync(() =>
        {
            var span = connection.Output.GetSpan(length)[..length];
            span.Clear();
            span[0] = 0xC1;
            span[1] = (byte)length;
            span[2] = Group;
            span[3] = SubCode;
            span[4] = 2;
            WriteUtf8(span.Slice(5, byteCount + 1), message);
            return length;
        }).ConfigureAwait(false);
    }

    private static void WriteUtf8(Span<byte> target, string value)
    {
        target.Clear();
        if (target.Length == 0 || string.IsNullOrEmpty(value))
        {
            return;
        }

        var max = Math.Max(target.Length - 1, 0);
        var encoded = Encoding.UTF8.GetBytes(value);
        var count = Math.Min(encoded.Length, max);
        encoded.AsSpan(0, count).CopyTo(target);
        if (count < target.Length)
        {
            target[count] = 0;
        }
    }
}
