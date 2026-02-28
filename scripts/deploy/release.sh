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

cd "$REPO_ROOT"
dotnet publish Source/NexusForever.WorldServer/NexusForever.WorldServer.csproj -c Release -o "$PUBLISH_DIR"
echo "Published release to: $PUBLISH_DIR"
