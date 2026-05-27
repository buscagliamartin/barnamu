// <copyright file="DuelLadderRequestHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.MuHelper;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlayerActions;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// BarnaMu: handler for the in-game Duel Ladder window's requests
/// (0xBF group, sub-code 0x32). Op byte at index 4:
/// 0 = request top-10 for bracket (arg at [5]: bracket id 1-5),
/// 1 = request own profile (no further args).
/// </summary>
[PlugIn]
[Display(Name = nameof(DuelLadderRequestHandlerPlugIn), Description = "BarnaMu: handles Duel Ladder top-10 and profile queries from the client.")]
[Guid("D5E30CF3-AAAA-4A3E-9F56-C3D4E5F6A7B8")]
[BelongsToGroup(MuHelperGroupHandler.GroupKey)]
public class DuelLadderRequestHandlerPlugIn : ISubPacketHandlerPlugIn
{
    /// <summary>
    /// Sub-code of the Duel Ladder packet within the MU Helper (0xBF) group.
    /// </summary>
    internal const byte SubCode = 0x32;

    private readonly DuelLadderQueryAction _action = new();

    /// <inheritdoc />
    public bool IsEncryptionExpected => false;

    /// <inheritdoc />
    public byte Key => SubCode;

    /// <inheritdoc />
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (packet.Length < 5)
        {
            return;
        }

        var span = packet.Span;
        switch (span[4])
        {
            case 0:
                if (packet.Length < 6)
                {
                    return;
                }

                await this._action.QueryTopAsync(player, span[5]).ConfigureAwait(false);
                break;
            case 1:
                await this._action.QueryProfileAsync(player).ConfigureAwait(false);
                break;
            default:
                break;
        }
    }
}
