#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

cd "$REPO_ROOT"

if ! pgrep -f "podman system service --time=0 unix:///tmp/podman.sock" >/dev/null 2>&1; then
  echo "[post-start] Starting Podman API service"
  nohup podman system service --time=0 unix:///tmp/podman.sock >/tmp/podman-service.log 2>&1 &
else
  echo "[post-start] Podman API service already running"
fi

echo "[post-start] Restoring local dotnet tools"
./build.sh --target restore

if [ ! -d "src/Agenda.Frontend/node_modules" ]; then
  echo "[post-start] Frontend dependencies missing, restoring them"
  ./build.sh restore-frontend --skip restore
else
  echo "[post-start] Frontend dependencies already present"
fi

echo "[post-start] Devcontainer is ready"
