// <copyright file="JewelBankBalancesPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.MuHelper;

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// BarnaMu: the default implementation of <see cref="IJewelBankBalancesPlugIn"/> which sends the
/// item bank balances to the client as a 0xBF, sub-code 0x30 packet (17 little-endian uint32 counts).
/// </summary>
[PlugIn]
[Display(Name = nameof(JewelBankBalancesPlugIn), Description = "BarnaMu: sends the MU Helper item bank balances to the client.")]
[Guid("A1B2C3D4-1111-4A2B-8C3D-001122334455")]
public class JewelBankBalancesPlugIn : IJewelBankBalancesPlugIn
{
    private const int SlotCount = 17;
    private const int PacketLength = 4 + (SlotCount * 4);

    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="JewelBankBalancesPlugIn"/> class.
    /// </summary>
    /// <param name="player">The player.</param>
    public JewelBankBalancesPlugIn(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowBalancesAsync()
    {
        var connection = this._player.Connection;
        if (connection is null || this._player.Account is not { } account)
        {
            return;
        }

        int Write()
        {
            var span = connection.Output.GetSpan(PacketLength)[..PacketLength];
            span.Clear();
            span[0] = 0xC1;
            span[1] = PacketLength;
            span[2] = 0xBF;
            span[3] = 0x30;

            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(4, 4), (uint)account.JewelBankBless);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(8, 4), (uint)account.JewelBankSoul);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(12, 4), (uint)account.JewelBankLife);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(16, 4), (uint)account.JewelBankCreation);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(20, 4), (uint)account.JewelBankGuardian);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(24, 4), (uint)account.JewelBankGemstone);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(28, 4), (uint)account.JewelBankHarmony);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(32, 4), (uint)account.JewelBankChaos);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(36, 4), (uint)account.JewelBankLowerRefineStone);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(40, 4), (uint)account.JewelBankHigherRefineStone);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(44, 4), (uint)account.JewelBankKundun1);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(48, 4), (uint)account.JewelBankKundun2);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(52, 4), (uint)account.JewelBankKundun3);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(56, 4), (uint)account.JewelBankKundun4);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(60, 4), (uint)account.JewelBankKundun5);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(64, 4), (uint)account.JewelBankChocoBlue);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(68, 4), (uint)account.JewelBankChocoPink);

            return PacketLength;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }
}
