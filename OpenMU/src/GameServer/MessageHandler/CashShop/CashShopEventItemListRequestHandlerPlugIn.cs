// <copyright file="CashShopEventItemListRequestHandlerPlugIn.cs" company="MUnique">
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
/// Handles requests for cash shop event item lists.
/// </summary>
[PlugIn]
[Display(Name = "Cash shop event item list handler", Description = "Sends cash shop event item list information to the client.")]
[Guid("C8EE30E6-6AC7-4988-8C1D-469D5D50CF42")]
[BelongsToGroup(CashShopGroupHandlerPlugIn.GroupKey)]
internal class CashShopEventItemListRequestHandlerPlugIn : ISubPacketHandlerPlugIn
{
    /// <inheritdoc/>
    public bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public byte Key => CashShopEventItemListRequest.SubCode;

    /// <inheritdoc/>
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (player is not RemotePlayer { Connection: { Connected: true } connection })
        {
            return;
        }

        await connection.SendEventItemListCountAsync(0).ConfigureAwait(false);
    }
}
