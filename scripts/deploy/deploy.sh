#!/usr/bin/env bash
set -euo pipefail

# Usage:
#   ./scripts/deploy/deploy.sh <releases_dir> <current_link> <release_path>
#
# Example:
#   ./scripts/deploy/deploy.sh /mnt/cache/nexusforever/releases /mnt/cache/nexusforever/current /mnt/cache/nexusforever/app/.out/2026.02.28-1

RELEASES_DIR="${1:?releases dir required}"
CURRENT_LINK="${2:?current symlink required}"
RELEASE_PATH="${3:?release path required}"

mkdir -p "$RELEASES_DIR"
TARGET_DIR="$RELEASES_DIR/$(basename "$RELEASE_PATH")"

if [[ ! -d "$TARGET_DIR" ]]; then
  cp -a "$RELEASE_PATH" "$TARGET_DIR"
fi

ln -sfn "$TARGET_DIR" "$CURRENT_LINK"
echo "Switched current release to: $TARGET_DIR"
