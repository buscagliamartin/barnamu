using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MUnique.OpenMU.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(EntityDataContext))]
    [Migration("20260525203000_AddAuctionHouseListings")]
    public partial class AddAuctionHouseListings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuctionListing",
                schema: "data",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingNumber = table.Column<long>(type: "bigint", nullable: false),
                    SellerAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    SellerCharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    SellerCharacterName = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    BuyerAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    BuyerCharacterId = table.Column<Guid>(type: "uuid", nullable: true),
                    BuyerCharacterName = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EscrowItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    ItemDisplayName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ItemGroup = table.Column<byte>(type: "smallint", nullable: false),
                    ItemNumber = table.Column<short>(type: "smallint", nullable: false),
                    ItemLevel = table.Column<byte>(type: "smallint", nullable: false),
                    Price = table.Column<long>(type: "bigint", nullable: false),
                    FeeAmount = table.Column<long>(type: "bigint", nullable: false),
                    SellerPayoutAmount = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    JewelBankSlot = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SoldAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveryClaimedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SellerPayoutClaimedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuctionListing", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuctionListing_Item_EscrowItemId",
                        column: x => x.EscrowItemId,
                        principalSchema: "data",
                        principalTable: "Item",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuctionListing_BuyerCharacterId_Status",
                schema: "data",
                table: "AuctionListing",
                columns: new[] { "BuyerCharacterId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AuctionListing_EscrowItemId",
                schema: "data",
                table: "AuctionListing",
                column: "EscrowItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AuctionListing_ListingNumber",
                schema: "data",
                table: "AuctionListing",
                column: "ListingNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuctionListing_SellerCharacterId_Status",
                schema: "data",
                table: "AuctionListing",
                columns: new[] { "SellerCharacterId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AuctionListing_Status_ExpiresAt",
                schema: "data",
                table: "AuctionListing",
                columns: new[] { "Status", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuctionListing",
                schema: "data");
        }
    }
}
