using GarageOS.Application.Abstractions;
using GarageOS.Infrastructure.Data;
using GarageOS.Infrastructure.Data.Platform;
using GarageOS.Tests.Integration.TestSupport;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
    private static readonly string[] IgnoredTables = ["__EFMigrationsHistory", "__ef_migrations_history_platform"];

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

    /// <summary>Singleton capture point for password-reset emails the app "sends" during
    /// a test run. Exposed as a static so individual test classes can reach it without
    /// threading a fixture-scoped instance through DI resolution -- there is exactly one
    /// app host (and therefore one email service instance) per fixture, so this is safe.
    /// Tests MUST call <see cref="CapturingEmailService.Reset"/> between cases that use it
    /// (WP-4 forgot/reset-password tests run in a collection to avoid cross-test bleed).</summary>
    public CapturingEmailService CapturedEmails { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // WP-3's TestAuthHandler (header-driven X-Test-* claims bypass) is RETIRED as of
        // WP-4, per its own doc comment's instruction and WP-4 brief section 16 point 3:
        // the test host now uses the REAL production AddJwtBearer scheme (registered by
        // Program.cs) and mints tokens via TestJwtTokenFactory, which calls the real
        // ITokenService -- not a fake "Test" authentication scheme. A spoofed GarageId
        // now requires forging a valid signature against the real (test-host) signing
        // key, not just setting a header. appsettings.Integration.json supplies a fixed,
        // non-secret, CSPRNG-generated test-only Jwt:SigningKey/Issuer/Audience (see
        // JwtOptions.cs remarks) so JwtOptions' mandatory ValidateOnStart() passes for
        // this host exactly as it does in Development/Production.

        builder.ConfigureTestServices(services =>
        {
            // Replace Program.cs's NoOpEmailService with a capturing double so
            // forgot/reset-password tests can observe the generated reset link -- the
            // anti-enumeration design (brief §14) never returns the raw token over HTTP,
            // so this is the only way to recover it in a test (WP-4 brief §18).
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(CapturedEmails);
        });

        // WP-4 is the first work package whose HTTP-level tests (AuthController) exercise
        // the DI-registered AppDbContext -- everything through WP-3B went via
        // CreateAppDbContext directly, bypassing this entirely. GarageOS.Api's own
        // appsettings.json/appsettings.Testing.json carry an intentionally-empty
        // ConnectionStrings:GarageOsDb (never a committed credential), so without this
        // override the real login/refresh/me/etc. HTTP paths would fail against an empty
        // Npgsql connection string. Point the host at the SAME database ConnectionString
        // resolves to (set in InitializeAsync, which always completes before any test
        // triggers host creation via CreateClient()/Services) -- this is also what makes
        // AuthTestFixtures.SeedActiveUserAsync's direct-DbContext writes visible to the
        // real HTTP login flow.
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:GarageOsDb"] = ConnectionString,
                ["ConnectionStrings:PlatformDb"] = ConnectionString,
            });
        });
    }

    /// <summary>Constructs a standalone AppDbContext against the fixture's real Postgres
    /// connection, with the given (fake) tenant — no HTTP involved. This is the primary
    /// mechanism WP-3's tenant-isolation tests use to prove the global query filter and
    /// TenantGuard both work per resource (brief §9/§14).</summary>
    public AppDbContext CreateAppDbContext(ICurrentTenant currentTenant)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;
        return new AppDbContext(options, currentTenant);
    }

    /// <summary>Constructs a standalone PlatformDbContext against the fixture's real
    /// Postgres connection — used only by the structural-unreachability tests.</summary>
    public PlatformDbContext CreatePlatformDbContext()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseNpgsql(ConnectionString, npgsqlOptions =>
                npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history_platform"))
            .UseSnakeCaseNamingConvention()
            .Options;
        return new PlatformDbContext(options);
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
