using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GarageOS.Infrastructure.Data.Migrations.App
{
    /// <inheritdoc />
    public partial class FixGaragesAccountIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "garages_account_idx",
                table: "garages",
                column: "account_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "garages_account_idx",
                table: "garages");
        }
    }
}
