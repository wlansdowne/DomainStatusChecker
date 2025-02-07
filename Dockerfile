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
    # Create necessary directories with proper permissions
    mkdir -p /app/nginx/logs /app/nginx/temp/body /app/nginx/temp/proxy /app/nginx/temp/fastcgi /app/nginx/temp/uwsgi /app/nginx/temp/scgi /app/nginx/run && \
    # Create directory for ASP.NET Core data protection keys
    mkdir -p /app/.aspnet/DataProtection-Keys && \
    # Set permissions
    chown -R www-data:www-data /app/nginx && \
    chmod 755 /app/nginx && \
    chmod 777 /app/nginx/logs && \
    # Remove default NGINX files we don't need
    rm -rf /var/log/nginx /var/lib/nginx /var/www/html && \
    # Create symlinks for NGINX to use our directories
    ln -s /app/nginx/logs /var/log/nginx && \
    ln -s /app/nginx/temp /var/lib/nginx && \
    ln -s /app/nginx/run /run/nginx

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
    # Ensure NGINX can still access its directories
    chown -R appuser:www-data /app/nginx && \
    chmod -R 775 /app/nginx && \
    # Ensure static files are accessible
    chmod -R 755 /app/wwwroot

# Copy and set up start script
COPY start.sh /start.sh
RUN chmod +x /start.sh && \
    chown appuser:appuser /start.sh

# Switch to non-root user
USER appuser

EXPOSE 80
CMD ["/start.sh"]