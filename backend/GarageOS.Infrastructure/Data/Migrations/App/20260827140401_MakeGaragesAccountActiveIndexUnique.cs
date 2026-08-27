using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GarageOS.Infrastructure.Data.Migrations.App
{
    /// <summary>
    /// Makes garages_account_active_idx a UNIQUE partial index (garages(account_id)
    /// WHERE deleted_at IS NULL) -- the DB-level backstop for WP-3B's Phase 1
    /// one-active-garage-per-account rule (see AccountProvisioningService).
    ///
    /// REQUIRED PRE-FLIGHT CHECK (Database Engineer review finding, WP-3B): this
    /// migration will fail with a unique_violation if any account already has more
    /// than one non-deleted garage. On a fresh Phase 1 database (the only case that
    /// exists as of WP-3B) this cannot happen -- AccountProvisioningService is the
    /// only insert path and it enforces the rule in-process. Before applying this
    /// migration to any non-fresh database, run:
    ///
    ///   SELECT account_id, COUNT(*) FROM garages
    ///   WHERE deleted_at IS NULL
    ///   GROUP BY account_id
    ///   HAVING COUNT(*) > 1;
    ///
    /// Remediation if that query returns any rows: for each affected account_id,
    /// decide (with the account owner / Owner) which garage row remains authoritative
    /// and soft-delete (DeletedAt = now) the other(s) -- never hard-delete -- before
    /// re-running this migration.
    /// </summary>
    public partial class MakeGaragesAccountActiveIndexUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "garages_account_active_idx",
                table: "garages");

            migrationBuilder.CreateIndex(
                name: "garages_account_active_idx",
                table: "garages",
                column: "account_id",
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "garages_account_active_idx",
                table: "garages");

            migrationBuilder.CreateIndex(
                name: "garages_account_active_idx",
                table: "garages",
                column: "account_id",
                filter: "deleted_at IS NULL");
        }
    }
}
