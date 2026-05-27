// <copyright file="ConnectionManager.ClientToServer.Custom.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.Client.Library;

using System;
using System.Runtime.InteropServices;
using System.Text;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets.ClientToServer;
using MUnique.OpenMU.Network.Xor;

/// <summary>
/// Extension methods to start writing messages of this namespace on a <see cref="IConnection"/>.
/// </summary>
public unsafe partial class ConnectionManager
{
    private static readonly Xor3Encryptor Xor3Encryptor = new(0);

    /// <summary>
    /// Sends a <see cref="LoginLongPassword" /> to this connection.
    /// </summary>
    /// <param name="handle">The handle of the connection.</param>
    /// <param name="username">The user name, "encrypted" with Xor3.</param>
    /// <param name="password">The password, "encrypted" with Xor3.</param>
    /// <param name="tickCount">The tick count.</param>
    /// <param name="clientVersion">The client version.</param>
    /// <param name="clientSerial">The client serial.</param>
    /// <remarks>
    /// Is sent by the client when: The player tries to log into the game.
    /// Causes reaction on server side: The server is authenticating the sent login name and password. If it's correct, the state of the player is proceeding to be logged in.
    /// </remarks>
    [UnmanagedCallersOnly(EntryPoint = "ConnectionManager_SendLogin")]
    public static void SendLogin(int handle, IntPtr username, IntPtr password, uint @tickCount, byte* @clientVersion, byte* @clientSerial)
    {
        if (!Connections.TryGetValue(handle, out var connection))
        {
            return;
        }

        try
        {
            var usernameStr = Marshal.PtrToStringAuto(@username);
            var passwordStr = Marshal.PtrToStringAuto(@password);

            // todo: check if username or password is too long
            connection.CreateAndSend(pipeWriter =>
            {
                Span<byte> usernameBytes = stackalloc byte[10];
                Span<byte> passwordBytes = stackalloc byte[20];
                Encoding.UTF8.GetBytes(usernameStr, usernameBytes);
                Encoding.UTF8.GetBytes(passwordStr, passwordBytes);
                Xor3Encryptor.Encrypt(usernameBytes);
                Xor3Encryptor.Encrypt(passwordBytes);

                var length = LoginLongPasswordRef.Length;
                var packet = new LoginLongPasswordRef(pipeWriter.GetSpan(length)[..length]);
                usernameBytes.CopyTo(packet.Username);
                passwordBytes.CopyTo(packet.Password);
                packet.TickCount = @tickCount;
                new Span<byte>(@clientVersion, packet.ClientVersion.Length).CopyTo(packet.ClientVersion);
                new Span<byte>(@clientSerial, packet.ClientSerial.Length).CopyTo(packet.ClientSerial);

                return length;
            });
        }
        catch
        {
            // Log exception
        }
    }

    /// <summary>
    /// BarnaMu: Sends a jewel bank request (0xBF, sub-code 0x30) to this connection.
    /// </summary>
    /// <param name="handle">The handle of the connection.</param>
    /// <param name="operation">0 = query, 1 = deposit slot, 2 = withdraw, 3 = deposit all.</param>
    /// <param name="arg1">Inventory slot (deposit) or jewel mix number (withdraw).</param>
    /// <param name="arg2">Single jewel count (withdraw).</param>
    /// <param name="arg3">Packed jewel count (withdraw).</param>
    /// <summary>
    /// BarnaMu: Sends a Duel Ladder request (0xBF, sub-code 0x32) to this connection.
    /// </summary>
    /// <param name="handle">The handle of the connection.</param>
    /// <param name="operation">0 = request top-10 for the bracket given in <paramref name="arg"/> (1-5); 1 = request own profile.</param>
    /// <param name="arg">When operation = 0: the reset bracket id. Ignored otherwise.</param>
    [UnmanagedCallersOnly(EntryPoint = "ConnectionManager_SendDuelLadderRequest")]
    public static void SendDuelLadderRequest(int handle, byte @operation, byte @arg)
    {
        if (!Connections.TryGetValue(handle, out var connection))
        {
            return;
        }

        try
        {
            connection.CreateAndSend(pipeWriter =>
            {
                const int length = 6;
                var span = pipeWriter.GetSpan(length)[..length];
                span.Clear();
                span[0] = 0xC1;
                span[1] = (byte)length;
                span[2] = 0xBF;
                span[3] = 0x32;
                span[4] = @operation;
                span[5] = @arg;
                return length;
            });
        }
        catch
        {
            // Log exception
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "ConnectionManager_SendJewelBankRequest")]
    public static void SendJewelBankRequest(int handle, byte @operation, byte @arg1, ushort @arg2, ushort @arg3)
    {
        if (!Connections.TryGetValue(handle, out var connection))
        {
            return;
        }

        try
        {
            connection.CreateAndSend(pipeWriter =>
            {
                const int length = 10;
                var span = pipeWriter.GetSpan(length)[..length];
                span.Clear();
                span[0] = 0xC1;
                span[1] = (byte)length;
                span[2] = 0xBF;
                span[3] = 0x30;
                span[4] = @operation;
                span[5] = @arg1;
                span[6] = (byte)(@arg2 & 0xFF);
                span[7] = (byte)(@arg2 >> 8);
                span[8] = (byte)(@arg3 & 0xFF);
                span[9] = (byte)(@arg3 >> 8);
                return length;
            });
        }
        catch
        {
            // Log exception
        }
    }

    /// <summary>
    /// BarnaMu: Sends an Auction House request (0xBF, sub-code 0x31) to this connection.
    /// </summary>
    /// <param name="handle">The handle of the connection.</param>
    /// <param name="operation">The auction house operation.</param>
    /// <param name="arg1">Small operation argument, such as page or inventory slot.</param>
    /// <param name="currency">Currency code: 0 = Zen, 1 = W Coin, 2 = jewel.</param>
    /// <param name="jewelSlot">Jewel bank slot, or 0xFF when unused.</param>
    /// <param name="arg2">Main numeric argument, such as listing id or price.</param>
    /// <param name="arg3">Secondary numeric argument, such as confirmed price.</param>
    [UnmanagedCallersOnly(EntryPoint = "ConnectionManager_SendAuctionHouseRequest")]
    public static void SendAuctionHouseRequest(int handle, byte @operation, byte @arg1, byte @currency, byte @jewelSlot, uint @arg2, uint @arg3)
    {
        if (!Connections.TryGetValue(handle, out var connection))
        {
            return;
        }

        try
        {
            connection.CreateAndSend(pipeWriter =>
            {
                const int length = 16;
                var span = pipeWriter.GetSpan(length)[..length];
                span.Clear();
                span[0] = 0xC1;
                span[1] = (byte)length;
                span[2] = 0xBF;
                span[3] = 0x31;
                span[4] = @operation;
                span[5] = @arg1;
                span[6] = @currency;
                span[7] = @jewelSlot;
                span[8] = (byte)(@arg2 & 0xFF);
                span[9] = (byte)((@arg2 >> 8) & 0xFF);
                span[10] = (byte)((@arg2 >> 16) & 0xFF);
                span[11] = (byte)(@arg2 >> 24);
                span[12] = (byte)(@arg3 & 0xFF);
                span[13] = (byte)((@arg3 >> 8) & 0xFF);
                span[14] = (byte)((@arg3 >> 16) & 0xFF);
                span[15] = (byte)(@arg3 >> 24);
                return length;
            });
        }
        catch
        {
            // Log exception
        }
    }
}
