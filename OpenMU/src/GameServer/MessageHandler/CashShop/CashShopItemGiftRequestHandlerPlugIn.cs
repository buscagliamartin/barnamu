// <copyright file="CashShopItemGiftRequestHandlerPlugIn.cs" company="MUnique">
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
/// Handles cash shop gift requests.
/// </summary>
[PlugIn]
[Display(Name = "Cash shop gift handler", Description = "Rejects unsupported cash shop gifts cleanly.")]
[Guid("275266AD-928F-4763-825C-DF3DB3E0EB8D")]
[BelongsToGroup(CashShopGroupHandlerPlugIn.GroupKey)]
internal class CashShopItemGiftRequestHandlerPlugIn : ISubPacketHandlerPlugIn
{
    /// <inheritdoc/>
    public bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public byte Key => CashShopItemGiftRequest.SubCode;

    /// <inheritdoc/>
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (player is not RemotePlayer { Connection: { Connected: true } connection })
        {
            return;
        }

        await connection.SendGiftResultAsync(1).ConfigureAwait(false);
    }
}
