#!/usr/bin/env bash
set -euo pipefail

# Usage:
#   ./scripts/deploy/release.sh <repo_root> <release_id>
#
# Example:
#   ./scripts/deploy/release.sh /mnt/cache/nexusforever/app 2026.02.28-1

REPO_ROOT="${1:?repo root required}"
RELEASE_ID="${2:?release id required}"
PUBLISH_DIR="$REPO_ROOT/.out/$RELEASE_ID"
WORLD_PUBLISH_DIR="$PUBLISH_DIR/WorldServer"
ASSET_TBL_DIR="$REPO_ROOT/assets/tbl"
ASSET_MAP_DIR="$REPO_ROOT/assets/map"

cd "$REPO_ROOT"
dotnet publish Source/NexusForever.WorldServer/NexusForever.WorldServer.csproj -c Release -o "$WORLD_PUBLISH_DIR"

# Include deploy-critical game data from the repo so Unraid releases are self-contained.
mkdir -p "$WORLD_PUBLISH_DIR/tbl" "$WORLD_PUBLISH_DIR/map"
cp -a "$ASSET_TBL_DIR"/. "$WORLD_PUBLISH_DIR/tbl/"
cp -a "$ASSET_MAP_DIR"/. "$WORLD_PUBLISH_DIR/map/"

echo "Published release to: $WORLD_PUBLISH_DIR"
