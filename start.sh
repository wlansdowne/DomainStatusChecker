#!/bin/sh

# Start the .NET application in the background
dotnet DomainStatusChecker.dll &

# Start NGINX in the foreground
nginx -g 'daemon off;'