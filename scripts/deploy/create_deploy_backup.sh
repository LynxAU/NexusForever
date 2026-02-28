#!/usr/bin/env bash
set -euo pipefail

# Usage:
#   ./scripts/deploy/create_deploy_backup.sh <backup_root> <auth_dir> <world_dir> [auth_config_file] [world_config_file]

BACKUP_ROOT="${1:?backup root required}"
AUTH_DIR="${2:?auth directory required}"
WORLD_DIR="${3:?world directory required}"
AUTH_CONFIG_FILE="${4:-}"
WORLD_CONFIG_FILE="${5:-}"
RETENTION_COUNT="${RETENTION_COUNT:-8}"

TIMESTAMP="$(date +%Y%m%d-%H%M%S)"
BACKUP_DIR="$BACKUP_ROOT/$TIMESTAMP"

safe_copy_dir() {
  local source="$1"
  local target="$2"

  mkdir -p "$target"
  if command -v rsync >/dev/null 2>&1; then
    rsync -a --delete "$source"/ "$target"/
  else
    find "$target" -mindepth 1 -maxdepth 1 -exec rm -rf {} +
    cp -a "$source"/. "$target"/
  fi
}

mkdir -p "$BACKUP_DIR"

if [[ -d "$AUTH_DIR" ]]; then
  safe_copy_dir "$AUTH_DIR" "$BACKUP_DIR/auth"
fi

if [[ -d "$WORLD_DIR" ]]; then
  safe_copy_dir "$WORLD_DIR" "$BACKUP_DIR/world"
fi

mkdir -p "$BACKUP_DIR/config"
if [[ -n "$AUTH_CONFIG_FILE" && -f "$AUTH_CONFIG_FILE" ]]; then
  cp -a "$AUTH_CONFIG_FILE" "$BACKUP_DIR/config/AuthServer.json"
fi

if [[ -n "$WORLD_CONFIG_FILE" && -f "$WORLD_CONFIG_FILE" ]]; then
  cp -a "$WORLD_CONFIG_FILE" "$BACKUP_DIR/config/WorldServer.json"
fi

cat > "$BACKUP_DIR/metadata.txt" <<EOF
created_utc=$(date -u +%Y-%m-%dT%H:%M:%SZ)
auth_dir=$AUTH_DIR
world_dir=$WORLD_DIR
auth_config=${AUTH_CONFIG_FILE:-none}
world_config=${WORLD_CONFIG_FILE:-none}
EOF

# Prune older backups.
if [[ -d "$BACKUP_ROOT" ]]; then
  mapfile -t backup_dirs < <(find "$BACKUP_ROOT" -mindepth 1 -maxdepth 1 -type d | sort)
  if (( ${#backup_dirs[@]} > RETENTION_COUNT )); then
    remove_count=$(( ${#backup_dirs[@]} - RETENTION_COUNT ))
    for ((i=0; i<remove_count; i++)); do
      rm -rf "${backup_dirs[$i]}"
    done
  fi
fi

echo "$BACKUP_DIR"
