// <copyright file="CashShopGroupHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.CashShop;

using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Handler for Season 6 in-game cash shop packets.
/// </summary>
[PlugIn]
[Display(Name = "Cash shop packet group", Description = "Handles in-game cash shop packet requests.")]
[Guid("C1F764FA-1D80-4D6D-AE92-2C37AC0E1186")]
internal class CashShopGroupHandlerPlugIn : GroupPacketHandlerPlugIn
{
    /// <summary>
    /// The group key.
    /// </summary>
    internal const byte GroupKey = (byte)PacketType.CashShopGroup;

    /// <summary>
    /// Initializes a new instance of the <see cref="CashShopGroupHandlerPlugIn"/> class.
    /// </summary>
    /// <param name="clientVersionProvider">The client version provider.</param>
    /// <param name="manager">The manager.</param>
    /// <param name="loggerFactory">The logger.</param>
    public CashShopGroupHandlerPlugIn(IClientVersionProvider clientVersionProvider, PlugInManager manager, ILoggerFactory loggerFactory)
        : base(clientVersionProvider, manager, loggerFactory)
    {
    }

    /// <inheritdoc/>
    public override bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public override byte Key => GroupKey;
}
