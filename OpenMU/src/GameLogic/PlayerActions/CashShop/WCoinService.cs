// <copyright file="WCoinService.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.CashShop;

using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Persistence;

/// <summary>
/// Applies authoritative W Coin balance mutations and writes the account ledger.
/// </summary>
public static class WCoinService
{
    /// <summary>
    /// Maximum W Coin balance per account.
    /// </summary>
    public const long MaximumBalance = 2_000_000_000;

    /// <summary>
    /// Applies a signed W Coin amount to an account and creates the matching ledger entry.
    /// </summary>
    public static bool TryApply(
        IContext context,
        Account account,
        long amount,
        string reason,
        string source,
        string actor,
        string? note,
        out string? error)
    {
        if (amount == 0)
        {
            error = "Amount must not be 0.";
            return false;
        }

        long balanceAfter;
        try
        {
            balanceAfter = checked(account.WCoin + amount);
        }
        catch (OverflowException)
        {
            error = "Invalid W Coin amount.";
            return false;
        }

        if (balanceAfter < 0)
        {
            error = "Insufficient W Coin balance.";
            return false;
        }

        if (balanceAfter > MaximumBalance)
        {
            error = $"W Coin balance cannot exceed {MaximumBalance}.";
            return false;
        }

        account.WCoin = balanceAfter;

        var transaction = context.CreateNew<WCoinTransaction>();
        transaction.Account = account;
        transaction.Timestamp = DateTime.UtcNow;
        transaction.Amount = amount;
        transaction.BalanceAfter = balanceAfter;
        transaction.Reason = reason;
        transaction.Source = source;
        transaction.Actor = actor;
        transaction.Note = note ?? string.Empty;

        error = null;
        return true;
    }
}
