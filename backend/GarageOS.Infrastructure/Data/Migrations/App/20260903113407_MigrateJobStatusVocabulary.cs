using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GarageOS.Infrastructure.Data.Migrations.App
{
    /// <inheritdoc />
    public partial class MigrateJobStatusVocabulary : Migration
    {
        /// <inheritdoc />
        /// <remarks>
        /// P2-WP3 hand-edit: `dotnet ef migrations add` scaffolded an AddColumn&lt;uint&gt;("xmin", ...)
        /// alongside the constraint swap, because it treats the new
        /// Property&lt;uint&gt;("xmin").IsRowVersion()-mapped shadow property like any other new
        /// model property. That operation is deliberately removed here -- "xmin" is a
        /// Postgres system column that already exists on every table; `ALTER TABLE jobs ADD
        /// COLUMN xmin ...` is rejected by Postgres outright ("column name "xmin" conflicts
        /// with a system column name"). Mapping it as a concurrency token needs no schema
        /// change at all, exactly as P2-WP3_ARCHITECTURE.md §3.4 specifies -- only the CHECK
        /// constraint swap is a real migration here. Verified by applying this migration to
        /// a fresh database and confirming the resulting `jobs` table has no user-defined
        /// "xmin" column (see PROGRESS.md's P2-WP3 acceptance entry for the verification
        /// transcript).
        /// </remarks>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_jobs_status",
                table: "jobs");

            migrationBuilder.AddCheckConstraint(
                name: "ck_jobs_status",
                table: "jobs",
                sql: "status IN ('checked_in','estimate_pending','awaiting_approval','approved','in_progress','completed','invoiced','closed','cancelled','deleted')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_jobs_status",
                table: "jobs");

            migrationBuilder.AddCheckConstraint(
                name: "ck_jobs_status",
                table: "jobs",
                sql: "status IN ('checked_in','diagnosing','waiting_approval','waiting_parts','ready_to_repair','repairing','qc','ready','delivered','cancelled')");
        }
    }
}
