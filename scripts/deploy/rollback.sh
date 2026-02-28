#!/usr/bin/env bash
set -euo pipefail

# Usage:
#   ./scripts/deploy/rollback.sh <current_link> <previous_release_path>
#
# Example:
#   ./scripts/deploy/rollback.sh /mnt/cache/nexusforever/current /mnt/cache/nexusforever/releases/2026.02.27-3

CURRENT_LINK="${1:?current symlink required}"
PREVIOUS_RELEASE_PATH="${2:?previous release path required}"

if [[ ! -d "$PREVIOUS_RELEASE_PATH" ]]; then
  echo "Previous release not found: $PREVIOUS_RELEASE_PATH" >&2
  exit 1
fi

ln -sfn "$PREVIOUS_RELEASE_PATH" "$CURRENT_LINK"
echo "Rolled back current release to: $PREVIOUS_RELEASE_PATH"
