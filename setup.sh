#!/usr/bin/env bash
# setup.sh — Instantiate the dotnet-clean-arch-template by replacing placeholder tokens.
#
# Usage:
#   ./setup.sh <ProjectName> [Description]
#   ./setup.sh MyApp
#   ./setup.sh MyApp "My project description."
#
# Operates only on git-tracked files (via `git ls-files`), so bin/obj/binaries are never touched.
# Uses `perl -pi -e` for portable in-place replacement (works identically on macOS and Linux).
# Safe to re-run — warns instead of crashing if tokens are already replaced.

set -euo pipefail

# ── Arguments ─────────────────────────────────────────────────────────────────
if [[ $# -lt 1 ]]; then
  echo "Usage: $0 <ProjectName> [\"Project description.\"]" >&2
  echo "Example: $0 MyApp \"My project description.\"" >&2
  exit 1
fi

PROJECT_NAME="$1"
DESCRIPTION="${2:-A clean-architecture .NET 10 application.}"

# ── Sanity check: is the name reasonable? ─────────────────────────────────────
if [[ ! "$PROJECT_NAME" =~ ^[A-Za-z][A-Za-z0-9._-]*$ ]]; then
  echo "Error: ProjectName must start with a letter and contain only letters, digits, '.', '_', or '-'." >&2
  exit 1
fi

# ── Idempotency check ─────────────────────────────────────────────────────────
NAME_TOKEN_FOUND=$(git ls-files -z | xargs -0 grep -rl '{{ProjectName}}' 2>/dev/null | wc -l | tr -d ' ')
DESC_TOKEN_FOUND=$(git ls-files -z | xargs -0 grep -rl '{{ProjectDescription}}' 2>/dev/null | wc -l | tr -d ' ')

if [[ "$NAME_TOKEN_FOUND" -eq 0 && "$DESC_TOKEN_FOUND" -eq 0 ]]; then
  echo "Warning: no {{ProjectName}} or {{ProjectDescription}} tokens found in git-tracked files."
  echo "The template may have already been instantiated. Skipping replacement."
  exit 0
fi

echo "Instantiating template..."
echo "  ProjectName  : $PROJECT_NAME"
echo "  Description  : $DESCRIPTION"
echo ""

# ── Replace tokens in file contents ───────────────────────────────────────────
# Use perl -pi -e for portable in-place substitution (BSD sed and GNU sed differ on -i syntax)
git ls-files -z | xargs -0 perl -pi -e \
  "s/\Q{{ProjectName}}\E/${PROJECT_NAME}/g; s/\Q{{ProjectDescription}}\E/${DESCRIPTION}/g"

echo "  [1/2] Token replacement complete in file contents."

# ── Rename the solution file ───────────────────────────────────────────────────
SLNX_OLD="{{ProjectName}}.slnx"
SLNX_NEW="${PROJECT_NAME}.slnx"

if [[ -f "$SLNX_OLD" ]]; then
  mv "$SLNX_OLD" "$SLNX_NEW"
  echo "  [2/2] Renamed: $SLNX_OLD → $SLNX_NEW"
elif [[ -f "$SLNX_NEW" ]]; then
  echo "  [2/2] Solution file already named $SLNX_NEW — skipping rename."
else
  echo "  [2/2] Warning: could not find $SLNX_OLD or $SLNX_NEW — skipping rename."
fi

# ── Done ───────────────────────────────────────────────────────────────────────
echo ""
echo "Done! Your project is ready as '$PROJECT_NAME'."
echo ""
echo "Next steps:"
echo "  1. Start the dev database:"
echo "       docker compose -f docker-compose.devdb.yml up -d"
echo "  2. Run the API:"
echo "       dotnet run --project Applications/Api"
echo "  3. Run the frontend:"
echo "       dotnet run --project Applications/Web"
echo "  4. Run the tests:"
echo "       dotnet test"
