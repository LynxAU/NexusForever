#!/usr/bin/env bash
set -euo pipefail

# Usage:
#   ./scripts/deploy/migrate.sh <repo_root> [config_file]
#
# Example:
#   ./scripts/deploy/migrate.sh /mnt/cache/nexusforever/app /mnt/cache/nexusforever/config/AspireMigrations.json

REPO_ROOT="${1:?repo root required}"
CONFIG_FILE="${2:-}"

cd "$REPO_ROOT"

if [[ -n "$CONFIG_FILE" ]]; then
  if [[ ! -f "$CONFIG_FILE" ]]; then
    echo "Config file not found: $CONFIG_FILE" >&2
    exit 1
  fi

  # The migration app loads AspireMigrations.json from its working directory.
  cp "$CONFIG_FILE" "Source/NexusForever.Aspire.Database.Migrations/AspireMigrations.json"
fi

dotnet run --project Source/NexusForever.Aspire.Database.Migrations/NexusForever.Aspire.Database.Migrations.csproj -c Release
echo "Database migrations completed."
