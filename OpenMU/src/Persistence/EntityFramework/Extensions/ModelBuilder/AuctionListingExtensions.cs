// <copyright file="AuctionListingExtensions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.EntityFramework.Extensions.ModelBuilder;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MUnique.OpenMU.Persistence.EntityFramework.Model;

/// <summary>
/// Extensions for the <see cref="EntityTypeBuilder{AuctionListing}"/>.
/// </summary>
internal static class AuctionListingExtensions
{
    /// <summary>
    /// Applies the settings for the <see cref="AuctionListing"/> entity.
    /// </summary>
    /// <param name="builder">The builder.</param>
    public static void Apply(this EntityTypeBuilder<AuctionListing> builder)
    {
        builder.Property(listing => listing.SellerCharacterName).HasMaxLength(32).IsRequired();
        builder.Property(listing => listing.BuyerCharacterName).HasMaxLength(32).IsRequired();
        builder.Property(listing => listing.ItemDisplayName).HasMaxLength(160).IsRequired();

        builder.HasIndex(listing => listing.ListingNumber).IsUnique();
        builder.HasIndex(listing => new { listing.Status, listing.ExpiresAt });
        builder.HasIndex(listing => new { listing.SellerCharacterId, listing.Status });
        builder.HasIndex(listing => new { listing.BuyerCharacterId, listing.Status });

        builder.HasOne(listing => listing.RawEscrowItem)
            .WithMany()
            .HasForeignKey(listing => listing.EscrowItemId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
