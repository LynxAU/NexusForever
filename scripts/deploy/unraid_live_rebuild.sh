#!/usr/bin/env bash
set -euo pipefail

# Rebuild and deploy the live emulator directly on Unraid from git source.
#
# Usage:
#   ./scripts/deploy/unraid_live_rebuild.sh <repo_root> [branch] [remote]
#
# Example:
#   ./scripts/deploy/unraid_live_rebuild.sh /mnt/user/nexusforever-live/repo main origin

REPO_ROOT="${1:?repo root required}"
BRANCH="${2:-main}"
REMOTE="${3:-origin}"

AUTH_CONTAINER="${AUTH_CONTAINER:-nf_iso_auth}"
WORLD_CONTAINER="${WORLD_CONTAINER:-nf_iso_world}"
AUTH_DEST="${AUTH_DEST:-/srv/AuthServer}"
WORLD_DEST="${WORLD_DEST:-/srv/WorldServer}"

AUTH_PORT="${AUTH_PORT:-23115}"
WORLD_PORT="${WORLD_PORT:-24000}"
HEALTH_HOST="${HEALTH_HOST:-127.0.0.1}"
HEALTH_TIMEOUT="${HEALTH_TIMEOUT:-45}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

require_cmd() {
  local cmd="$1"
  command -v "$cmd" >/dev/null 2>&1 || {
    echo "Missing required command: $cmd" >&2
    exit 1
  }
}

resolve_mount_source() {
  local container="$1"
  local destination="$2"

  docker inspect "$container" \
    --format "{{range .Mounts}}{{if eq .Destination \"$destination\"}}{{.Source}}{{end}}{{end}}"
}

safe_sync_dir() {
  local source="$1"
  local target="$2"

  if command -v rsync >/dev/null 2>&1; then
    rsync -a --delete "$source"/ "$target"/
    return 0
  fi

  mkdir -p "$target"
  find "$target" -mindepth 1 -maxdepth 1 -exec rm -rf {} +
  cp -a "$source"/. "$target"/
}

require_cmd git
require_cmd dotnet
require_cmd docker

cd "$REPO_ROOT"

echo "[deploy] Fetching $REMOTE/$BRANCH ..."
git fetch "$REMOTE" "$BRANCH"
git checkout "$BRANCH"
git pull --ff-only "$REMOTE" "$BRANCH"

GIT_SHA="$(git rev-parse --short HEAD)"
RELEASE_ID="$(date +%Y.%m.%d-%H%M%S)-${GIT_SHA}"
OUT_ROOT="$REPO_ROOT/.out/live-$RELEASE_ID"
AUTH_OUT="$OUT_ROOT/AuthServer"
WORLD_OUT="$OUT_ROOT/WorldServer"

echo "[deploy] Publishing AuthServer ($RELEASE_ID) ..."
dotnet publish Source/NexusForever.AuthServer/NexusForever.AuthServer.csproj \
  -c Release -r linux-x64 --self-contained true -o "$AUTH_OUT"

echo "[deploy] Publishing WorldServer ($RELEASE_ID) ..."
dotnet publish Source/NexusForever.WorldServer/NexusForever.WorldServer.csproj \
  -c Release -r linux-x64 --self-contained true -o "$WORLD_OUT"

if [[ -d "$REPO_ROOT/assets/tbl" ]]; then
  mkdir -p "$WORLD_OUT/tbl"
  cp -a "$REPO_ROOT/assets/tbl"/. "$WORLD_OUT/tbl/"
fi

if [[ -d "$REPO_ROOT/assets/map" ]]; then
  mkdir -p "$WORLD_OUT/map"
  cp -a "$REPO_ROOT/assets/map"/. "$WORLD_OUT/map/"
fi

AUTH_SRC="$(resolve_mount_source "$AUTH_CONTAINER" "$AUTH_DEST")"
WORLD_SRC="$(resolve_mount_source "$WORLD_CONTAINER" "$WORLD_DEST")"

if [[ -z "$AUTH_SRC" || -z "$WORLD_SRC" ]]; then
  echo "Unable to resolve auth/world bind mounts. Check container names and mounts." >&2
  exit 1
fi

echo "[deploy] Stopping $AUTH_CONTAINER and $WORLD_CONTAINER ..."
docker stop "$AUTH_CONTAINER" "$WORLD_CONTAINER" >/dev/null

echo "[deploy] Syncing AuthServer to $AUTH_SRC ..."
safe_sync_dir "$AUTH_OUT" "$AUTH_SRC"

echo "[deploy] Syncing WorldServer to $WORLD_SRC ..."
safe_sync_dir "$WORLD_OUT" "$WORLD_SRC"

echo "[deploy] Starting $AUTH_CONTAINER and $WORLD_CONTAINER ..."
docker start "$AUTH_CONTAINER" "$WORLD_CONTAINER" >/dev/null

echo "[deploy] Running health checks ..."
"$SCRIPT_DIR/healthcheck.sh" "$HEALTH_HOST" "$AUTH_PORT" "$HEALTH_TIMEOUT"
"$SCRIPT_DIR/healthcheck.sh" "$HEALTH_HOST" "$WORLD_PORT" "$HEALTH_TIMEOUT"

echo "[deploy] Tail logs (last 40 lines each) ..."
docker logs --tail 40 "$AUTH_CONTAINER" || true
docker logs --tail 40 "$WORLD_CONTAINER" || true

echo "[deploy] Completed release $RELEASE_ID"
