using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GarageOS.Infrastructure.Data.Migrations.App
{
    /// <inheritdoc />
    public partial class AddPendingOwnerApprovalEstimateStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_estimates_status",
                table: "estimates");

            migrationBuilder.AddCheckConstraint(
                name: "ck_estimates_status",
                table: "estimates",
                sql: "status IN ('draft','sent','pending_owner_approval','approved','partially_approved','rejected','superseded')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_estimates_status",
                table: "estimates");

            migrationBuilder.AddCheckConstraint(
                name: "ck_estimates_status",
                table: "estimates",
                sql: "status IN ('draft','sent','approved','partially_approved','rejected','superseded')");
        }
    }
}
