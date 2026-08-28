#!/usr/bin/env bash
# WP-7 brief §5 / 13_phase1_execution_plan.md WP-9 acceptance criterion (line ~127).
# Placeholder-brand regression check.
#
# The final customer-facing product/brand name is undecided (DECISIONS.md #6,
# 11_engineering_handoff.md §7A). "Rashid" and "GarageOS" are internal-only codenames:
# "GarageOS" is fine (and expected) inside namespaces, package names, repo/file paths,
# and internal docs -- it must never be hardcoded as the FINAL customer-facing brand in
# UI copy, email templates, or other customer-facing surfaces once WP-7's BrandingOptions
# exists for exactly that purpose. "Rashid" specifically must never appear in
# customer-facing application source at all -- it has no legitimate reason to be there.
#
# This script is a deliberate, blocking CI gate (wired into .github/workflows/ci.yml),
# not a one-off manual grep: it runs on every push/PR and fails the build the moment a
# literal "Rashid" reference reappears anywhere in backend/ or frontend/.
#
# Deliberately UNRESTRICTED by file extension -- matches the Owner-approved acceptance
# criterion in 13_phase1_execution_plan.md verbatim ("grep -rln 'Rashid' backend frontend
# --exclude-dir=node_modules --exclude-dir=bin --exclude-dir=obj"), widened only to add
# --exclude-dir=dist (a standard build-artifact directory in the same family as bin/obj,
# for the future frontend build output), --exclude-dir=.git, and
# --exclude-dir=.review-snapshots (repository/tooling artifacts that did not exist when
# that line was written, never a source of real customer-facing copy). A prior version of this script restricted matching to a fixed list of source
# file extensions (.cs/.ts/.html/.json/etc.); QA Lead's WP-7 gate review (round 1) proved
# by direct execution that this missed real customer-facing file types a future WP-6/WP-8
# could plausibly introduce (.txt email templates, .yaml/.yml, .svg logo assets, .resx
# localization resources) and was an unauthorized narrowing versus the approved plan.
# Fixed by removing the extension allow-list entirely.
#
# Known, accepted limitation (not fixed by this or any grep-based check): a literal split
# across concatenated string fragments (e.g. "Ras" + "hid") will not match. Closing this
# would require semantic/AST-level source analysis, which no check in this codebase
# performs (see SourceScanUtilities.cs's own doc comment making the same tradeoff
# explicit for a different, unrelated check) -- flagged here for visibility, not treated
# as a blocking gap for a text-based regression check.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$REPO_ROOT"

MATCHES="$(grep -rlni "rashid" \
  --exclude-dir=node_modules --exclude-dir=bin --exclude-dir=obj --exclude-dir=dist \
  --exclude-dir=.git --exclude-dir=.review-snapshots \
  backend frontend 2>/dev/null || true)"

if [ -n "$MATCHES" ]; then
  echo "ERROR: placeholder brand name 'Rashid' found in customer-facing application source:" >&2
  echo "$MATCHES" >&2
  echo "" >&2
  echo "Rashid is an internal-only codename and must never appear in backend/ or frontend/ application source." >&2
  echo "Use BrandingOptions (WP-7) for any customer-facing product name instead." >&2
  exit 1
fi

echo "OK: no 'Rashid' references found in backend/ or frontend/ application source."
exit 0
