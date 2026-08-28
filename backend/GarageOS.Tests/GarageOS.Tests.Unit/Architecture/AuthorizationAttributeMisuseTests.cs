namespace GarageOS.Tests.Unit.Architecture;

using System.Text.RegularExpressions;

/// <summary>
/// WP-5 brief §3/§9 test 22 (Technical Architect required change). "DiscountLimit" and
/// "EstimateApprovalThreshold" are resource-based policies that must only ever be invoked
/// explicitly via IBusinessRuleAuthorizer -- see DiscountLimitRequirement.cs's and
/// EstimateApprovalThresholdRequirement.cs's doc comments for why a bare
/// [Authorize(Policy = "...")] attribute would silently break each policy in a different,
/// dangerous way (fail-closed for DiscountLimit; a misleading 403 instead of a reroute for
/// EstimateApprovalThreshold). No allow-listed exception -- there is no file where
/// attaching either policy this way is ever correct.
///
/// QA Automation review (post-implementation) found that an earlier version of this test
/// -- a block-list matching the two literal policy-name strings -- is bypassed by a level
/// of indirection (e.g. a named constant: `[Authorize(Policy = SomePolicyNames.DiscountLimit)]`),
/// since the literal text "DiscountLimit" never appears inside the attribute itself. Fixed
/// by inverting the design to an ALLOW-list: every [Authorize(Policy = ...)] attribute in
/// GarageOS.Api must reference one of the two known-safe, already-reviewed WP-4 literal
/// policy names ("GarageTenant", "PlatformAdminOnly"); anything else -- a different
/// literal, a non-literal expression, a constant reference -- is flagged for manual
/// review. This closes the constant-indirection gap without needing semantic/Roslyn
/// analysis, because it no longer matters what a non-literal expression's value resolves
/// to -- only whether the attribute uses one of the two pre-approved literals at all.
///
/// QA Automation round-2 review then found that the real-attribute-usage guard used at
/// that point -- "the match's own line, trimmed, must start with [Authorize" -- is
/// bypassed by C#'s legal attribute-stacking syntax, e.g.
/// `[HttpPost("approve"), Authorize(Policy = "EstimateApprovalThreshold")]`, where
/// Authorize is a real, live attribute application but is not the first attribute on its
/// line/bracket -- confirmed by execution to produce zero violations. Fixed by dropping
/// the line-start heuristic entirely and instead searching for the pattern in the output
/// of SourceScanUtilities.MaskLiteralsAndComments rather than the raw file text: that
/// helper blanks the CONTENT of every string literal and comment (preserving length/line
/// breaks), so an "Authorize(Policy = ...)" that exists only inside a `//` comment or a
/// string literal (e.g. GlobalExceptionHandler.cs's log message spelling out the expected
/// attribute for a human reader) simply is not present in the masked text at all and can
/// no longer produce a match -- while a real attribute application, wherever it sits
/// inside its `[...]` bracket, is untouched by masking and still matches. This is a more
/// accurate (lexical-position-based) real-usage test than the old line-start heuristic,
/// not merely a patch for the one reported stacking shape.
/// </summary>
public class AuthorizationAttributeMisuseTests
{
    // Deliberately NOT anchored on a literal '[' immediately before "Authorize" or a
    // literal ']' immediately after its closing paren (round-2 Finding 2: that adjacency
    // requirement is exactly what a stacked attribute list --
    // [HttpPost(...), Authorize(Policy = "...")] -- fails to satisfy, since another
    // attribute's text sits between the opening '[' and "Authorize"). Matching is instead
    // scoped by running this pattern against the MASKED text (see class doc comment),
    // which is what actually distinguishes a real attribute application from the same
    // text appearing in a comment or string literal.
    private static readonly Regex AuthorizeWithPolicyPattern =
        new(@"\bAuthorize\s*\(\s*Policy\s*=\s*(?<arg>[^,\)\]]+?)\s*\)", RegexOptions.Compiled);

    private static readonly HashSet<string> AllowedLiteralPolicyNames =
        new(StringComparer.Ordinal) { "\"GarageTenant\"", "\"PlatformAdminOnly\"" };

    [Fact]
    public void EveryAuthorizePolicyAttributeUsesOnlyAnAlreadyApprovedLiteralPolicyName()
    {
        var backendDir = FindBackendDirectory();
        var violations = new List<string>();

        var dir = Path.Combine(backendDir, "GarageOS.Api");
        if (Directory.Exists(dir))
        {
            foreach (var file in Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories))
            {
                var normalized = file.Replace('\\', '/');
                if (normalized.Contains("/obj/") || normalized.Contains("/bin/"))
                    continue;

                var text = File.ReadAllText(file);
                var masked = SourceScanUtilities.MaskLiteralsAndComments(text);

                foreach (Match match in AuthorizeWithPolicyPattern.Matches(masked))
                {
                    // The masked text tells us WHETHER this is a real (non-comment,
                    // non-string-literal) attribute application and WHERE it sits, but its
                    // "arg" capture is blanked-out if the argument itself is a string
                    // literal -- so the actual policy-name text is read back from the
                    // ORIGINAL text at the same indices (masking never changes length or
                    // shifts positions).
                    var argGroup = match.Groups["arg"];
                    var argOriginal = text.Substring(argGroup.Index, argGroup.Length);
                    var arg = Regex.Replace(argOriginal, @"\s+", " ").Trim();

                    if (!AllowedLiteralPolicyNames.Contains(arg))
                    {
                        var lineNum = CountLines(text, match.Index);
                        violations.Add($"{normalized}:{lineNum}: [Authorize(Policy = {arg})]");
                    }
                }
            }
        }

        Assert.True(violations.Count == 0,
            "Every [Authorize(Policy = ...)] attribute in GarageOS.Api must reference one "
            + "of the two already-approved WP-4 literal policy names (\"GarageTenant\", "
            + "\"PlatformAdminOnly\") directly -- \"DiscountLimit\"/\"EstimateApprovalThreshold\" "
            + "must only be invoked via IBusinessRuleAuthorizer, never attached to a "
            + "controller action, however indirectly referenced, however it is stacked "
            + "alongside other attributes. Violating usage(s): "
            + string.Join(", ", violations));
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
