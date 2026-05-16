// <copyright file="VipExpirationCheckPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// BarnaMu: periodically checks online players and, when their VIP timer has expired,
/// reverts their account state back to <see cref="AccountState.Normal"/> without requiring
/// a reconnect.
/// Offline accounts whose VIP expired are handled on their next login (see LoginAction).
/// </summary>
[PlugIn]
[Display(Name = "BarnaMu VIP Expiration Check", Description = "Reverts VIP accounts to Normal when their timer expires, including players that are currently online.")]
[Guid("E5F6A7B8-9C0D-4E1F-A2B3-C4D5E6F7A8B9")]
public class VipExpirationCheckPlugIn : IPeriodicTaskPlugIn
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    private DateTime _nextRunUtc = DateTime.UtcNow;

    /// <inheritdoc />
    public async ValueTask ExecuteTaskAsync(GameContext gameContext)
    {
        if (DateTime.UtcNow < this._nextRunUtc)
        {
            return;
        }

        this._nextRunUtc = DateTime.UtcNow + CheckInterval;

        var logger = gameContext.LoggerFactory.CreateLogger(this.GetType().Name);
        using var scope = logger.BeginScope(gameContext);

        try
        {
            var now = DateTime.UtcNow;
            var players = await gameContext.GetPlayersAsync().ConfigureAwait(false);
            foreach (var player in players)
            {
                try
                {
                    if (player.Account is not { State: AccountState.Vip } account)
                    {
                        continue;
                    }

                    if (account.VipExpirationDate is not { } expiration || expiration > now)
                    {
                        continue;
                    }

                    account.State = AccountState.Normal;
                    account.VipExpirationDate = null;
                    await player.SaveProgressAsync().ConfigureAwait(false);
                    await player.ShowBlueMessageAsync("Tu VIP expiro. Tu cuenta volvio al estado Normal.").ConfigureAwait(false);
                    logger.LogInformation("VIP expired for account '{Account}', reverted to Normal.", account.LoginName);
                }
                catch (Exception ex)
                {
                    player.Logger.LogError(ex, "Error checking VIP expiration for player '{Player}'.", player);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during VIP expiration check.");
        }
    }

    /// <inheritdoc />
    public void ForceStart()
    {
        this._nextRunUtc = DateTime.UtcNow;
    }
}
