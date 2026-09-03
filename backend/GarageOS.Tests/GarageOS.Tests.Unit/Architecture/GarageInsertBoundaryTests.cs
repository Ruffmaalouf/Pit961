namespace GarageOS.Tests.Unit.Architecture;

using System.Text.RegularExpressions;

/// <summary>
/// Source-scanning architecture test (WP-3B brief §6) -- proves no production code path
/// other than AccountProvisioningService can insert a row into `garages`. This is a
/// call-site text pattern, not something EF's runtime model metadata can see, so this is
/// a regex source scan rather than IL reflection (same technique WP-6/WP-7 use for their
/// Resend-SDK-isolation and placeholder-brand-name checks).
///
/// Scope is production code only (Api/Application/Domain/Infrastructure) -- deliberately
/// excludes GarageOS.Tests.* because TwoTenantFixture.cs already does a direct
/// db.Garages.Add(...) for its own two-independent-tenants test setup (WP-3, already
/// reviewed/accepted), and AccountProvisioningServiceTests' own bypass test intentionally
/// does a direct insert to prove the DB constraint is the ultimate backstop.
///
/// KI-8 fix (P2-WP1, Backend Engineer; QA Automation Engineer + Security Reviewer gates):
/// the original version of this test only matched patterns where "Garages"/"Garage" was
/// textually present at the call site itself (`Garages.Add(`, `Set&lt;Garage&gt;().Add`,
/// `INSERT INTO garages`). It did not catch EF Core's non-generic
/// `DbContext.Add(object)`/`AddAsync(object)`/`AddRange(IEnumerable&lt;object&gt;)` overloads
/// called with a `new Garage` object literal, the same shapes for
/// `DbContext.Entry(...).State = EntityState.Added`, or either shape called with a
/// Garage-typed local variable instead of an inline `new Garage`. Round 1 of this fix
/// closed the inline-`new Garage` shapes and an explicitly-`Garage`-typed-variable
/// cross-reference; QA Automation Engineer's gate review then found and blocked on a real
/// gap in that round: an implicitly-typed (`var g = new Garage {...}`) declaration was
/// never registered as a tracked variable name, even though `var` is this codebase's
/// dominant declaration style -- making it the *most likely* real-world phrasing of the
/// exact gap KI-8 exists to close, not an edge case. Round 2 (this version) closes that:
///  - DirectBypassPatterns: two patterns for the inline-`new Garage` shape of
///    Add/AddRange/AddAsync and Entry(...).State = EntityState.Added (round 1).
///  - GarageVariableDeclarationPatterns now has TWO patterns feeding the same tracked-name
///    set: an explicit `Garage`/`Garage?` type declaration (round 1), and a `var name =
///    new Garage` declaration (round 2, QA-required) -- the latter needs no semantic type
///    resolution since the type is lexically present in the same statement as `new
///    Garage`, so it's a legitimate heuristic-scanner extension, not an exception to the
///    "no full type resolution" limitation.
///  - AddCallSingleArgPattern (round 1) covers a Garage-typed variable passed as the SOLE
///    argument to Add/AddAsync/AddRange. AddRangeArgsListPattern (round 2, Security-Reviewer
///    MEDIUM #3) additionally covers a Garage-typed variable passed as ONE OF SEVERAL
///    arguments to AddRange/AddRangeAsync (e.g. `context.AddRange(garageVar,
///    otherEntityVar)`), which the sole-argument check alone would miss.
///  - EntryStateAddedCallPattern (round 1) covers a Garage-typed variable as the receiver
///    of Entry(...).State = EntityState.Added.
///  - All checks run against SourceScanUtilities.MaskLiteralsAndComments's output (the
///    same shared helper EstimateMutationBoundaryTests uses), not raw file text, so a
///    ';'/"Garage"/"Add(" that only exists inside a string literal, interpolation-hole-
///    adjacent template text, or a comment can't be mistaken for real code.
///
/// Proof (P2-WP1 report has the full transcript): the full unit suite (47 tests, then 48
/// after this file's own test-adjacent files are unaffected) passes clean with no fixture
/// present -- zero false positives against the current codebase. Five isolated one-
/// statement bypass fixtures (direct inline-new-Garage Add; indirect explicitly-typed-
/// variable Add; indirect explicitly-typed-variable Entry+EntityState.Added; indirect
/// `var`-declared-variable Add per the QA-required round-2 fix; direct inline-new-Garage
/// Entry+EntityState.Added; and a multi-argument AddRange containing a Garage-typed
/// identifier alongside another argument) were each added, individually confirmed to make
/// this test FAIL and be listed as a violation, then all were deleted and the suite
/// re-confirmed to pass clean.
///
/// Known, documented residual limitations (same class already accepted for KI-16 on
/// EstimateMutationBoundaryTests -- a heuristic text scanner, not a C# type resolver):
///  - Does not follow a Garage-typed value through a method parameter, a cast, a
///    collection/array element, or a property access (e.g. `entity.SomeGarage`).
///  - A multi-declarator statement (`Garage g1 = ..., g2 = ...;`) only registers the
///    FIRST declared name; a subsequent declarator on the same statement is untracked.
///    (No such declaration exists anywhere in the current codebase, confirmed by the
///    zero-false-positive full-suite run above.)
///  - Variable-name tracking is file-scoped, not method/block-scoped: a short name (e.g.
///    `g`) reused for an explicitly-Garage-typed value in one method and an unrelated
///    type in another method within the SAME file could, in principle, produce a false
///    positive if the unrelated value is later passed to Add/AddRange/Entry(...).State =
///    EntityState.Added. Not triggered by any code that exists today (confirmed by the
///    same zero-false-positive run); flagged here per QA Automation Engineer's review so
///    it's a disclosed, accepted tradeoff rather than a silent one.
///  - This is a CI-time compensating/coding-standard control, not a runtime enforcement
///    mechanism: it has no visibility into raw ADO.NET/Dapper inserts on the same
///    connection, DbContext.Database.ExecuteSqlRaw/ExecuteSqlInterpolated, bulk-insert
///    libraries, or reflection/dynamic/expression-tree-built inserts (Security Reviewer
///    MEDIUM #1). The DB-level partial unique index (`garages_account_active_idx`)
///    prevents a SECOND active garage per account but does NOT by itself prevent an
///    unauthorized FIRST garage insert via one of those uncovered paths -- it is a
///    duplicate-prevention control, not a general backstop for "only
///    AccountProvisioningService may create a Garage." If that invariant is ever treated
///    as load-bearing for tenant isolation beyond coding-standard enforcement, a runtime
///    mechanism (e.g. a distinguished DB role/trigger) would be needed; tracked as
///    optional future-hardening, out of KI-8's ticketed scope (which was specifically the
///    EF-Core-tracked-insert-API textual-matching gap).
/// </summary>
public class GarageInsertBoundaryTests
{
    // Direct patterns: the DbSet/type name is textually present at the call site itself.
    private static readonly Regex[] DirectBypassPatterns =
    {
        new(@"\bGarages\s*\.\s*Add(Range)?\s*\(", RegexOptions.Compiled),
        new(@"\bSet<\s*Garage\s*>\s*\(\s*\)\s*\.\s*Add", RegexOptions.Compiled),
        new(@"INSERT\s+INTO\s+garages\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),

        // KI-8: EF Core's non-generic DbContext.Add(object)/AddAsync(object)/
        // AddRange(IEnumerable<object>) overloads, called with an inline `new Garage`.
        new(@"\.\s*Add(Range)?(Async)?\s*\([^;]*?\bnew\s+Garage\b",
            RegexOptions.Compiled | RegexOptions.Singleline),

        // KI-8: EF Core's change-tracker API used directly instead of Add/AddRange,
        // called with an inline `new Garage`.
        new(@"\.\s*Entry\s*\([^;]*?\bnew\s+Garage\b[^;]*?\)\s*\.\s*State\s*=\s*EntityState\s*\.\s*Added",
            RegexOptions.Compiled | RegexOptions.Singleline),
    };

    // Indirect (variable-based) shapes -- KI-8. `Garage g = ...; _db.Add(g);` or
    // `var g = new Garage {...}; _db.Add(g);` don't have the word "Garage" textually
    // present at the call site at all, only at the variable's declaration.
    private static readonly Regex[] GarageVariableDeclarationPatterns =
    {
        // Explicitly-typed: `Garage g = ...;` / `Garage? g = ...;`
        new(@"\bGarage\s*\??\s+(\w+)\s*=", RegexOptions.Compiled),
        // Implicitly-typed with an inline `new Garage` initializer: `var g = new Garage
        // {...};`. No semantic type resolution needed -- the type is lexically present in
        // the same declaration statement -- so this is a legitimate heuristic-scanner
        // extension, not an exception to the "no full type resolution" limitation.
        new(@"\bvar\s+(\w+)\s*=\s*new\s+Garage\b", RegexOptions.Compiled),
    };

    private static readonly Regex AddCallSingleArgPattern =
        new(@"\.\s*Add(Range)?(Async)?\s*\(\s*(\w+)\s*\)", RegexOptions.Compiled);

    // AddRange/AddRangeAsync called with MULTIPLE arguments, one of which may be a
    // Garage-typed identifier alongside other, unrelated arguments (the sole-argument
    // check above only catches the single-argument form).
    private static readonly Regex AddRangeArgsListPattern =
        new(@"\.\s*AddRange(Async)?\s*\(([^;]*?)\)", RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex EntryStateAddedCallPattern =
        new(@"\.\s*Entry\s*\(\s*(\w+)\s*\)\s*\.\s*State\s*=\s*EntityState\s*\.\s*Added", RegexOptions.Compiled);

    private const string AllowedRelativePath =
        "GarageOS.Infrastructure/Data/Provisioning/AccountProvisioningService.cs";

    private static readonly string[] ScannedProjectDirs =
        { "GarageOS.Api", "GarageOS.Application", "GarageOS.Domain", "GarageOS.Infrastructure" };

    [Fact]
    public void OnlyAccountProvisioningService_MayInsertIntoGaragesDbSet()
    {
        var backendDir = FindBackendDirectory();
        var violations = new List<string>();

        foreach (var (normalized, text) in ScanFiles(backendDir))
        {
            var masked = SourceScanUtilities.MaskLiteralsAndComments(text);

            if (DirectBypassPatterns.Any(p => p.IsMatch(masked)))
            {
                violations.Add($"{normalized} (direct bypass pattern)");
                continue;
            }

            if (HasIndirectGarageInsert(masked))
            {
                violations.Add(
                    $"{normalized} (indirect: Garage-typed variable passed to " +
                    "Add/AddRange/AddAsync, or to Entry(...).State = EntityState.Added)");
            }
        }

        Assert.True(violations.Count == 0,
            "Only AccountProvisioningService may insert into AppDbContext.Garages. Violating file(s): "
            + string.Join(", ", violations));
    }

    private static bool HasIndirectGarageInsert(string masked)
    {
        var garageVarNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var declPattern in GarageVariableDeclarationPatterns)
            foreach (Match m in declPattern.Matches(masked))
                garageVarNames.Add(m.Groups[1].Value);

        if (garageVarNames.Count == 0) return false;

        foreach (Match m in AddCallSingleArgPattern.Matches(masked))
            if (garageVarNames.Contains(m.Groups[3].Value)) return true;

        foreach (Match m in AddRangeArgsListPattern.Matches(masked))
        {
            var argsText = m.Groups[2].Value;
            foreach (var name in garageVarNames)
                if (Regex.IsMatch(argsText, $@"\b{Regex.Escape(name)}\b")) return true;
        }

        foreach (Match m in EntryStateAddedCallPattern.Matches(masked))
            if (garageVarNames.Contains(m.Groups[1].Value)) return true;

        return false;
    }

    private static IEnumerable<(string Normalized, string Text)> ScanFiles(string backendDir)
    {
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

                yield return (normalized, File.ReadAllText(file));
            }
        }
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
