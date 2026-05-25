using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MUnique.OpenMU.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(EntityDataContext))]
    [Migration("20260524120000_AddJewelBank")]
    public partial class AddJewelBank : Migration
    {
        private static readonly string[] ColumnNames =
        {
            "JewelBankBless",
            "JewelBankSoul",
            "JewelBankLife",
            "JewelBankCreation",
            "JewelBankGuardian",
            "JewelBankGemstone",
            "JewelBankHarmony",
            "JewelBankChaos",
            "JewelBankLowerRefineStone",
            "JewelBankHigherRefineStone",
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var column in ColumnNames)
            {
                migrationBuilder.AddColumn<int>(
                    name: column,
                    schema: "data",
                    table: "Account",
                    type: "integer",
                    nullable: false,
                    defaultValue: 0);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var column in ColumnNames)
            {
                migrationBuilder.DropColumn(
                    name: column,
                    schema: "data",
                    table: "Account");
            }
        }
    }
}
