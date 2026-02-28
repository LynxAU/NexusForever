#!/usr/bin/env bash
set -euo pipefail

# Usage:
#   ./scripts/deploy/restore_deploy_backup.sh <backup_dir> <auth_dir> <world_dir> [auth_config_file] [world_config_file]

BACKUP_DIR="${1:?backup dir required}"
AUTH_DIR="${2:?auth directory required}"
WORLD_DIR="${3:?world directory required}"
AUTH_CONFIG_FILE="${4:-}"
WORLD_CONFIG_FILE="${5:-}"

safe_sync_dir() {
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

if [[ ! -d "$BACKUP_DIR" ]]; then
  echo "Backup directory not found: $BACKUP_DIR" >&2
  exit 1
fi

if [[ -d "$BACKUP_DIR/auth" ]]; then
  safe_sync_dir "$BACKUP_DIR/auth" "$AUTH_DIR"
fi

if [[ -d "$BACKUP_DIR/world" ]]; then
  safe_sync_dir "$BACKUP_DIR/world" "$WORLD_DIR"
fi

if [[ -n "$AUTH_CONFIG_FILE" && -f "$BACKUP_DIR/config/AuthServer.json" ]]; then
  cp -a "$BACKUP_DIR/config/AuthServer.json" "$AUTH_CONFIG_FILE"
fi

if [[ -n "$WORLD_CONFIG_FILE" && -f "$BACKUP_DIR/config/WorldServer.json" ]]; then
  cp -a "$BACKUP_DIR/config/WorldServer.json" "$WORLD_CONFIG_FILE"
fi

echo "Restored backup from: $BACKUP_DIR"
