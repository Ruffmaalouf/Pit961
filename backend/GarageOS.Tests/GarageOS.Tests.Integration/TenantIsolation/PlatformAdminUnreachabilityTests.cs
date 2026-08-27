using GarageOS.Domain.Platform;
using GarageOS.Tests.Integration.TestSupport;

namespace GarageOS.Tests.Integration.TenantIsolation;

/// <summary>Structural-unreachability proof (WP-3 brief §6/§14): platform_admins must
/// never be reachable through any garage-scoped query path.</summary>
[Collection("Integration")]
public class PlatformAdminUnreachabilityTests(IntegrationTestFixture fixture)
{
    [Fact]
    public void AppDbContext_Model_DoesNotContainPlatformAdmin()
    {
        using var db = fixture.CreateAppDbContext(new FakeCurrentTenant());

        Assert.DoesNotContain(db.Model.GetEntityTypes(), et => et.ClrType == typeof(PlatformAdmin));
    }

    [Fact]
    public void PlatformAdmin_HasNoTenantColumnsOrForeignKeys()
    {
        using var platformDb = fixture.CreatePlatformDbContext();
        var entityType = platformDb.Model.FindEntityType(typeof(PlatformAdmin))!;

        Assert.DoesNotContain(entityType.GetProperties(), p => p.Name is "GarageId" or "AccountId");
        Assert.Empty(entityType.GetForeignKeys());
    }

    [Fact]
    public async Task PlatformAdmins_Table_HasNoGarageOrAccountForeignKey_AtTheDatabaseLevel()
    {
        await fixture.ResetDatabaseAsync();
        await using var connection = new Npgsql.NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT COUNT(*)
            FROM information_schema.table_constraints tc
            JOIN information_schema.referential_constraints rc
              ON tc.constraint_name = rc.constraint_name AND tc.constraint_schema = rc.constraint_schema
            WHERE tc.table_schema = 'platform'
              AND tc.table_name = 'platform_admins'
              AND tc.constraint_type = 'FOREIGN KEY';";
        var count = (long)(await command.ExecuteScalarAsync() ?? 0L);

        Assert.Equal(0, count);
    }
}
