namespace GarageOS.Tests.Unit.Architecture;

using System.Text.RegularExpressions;

/// <summary>
/// P2-WP3. Proves no production code path other than
/// GarageOS.Infrastructure/Data/Jobs/JobMutationRepository.cs can mutate Job or
/// JobHistoryEntry rows (via an EF-tracked update, a bulk update, Attach() plus in-memory
/// mutation, or raw SQL), AND that no other file holds a tracked (non AsNoTracking)
/// reference to a Jobs/JobHistory query at all. Directly follows
/// CustomerMutationBoundaryTests' pattern (P2-WP2), extended to TWO DbSet roots (Jobs AND
/// JobHistory) since TransitionStatusAsync legitimately mutates both tables in one unit of
/// work inside the single allow-listed file.
///
/// Deliberately does NOT restrict Jobs.Add(...)/JobHistory.Add(...) -- creating rows isn't
/// the guarded mutation path, same reasoning as CustomerMutationBoundaryTests' remarks on
/// Customers.Add(...).
/// </summary>
public class JobMutationBoundaryTests
{
    private static readonly Regex[] BypassPatterns =
    {
        new(@"\bJobs\s*\.\s*Update(Range)?\s*\(", RegexOptions.Compiled),
        new(@"Set<\s*Job\s*>\s*\(\s*\)\s*\.\s*Update", RegexOptions.Compiled),
        new(@"UPDATE\s+jobs\s+SET", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"\bJobs\s*\.\s*Attach(Range)?\s*\(", RegexOptions.Compiled),
        new(@"Set<\s*Job\s*>\s*\(\s*\)\s*\.\s*Attach", RegexOptions.Compiled),

        new(@"\bJobHistory\s*\.\s*Update(Range)?\s*\(", RegexOptions.Compiled),
        new(@"Set<\s*JobHistoryEntry\s*>\s*\(\s*\)\s*\.\s*Update", RegexOptions.Compiled),
        new(@"UPDATE\s+job_history\s+SET", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"\bJobHistory\s*\.\s*Attach(Range)?\s*\(", RegexOptions.Compiled),
        new(@"Set<\s*JobHistoryEntry\s*>\s*\(\s*\)\s*\.\s*Attach", RegexOptions.Compiled),

        // Unanchored on receiver name from the start -- see
        // CustomerMutationBoundaryTests'/EstimateMutationBoundaryTests' doc comments for why
        // a `db`/`_db`-anchored version would leave a tracked gap.
        new(@"\.\s*Update(Range)?\s*\(", RegexOptions.Compiled),
        new(@"\.\s*Attach(Range)?\s*\(", RegexOptions.Compiled),
    };

    private static readonly Regex JobsOrSetJobRootPattern =
        new(@"\bJobs\s*\.|\bSet<\s*Job\s*>\s*\(\s*\)", RegexOptions.Compiled);

    private static readonly Regex JobHistoryOrSetEntryRootPattern =
        new(@"\bJobHistory\s*\.|\bSet<\s*JobHistoryEntry\s*>\s*\(\s*\)", RegexOptions.Compiled);

    private static readonly Regex ExecuteUpdateAsyncPattern =
        new(@"\.\s*ExecuteUpdateAsync\s*\(", RegexOptions.Compiled);

    // Security-review finding (P2-WP3 gate): the original scan covered ExecuteUpdateAsync
    // bulk updates but not ExecuteDeleteAsync bulk deletes. Job is meant to be exclusively
    // soft-deleted via TransitionStatusAsync (DeletedAt/DeletedBy, with an audit trail) --
    // never hard-deleted -- so a Jobs/JobHistory-rooted ExecuteDeleteAsync anywhere outside
    // the allow-listed file is exactly as much of a bypass as ExecuteUpdateAsync.
    private static readonly Regex ExecuteDeleteAsyncPattern =
        new(@"\.\s*ExecuteDeleteAsync\s*\(", RegexOptions.Compiled);

    private static readonly Regex JobsAccessPattern =
        new(@"\bJobs\s*\.|\bSet<\s*Job\s*>\s*\(\s*\)", RegexOptions.Compiled);

    private static readonly Regex JobHistoryAccessPattern =
        new(@"\bJobHistory\s*\.|\bSet<\s*JobHistoryEntry\s*>\s*\(\s*\)", RegexOptions.Compiled);

    private static readonly Regex JobsDbSetDeclarationPattern =
        new(@"DbSet<\s*Job\s*>\s*Jobs", RegexOptions.Compiled);

    private static readonly Regex JobHistoryDbSetDeclarationPattern =
        new(@"DbSet<\s*JobHistoryEntry\s*>\s*JobHistory", RegexOptions.Compiled);

    private static readonly Regex AsNoTrackingPattern =
        new(@"\.AsNoTracking(WithIdentityResolution)?\s*\(", RegexOptions.Compiled);

    private static readonly Regex JobsAddCallPattern =
        new(@"\bJobs\s*\.\s*Add(Range)?\s*\(", RegexOptions.Compiled);

    private static readonly Regex JobHistoryAddCallPattern =
        new(@"\bJobHistory\s*\.\s*Add(Range)?\s*\(", RegexOptions.Compiled);

    private const string AllowedRelativePath =
        "GarageOS.Infrastructure/Data/Jobs/JobMutationRepository.cs";

    private static readonly string[] ScannedProjectDirs =
        { "GarageOS.Api", "GarageOS.Application", "GarageOS.Domain", "GarageOS.Infrastructure" };

    [Fact]
    public void OnlyJobMutationRepository_MayMutateJobsOrJobHistoryRows()
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
                var touchesJobs = JobsOrSetJobRootPattern.IsMatch(statement);
                var touchesHistory = JobHistoryOrSetEntryRootPattern.IsMatch(statement);
                if ((touchesJobs || touchesHistory) && ExecuteUpdateAsyncPattern.IsMatch(statement))
                {
                    violations.Add($"{normalized} ({(touchesJobs ? "Jobs" : "JobHistory")}-rooted ExecuteUpdateAsync bulk update)");
                }
                if ((touchesJobs || touchesHistory) && ExecuteDeleteAsyncPattern.IsMatch(statement))
                {
                    violations.Add($"{normalized} ({(touchesJobs ? "Jobs" : "JobHistory")}-rooted ExecuteDeleteAsync bulk delete)");
                }
            }
        }

        Assert.True(violations.Count == 0,
            "Only JobMutationRepository may mutate Jobs/JobHistory rows. Violating file(s): "
            + string.Join(", ", violations));
    }

    [Fact]
    public void OnlyJobMutationRepository_MayHoldATrackedJobsOrJobHistoryReference()
    {
        var backendDir = FindBackendDirectory();
        var violations = new List<string>();

        foreach (var (normalized, text) in ScanFiles(backendDir))
        {
            var masked = SourceScanUtilities.MaskLiteralsAndComments(text);

            CheckRoot(masked, normalized, JobsAccessPattern, JobsDbSetDeclarationPattern, JobsAddCallPattern, violations);
            CheckRoot(masked, normalized, JobHistoryAccessPattern, JobHistoryDbSetDeclarationPattern, JobHistoryAddCallPattern, violations);
        }

        Assert.True(violations.Count == 0,
            "Only JobMutationRepository may hold a tracked (non-AsNoTracking) reference to "
            + "a Jobs/JobHistory query. Violating location(s): " + string.Join(", ", violations));
    }

    private static void CheckRoot(
        string masked, string normalized, Regex accessPattern, Regex dbSetDeclarationPattern,
        Regex addCallPattern, List<string> violations)
    {
        foreach (Match match in accessPattern.Matches(masked))
        {
            var lineStart = masked.LastIndexOf('\n', Math.Max(0, match.Index - 1)) + 1;
            var lineEnd = masked.IndexOf('\n', match.Index);
            if (lineEnd < 0) lineEnd = masked.Length;
            var line = masked[lineStart..lineEnd];
            if (dbSetDeclarationPattern.IsMatch(line)) continue;

            var statement = StatementContaining(masked, match.Index);
            if (addCallPattern.IsMatch(statement)) continue; // Add(...) is not a tracked-read concern
            if (!AsNoTrackingPattern.IsMatch(statement))
            {
                violations.Add($"{normalized}:{CountLines(masked, match.Index)}");
            }
        }
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
