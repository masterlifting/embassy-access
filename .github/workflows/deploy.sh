#!/bin/bash

set -e

cd /usr/src/embassy-access/src/embassy-access-worker/

git stash
git pull origin main --recurse-submodules

# Check if the PID file exists and contains a running process
PID_FILE="/tmp/embassy-access-pid"
if [[ -f "$PID_FILE" ]]; then
    LAST_PID=$(cat "$PID_FILE")
    if kill -0 $LAST_PID 2>/dev/null; then
        kill $LAST_PID
    fi
fi

nohup dotnet run -c Release &
echo $! > "$PID_FILE"
