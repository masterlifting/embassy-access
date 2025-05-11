#!/bin/bash
set -e

cd /usr/src/embassy-access/src/embassy-access-worker/

PID_FILE="/tmp/embassy-access-pid"

if [[ -f "$PID_FILE" ]]; then
    LAST_PID=$(cat "$PID_FILE")
    if kill -0 $LAST_PID 2>/dev/null; then
        kill $LAST_PID
        echo "Killed process with PID $LAST_PID"
    fi
fi

echo "Starting dotnet application in Release mode"
nohup dotnet run -c Release > nohup.out 2>&1 &
echo $! > "$PID_FILE"
echo "New process started with PID $!"

exit 0