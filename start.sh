#!/bin/bash

# Ensure our custom NGINX directories exist and have correct permissions
echo "Setting up NGINX directories..."
for dir in body proxy fastcgi uwsgi scgi; do
    mkdir -p "/app/nginx/temp/$dir"
done

mkdir -p "/app/nginx/logs"
mkdir -p "/app/nginx/run"

# Create log files with correct permissions
touch "/app/nginx/logs/error.log"
touch "/app/nginx/logs/access.log"

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

# Wait a moment to check if NGINX started successfully
sleep 2

# Check if NGINX is running
if ! kill -0 $NGINX_PID 2>/dev/null; then
    echo "NGINX failed to start. Checking logs:"
    if [ -f "/app/nginx/logs/error.log" ]; then
        cat "/app/nginx/logs/error.log"
    fi
    echo "Shutting down..."
    kill -TERM $APP_PID 2>/dev/null || true
    exit 1
fi

# Function to handle shutdown
shutdown() {
    echo "Shutting down..."
    if [ -n "$NGINX_PID" ] && kill -0 $NGINX_PID 2>/dev/null; then
        echo "Stopping NGINX..."
        kill -TERM $NGINX_PID
        wait $NGINX_PID 2>/dev/null || true
    fi
    if [ -n "$APP_PID" ] && kill -0 $APP_PID 2>/dev/null; then
        echo "Stopping .NET application..."
        kill -TERM $APP_PID
        wait $APP_PID 2>/dev/null || true
    fi
    exit 0
}

# Handle shutdown signals
trap shutdown SIGTERM SIGINT

echo "All processes started. Monitoring..."

# Keep the script running and monitor child processes
while true; do
    if ! kill -0 $APP_PID 2>/dev/null; then
        echo ".NET application exited unexpectedly"
        if [ -n "$NGINX_PID" ] && kill -0 $NGINX_PID 2>/dev/null; then
            kill -TERM $NGINX_PID
        fi
        exit 1
    fi
    if ! kill -0 $NGINX_PID 2>/dev/null; then
        echo "NGINX exited unexpectedly"
        if [ -f "/app/nginx/logs/error.log" ]; then
            echo "NGINX error log:"
            cat "/app/nginx/logs/error.log"
        fi
        if [ -n "$APP_PID" ] && kill -0 $APP_PID 2>/dev/null; then
            kill -TERM $APP_PID
        fi
        exit 1
    fi
    sleep 1
done