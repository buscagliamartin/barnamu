// <copyright file="ItemMoveHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.Items;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlayerActions.Items;
using MUnique.OpenMU.GameServer.RemoteView;
using MUnique.OpenMU.GameServer.RemoteView.Inventory;
using MUnique.OpenMU.Network.Packets;
using MUnique.OpenMU.Network.Packets.ClientToServer;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Handler for item move packets.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.ItemMoveHandlerPlugIn_Name), Description = nameof(PlugInResources.ItemMoveHandlerPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("c499c596-7711-4971-bc83-7abd9e6b5553")]
internal class ItemMoveHandlerPlugIn : IPacketHandlerPlugIn
{
    private readonly MoveItemAction _moveAction = new();

    /// <inheritdoc/>
    public bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public byte Key => ItemMoveRequest.Code;

    /// <inheritdoc/>
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        var header = packet.Span[0];
        var length = packet.Length;

        ItemStorageKind fromStorage;
        byte fromSlot;
        ItemStorageKind toStorage;
        byte toSlot;

        if (length == 7)
        {
            // Extended move (e.g. Right click)
            fromStorage = (ItemStorageKind)packet.Span[3];
            fromSlot = packet.Span[4];
            toStorage = (ItemStorageKind)packet.Span[5];
            toSlot = packet.Span[6];
        }
        else
        {
            // Standard move with item data
            ItemMoveRequest message = packet;
            fromStorage = message.FromStorage;
            fromSlot = message.FromSlot;
            
            var itemSize = 12;
            if (player is RemotePlayer remotePlayer)
            {
                itemSize = remotePlayer.ItemSerializer.NeededSpace;
            }

            toStorage = (ItemStorageKind)packet.Span[5 + itemSize];
            toSlot = packet.Span[6 + itemSize];
        }

        await this._moveAction.MoveItemAsync(player, fromSlot, fromStorage.Convert(), toSlot, toStorage.Convert()).ConfigureAwait(false);
    }
}