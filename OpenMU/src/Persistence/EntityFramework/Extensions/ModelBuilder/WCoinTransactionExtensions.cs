// <copyright file="WCoinTransactionExtensions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.EntityFramework.Extensions.ModelBuilder;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MUnique.OpenMU.Persistence.EntityFramework.Model;

/// <summary>
/// Extensions for the <see cref="EntityTypeBuilder{WCoinTransaction}"/>.
/// </summary>
internal static class WCoinTransactionExtensions
{
    /// <summary>
    /// Applies the settings for the <see cref="WCoinTransaction"/> entity.
    /// </summary>
    /// <param name="builder">The builder.</param>
    public static void Apply(this EntityTypeBuilder<WCoinTransaction> builder)
    {
        builder.HasOne(transaction => transaction.RawAccount)
            .WithMany()
            .HasForeignKey(transaction => transaction.AccountId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasIndex(transaction => new { transaction.AccountId, transaction.Timestamp });
        builder.Property(transaction => transaction.Reason).HasMaxLength(32).IsRequired();
        builder.Property(transaction => transaction.Source).HasMaxLength(64).IsRequired();
        builder.Property(transaction => transaction.Actor).HasMaxLength(64).IsRequired();
        builder.Property(transaction => transaction.Note).HasMaxLength(256).IsRequired();
    }
}
