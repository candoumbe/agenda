#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

cd "$REPO_ROOT"

echo "[post-start] Verifying Docker daemon availability"
MAX_DOCKER_WAIT_SECONDS=60
SLEEP_BETWEEN_DOCKER_CHECKS=2
DOCKER_DEADLINE=$(( $(date +%s) + MAX_DOCKER_WAIT_SECONDS ))

while ! docker info >/dev/null 2>&1; do
  if [ "$(date +%s)" -ge "$DOCKER_DEADLINE" ]; then
    echo "[post-start] Docker daemon is not ready after ${MAX_DOCKER_WAIT_SECONDS}s; giving up"
    exit 1
  fi
  echo "[post-start] Docker daemon not ready yet; waiting ${SLEEP_BETWEEN_DOCKER_CHECKS}s..."
  sleep "$SLEEP_BETWEEN_DOCKER_CHECKS"
done
echo "[post-start] Docker daemon is ready"

echo "[post-start] Restoring local dotnet tools"
./build.sh --target restore

if ! command -v npm >/dev/null 2>&1; then
  echo "[post-start] npm not found in PATH; skipping frontend restore"
elif [ ! -d "src/Agenda.Frontend/node_modules" ]; then
  echo "[post-start] Frontend dependencies missing, restoring them"
  ./build.sh restore-frontend --skip restore
else
  echo "[post-start] Frontend dependencies already present"
fi

echo "[post-start] Devcontainer is ready"
