#!/usr/bin/env bash
set -euo pipefail

# Usage:
#   ./scripts/deploy/healthcheck.sh <host> <port> [timeout_seconds]
#
# Example:
#   ./scripts/deploy/healthcheck.sh 127.0.0.1 25000 30

HOST="${1:?host required}"
PORT="${2:?port required}"
TIMEOUT_SECONDS="${3:-30}"

end=$((SECONDS + TIMEOUT_SECONDS))
while (( SECONDS < end )); do
  if bash -c ">/dev/tcp/$HOST/$PORT" >/dev/null 2>&1; then
    echo "Health check passed: $HOST:$PORT reachable."
    exit 0
  fi
  sleep 1
done

echo "Health check failed: $HOST:$PORT not reachable within ${TIMEOUT_SECONDS}s." >&2
exit 1
