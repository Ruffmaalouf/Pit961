using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Respawn;

namespace GarageOS.Tests.Integration;

/// <summary>
/// WP-2 integration-test harness. Boots the real API via <see cref="WebApplicationFactory{Program}"/>
/// and connects to a real, locally reachable PostgreSQL 15+ instance — no Testcontainers,
/// no Docker, no EF Core InMemory/SQLite substitute (Owner decision, DECISIONS.md #10;
/// 13_phase1_execution_plan.md WP-2).
///
/// Connection string resolution order (never hardcoded in test source):
///   1. Environment variable <c>ConnectionStrings__IntegrationTestDb</c> (the convention
///      ASP.NET Core / CI use for nested configuration overrides).
///   2. <c>ConnectionStrings:IntegrationTestDb</c> in appsettings.Integration.json (a
///      safe, credential-free local default — see that file).
///
/// If the resolved database is unreachable, <see cref="InitializeAsync"/> throws and the
/// test run fails loudly with a clear connection error — it must never silently skip
/// (WP-2 acceptance criteria), which would create false local confidence while CI (always
/// provisioned) actually runs the suite.
/// </summary>
public sealed class IntegrationTestFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly string[] IgnoredTables = ["__EFMigrationsHistory"];

    // Null only when the schema genuinely has zero in-scope tables yet (true for all of
    // WP-2 — the schema itself lands in WP-3). Respawn's own Respawner.CreateAsync throws
    // if asked to build a delete graph over zero tables, so we detect that case up front
    // via a real catalog query (not by swallowing exceptions) and no-op the reset until
    // there is something to truncate. This still proves DB connectivity/reachability
    // (the "fail loudly if unreachable" requirement) — it's the truncation step alone
    // that's a legitimate no-op on a table-less schema.
    private Respawner? _respawner;
    private NpgsqlConnection _connection = null!;

    public string ConnectionString { get; private set; } = string.Empty;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }

    public async Task InitializeAsync()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.Integration.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        ConnectionString = configuration.GetConnectionString("IntegrationTestDb")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:IntegrationTestDb is not configured. Set it in " +
                "appsettings.Integration.json or override via the " +
                "ConnectionStrings__IntegrationTestDb environment variable.");

        // Fail loudly and immediately if the configured database is unreachable —
        // never silently skip (WP-2 acceptance criteria).
        _connection = new NpgsqlConnection(ConnectionString);
        await _connection.OpenAsync();

        if (await HasAnyInScopeTableAsync(_connection))
        {
            _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = ["public"],
                // __EFMigrationsHistory must never be truncated — doing so would wipe
                // EF Core's migration-tracking state on every test run (a known Respawn
                // gotcha called out explicitly in WP-2).
                TablesToIgnore = IgnoredTables.Select(t => (Respawn.Graph.Table)t).ToArray(),
            });
        }

        await ResetDatabaseAsync();
    }

    private static async Task<bool> HasAnyInScopeTableAsync(NpgsqlConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT COUNT(*) FROM information_schema.tables " +
            "WHERE table_schema = 'public' AND table_type = 'BASE TABLE' " +
            "AND table_name <> ALL (@ignored)";
        command.Parameters.AddWithValue("ignored", IgnoredTables);
        var count = (long)(await command.ExecuteScalarAsync() ?? 0L);
        return count > 0;
    }

    /// <summary>Truncates all in-scope tables back to a clean, known state. Call at the
    /// start of every test (or test collection) that touches the database — this is the
    /// Respawn-based reset mechanism WP-2 requires in place of a per-test migration
    /// down/up cycle. No-ops only while the schema has zero in-scope tables (WP-2); from
    /// WP-3 onward this performs a real truncation on every call.</summary>
    public async Task ResetDatabaseAsync()
    {
        if (_respawner is not null)
        {
            await _respawner.ResetAsync(_connection);
        }
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _connection.DisposeAsync();
        await base.DisposeAsync();
    }
}
