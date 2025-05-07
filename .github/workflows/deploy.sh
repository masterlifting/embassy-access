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

nohup dotnet build -c Release > build.out 2>&1 &
BUILD_PID=$!
wait $BUILD_PID

chmod +x ./bin/Release/net9.0/playwright.ps1
pwsh ./bin/Release/net9.0/playwright.ps1 install > playwright_install.out 2>&1 &
PLAYWRIGHT_PID=$!
wait $PLAYWRIGHT_PID

nohup dotnet run > app.out 2>&1 &

echo $! > "$PID_FILE"
echo "New process started with PID $!"

exit 0
