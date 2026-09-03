using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GarageOS.Infrastructure.Data.Migrations.App
{
    /// <inheritdoc />
    public partial class AddCustomerVehicleSoftDeleteAndFixPlateIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "vehicles_plate_idx",
                table: "vehicles");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "vehicles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by",
                table: "vehicles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "customers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by",
                table: "customers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_deleted_by",
                table: "vehicles",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "vehicles_garage_deleted_idx",
                table: "vehicles",
                columns: new[] { "garage_id", "deleted_at" });

            migrationBuilder.CreateIndex(
                name: "vehicles_plate_idx",
                table: "vehicles",
                columns: new[] { "garage_id", "plate_number", "plate_country" });

            migrationBuilder.CreateIndex(
                name: "customers_garage_deleted_idx",
                table: "customers",
                columns: new[] { "garage_id", "deleted_at" });

            migrationBuilder.CreateIndex(
                name: "ix_customers_deleted_by",
                table: "customers",
                column: "deleted_by");

            migrationBuilder.AddForeignKey(
                name: "fk_customers_users_deleted_by",
                table: "customers",
                column: "deleted_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_vehicles_users_deleted_by",
                table: "vehicles",
                column: "deleted_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_customers_users_deleted_by",
                table: "customers");

            migrationBuilder.DropForeignKey(
                name: "fk_vehicles_users_deleted_by",
                table: "vehicles");

            migrationBuilder.DropIndex(
                name: "ix_vehicles_deleted_by",
                table: "vehicles");

            migrationBuilder.DropIndex(
                name: "vehicles_garage_deleted_idx",
                table: "vehicles");

            migrationBuilder.DropIndex(
                name: "vehicles_plate_idx",
                table: "vehicles");

            migrationBuilder.DropIndex(
                name: "customers_garage_deleted_idx",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "ix_customers_deleted_by",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "customers");

            migrationBuilder.CreateIndex(
                name: "vehicles_plate_idx",
                table: "vehicles",
                columns: new[] { "garage_id", "plate_number", "plate_country" },
                unique: true);
        }
    }
}
