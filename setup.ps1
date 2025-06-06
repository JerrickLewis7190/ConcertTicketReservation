# Concert Ticket Management API Setup Script
# This script helps resolve common NuGet package issues and sets up the project

Write-Host "Setting up Concert Ticket Management API..." -ForegroundColor Green

# Navigate to the API directory
Set-Location "ConcertTicketAPI"

Write-Host "Cleaning project..." -ForegroundColor Yellow
dotnet clean

Write-Host "Clearing NuGet caches..." -ForegroundColor Yellow
dotnet nuget locals all --clear

Write-Host "Removing obj and bin directories..." -ForegroundColor Yellow
if (Test-Path "obj") { Remove-Item -Recurse -Force "obj" }
if (Test-Path "bin") { Remove-Item -Recurse -Force "bin" }

Write-Host "Restoring packages with specific source..." -ForegroundColor Yellow
try {
    dotnet restore --source "https://api.nuget.org/v3/index.json" --no-cache --force
    Write-Host "Package restoration completed successfully!" -ForegroundColor Green
    
    Write-Host "Building project..." -ForegroundColor Yellow
    dotnet build
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Setup completed successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "To run the application:" -ForegroundColor Cyan
        Write-Host "  dotnet run" -ForegroundColor White
        Write-Host ""
        Write-Host "The API will be available at:" -ForegroundColor Cyan
        Write-Host "  HTTPS: https://localhost:7000 (or check console output)" -ForegroundColor White
        Write-Host "  HTTP:  http://localhost:5000 (or check console output)" -ForegroundColor White
        Write-Host "  Swagger UI: Navigate to the root URL" -ForegroundColor White
    } else {
        Write-Host "Build failed. Please check the error messages above." -ForegroundColor Red
    }
} catch {
    Write-Host "Package restoration failed. Please try the manual steps below:" -ForegroundColor Red
    Write-Host ""
    Write-Host "Manual Setup Steps:" -ForegroundColor Yellow
    Write-Host "1. Ensure .NET 9.0 SDK is installed" -ForegroundColor White
    Write-Host "2. Run: dotnet --version (should show 9.0.x)" -ForegroundColor White
    Write-Host "3. Run: dotnet nuget list source" -ForegroundColor White
    Write-Host "4. If needed, reset NuGet sources:" -ForegroundColor White
    Write-Host "   dotnet nuget remove source (for each existing source)" -ForegroundColor Gray
    Write-Host "   dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org" -ForegroundColor Gray
    Write-Host "5. Try the restore again: dotnet restore" -ForegroundColor White
} 