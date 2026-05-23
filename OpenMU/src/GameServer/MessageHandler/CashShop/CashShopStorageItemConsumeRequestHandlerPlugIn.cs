// <copyright file="CashShopStorageItemConsumeRequestHandlerPlugIn.cs" company="MUnique">
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
/// Handles cash shop storage item use requests.
/// </summary>
[PlugIn]
[Display(Name = "Cash shop storage item consume handler", Description = "Rejects unsupported cash shop storage item use requests cleanly.")]
[Guid("B8500DEB-D2D6-4274-983A-8B3C87849BB0")]
[BelongsToGroup(CashShopGroupHandlerPlugIn.GroupKey)]
internal class CashShopStorageItemConsumeRequestHandlerPlugIn : ISubPacketHandlerPlugIn
{
    /// <inheritdoc/>
    public bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public byte Key => CashShopStorageItemConsumeRequest.SubCode;

    /// <inheritdoc/>
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (player is not RemotePlayer { Connection: { Connected: true } connection })
        {
            return;
        }

        await connection.SendStorageItemUseResultAsync(1).ConfigureAwait(false);
    }
}
