namespace GarageOS.Tests.Unit.Architecture;

using System.Text.RegularExpressions;

/// <summary>
/// Source-scanning architecture test (WP-3B brief §6) — proves no production code path
/// other than AccountProvisioningService can insert a row into `garages`. This is a
/// call-site text pattern, not something EF's runtime model metadata can see, so this is
/// a regex source scan rather than IL reflection (same technique WP-6/WP-7 use for their
/// Resend-SDK-isolation and placeholder-brand-name checks).
///
/// Scope is production code only (Api/Application/Domain/Infrastructure) — deliberately
/// excludes GarageOS.Tests.* because TwoTenantFixture.cs already does a direct
/// db.Garages.Add(...) for its own two-independent-tenants test setup (WP-3, already
/// reviewed/accepted), and AccountProvisioningServiceTests' own bypass test intentionally
/// does a direct insert to prove the DB constraint is the ultimate backstop.
/// </summary>
public class GarageInsertBoundaryTests
{
    private static readonly Regex[] BypassPatterns =
    {
        new(@"\bGarages\s*\.\s*Add(Range)?\s*\(", RegexOptions.Compiled),
        new(@"\bSet<\s*Garage\s*>\s*\(\s*\)\s*\.\s*Add", RegexOptions.Compiled),
        new(@"INSERT\s+INTO\s+garages\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
    };

    private const string AllowedRelativePath =
        "GarageOS.Infrastructure/Data/Provisioning/AccountProvisioningService.cs";

    private static readonly string[] ScannedProjectDirs =
        { "GarageOS.Api", "GarageOS.Application", "GarageOS.Domain", "GarageOS.Infrastructure" };

    [Fact]
    public void OnlyAccountProvisioningService_MayInsertIntoGaragesDbSet()
    {
        var backendDir = FindBackendDirectory();
        var violations = new List<string>();

        foreach (var projectDir in ScannedProjectDirs)
        {
            var dir = Path.Combine(backendDir, projectDir);
            if (!Directory.Exists(dir)) continue;

            foreach (var file in Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories))
            {
                var normalized = file.Replace('\\', '/');
                if (normalized.Contains("/obj/") || normalized.Contains("/bin/")
                    || normalized.Contains("/Migrations/"))
                    continue;
                if (normalized.EndsWith(AllowedRelativePath, StringComparison.Ordinal))
                    continue;

                var text = File.ReadAllText(file);
                if (BypassPatterns.Any(p => p.IsMatch(text)))
                    violations.Add(normalized);
            }
        }

        Assert.True(violations.Count == 0,
            "Only AccountProvisioningService may insert into AppDbContext.Garages. Violating file(s): "
            + string.Join(", ", violations));
    }

    private static string FindBackendDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "GarageOS.sln")))
            dir = dir.Parent;
        if (dir is null)
            throw new InvalidOperationException("Could not locate GarageOS.sln from the test output directory.");
        return dir.FullName;
    }
}
