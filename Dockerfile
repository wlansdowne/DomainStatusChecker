# Build stage
FROM mcr.microsoft.com/dotnet/sdk:7.0.405 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY DomainStatusChecker/*.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY DomainStatusChecker/. ./
RUN dotnet publish -c Release -o /app

# Runtime stage with NGINX
FROM nginx:alpine

# Install .NET Runtime
RUN apk add --no-cache \
    aspnetcore7-runtime \
    curl \
    tzdata

# Copy NGINX configuration
COPY docker/nginx.conf /etc/nginx/nginx.conf

# Copy published app
WORKDIR /app
COPY --from=build /app ./

# Copy static files to NGINX directory
COPY DomainStatusChecker/wwwroot /app/wwwroot

# Environment variables
ENV ASPNETCORE_URLS=http://+:80 \
    ASPNETCORE_ENVIRONMENT=Production \
    TZ=America/New_York

# Create non-root user
RUN adduser -D -H -u 1000 -s /sbin/nologin appuser && \
    chown -R appuser:appuser /app /var/cache/nginx /var/run/nginx.pid

# Start both NGINX and the .NET application
COPY start.sh /start.sh
RUN chmod +x /start.sh

EXPOSE 80
CMD ["/start.sh"]