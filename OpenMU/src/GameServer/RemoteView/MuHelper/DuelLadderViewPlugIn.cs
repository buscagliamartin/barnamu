// <copyright file="DuelLadderViewPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.MuHelper;

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using MUnique.OpenMU.GameLogic.PlayerActions;
using MUnique.OpenMU.GameLogic.Views.Duel;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// BarnaMu: default implementation of the Duel Ladder view plugins. Writes the two
/// Duel Ladder responses to the client as 0xBF, sub-code 0x32 packets:
/// op=0 top-10 (variable length), op=1 own profile (21 bytes).
/// </summary>
[PlugIn]
[Display(Name = nameof(DuelLadderViewPlugIn), Description = "BarnaMu: sends Duel Ladder top-10 and profile responses to the client.")]
[Guid("D5E30CF3-9D4A-4A3E-9F56-C3D4E5F6A7B8")]
public class DuelLadderViewPlugIn : IDuelLadderTopPlugIn, IDuelLadderProfilePlugIn
{
    private const int NameBytes = 10;
    private const int EntryBytes = NameBytes + 1 + 4 + 4 + 4; // name[10] + class[1] + rating[4] + wins[4] + losses[4] = 23
    private const int TopHeaderBytes = 7; // C1 + len + 0xBF + 0x32 + op + bracket + count
    private const int ProfilePacketLength = 21; // header(4) + op(1) + bracket(1) + tier(1) + rating(4) + wins(4) + losses(4) + rank(2)
    private const int MaxTopEntries = 10;

    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="DuelLadderViewPlugIn"/> class.
    /// </summary>
    /// <param name="player">The player.</param>
    public DuelLadderViewPlugIn(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowTopAsync(byte bracket, IReadOnlyList<DuelLadderEntry> entries)
    {
        var connection = this._player.Connection;
        if (connection is null)
        {
            return;
        }

        var count = Math.Min(entries.Count, MaxTopEntries);
        var length = TopHeaderBytes + (count * EntryBytes);

        int Write()
        {
            var span = connection.Output.GetSpan(length)[..length];
            span.Clear();
            span[0] = 0xC1;
            span[1] = (byte)length;
            span[2] = 0xBF;
            span[3] = 0x32;
            span[4] = 0x00; // op = top
            span[5] = bracket;
            span[6] = (byte)count;

            for (int i = 0; i < count; i++)
            {
                var entry = entries[i];
                var offset = TopHeaderBytes + (i * EntryBytes);
                WriteName(span.Slice(offset, NameBytes), entry.Name);
                span[offset + NameBytes] = entry.ClassNumber;
                BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset + NameBytes + 1, 4), (uint)entry.Rating);
                BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset + NameBytes + 5, 4), (uint)entry.Wins);
                BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset + NameBytes + 9, 4), (uint)entry.Losses);
            }

            return length;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask ShowProfileAsync(byte bracket, byte skillTier, int rating, int wins, int losses, ushort rankInBracket)
    {
        var connection = this._player.Connection;
        if (connection is null)
        {
            return;
        }

        int Write()
        {
            var span = connection.Output.GetSpan(ProfilePacketLength)[..ProfilePacketLength];
            span.Clear();
            span[0] = 0xC1;
            span[1] = ProfilePacketLength;
            span[2] = 0xBF;
            span[3] = 0x32;
            span[4] = 0x01; // op = profile
            span[5] = bracket;
            span[6] = skillTier;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(7, 4), (uint)rating);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(11, 4), (uint)wins);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(15, 4), (uint)losses);
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(19, 2), rankInBracket);
            return ProfilePacketLength;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    private static void WriteName(Span<byte> dest, string name)
    {
        dest.Clear();
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        var trimmed = name.Length > dest.Length ? name.Substring(0, dest.Length) : name;
        var bytes = Encoding.ASCII.GetBytes(trimmed);
        bytes.AsSpan(0, Math.Min(bytes.Length, dest.Length)).CopyTo(dest);
    }
}
