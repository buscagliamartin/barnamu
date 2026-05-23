// <copyright file="CashShopPacketSender.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.CashShop;

using System.Buffers.Binary;
using MUnique.OpenMU.Network;

/// <summary>
/// Sends Season 6 in-game cash shop packets which are not generated yet.
/// </summary>
internal static class CashShopPacketSender
{
    private const byte HeaderType = 0xC1;
    private const byte CashShopCode = 0xD2;

    /// <summary>
    /// Sends the cash shop product script version.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="saleZone">The sale zone.</param>
    /// <param name="year">The script year.</param>
    /// <param name="yearId">The script year id.</param>
    /// <returns>The async task.</returns>
    internal static ValueTask SendVersionUpdateAsync(this IConnection connection, ushort saleZone, ushort year, ushort yearId)
        => connection.SendCashShopVersionAsync(0x0C, saleZone, year, yearId);

    /// <summary>
    /// Sends the cash shop banner script version.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="bannerZone">The banner zone.</param>
    /// <param name="year">The banner year.</param>
    /// <param name="yearId">The banner year id.</param>
    /// <returns>The async task.</returns>
    internal static ValueTask SendBannerUpdateAsync(this IConnection connection, ushort bannerZone, ushort year, ushort yearId)
        => connection.SendCashShopVersionAsync(0x15, bannerZone, year, yearId);

    /// <summary>
    /// Sends the cash shop open state response.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="isOpen">If set to <c>true</c>, the shop is open.</param>
    /// <returns>The async task.</returns>
    internal static ValueTask SendOpenStateAsync(this IConnection connection, bool isOpen)
    {
        const int size = 5;
        return connection.SendAsync(() =>
        {
            var span = connection.Output.GetSpan(size)[..size];
            WriteHeader(span, size, 0x02);
            span[4] = isOpen ? (byte)1 : (byte)0;
            return size;
        });
    }

    /// <summary>
    /// Sends the current cash shop point balances.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="wCoinC">The WCoin C balance.</param>
    /// <param name="wCoinP">The WCoin P balance.</param>
    /// <param name="goblinPoints">The goblin point balance.</param>
    /// <param name="mileage">The mileage balance.</param>
    /// <returns>The async task.</returns>
    internal static ValueTask SendPointInfoAsync(this IConnection connection, double wCoinC, double wCoinP, double goblinPoints, double mileage)
    {
        const int size = 45;
        return connection.SendAsync(() =>
        {
            var span = connection.Output.GetSpan(size)[..size];
            WriteHeader(span, size, 0x01);
            span[4] = 0;
            WriteDouble(span[5..], wCoinC + wCoinP);
            WriteDouble(span[13..], wCoinC);
            WriteDouble(span[21..], wCoinP);
            WriteDouble(span[29..], goblinPoints);
            WriteDouble(span[37..], mileage);
            return size;
        });
    }

    /// <summary>
    /// Sends the cash shop storage item count.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="totalItemCount">The total storage item count.</param>
    /// <param name="currentItemCount">The current page item count.</param>
    /// <param name="pageIndex">The page index.</param>
    /// <param name="totalPageCount">The total page count.</param>
    /// <returns>The async task.</returns>
    internal static ValueTask SendStorageCountAsync(this IConnection connection, ushort totalItemCount, ushort currentItemCount, ushort pageIndex, ushort totalPageCount)
    {
        const int size = 12;
        return connection.SendAsync(() =>
        {
            var span = connection.Output.GetSpan(size)[..size];
            WriteHeader(span, size, 0x06);
            BinaryPrimitives.WriteUInt16LittleEndian(span[4..], totalItemCount);
            BinaryPrimitives.WriteUInt16LittleEndian(span[6..], currentItemCount);
            BinaryPrimitives.WriteUInt16LittleEndian(span[8..], pageIndex);
            BinaryPrimitives.WriteUInt16LittleEndian(span[10..], totalPageCount);
            return size;
        });
    }

    /// <summary>
    /// Sends the result of a cash shop buy request.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="resultCode">The result code.</param>
    /// <param name="itemLeftCount">The remaining item count.</param>
    /// <returns>The async task.</returns>
    internal static ValueTask SendBuyResultAsync(this IConnection connection, byte resultCode, int itemLeftCount = 0)
    {
        const int size = 9;
        return connection.SendAsync(() =>
        {
            var span = connection.Output.GetSpan(size)[..size];
            WriteHeader(span, size, 0x03);
            span[4] = resultCode;
            BinaryPrimitives.WriteInt32LittleEndian(span[5..], itemLeftCount);
            return size;
        });
    }

    /// <summary>
    /// Sends the result of a cash shop gift request.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="resultCode">The result code.</param>
    /// <param name="itemLeftCount">The remaining item count.</param>
    /// <param name="limitedCash">The limited cash value.</param>
    /// <returns>The async task.</returns>
    internal static ValueTask SendGiftResultAsync(this IConnection connection, byte resultCode, int itemLeftCount = 0, double limitedCash = 0)
    {
        const int size = 17;
        return connection.SendAsync(() =>
        {
            var span = connection.Output.GetSpan(size)[..size];
            WriteHeader(span, size, 0x04);
            span[4] = resultCode;
            BinaryPrimitives.WriteInt32LittleEndian(span[5..], itemLeftCount);
            WriteDouble(span[9..], limitedCash);
            return size;
        });
    }

    /// <summary>
    /// Sends the result of a cash shop storage item delete request.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="resultCode">The result code.</param>
    /// <returns>The async task.</returns>
    internal static ValueTask SendStorageItemDeleteResultAsync(this IConnection connection, byte resultCode)
        => connection.SendStorageItemActionResultAsync(0x0A, resultCode);

    /// <summary>
    /// Sends the result of a cash shop storage item use request.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="resultCode">The result code.</param>
    /// <returns>The async task.</returns>
    internal static ValueTask SendStorageItemUseResultAsync(this IConnection connection, byte resultCode)
        => connection.SendStorageItemActionResultAsync(0x0B, resultCode);

    /// <summary>
    /// Sends the cash shop event item list count.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="count">The item count.</param>
    /// <returns>The async task.</returns>
    internal static ValueTask SendEventItemListCountAsync(this IConnection connection, ushort count)
    {
        const int size = 6;
        return connection.SendAsync(() =>
        {
            var span = connection.Output.GetSpan(size)[..size];
            WriteHeader(span, size, 0x13);
            BinaryPrimitives.WriteUInt16LittleEndian(span[4..], count);
            return size;
        });
    }

    private static ValueTask SendCashShopVersionAsync(this IConnection connection, byte subCode, ushort zone, ushort year, ushort yearId)
    {
        const int size = 10;
        return connection.SendAsync(() =>
        {
            var span = connection.Output.GetSpan(size)[..size];
            WriteHeader(span, size, subCode);
            BinaryPrimitives.WriteUInt16LittleEndian(span[4..], zone);
            BinaryPrimitives.WriteUInt16LittleEndian(span[6..], year);
            BinaryPrimitives.WriteUInt16LittleEndian(span[8..], yearId);
            return size;
        });
    }

    private static ValueTask SendStorageItemActionResultAsync(this IConnection connection, byte subCode, byte resultCode)
    {
        const int size = 5;
        return connection.SendAsync(() =>
        {
            var span = connection.Output.GetSpan(size)[..size];
            WriteHeader(span, size, subCode);
            span[4] = resultCode;
            return size;
        });
    }

    private static void WriteHeader(Span<byte> span, int length, byte subCode)
    {
        span[0] = HeaderType;
        span[1] = (byte)length;
        span[2] = CashShopCode;
        span[3] = subCode;
    }

    private static void WriteDouble(Span<byte> span, double value)
        => BinaryPrimitives.WriteInt64LittleEndian(span, BitConverter.DoubleToInt64Bits(value));
}
