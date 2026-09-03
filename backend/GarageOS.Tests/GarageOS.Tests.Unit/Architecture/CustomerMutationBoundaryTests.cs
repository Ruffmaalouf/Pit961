namespace GarageOS.Tests.Unit.Architecture;

using System.Text.RegularExpressions;

/// <summary>
/// P2-WP2. Proves no production code path other than
/// GarageOS.Infrastructure/Data/Customers/CustomerMutationRepository.cs can mutate
/// Customer rows (via an EF-tracked update, a bulk update, Attach() plus in-memory
/// mutation, or raw SQL), AND that no other file holds a tracked (non AsNoTracking)
/// reference to a Customers query at all. Directly follows the pattern established by
/// EstimateMutationBoundaryTests (P2-WP1/WP-5) -- including its own two rounds of
/// QA-review-driven bypass closures (unanchored .Update(/.Attach( patterns; masked-text
/// statement scoping via SourceScanUtilities.MaskLiteralsAndComments so a ';' or the word
/// "Customers" inside a string/comment can never be mistaken for real code) -- applied
/// here from the start rather than re-discovered the hard way a third time.
///
/// Design note: unlike Estimate's DiscountAmount/Total/Status (not globally unique
/// property names), Customer has no guarded PROPERTY at all -- soft delete only ever
/// touches DeletedAt/DeletedBy, which also aren't globally unique names (User/Vehicle both
/// have DeletedBy too). Every pattern below is anchored on the DbSet-rooted access
/// (`Customers`/`Set&lt;Customer&gt;()`), exactly like GarageInsertBoundaryTests/
/// EstimateMutationBoundaryTests use their own DbSet-rooted anchors for the same reason.
///
/// Deliberately does NOT restrict Customers.Add(...) -- creating a new Customer is not a
/// guarded mutation path (CustomerManagementService.CreateAsync calls
/// ICustomerMutationRepository.InsertAsync, itself calling Customers.Add(...) legitimately
/// inside the one allow-listed file; nothing about "creating a brand-new row" needs
/// single-caller protection the way "mutating an existing row's guarded fields" does).
/// </summary>
public class CustomerMutationBoundaryTests
{
    private static readonly Regex[] BypassPatterns =
    {
        new(@"\bCustomers\s*\.\s*Update(Range)?\s*\(", RegexOptions.Compiled),
        new(@"Set<\s*Customer\s*>\s*\(\s*\)\s*\.\s*Update", RegexOptions.Compiled),
        new(@"UPDATE\s+customers\s+SET", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"\bCustomers\s*\.\s*Attach(Range)?\s*\(", RegexOptions.Compiled),
        new(@"Set<\s*Customer\s*>\s*\(\s*\)\s*\.\s*Attach", RegexOptions.Compiled),
        // Unanchored on receiver name from the start -- see EstimateMutationBoundaryTests'
        // doc comment for why a `db`/`_db`-anchored version would leave a tracked gap.
        new(@"\.\s*Update(Range)?\s*\(", RegexOptions.Compiled),
        new(@"\.\s*Attach(Range)?\s*\(", RegexOptions.Compiled),
    };

    private static readonly Regex CustomersOrSetCustomerRootPattern =
        new(@"\bCustomers\s*\.|\bSet<\s*Customer\s*>\s*\(\s*\)", RegexOptions.Compiled);

    private static readonly Regex ExecuteUpdateAsyncPattern =
        new(@"\.\s*ExecuteUpdateAsync\s*\(", RegexOptions.Compiled);

    private static readonly Regex CustomersAccessPattern =
        new(@"\bCustomers\s*\.|\bSet<\s*Customer\s*>\s*\(\s*\)", RegexOptions.Compiled);

    private static readonly Regex DbSetDeclarationPattern =
        new(@"DbSet<\s*Customer\s*>\s*Customers", RegexOptions.Compiled);

    private static readonly Regex AsNoTrackingPattern =
        new(@"\.AsNoTracking(WithIdentityResolution)?\s*\(", RegexOptions.Compiled);

    // Customers.Add(...) is legitimate inside the allow-listed file (create path) -- the
    // tracked-reference check below only cares about reads that then get mutated
    // elsewhere, not the fresh entity InsertAsync itself constructs and adds.
    private static readonly Regex AddCallPattern =
        new(@"\bCustomers\s*\.\s*Add(Range)?\s*\(", RegexOptions.Compiled);

    private const string AllowedRelativePath =
        "GarageOS.Infrastructure/Data/Customers/CustomerMutationRepository.cs";

    private static readonly string[] ScannedProjectDirs =
        { "GarageOS.Api", "GarageOS.Application", "GarageOS.Domain", "GarageOS.Infrastructure" };

    [Fact]
    public void OnlyCustomerMutationRepository_MayMutateCustomersRows()
    {
        var backendDir = FindBackendDirectory();
        var violations = new List<string>();

        foreach (var (normalized, text) in ScanFiles(backendDir))
        {
            var masked = SourceScanUtilities.MaskLiteralsAndComments(text);

            if (BypassPatterns.Any(p => p.IsMatch(masked)))
            {
                violations.Add($"{normalized} (mutation bypass pattern)");
            }

            foreach (var statement in masked.Split(';'))
            {
                if (CustomersOrSetCustomerRootPattern.IsMatch(statement)
                    && ExecuteUpdateAsyncPattern.IsMatch(statement))
                {
                    violations.Add($"{normalized} (Customers-rooted ExecuteUpdateAsync bulk update)");
                }
            }
        }

        Assert.True(violations.Count == 0,
            "Only CustomerMutationRepository may mutate Customers rows. Violating file(s): "
            + string.Join(", ", violations));
    }

    [Fact]
    public void OnlyCustomerMutationRepository_MayHoldATrackedCustomersReference()
    {
        var backendDir = FindBackendDirectory();
        var violations = new List<string>();

        foreach (var (normalized, text) in ScanFiles(backendDir))
        {
            var masked = SourceScanUtilities.MaskLiteralsAndComments(text);

            foreach (Match match in CustomersAccessPattern.Matches(masked))
            {
                var lineStart = masked.LastIndexOf('\n', Math.Max(0, match.Index - 1)) + 1;
                var lineEnd = masked.IndexOf('\n', match.Index);
                if (lineEnd < 0) lineEnd = masked.Length;
                var line = masked[lineStart..lineEnd];
                if (DbSetDeclarationPattern.IsMatch(line)) continue;

                var statement = StatementContaining(masked, match.Index);
                if (AddCallPattern.IsMatch(statement)) continue; // Add(...) is not a tracked-read concern
                if (!AsNoTrackingPattern.IsMatch(statement))
                {
                    violations.Add($"{normalized}:{CountLines(masked, match.Index)}");
                }
            }
        }

        Assert.True(violations.Count == 0,
            "Only CustomerMutationRepository may hold a tracked (non-AsNoTracking) "
            + "reference to a Customers query. Violating location(s): "
            + string.Join(", ", violations));
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

    private static string StatementContaining(string maskedText, int index)
    {
        var start = maskedText.LastIndexOf(';', Math.Max(0, index - 1)) + 1;
        var end = maskedText.IndexOf(';', index);
        if (end < 0) end = maskedText.Length;
        return maskedText[start..end];
    }

    private static int CountLines(string text, int index) =>
        text[..index].Count(c => c == '\n') + 1;

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
