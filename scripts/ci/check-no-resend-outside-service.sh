#!/usr/bin/env bash
# WP-6 brief (13_phase1_execution_plan.md WP-6 acceptance criterion) /
# 11_engineering_handoff.md §11A. Resend-SDK/API-isolation regression check.
#
# ResendEmailService.cs is the ONLY class in the codebase permitted to reference the
# Resend API (Decision #8, IEmailService.cs governance comment). This implementation is a
# hand-rolled HttpClient call (no third-party Resend SDK package is referenced anywhere in
# the repo -- see ResendEmailService.cs's own doc comment for why), so the surface that
# must never leak outside ResendEmailService.cs is the literal Resend API host
# ("api.resend.com"): any other backend/frontend file referencing it would mean some other
# class is constructing its own request against Resend directly, bypassing the
# IEmailService abstraction entirely.
#
# This is a deliberate, blocking CI gate (wired into .github/workflows/ci.yml), not a
# one-off manual grep -- same pattern as check-no-legacy-brand.sh (WP-7): unrestricted by
# file extension, excluding only build-artifact/tooling directories.
#
# Known, accepted limitation (not fixed by this or any grep-based check, same tradeoff
# check-no-legacy-brand.sh's own header comment already documents for the Rashid check):
# a literal split across concatenated string fragments (e.g. "api.resend" + ".com") will
# not match. Closing this would require semantic/AST-level source analysis, which no
# check in this codebase performs -- flagged here for visibility, not treated as a
# blocking gap for a text-based regression check.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$REPO_ROOT"

ALLOWED_FILE="backend/GarageOS.Infrastructure/Email/ResendEmailService.cs"

MATCHES="$(grep -rlni "api\.resend\.com" \
  --exclude-dir=node_modules --exclude-dir=bin --exclude-dir=obj --exclude-dir=dist \
  --exclude-dir=.git --exclude-dir=.review-snapshots \
  backend frontend 2>/dev/null | grep -v -F "$ALLOWED_FILE" || true)"

if [ -n "$MATCHES" ]; then
  echo "ERROR: Resend API reference found outside ResendEmailService.cs:" >&2
  echo "$MATCHES" >&2
  echo "" >&2
  echo "ResendEmailService is the ONLY class permitted to reference the Resend API" >&2
  echo "(Decision #8). Route all email sending through IEmailService instead." >&2
  exit 1
fi

echo "OK: no Resend API references found outside ResendEmailService.cs."
exit 0
