using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MUnique.OpenMU.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(EntityDataContext))]
    [Migration("20260515120000_AddAccountVipExpirationDate")]
    public partial class AddAccountVipExpirationDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime?>(
                name: "VipExpirationDate",
                schema: "data",
                table: "Account",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VipExpirationDate",
                schema: "data",
                table: "Account");
        }
    }
}
