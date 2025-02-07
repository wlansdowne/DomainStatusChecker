#!/bin/bash

# Start the .NET application in the background
dotnet DomainStatusChecker.dll &
APP_PID=$!

# Wait a moment for the .NET app to start
sleep 5

# Start NGINX in the foreground
# NGINX will run as www-data user since we set proper permissions in Dockerfile
nginx -g 'daemon off;' &
NGINX_PID=$!

# Function to handle shutdown
shutdown() {
    echo "Shutting down..."
    kill -TERM $APP_PID
    kill -TERM $NGINX_PID
    wait $APP_PID
    wait $NGINX_PID
    exit 0
}

# Handle shutdown signals
trap shutdown SIGTERM SIGINT

# Keep the script running and monitor child processes
while true; do
    # Check if either process has exited
    if ! kill -0 $APP_PID 2>/dev/null; then
        echo ".NET application exited unexpectedly"
        shutdown
    fi
    if ! kill -0 $NGINX_PID 2>/dev/null; then
        echo "NGINX exited unexpectedly"
        shutdown
    fi
    sleep 1
done