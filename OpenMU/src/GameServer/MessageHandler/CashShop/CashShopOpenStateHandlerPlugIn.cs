// <copyright file="CashShopOpenStateHandlerPlugIn.cs" company="MUnique">
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
/// Handles cash shop open and close state requests.
/// </summary>
[PlugIn]
[Display(Name = "Cash shop open state handler", Description = "Handles requests to open or close the in-game cash shop.")]
[Guid("31F36C09-4486-4C2C-A960-878290E493B5")]
[BelongsToGroup(CashShopGroupHandlerPlugIn.GroupKey)]
internal class CashShopOpenStateHandlerPlugIn : ISubPacketHandlerPlugIn
{
    /// <inheritdoc/>
    public bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public byte Key => CashShopOpenState.SubCode;

    /// <inheritdoc/>
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        CashShopOpenState message = packet;
        if (player is not RemotePlayer { Connection: { Connected: true } connection })
        {
            return;
        }

        await connection.SendOpenStateAsync(!message.IsClosed).ConfigureAwait(false);
    }
}
