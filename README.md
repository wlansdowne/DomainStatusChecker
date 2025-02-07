# Domain Status Checker

A .NET Core application for monitoring website domains, checking their DNS resolution status, and detecting CDN usage.

## Features

- CSV file upload for bulk domain checking
- DNS resolution status monitoring
- Subnet validation
- CDN detection (AWS, Azure, Cloudflare, etc.)
- Concurrent processing with rate limiting
- Responsive web interface with status grouping

## Technology Stack

- ASP.NET Core 7.0
- NGINX (as reverse proxy)
- Bootstrap for UI

## Deployment with EasyPanel

### Prerequisites

1. A server with EasyPanel installed
2. A GitHub repository containing this code
3. Domain name (optional, but recommended)

### Deployment Steps

1. In EasyPanel:
   - Click "Create App"
   - Select your GitHub repository
   - Build Configuration:
     * Dockerfile name: `Dockerfile` (in root directory)
     * Port: 80
   - Environment Variables (optional):
     * TZ: Your timezone (e.g., America/New_York)

2. After Deployment:
   - Access your application at the provided domain/IP
   - Go to the Settings page (/Home/Settings)
   - Configure your subnets and CDN organizations
   - Upload your websites.csv file

## Container Architecture

The application runs in a single container that includes:
- NGINX as reverse proxy (port 80)
- .NET Core application (internal port 5000)
- Shared volume for static files
- Health check endpoints

### Security Features

- Non-root user for both NGINX and .NET
- Security headers configured
- Request size limits
- Static file caching
- GZIP compression

## Development

### Local Development Setup

1. Clone the repository
2. Build the Docker image:
   ```bash
   docker build -t domain-status-checker .
   ```
3. Run the container:
   ```bash
   docker run -p 80:80 domain-status-checker
   ```
4. Access the application at http://localhost

## Monitoring

The application includes health check endpoints:
- `/health`: Basic health check for both NGINX and .NET application

## Data Persistence

The application uses Docker volumes for:
- Application logs
- Uploaded CSV files
- Configuration data

## Support

For issues and feature requests, please create an issue in the GitHub repository.