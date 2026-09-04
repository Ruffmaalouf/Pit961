using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GarageOS.Infrastructure.Data.Migrations.App
{
    /// <inheritdoc />
    /// <remarks>
    /// P2-WP4: adds the EF Core "xmin" shadow property (IsRowVersion) to EstimateConfiguration
    /// as an optimistic-concurrency token. `dotnet ef migrations add` scaffolds a spurious
    /// AddColumn/DropColumn for it -- hand-removed here, exactly as
    /// MigrateJobStatusVocabulary's remarks document for the identical Job-entity case:
    /// Postgres's built-in "xmin" system column already exists on every table, and Postgres
    /// rejects adding a column by that name. This migration is a deliberate no-op left in
    /// place only so the model snapshot and the migration history stay consistent with each
    /// other.
    /// </remarks>
    public partial class AddEstimateConcurrencyToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty -- see remarks above.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty -- see remarks above.
        }
    }
}
