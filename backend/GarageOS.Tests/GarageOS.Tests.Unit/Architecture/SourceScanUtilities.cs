namespace GarageOS.Tests.Unit.Architecture;

/// <summary>
/// Shared source-text-scanning helper for the WP-5 regex-based architecture tests
/// (EstimateMutationBoundaryTests, AuthorizationAttributeMisuseTests).
///
/// QA Automation round-2 review found that both tests' statement/line-based text
/// splitting was naive about C# string/char literal and comment CONTENT: a ';' embedded
/// inside a string literal value (round-2 Finding 1) fooled
/// EstimateMutationBoundaryTests' statement-scoping into cutting a real violating
/// statement short, and AuthorizationAttributeMisuseTests' "line must start with
/// [Authorize" real-usage guard (round-2 Finding 2) never accounted for C#'s legal
/// attribute-stacking syntax ([HttpPost(...), Authorize(Policy = "...")]), where
/// Authorize is not the first attribute in its bracket/on its line.
///
/// MaskLiteralsAndComments fixes both at the root: it walks the raw source text and
/// replaces the CONTENT of every string literal (regular "...", verbatim @"...") and char
/// literal ('...'), plus every // line comment and /* */ block comment, with blank space
/// -- character-for-character, so the masked text is EXACTLY the same length as the
/// original and every index into one is a valid index into the other. Only the delimiters
/// themselves (opening/closing quotes, comment markers) survive unmasked, so code
/// structure (where statements/attributes/brackets actually start and end) is preserved
/// while any ';', '[', ']', or keyword TEXT that only exists inside a literal or comment
/// can no longer be mistaken for real code by a downstream regex or Split(';').
///
/// QA LEAD review (WP-5 independent QA gate) then found and this file now closes a third,
/// more serious bypass than either round-2 finding: an INTERPOLATED string ($"...",
/// $@"...") is not just a literal -- the content inside each `{ }` interpolation hole is
/// live, executing C# code (e.g. `$"{db.Estimates.Update(e)}"` really does call
/// `.Update(` when that line runs), not string data. The first version of this masking
/// fix blanked interpolated strings exactly like plain string literals -- content and all
/// -- which hid real, compiling, executing mutation/attribute code from every downstream
/// check as a side effect, a strictly worse outcome than either round-2 finding (it
/// defeated the UNQUALIFIED "Estimates.Update(" pattern itself, not just a narrower
/// variable-naming-convention gap). Confirmed by QA Lead via an executed PoC
/// (`$"{db.Estimates.Update(e)}"` outside the allow-listed file; both EstimateMutationBoundaryTests
/// passed when they should have failed) before this fix.
///
/// Fixed by giving interpolated strings hole-aware handling: text OUTSIDE `{ }` holes
/// (the literal template segments) is masked exactly like a plain string's content always
/// was; text INSIDE a hole is left completely unmasked -- it is real code and must stay
/// visible to every downstream regex/statement-scan, which is exactly what closes the
/// bypass. A nested string or char literal declared INSIDE a hole (e.g.
/// `$"{(x == "y" ? 1 : 2)}"`) still has its own content masked by recursing into the same
/// literal-masking logic, so a ';' or "Estimates." hidden inside THAT nested literal is
/// still safely neutralized -- only genuine code stays visible, never re-opening either
/// round-2 finding. Doubled braces ("{{"/"}}", C#'s escape for a literal brace character
/// in an interpolated string's template text) are recognized and masked as literal text,
/// not mistaken for hole delimiters.
///
/// Security Reviewer (WP-5 security gate) found and this now closes a fourth, more
/// serious bypass than any round-2 finding: the nested-string branch inside a hole used
/// to assume every nested literal was a PLAIN string, ignoring any '@'/'$' prefix the
/// nested literal actually had. A nested interpolated string's own hole was wrongly
/// blanked as literal content (hiding a real call inside it), and a nested verbatim
/// string's backslash was wrongly treated as a regular-string escape, running the mask
/// past the literal's real terminating quote and corrupting everything scanned after it.
/// Fixed by detecting the same four prefix shapes inside a hole that the top-level
/// dispatcher already detects, and propagating the correct verbatim/interpolated flags
/// into the recursive call. This recursion is NOT depth-limited -- Security Reviewer
/// independently traced and PoC-verified arbitrary nesting depth (a hole containing a
/// nested interpolated string containing ITS OWN hole with the real call, three levels
/// deep) and mixed prefix ordering (@$"..." vs $@"...) both close correctly; a nested
/// interpolation hole containing another interpolated string with its own holes is
/// therefore already handled, not an open gap.
///
/// Deliberately still a text-based heuristic, not a real C# lexer/parser -- it does not
/// handle every corner of the C# grammar (e.g. raw string literals \"\"\"...\"\"\", which
/// this codebase does not use anywhere today, confirmed by grep). If a future WP
/// introduces raw string literals near estimate-mutation or authorization-attribute code,
/// this masker will need a corresponding case added -- the same kind of visible,
/// intentional speed bump as EstimateMutationBoundaryTests' existing Update()/Attach()
/// allow-list note.
/// </summary>
internal static class SourceScanUtilities
{
    public static string MaskLiteralsAndComments(string text)
    {
        var sb = new System.Text.StringBuilder(text.Length);
        var i = 0;
        var n = text.Length;

        while (i < n)
        {
            var c = text[i];

            // // line comment
            if (c == '/' && i + 1 < n && text[i + 1] == '/')
            {
                while (i < n && text[i] != '\n')
                {
                    sb.Append(' ');
                    i++;
                }
                continue;
            }

            // /* block comment */
            if (c == '/' && i + 1 < n && text[i + 1] == '*')
            {
                sb.Append(' ').Append(' ');
                i += 2;
                while (i < n && !(text[i] == '*' && i + 1 < n && text[i + 1] == '/'))
                {
                    sb.Append(text[i] == '\n' ? '\n' : ' ');
                    i++;
                }
                if (i < n)
                {
                    sb.Append(' ').Append(' ');
                    i += 2;
                }
                continue;
            }

            // Verbatim string, plain or interpolated: @"...", $@"...", @$"..."
            var isVerbatim = c == '@' && i + 1 < n && text[i + 1] == '"';
            var isInterpVerbatimA = c == '$' && i + 1 < n && text[i + 1] == '@' && i + 2 < n && text[i + 2] == '"';
            var isInterpVerbatimB = c == '@' && i + 1 < n && text[i + 1] == '$' && i + 2 < n && text[i + 2] == '"';
            if (isVerbatim || isInterpVerbatimA || isInterpVerbatimB)
            {
                var interpolated = isInterpVerbatimA || isInterpVerbatimB;
                var quoteIndex = isVerbatim ? i + 1 : i + 2;
                for (var k = i; k <= quoteIndex; k++) sb.Append(text[k]);
                i = quoteIndex + 1;
                i = ConsumeStringBody(text, sb, i, n, verbatim: true, interpolated: interpolated);
                continue;
            }

            // Regular or interpolated non-verbatim string: "...", $"..."
            var isInterp = c == '$' && i + 1 < n && text[i + 1] == '"';
            if (c == '"' || isInterp)
            {
                var quoteIndex = isInterp ? i + 1 : i;
                for (var k = i; k <= quoteIndex; k++) sb.Append(text[k]);
                i = quoteIndex + 1;
                i = ConsumeStringBody(text, sb, i, n, verbatim: false, interpolated: isInterp);
                continue;
            }

            // Char literal: '...'
            if (c == '\'')
            {
                sb.Append('\'');
                i++;
                i = ConsumeCharBody(text, sb, i, n);
                continue;
            }

            sb.Append(c);
            i++;
        }

        return sb.ToString();
    }

    // Consumes a string body starting just after the opening quote, appending masked
    // (space, for literal template text) or unmasked (for live code inside interpolation
    // holes) characters to sb, up to and including the closing quote. Returns the index
    // just past the closing quote (or end-of-text/end-of-line for an unterminated
    // literal). depth 0 == literal text (mask); depth > 0 == inside a `{ }` interpolation
    // hole (real code -- leave unmasked, but still recurse into any nested string/char
    // literal so ITS content is masked).
    private static int ConsumeStringBody(string text, System.Text.StringBuilder sb, int i, int n, bool verbatim, bool interpolated)
    {
        var depth = 0;

        while (i < n)
        {
            var ch = text[i];

            if (depth == 0)
            {
                if (interpolated && ch == '{' && i + 1 < n && text[i + 1] == '{')
                {
                    // Literal "{{" -- an escaped brace in the template text, not a hole.
                    sb.Append(' ').Append(' ');
                    i += 2;
                    continue;
                }
                if (interpolated && ch == '}' && i + 1 < n && text[i + 1] == '}')
                {
                    // Literal "}}" -- an escaped brace in the template text.
                    sb.Append(' ').Append(' ');
                    i += 2;
                    continue;
                }
                if (interpolated && ch == '{')
                {
                    // Start of a real interpolation hole -- its content is live code and
                    // must NOT be masked.
                    sb.Append('{');
                    depth = 1;
                    i++;
                    continue;
                }
                if (!verbatim && ch == '\\' && i + 1 < n)
                {
                    sb.Append(' ').Append(' ');
                    i += 2;
                    continue;
                }
                if (ch == '"')
                {
                    if (verbatim && i + 1 < n && text[i + 1] == '"')
                    {
                        sb.Append(' ').Append(' ');
                        i += 2;
                        continue;
                    }
                    sb.Append('"');
                    return i + 1;
                }
                if (!verbatim && ch == '\n')
                {
                    // Unterminated on this line (shouldn't happen in valid C#) -- stop
                    // masking rather than eat the rest of the file.
                    sb.Append('\n');
                    return i + 1;
                }
                sb.Append(ch == '\n' ? '\n' : ' ');
                i++;
                continue;
            }

            // depth > 0: inside a live interpolation hole. Real code stays unmasked, but
            // a nested string/char literal's CONTENT still gets masked (recursing keeps
            // closing the same class of bypass this whole helper exists to close, even
            // when it's nested inside a hole).
            //
            // Security Reviewer (WP-5 security gate) found and this now closes a fourth
            // real, execution-confirmed bypass: the nested-string branch below used to
            // unconditionally recurse with verbatim:false, interpolated:false, regardless
            // of whether the nested literal actually had an '@'/'$' prefix. Two confirmed
            // consequences: (1) a nested INTERPOLATED string's own hole (e.g.
            // `$"{Foo($"{db.Estimates.Update(e)}")}"`) was wrongly treated as plain
            // literal content and blanked whole -- hiding a real, live nested call; (2) a
            // nested VERBATIM string (e.g. `$"{Foo(@"\")}"`) had its backslash wrongly
            // treated as a regular-string escape character, consuming one extra character
            // and running the mask past the literal's real terminating quote, corrupting
            // everything scanned after it. Fixed by detecting the same four prefix shapes
            // (@"..., $"..., $@"..., @$"...) here that the top-level dispatch already
            // detects, and propagating the correct verbatim/interpolated flags into the
            // recursive call instead of assuming plain-string semantics.
            if (ch == '{')
            {
                depth++;
                sb.Append('{');
                i++;
                continue;
            }
            if (ch == '}')
            {
                depth--;
                sb.Append('}');
                i++;
                continue;
            }
            {
                var nestedVerbatim = ch == '@' && i + 1 < n && text[i + 1] == '"';
                var nestedInterpVerbatimA = ch == '$' && i + 1 < n && text[i + 1] == '@' && i + 2 < n && text[i + 2] == '"';
                var nestedInterpVerbatimB = ch == '@' && i + 1 < n && text[i + 1] == '$' && i + 2 < n && text[i + 2] == '"';
                if (nestedVerbatim || nestedInterpVerbatimA || nestedInterpVerbatimB)
                {
                    var nestedInterpolated = nestedInterpVerbatimA || nestedInterpVerbatimB;
                    var quoteIndex = nestedVerbatim ? i + 1 : i + 2;
                    for (var k = i; k <= quoteIndex; k++) sb.Append(text[k]);
                    i = quoteIndex + 1;
                    i = ConsumeStringBody(text, sb, i, n, verbatim: true, interpolated: nestedInterpolated);
                    continue;
                }
                var nestedInterp = ch == '$' && i + 1 < n && text[i + 1] == '"';
                if (nestedInterp)
                {
                    sb.Append(text[i]).Append(text[i + 1]);
                    i += 2;
                    i = ConsumeStringBody(text, sb, i, n, verbatim: false, interpolated: true);
                    continue;
                }
            }
            if (ch == '"')
            {
                sb.Append('"');
                i++;
                i = ConsumeStringBody(text, sb, i, n, verbatim: false, interpolated: false);
                continue;
            }
            if (ch == '\'')
            {
                sb.Append('\'');
                i++;
                i = ConsumeCharBody(text, sb, i, n);
                continue;
            }
            // Real, live code character inside the hole -- keep it exactly as written.
            sb.Append(ch);
            i++;
        }

        return i;
    }

    private static int ConsumeCharBody(string text, System.Text.StringBuilder sb, int i, int n)
    {
        while (i < n)
        {
            if (text[i] == '\\' && i + 1 < n)
            {
                sb.Append(' ').Append(' ');
                i += 2;
                continue;
            }
            if (text[i] == '\'')
            {
                sb.Append('\'');
                return i + 1;
            }
            if (text[i] == '\n')
            {
                sb.Append('\n');
                return i + 1;
            }
            sb.Append(' ');
            i++;
        }
        return i;
    }
}
