# Build stage
FROM mcr.microsoft.com/dotnet/sdk:7.0.405 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY DomainStatusChecker/*.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY DomainStatusChecker/. ./
RUN dotnet publish -c Release -o /app

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:7.0.15
WORKDIR /app

# Install NGINX
RUN apt-get update && \
    apt-get install -y nginx curl tzdata && \
    rm -rf /var/lib/apt/lists/* && \
    # Create necessary directories and set permissions
    mkdir -p /var/log/nginx /var/lib/nginx/tmp /var/run && \
    chown -R www-data:www-data /var/log/nginx /var/lib/nginx /var/run

# Copy NGINX configuration
COPY docker/nginx.conf /etc/nginx/nginx.conf

# Copy published app and static files
COPY --from=build /app ./
COPY DomainStatusChecker/wwwroot ./wwwroot

# Environment variables
ENV ASPNETCORE_URLS=http://+:5000 \
    ASPNETCORE_ENVIRONMENT=Production \
    TZ=America/New_York

# Set up non-root user
RUN useradd -r -s /bin/false appuser && \
    # Set permissions for application
    chown -R appuser:appuser /app && \
    # Set permissions for NGINX
    touch /var/run/nginx.pid && \
    chown -R appuser:www-data /var/run/nginx.pid && \
    chmod 2755 /var/log/nginx /var/lib/nginx /var/run

# Copy and set up start script
COPY start.sh /start.sh
RUN chmod +x /start.sh && \
    chown appuser:appuser /start.sh

# Switch to non-root user
USER appuser

EXPOSE 80
CMD ["/start.sh"]