using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MUnique.OpenMU.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(EntityDataContext))]
    [Migration("20260525120000_AddItemBankBoxes")]
    public partial class AddItemBankBoxes : Migration
    {
        private static readonly string[] ColumnNames =
        {
            "JewelBankKundun1",
            "JewelBankKundun2",
            "JewelBankKundun3",
            "JewelBankKundun4",
            "JewelBankKundun5",
            "JewelBankChocoBlue",
            "JewelBankChocoPink",
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
