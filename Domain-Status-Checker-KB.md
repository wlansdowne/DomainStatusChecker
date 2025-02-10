# Domain Status Checker - Knowledge Base

## Overview

The Domain Status Checker is a web-based tool that helps you monitor the status of multiple websites by analyzing their DNS resolution, checking if they're hosted on specific subnets, and detecting CDN usage. It processes CSV files containing website information and provides detailed reports about their status.

## Features

- Bulk domain status checking via CSV upload
- DNS resolution status monitoring
- Subnet validation for specific IP ranges
- CDN detection for major providers
- Nameserver lookup for domains
- Detailed reporting with status grouping
- Progress tracking during processing

## CSV File Format

### Required Format
```
Site Name Status IP Port Host
```

### Example CSV Content
```
example.com (W3SVC/1)    STARTED    174.136.64.10    80    example.com
test.com (W3SVC/2)    STARTED    216.167.192.15    443    test.com
cdn-site.com (W3SVC/3)    STARTED    104.16.132.229    80    cdn-site.com
stopped-site.com (W3SVC/4)    STOPPED    192.168.1.1    80    stopped-site.com
```

### Field Descriptions
- **Site Name**: Website name (can include IIS site ID in parentheses)
- **Status**: STARTED or STOPPED
- **IP**: IP address of the website
- **Port**: Port number (typically 80 or 443)
- **Host**: Domain name to check

## Domain Status Types

1. **Alive** (🟢)
   - Domain resolves to an IP within configured subnets
   - Nameservers are displayed
   - Example: `Alive`

2. **CDN Protected** (🔵)
   - Domain is behind a known CDN provider
   - Shows which CDN is being used
   - Nameservers are displayed
   - Example: `CDN Protected (Cloudflare)`

3. **Resolves Elsewhere** (🟡)
   - Domain resolves but not to configured subnets
   - Example: `Resolves Elsewhere`

4. **Not Found** (⚫)
   - Domain doesn't resolve
   - DNS lookup failed
   - Example: `Not Found`

5. **DNS Error** (🔴)
   - Error during DNS resolution
   - Example: `DNS Error`

6. **N/A** (⚪)
   - Website status is STOPPED
   - No DNS check performed
   - Example: `N/A`

## Using the Application

### 1. Accessing the Application
- Open your web browser
- Navigate to the application URL
- You'll see the main upload page

### 2. Configuring Settings
1. Click the "Settings" link
2. Configure Target Subnets:
   - Enter subnet in CIDR notation (e.g., `174.136.64.0/19`)
   - Click "Add Subnet"
   - Repeat for additional subnets

3. Configure CDN Organizations:
   - Enter CDN provider name (e.g., `Cloudflare`)
   - Click "Add CDN Provider"
   - Repeat for additional providers

### 3. Uploading CSV File
1. Click "Choose File" on the main page
2. Select your CSV file (max 50MB)
3. Click "Upload"
4. Wait for processing to complete

### 4. Reading the Report
The report is divided into sections:

1. **Summary Section**
   - Total number of websites
   - Count by status (STARTED/STOPPED)

2. **Priority Section** (Green Background)
   - Shows websites in target subnets
   - Displays status as "Alive"
   - Shows nameservers

3. **Status Groups**
   - STARTED websites
   - STOPPED websites
   - Each showing:
     * Website name
     * IP address
     * Port
     * Host
     * Domain status
     * Nameservers (for Alive/CDN)

## Example Output

### Priority Section Example
```
Website Name          IP              Port  Host           Status  Nameservers
example.com          174.136.64.10   80    example.com    Alive   ns1.provider.net
                                                                  ns2.provider.net
```

### Regular Section Example
```
Website Name          IP              Port  Host           Status              Nameservers
cdn-site.com         104.16.132.229  80    cdn-site.com   CDN Protected      ns1.cloudflare.com
                                                          (Cloudflare)        ns2.cloudflare.com
```

## Troubleshooting

### Common Issues

1. **File Upload Errors**
   - **Issue**: "No file uploaded" or "File too large"
   - **Solution**: 
     * Ensure file is selected
     * Check file size (max 50MB)
     * Try splitting large files

2. **Processing Timeouts**
   - **Issue**: "Request timed out"
   - **Solution**:
     * Reduce number of domains in CSV
     * Try processing in smaller batches
     * Check network connectivity

3. **Invalid CSV Format**
   - **Issue**: Domains not processing
   - **Solution**:
     * Check CSV format matches example
     * Ensure proper spacing between fields
     * Remove any special characters

4. **DNS Resolution Issues**
   - **Issue**: Many "DNS Error" results
   - **Solution**:
     * Verify domain names are correct
     * Check network DNS settings
     * Try again after a few minutes

### Best Practices

1. **CSV File Preparation**
   - Keep files under 1000 domains per batch
   - Use standard spacing between fields
   - Verify domain names before upload

2. **Performance Optimization**
   - Process during off-peak hours
   - Split large files into smaller batches
   - Allow sufficient time for processing

3. **Regular Maintenance**
   - Update CDN provider list regularly
   - Verify subnet configurations
   - Clean up old reports

## Support

If you encounter issues not covered in this guide:
1. Check application logs
2. Verify network connectivity
3. Ensure CSV format is correct
4. Contact system administrator with:
   - CSV file sample
   - Error messages
   - Time of occurrence
   - Steps to reproduce

## Limitations

- Maximum file size: 50MB
- Processing timeout: 30 minutes
- Individual domain timeout: 30 seconds
- Concurrent processing: 20 domains
- Batch size: 25 domains