# Stop any existing processes
Write-Host "Stopping any existing processes..." -ForegroundColor Yellow
Get-Process -Name "dotnet", "DomainStatusChecker" -ErrorAction SilentlyContinue | Stop-Process -Force

# Set working directory to the script location
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptPath

# Clean and build the solution
Write-Host "Building application..." -ForegroundColor Yellow
Set-Location "DomainStatusChecker"
dotnet clean | Out-Null
dotnet build --configuration Release | Out-Null

# Start the application
Write-Host "Starting application..." -ForegroundColor Green
try {
    # Start the application in a new job
    $job = Start-Job -ScriptBlock {
        Set-Location $using:PWD
        dotnet run --configuration Release --urls="http://0.0.0.0:5000" --no-https
    }

    # Wait for the application to start
    Write-Host "Waiting for application to start..." -ForegroundColor Yellow
    $retries = 0
    $maxRetries = 10
    $started = $false

    while (-not $started -and $retries -lt $maxRetries) {
        try {
            $response = Invoke-WebRequest -Uri "http://127.0.0.1:5000/Home/Settings" -UseBasicParsing -ErrorAction Stop
            if ($response.StatusCode -eq 200) {
                $started = $true
            }
        }
        catch {
            Start-Sleep -Seconds 1
            $retries++
        }
    }

    if ($started) {
        # Open the browser
        Write-Host "Opening browser..." -ForegroundColor Green
        Start-Process "http://127.0.0.1:5000/Home/Settings"

        # Wait for the job to complete
        Write-Host "Application is running. Press Ctrl+C to stop." -ForegroundColor Cyan
        Wait-Job $job
        Receive-Job $job
    }
    else {
        Write-Host "Failed to start application after $maxRetries attempts." -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "Error starting application: $_" -ForegroundColor Red
    exit 1
}
finally {
    # Cleanup
    Remove-Job -Job $job -Force -ErrorAction SilentlyContinue
}