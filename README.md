# Domain Status Checker

A .NET Core application for monitoring website domains, checking their DNS resolution status, and detecting CDN usage. Built with performance and scalability in mind.

## Features

- CSV file upload for bulk domain checking (supports large files)
- DNS resolution status monitoring
- Subnet validation
- CDN detection (AWS, Azure, Cloudflare, etc.)
- Batch processing with timeouts
- Responsive web interface with status grouping

## Performance Features

- Batch processing (50 domains per batch)
- Concurrent processing (20 simultaneous checks)
- Individual domain timeouts (30 seconds)
- Batch timeouts (5 minutes)
- Graceful error handling and recovery

## Technology Stack

- ASP.NET Core 7.0
- NGINX (as reverse proxy)
- Docker
- Bootstrap for UI

## Deployment with EasyPanel

### Prerequisites

1. A server with EasyPanel installed
2. A GitHub repository containing this code
3. Domain name (optional, but recommended)

### Deployment Steps

1. In EasyPanel:
   - Create new app
   - Select your GitHub repository
   - Build Configuration:
     * Dockerfile path: `Dockerfile`
     * Port: 80
   - Environment Variables (optional):
     * TZ: Your timezone (e.g., America/New_York)

2. After Deployment:
   - Access your application at the provided domain/IP
   - Go to the Settings page (/Home/Settings)
   - Configure your subnets and CDN organizations
   - Upload your websites.csv file

## Configuration

### NGINX Configuration

- Request timeouts: 5 minutes
- Max body size: 50MB
- Header buffer size: 64KB
- Proxy buffers optimized for large requests
- GZIP compression enabled

### Kestrel Configuration

- Request timeouts: 5 minutes
- Max request body size: 50MB
- Header size limits increased
- Connection logging enabled

## CSV Processing

The application processes CSV files efficiently:
- Processes domains in batches of 50
- Handles up to 20 concurrent domain checks
- Individual domain checks timeout after 30 seconds
- Each batch has a 5-minute timeout
- Continues processing even if some domains fail

## Security Features

- Non-root container execution
- Security headers configured
- Request size limits
- Static file caching
- GZIP compression

## Monitoring

The application includes:
- Health check endpoints
- Progress logging
- Error tracking
- Connection logging

## Data Persistence

The application uses Docker volumes for:
- Application logs
- NGINX logs
- Uploaded CSV files
- Configuration data

## Troubleshooting

Common issues and solutions:

1. Request Timeout:
   - Default timeout is 5 minutes per batch
   - Increase batch size for faster processing
   - Adjust timeouts in nginx.conf if needed

2. Large CSV Files:
   - Maximum file size: 50MB
   - Files are processed in batches
   - Progress is logged

3. DNS Resolution:
   - Individual lookups timeout after 30 seconds
   - Failed lookups are logged
   - Processing continues for other domains

## Support

For issues and feature requests, please create an issue in the GitHub repository.