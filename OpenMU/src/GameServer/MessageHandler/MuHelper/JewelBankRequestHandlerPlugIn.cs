// <copyright file="JewelBankRequestHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.MuHelper;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlayerActions.Items;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// BarnaMu: handler for the MU Helper item bank sub-packets (0xBF group, sub-code 0x30).
/// Layout: [4] = operation, [5] = bank slot (0-16), [6] = mode (0 = single, 1 = pack).
/// Operations: 0 = query balances, 1 = deposit, 2 = withdraw.
/// </summary>
[PlugIn]
[Display(Name = nameof(JewelBankRequestHandlerPlugIn), Description = "BarnaMu: handles MU Helper item bank query/deposit/withdraw requests.")]
[Guid("A1B2C3D4-2222-4A2B-8C3D-001122334455")]
[BelongsToGroup(MuHelperGroupHandler.GroupKey)]
public class JewelBankRequestHandlerPlugIn : ISubPacketHandlerPlugIn
{
    /// <summary>
    /// The sub-code of the item bank packet within the MU Helper (0xBF) group.
    /// </summary>
    internal const byte SubCode = 0x30;

    private readonly JewelBankAction _action = new();

    /// <inheritdoc/>
    public bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public byte Key => SubCode;

    /// <inheritdoc/>
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
                await this._action.ShowBalancesAsync(player).ConfigureAwait(false);
                break;
            case 1:
                if (packet.Length < 7)
                {
                    return;
                }

                await this._action.DepositAsync(player, span[5], span[6] != 0).ConfigureAwait(false);
                break;
            case 2:
                if (packet.Length < 7)
                {
                    return;
                }

                await this._action.WithdrawAsync(player, span[5], span[6] != 0).ConfigureAwait(false);
                break;
            default:
                break;
        }
    }
}
