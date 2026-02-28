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
AUTH_CONFIG_DEST="${AUTH_CONFIG_DEST:-/srv/AuthServer/AuthServer.json}"
WORLD_CONFIG_DEST="${WORLD_CONFIG_DEST:-/srv/WorldServer/WorldServer.json}"

AUTH_PORT="${AUTH_PORT:-23115}"
WORLD_PORT="${WORLD_PORT:-24000}"
HEALTH_HOST="${HEALTH_HOST:-127.0.0.1}"
HEALTH_TIMEOUT="${HEALTH_TIMEOUT:-45}"
MYSQL_HOST="${MYSQL_HOST:-127.0.0.1}"
MYSQL_PORT="${MYSQL_PORT:-33306}"
RABBIT_HOST="${RABBIT_HOST:-127.0.0.1}"
RABBIT_PORT="${RABBIT_PORT:-35672}"
DEPENDENCY_TIMEOUT="${DEPENDENCY_TIMEOUT:-20}"
BACKUP_ROOT="${BACKUP_ROOT:-/mnt/user/nexusforever-live/backups/deploy-pre}"
CREATE_BACKUP="${CREATE_BACKUP:-true}"
AUTO_RESTORE_ON_FAILURE="${AUTO_RESTORE_ON_FAILURE:-true}"
LOG_WINDOW_SECONDS="${LOG_WINDOW_SECONDS:-45}"
MANIFEST_ROOT="${MANIFEST_ROOT:-/mnt/user/nexusforever-live/backups/deploy-manifests}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CONTAINERS_STOPPED="false"
AUTH_SRC=""
WORLD_SRC=""
AUTH_CONFIG_SRC=""
WORLD_CONFIG_SRC=""
LAST_BACKUP_DIR=""

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

wait_for_tcp() {
  local host="$1"
  local port="$2"
  local timeout_seconds="$3"
  local end=$((SECONDS + timeout_seconds))

  while (( SECONDS < end )); do
    if bash -c ">/dev/tcp/$host/$port" >/dev/null 2>&1; then
      return 0
    fi
    sleep 1
  done

  return 1
}

container_exists() {
  local container="$1"
  docker inspect "$container" >/dev/null 2>&1
}

recover_containers_on_error() {
  local rc=$?
  set +e

  if [[ "$AUTO_RESTORE_ON_FAILURE" == "true" && -n "$LAST_BACKUP_DIR" && -d "$LAST_BACKUP_DIR" ]]; then
    echo "[deploy] Error occurred. Restoring from backup: $LAST_BACKUP_DIR"
    "$SCRIPT_DIR/restore_deploy_backup.sh" \
      "$LAST_BACKUP_DIR" \
      "$AUTH_SRC" \
      "$WORLD_SRC" \
      "$AUTH_CONFIG_SRC" \
      "$WORLD_CONFIG_SRC" || true
  fi

  if [[ "$CONTAINERS_STOPPED" == "true" ]]; then
    echo "[deploy] Error occurred. Attempting to bring $AUTH_CONTAINER and $WORLD_CONTAINER back up ..."
    docker start "$AUTH_CONTAINER" "$WORLD_CONTAINER" >/dev/null 2>&1 || true
  fi

  exit "$rc"
}

trap recover_containers_on_error ERR

require_cmd git
require_cmd dotnet
require_cmd docker
require_cmd bash

cd "$REPO_ROOT"

if ! container_exists "$AUTH_CONTAINER" || ! container_exists "$WORLD_CONTAINER"; then
  echo "Missing expected containers: $AUTH_CONTAINER and/or $WORLD_CONTAINER." >&2
  exit 1
fi

echo "[deploy] Preflight dependency checks ..."
if ! wait_for_tcp "$MYSQL_HOST" "$MYSQL_PORT" "$DEPENDENCY_TIMEOUT"; then
  echo "MySQL dependency is not reachable at $MYSQL_HOST:$MYSQL_PORT." >&2
  exit 1
fi
if ! wait_for_tcp "$RABBIT_HOST" "$RABBIT_PORT" "$DEPENDENCY_TIMEOUT"; then
  echo "RabbitMQ dependency is not reachable at $RABBIT_HOST:$RABBIT_PORT." >&2
  exit 1
fi

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
AUTH_CONFIG_SRC="$(resolve_mount_source "$AUTH_CONTAINER" "$AUTH_CONFIG_DEST")"
WORLD_CONFIG_SRC="$(resolve_mount_source "$WORLD_CONTAINER" "$WORLD_CONFIG_DEST")"

if [[ -z "$AUTH_SRC" || -z "$WORLD_SRC" ]]; then
  echo "Unable to resolve auth/world bind mounts. Check container names and mounts." >&2
  exit 1
fi

if [[ "$CREATE_BACKUP" == "true" ]]; then
  echo "[deploy] Creating pre-deploy backup ..."
  LAST_BACKUP_DIR="$("$SCRIPT_DIR/create_deploy_backup.sh" \
    "$BACKUP_ROOT" \
    "$AUTH_SRC" \
    "$WORLD_SRC" \
    "$AUTH_CONFIG_SRC" \
    "$WORLD_CONFIG_SRC")"
  echo "[deploy] Backup created: $LAST_BACKUP_DIR"
fi

echo "[deploy] Stopping $AUTH_CONTAINER and $WORLD_CONTAINER ..."
docker stop "$AUTH_CONTAINER" "$WORLD_CONTAINER" >/dev/null
CONTAINERS_STOPPED="true"

echo "[deploy] Syncing AuthServer to $AUTH_SRC ..."
safe_sync_dir "$AUTH_OUT" "$AUTH_SRC"

echo "[deploy] Syncing WorldServer to $WORLD_SRC ..."
safe_sync_dir "$WORLD_OUT" "$WORLD_SRC"

echo "[deploy] Starting $AUTH_CONTAINER and $WORLD_CONTAINER ..."
docker start "$AUTH_CONTAINER" "$WORLD_CONTAINER" >/dev/null
CONTAINERS_STOPPED="false"

echo "[deploy] Running health checks ..."
"$SCRIPT_DIR/healthcheck.sh" "$HEALTH_HOST" "$AUTH_PORT" "$HEALTH_TIMEOUT"
"$SCRIPT_DIR/healthcheck.sh" "$HEALTH_HOST" "$WORLD_PORT" "$HEALTH_TIMEOUT"
"$SCRIPT_DIR/verify_live_stack.sh" \
  "$AUTH_CONTAINER" \
  "$WORLD_CONTAINER" \
  "$AUTH_PORT" \
  "$WORLD_PORT" \
  "$HEALTH_HOST" \
  "$LOG_WINDOW_SECONDS"

echo "[deploy] Tail logs (last 40 lines each) ..."
docker logs --tail 40 "$AUTH_CONTAINER" || true
docker logs --tail 40 "$WORLD_CONTAINER" || true

mkdir -p "$MANIFEST_ROOT"
MANIFEST_FILE="$MANIFEST_ROOT/$RELEASE_ID.txt"
cat > "$MANIFEST_FILE" <<EOF
release_id=$RELEASE_ID
deployed_utc=$(date -u +%Y-%m-%dT%H:%M:%SZ)
repo_root=$REPO_ROOT
branch=$BRANCH
remote=$REMOTE
git_sha=$GIT_SHA
auth_container=$AUTH_CONTAINER
world_container=$WORLD_CONTAINER
auth_mount_source=$AUTH_SRC
world_mount_source=$WORLD_SRC
auth_config_source=${AUTH_CONFIG_SRC:-none}
world_config_source=${WORLD_CONFIG_SRC:-none}
backup_dir=${LAST_BACKUP_DIR:-none}
EOF

echo "[deploy] Wrote release manifest: $MANIFEST_FILE"
echo "[deploy] Completed release $RELEASE_ID"
