// <copyright file="CashShopInitializationPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.CashShop;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Views.CashShop;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Initializes the Season 6 in-game cash shop client state.
/// </summary>
[PlugIn]
[Display(Name = "Cash shop initialization", Description = "Initializes the in-game cash shop on the client.")]
[Guid("B0F4B1BE-1165-48A5-A72B-6CE1066AF92E")]
public class CashShopInitializationPlugIn : ICashShopInitializationPlugIn
{
    private const ushort ProductListZone = 512;
    private const ushort ProductListYear = 2012;
    private const ushort ProductListYearId = 84;
    private const ushort BannerListZone = 583;
    private const ushort BannerListYear = 2011;
    private const ushort BannerListYearId = 1;

    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="CashShopInitializationPlugIn"/> class.
    /// </summary>
    /// <param name="player">The player.</param>
    public CashShopInitializationPlugIn(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask InitializeCashShopAsync()
    {
        if (this._player.Connection is not { Connected: true } connection)
        {
            return;
        }

        await connection.SendVersionUpdateAsync(ProductListZone, ProductListYear, ProductListYearId).ConfigureAwait(false);
        await connection.SendBannerUpdateAsync(BannerListZone, BannerListYear, BannerListYearId).ConfigureAwait(false);
    }
}
