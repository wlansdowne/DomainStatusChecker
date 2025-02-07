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
- Docker & Docker Compose
- NGINX
- Bootstrap for UI

## Deployment with EasyPanel

### Prerequisites

1. A server with EasyPanel installed
2. A GitHub account
3. Domain name (optional, but recommended)

### Deployment Steps

1. **Fork/Push to GitHub:**
   - Create a new GitHub repository
   - Push the code to your repository:
   ```bash
   git init
   git add .
   git commit -m "Initial commit"
   git branch -M main
   git remote add origin https://github.com/YOUR_USERNAME/domain-status-checker.git
   git push -u origin main
   ```

2. **EasyPanel Setup:**
   - Log into your EasyPanel dashboard
   - Click "New Service"
   - Select "Docker Compose"
   - Connect your GitHub repository
   - Configure the following:
     * Service Name: domain-status-checker
     * Branch: main
     * Port: 80 (or your preferred port)
     * Environment Variables (optional):
       - TZ=Your_Timezone

3. **Configuration:**
   - After deployment, update the subnets and CDN organizations in Settings page
   - Upload your websites.csv file through the web interface

### Environment Variables

- `PORT`: The port to expose (default: 80)
- `TZ`: Timezone (default: America/New_York)
- `ASPNETCORE_ENVIRONMENT`: Set to Production by default

## Development

### Local Development Setup

1. Clone the repository
2. Install Docker and Docker Compose
3. Run:
   ```bash
   docker compose build
   docker compose up
   ```
4. Access the application at http://localhost:80

### Project Structure

- `/DomainStatusChecker`: Main application code
- `/docker`: Docker-related configurations
  - `nginx.conf`: NGINX reverse proxy configuration
- `docker-compose.yml`: Service orchestration
- `Dockerfile`: .NET application container definition

## Monitoring

The application includes health check endpoints:
- `/health`: Basic health check
- Both the app and NGINX containers have built-in health checks

## Data Persistence

The application uses Docker volumes for data persistence:
- `app_data`: Application data
- `app_logs`: Application logs

## Security Notes

- The application runs with a non-root user in the container
- NGINX is configured with security headers
- Static file caching is enabled
- Request size limits are configured (50MB max)

## Support

For issues and feature requests, please create an issue in the GitHub repository.