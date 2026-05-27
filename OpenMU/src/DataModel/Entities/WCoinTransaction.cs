// <copyright file="WCoinTransaction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.DataModel.Entities;

/// <summary>
/// Immutable ledger entry for an account W Coin balance change.
/// </summary>
[AggregateRoot]
public class WCoinTransaction
{
    /// <summary>
    /// Gets or sets the account whose W Coin balance changed.
    /// </summary>
    [Required]
    public virtual Account Account { get; set; } = null!;

    /// <summary>
    /// Gets or sets the UTC timestamp of the balance mutation.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the signed W Coin amount.
    /// </summary>
    public long Amount { get; set; }

    /// <summary>
    /// Gets or sets the account balance after this transaction.
    /// </summary>
    public long BalanceAfter { get; set; }

    /// <summary>
    /// Gets or sets the reason code, e.g. GM grant, web top-up, auction sale, or auction purchase.
    /// </summary>
    [Required]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subsystem which created the transaction.
    /// </summary>
    [Required]
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the admin, service, or system actor which created the transaction.
    /// </summary>
    public string Actor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional note for auditing.
    /// </summary>
    public string Note { get; set; } = string.Empty;
}
