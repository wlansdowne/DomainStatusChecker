#!/bin/bash

# Ensure NGINX temp directories exist with correct permissions
for dir in body proxy fastcgi uwsgi scgi; do
    if [ ! -d "/var/lib/nginx/$dir" ]; then
        echo "Creating /var/lib/nginx/$dir"
        mkdir -p "/var/lib/nginx/$dir"
    fi
done

# Ensure NGINX pid directory exists
if [ ! -d "/run/nginx" ]; then
    echo "Creating /run/nginx"
    mkdir -p /run/nginx
fi

# Start the .NET application in the background
echo "Starting .NET application..."
dotnet DomainStatusChecker.dll &
APP_PID=$!

# Wait for .NET app to start
echo "Waiting for .NET application to start..."
sleep 5

# Start NGINX in the background
echo "Starting NGINX..."
nginx -g 'daemon off;' &
NGINX_PID=$!

# Function to handle shutdown
shutdown() {
    echo "Shutting down..."
    if [ -n "$NGINX_PID" ]; then
        echo "Stopping NGINX..."
        kill -TERM $NGINX_PID || true
    fi
    if [ -n "$APP_PID" ]; then
        echo "Stopping .NET application..."
        kill -TERM $APP_PID || true
    fi
    wait $APP_PID 2>/dev/null || true
    wait $NGINX_PID 2>/dev/null || true
    exit 0
}

# Handle shutdown signals
trap shutdown SIGTERM SIGINT

echo "All processes started. Monitoring..."

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