// <copyright file="ItemTradeAuditPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// BarnaMu: audits every item handed to another player through a trade. The receiver
/// is recorded as the actor and the sender is recorded in the extra field, so the
/// audit log keeps a provenance chain across player-to-player transfers.
/// </summary>
[PlugIn]
[Display(Name = "BarnaMu Item Trade Audit", Description = "Logs items moved between players through trades for forensic item-origin tracking.")]
[Guid("C8B2D3E4-5F6A-4B7C-9D0E-1F2A3B4C5D6E")]
public class ItemTradeAuditPlugIn : IItemTradedToOtherPlayerPlugIn
{
    /// <inheritdoc />
    public void ItemTraded(ITrader source, ITrader target, Item item)
    {
        if (target is Player receiver)
        {
            ItemAuditLogger.Log(ItemAuditLogger.AuditSource.Trade, receiver, item, $"from {source.Name}");
        }
    }
}
