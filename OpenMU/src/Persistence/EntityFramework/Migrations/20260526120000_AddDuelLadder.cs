using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MUnique.OpenMU.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(EntityDataContext))]
    [Migration("20260526120000_AddDuelLadder")]
    public partial class AddDuelLadder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DuelRating",
                schema: "data",
                table: "Character",
                type: "integer",
                nullable: false,
                defaultValue: 1200);

            migrationBuilder.AddColumn<int>(
                name: "DuelWins",
                schema: "data",
                table: "Character",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DuelLosses",
                schema: "data",
                table: "Character",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte>(
                name: "DuelResetBracket",
                schema: "data",
                table: "Character",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "DuelRating", schema: "data", table: "Character");
            migrationBuilder.DropColumn(name: "DuelWins", schema: "data", table: "Character");
            migrationBuilder.DropColumn(name: "DuelLosses", schema: "data", table: "Character");
            migrationBuilder.DropColumn(name: "DuelResetBracket", schema: "data", table: "Character");
        }
    }
}
