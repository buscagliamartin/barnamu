// <copyright file="CashShopDeleteStorageItemRequestHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.CashShop;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameServer.RemoteView;
using MUnique.OpenMU.GameServer.RemoteView.CashShop;
using MUnique.OpenMU.Network.Packets.ClientToServer;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Handles cash shop storage item delete requests.
/// </summary>
[PlugIn]
[Display(Name = "Cash shop delete storage item handler", Description = "Rejects unsupported cash shop storage item delete requests cleanly.")]
[Guid("5265E702-E5CC-4699-9754-240F43117A60")]
[BelongsToGroup(CashShopGroupHandlerPlugIn.GroupKey)]
internal class CashShopDeleteStorageItemRequestHandlerPlugIn : ISubPacketHandlerPlugIn
{
    /// <inheritdoc/>
    public bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public byte Key => CashShopDeleteStorageItemRequest.SubCode;

    /// <inheritdoc/>
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (player is not RemotePlayer { Connection: { Connected: true } connection })
        {
            return;
        }

        await connection.SendStorageItemDeleteResultAsync(1).ConfigureAwait(false);
    }
}
