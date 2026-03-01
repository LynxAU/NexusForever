#!/usr/bin/env bash
set -euo pipefail

# Bootstrap or repair the nf_iso stack on Unraid from the canonical compose file.
#
# Usage:
#   ./scripts/deploy/unraid/bootstrap_nf_iso_stack.sh [env_file]
#
# Example:
#   ./scripts/deploy/unraid/bootstrap_nf_iso_stack.sh .env

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
COMPOSE_FILE="$SCRIPT_DIR/docker-compose.nf_iso.yml"
ENV_FILE="${1:-$SCRIPT_DIR/.env}"
NETWORK_NAME="${NETWORK_NAME:-nf_iso_net}"

require_cmd() {
  local cmd="$1"
  command -v "$cmd" >/dev/null 2>&1 || {
    echo "Missing required command: $cmd" >&2
    exit 1
  }
}

require_cmd docker
require_cmd bash

if [[ ! -f "$COMPOSE_FILE" ]]; then
  echo "Compose file not found: $COMPOSE_FILE" >&2
  exit 1
fi

if [[ ! -f "$ENV_FILE" ]]; then
  echo "Environment file not found: $ENV_FILE" >&2
  echo "Copy .env.example first and fill values." >&2
  exit 1
fi

if ! docker network inspect "$NETWORK_NAME" >/dev/null 2>&1; then
  echo "[bootstrap] Creating missing docker network: $NETWORK_NAME"
  docker network create "$NETWORK_NAME" >/dev/null
fi

echo "[bootstrap] Applying compose stack..."
if docker compose version >/dev/null 2>&1; then
  docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" up -d
else
  if ! command -v docker-compose >/dev/null 2>&1; then
    echo "Neither 'docker compose' nor 'docker-compose' is available." >&2
    exit 1
  fi
  docker-compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" up -d
fi

echo "[bootstrap] Waiting for dependency ports..."
for dep in 33306 35672; do
  end=$((SECONDS + 30))
  until bash -c ">/dev/tcp/127.0.0.1/$dep" >/dev/null 2>&1; do
    if (( SECONDS >= end )); then
      echo "Dependency port failed readiness check: $dep" >&2
      exit 1
    fi
    sleep 1
  done
done

echo "[bootstrap] Restarting auth/world to bind to healthy dependencies..."
docker restart nf_iso_auth nf_iso_world >/dev/null

echo "[bootstrap] Stack status:"
docker ps --format '{{.Names}}\t{{.Status}}' | grep -E '^nf_iso_' || true
