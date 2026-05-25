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
}