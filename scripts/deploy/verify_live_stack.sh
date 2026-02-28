#!/usr/bin/env bash
set -euo pipefail

# Usage:
#   ./scripts/deploy/verify_live_stack.sh <auth_container> <world_container> <auth_port> <world_port> [host] [log_window_seconds]

AUTH_CONTAINER="${1:?auth container required}"
WORLD_CONTAINER="${2:?world container required}"
AUTH_PORT="${3:?auth port required}"
WORLD_PORT="${4:?world port required}"
HOST="${5:-127.0.0.1}"
LOG_WINDOW_SECONDS="${6:-45}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

is_running() {
  local container="$1"
  [[ "$(docker inspect --format '{{.State.Running}}' "$container" 2>/dev/null || echo false)" == "true" ]]
}

if ! is_running "$AUTH_CONTAINER"; then
  echo "Auth container is not running: $AUTH_CONTAINER" >&2
  exit 1
fi

if ! is_running "$WORLD_CONTAINER"; then
  echo "World container is not running: $WORLD_CONTAINER" >&2
  exit 1
fi

"$SCRIPT_DIR/healthcheck.sh" "$HOST" "$AUTH_PORT" 45
"$SCRIPT_DIR/healthcheck.sh" "$HOST" "$WORLD_PORT" 45

ERROR_PATTERNS='FATAL|Unhandled exception|ObjectDisposedException|NullReferenceException|Unable to connect to any of the specified MySQL hosts'

auth_errors="$(docker logs --since "${LOG_WINDOW_SECONDS}s" "$AUTH_CONTAINER" 2>&1 | grep -E "$ERROR_PATTERNS" || true)"
world_errors="$(docker logs --since "${LOG_WINDOW_SECONDS}s" "$WORLD_CONTAINER" 2>&1 | grep -E "$ERROR_PATTERNS" || true)"

if [[ -n "$auth_errors" ]]; then
  echo "Recent auth errors detected:" >&2
  echo "$auth_errors" >&2
  exit 1
fi

if [[ -n "$world_errors" ]]; then
  echo "Recent world errors detected:" >&2
  echo "$world_errors" >&2
  exit 1
fi

echo "Live stack verification passed."
