namespace GarageOS.Tests.Unit.Architecture;

using System.Text.RegularExpressions;

/// <summary>
/// WP-5 brief §8 -- proves no production code path other than
/// GarageOS.Infrastructure/Data/Estimates/EstimateMutationRepository.cs can mutate
/// Estimate.DiscountAmount/Total/Status via an EF-tracked update, a bulk update, Attach()
/// plus in-memory mutation, or raw SQL, AND that no other file holds a tracked (non
/// AsNoTracking) reference to an Estimate/Estimates query at all (the AsNoTracking
/// whitelist check below). Deliberately does NOT restrict Estimates.Add(...) -- creating a
/// new Estimate row is out of WP-5 scope (brief §6); only post-creation MUTATION of the
/// guarded columns is guarded here.
///
/// Design constraint (brief §8): unlike GarageInsertBoundaryTests's `Garages` DbSet name,
/// Estimate's own property names (DiscountAmount/Total/Status) are NOT unique in the
/// schema -- Invoice also has Total/DiscountAmount, Job also has Status. Every pattern
/// below is therefore anchored on the DbSet-rooted access (`Estimates`/`Set&lt;Estimate&gt;()`),
/// which -- like `Garages` -- is unique to this entity, rather than on the guarded
/// property names alone.
///
/// QA Automation review (post-implementation) found and this file now closes two real,
/// execution-verified bypasses of an earlier version of this test:
///  - EF Core's non-generic DbContext.Update(entity)/Attach(entity) overloads mutate an
///    entity without the text "Estimates.Update("/"Estimates.Attach(" ever appearing --
///    they operate on whatever object reference is passed in, inferring the entity type
///    at runtime. Confirmed (by grep across the whole solution) that this codebase has
///    ZERO existing legitimate uses of DbContext.Update(/.Attach(/.UpdateRange(/.AttachRange(
///    on ANY entity anywhere -- every write path instead re-fetches a tracked instance and
///    sets properties directly (EstimateMutationRepository's own pattern). Given that,
///    banning these method names entirely outside the allow-listed file (not just when
///    DbSet-rooted) has zero false-positive risk against the current codebase and closes
///    the gap. A future WP adding a legitimate Update()/Attach() call for a *different*
///    entity in a *different* file will need to widen this test's allow-list -- an
///    intentional, visible speed bump given how central "single mutation path" is here.
///  - The original ExecuteUpdateAsync check used a fixed 200-character forward window from
///    "Estimates", which a realistic multi-.Where()-clause LINQ chain can push the actual
///    .ExecuteUpdateAsync( call past. Replaced with a statement-scoped check (splitting on
///    ';', the natural C# statement boundary) instead of an arbitrary character count, so
///    an arbitrarily long method chain within one statement is still caught.
///
/// QA Automation round-2 review then found that the ';'-statement-scoping fix above was
/// itself naive about string literal CONTENT: a ';' embedded inside an Estimates-query
/// .Where(...) string argument (e.g. a filter value containing "; ") splits the
/// "statement" early, separating the root-access token from the real guarded call within
/// what should be one statement -- a complete, silent bypass, confirmed by execution.
/// Fixed by scanning SourceScanUtilities.MaskLiteralsAndComments's output instead of the
/// raw text for EVERYTHING in this file -- both the initial pattern matches and the
/// statement splitting/scoping. That helper blanks the CONTENT of every string/char
/// literal and comment (preserving length and line breaks, so line numbers still line up)
/// while leaving real code tokens completely untouched, so a ';' that only exists inside a
/// string value can no longer be mistaken for a real C# statement terminator, AND (a
/// related false-positive this same switch closes, found while re-verifying the round-2
/// fix) a doc comment that merely *mentions* "Estimates." in passing -- as several
/// comments in this very file do -- can no longer be mistaken for a real DbSet access
/// either.
/// </summary>
public class EstimateMutationBoundaryTests
{
    private static readonly Regex[] BypassPatterns =
    {
        // AppDbContext.Estimates.Update(...) / UpdateRange(...)
        new(@"\bEstimates\s*\.\s*Update(Range)?\s*\(", RegexOptions.Compiled),
        // db.Set<Estimate>().Update(...)
        new(@"Set<\s*Estimate\s*>\s*\(\s*\)\s*\.\s*Update", RegexOptions.Compiled),
        // Raw/interpolated SQL bypass vector (same style AccountProvisioningService itself
        // uses legitimately via FromSqlInterpolated -- guarding against the same pattern
        // being misused against `estimates`).
        new(@"UPDATE\s+estimates\s+SET", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        // Attach() puts an entity into the Unchanged tracked state; EF's snapshot change
        // detection then marks any subsequently-set property Modified WITHOUT any call
        // ever containing the text "Update(".
        new(@"\bEstimates\s*\.\s*Attach(Range)?\s*\(", RegexOptions.Compiled),
        new(@"Set<\s*Estimate\s*>\s*\(\s*\)\s*\.\s*Attach", RegexOptions.Compiled),
        // QA-review required fix (round-1 Bypass A): DbContext's non-generic
        // Update(entity)/Attach(entity)/UpdateRange(entities)/AttachRange(entities)
        // overloads operate on whatever object is passed in -- no "Estimates."/
        // "Set<Estimate>()" text is required to trigger them, and there is no
        // Estimate-specific text of any kind in this bypass shape to anchor on. The
        // original round-1 fix anchored these two patterns on the `db`/`_db`
        // variable-name convention this codebase happened to use everywhere for
        // AppDbContext -- QA Lead's WP-5 gate review pointed out that anchor was an
        // unnecessarily narrow choice (KI-16): it left a documented, tracked gap for any
        // AppDbContext variable named something else (e.g. `context.Update(estimate)`).
        // Confirmed by grep (GarageOS.Api/Application/Domain/Infrastructure, current as
        // of WP-5): there are ZERO legitimate call sites for .Update(/.UpdateRange(/
        // .Attach(/.AttachRange( anywhere in the ENTIRE solution -- not even inside the
        // allow-listed EstimateMutationRepository.cs itself, which deliberately avoids
        // these methods in favor of a fresh re-fetch-and-set-properties pattern instead.
        // Given that, these two patterns are fully UNANCHORED -- matching the method call
        // regardless of the receiver's variable name -- which has zero false-positive
        // risk against the current codebase and fully closes KI-16 (no longer a tracked
        // residual limitation; see KNOWN_ISSUES.md for the corresponding entry marked
        // RESOLVED). A future WP introducing a legitimate .Update(/.Attach( call on ANY
        // entity, anywhere, will need to widen this allow-list -- an intentional, visible
        // speed bump, the same trade-off already accepted for the DbSet-rooted patterns
        // above.
        new(@"\.\s*Update(Range)?\s*\(", RegexOptions.Compiled),
        new(@"\.\s*Attach(Range)?\s*\(", RegexOptions.Compiled),
    };

    // Statement boundaries are found in the MASKED text (see class doc comment), so a ';'
    // that only exists inside a string literal value or a comment can never be mistaken
    // for a real C# statement terminator. A statement is flagged if it references
    // Estimates/Set<Estimate>() AND calls ExecuteUpdateAsync( anywhere in that same
    // statement.
    private static readonly Regex EstimatesOrSetEstimateRootPattern =
        new(@"\bEstimates\s*\.|\bSet<\s*Estimate\s*>\s*\(\s*\)", RegexOptions.Compiled);

    private static readonly Regex ExecuteUpdateAsyncPattern =
        new(@"\.\s*ExecuteUpdateAsync\s*\(", RegexOptions.Compiled);

    // Whitelist check: matches every DbSet-rooted access to Estimates outside the allowed
    // file and requires AsNoTracking chained in the SAME (masked-text-scoped) statement.
    private static readonly Regex EstimatesAccessPattern =
        new(@"\bEstimates\s*\.|\bSet<\s*Estimate\s*>\s*\(\s*\)", RegexOptions.Compiled);

    private static readonly Regex DbSetDeclarationPattern =
        new(@"DbSet<\s*Estimate\s*>\s*Estimates", RegexOptions.Compiled);

    private static readonly Regex AsNoTrackingPattern =
        new(@"\.AsNoTracking(WithIdentityResolution)?\s*\(", RegexOptions.Compiled);

    private const string AllowedRelativePath =
        "GarageOS.Infrastructure/Data/Estimates/EstimateMutationRepository.cs";

    private static readonly string[] ScannedProjectDirs =
        { "GarageOS.Api", "GarageOS.Application", "GarageOS.Domain", "GarageOS.Infrastructure" };

    [Fact]
    public void OnlyEstimateMutationRepository_MayMutateEstimatesRows()
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
                if (EstimatesOrSetEstimateRootPattern.IsMatch(statement)
                    && ExecuteUpdateAsyncPattern.IsMatch(statement))
                {
                    violations.Add($"{normalized} (Estimates-rooted ExecuteUpdateAsync bulk update)");
                }
            }
        }

        Assert.True(violations.Count == 0,
            "Only EstimateMutationRepository may mutate Estimates rows. Violating file(s): "
            + string.Join(", ", violations));
    }

    [Fact]
    public void OnlyEstimateMutationRepository_MayHoldATrackedEstimatesReference()
    {
        var backendDir = FindBackendDirectory();
        var violations = new List<string>();

        foreach (var (normalized, text) in ScanFiles(backendDir))
        {
            var masked = SourceScanUtilities.MaskLiteralsAndComments(text);

            foreach (Match match in EstimatesAccessPattern.Matches(masked))
            {
                // Skip the AppDbContext DbSet property declaration itself
                // ("public DbSet<Estimate> Estimates => Set<Estimate>();") -- that's a
                // property definition, not a query, and isn't chained to anything.
                var lineStart = masked.LastIndexOf('\n', Math.Max(0, match.Index - 1)) + 1;
                var lineEnd = masked.IndexOf('\n', match.Index);
                if (lineEnd < 0) lineEnd = masked.Length;
                var line = masked[lineStart..lineEnd];
                if (DbSetDeclarationPattern.IsMatch(line)) continue;

                var statement = StatementContaining(masked, match.Index);
                if (!AsNoTrackingPattern.IsMatch(statement))
                {
                    violations.Add($"{normalized}:{CountLines(masked, match.Index)}");
                }
            }
        }

        Assert.True(violations.Count == 0,
            "Only EstimateMutationRepository may hold a tracked (non-AsNoTracking) "
            + "reference to an Estimates query. Violating location(s): "
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
