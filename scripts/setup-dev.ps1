# AuthCore Development Setup Script
# Automates the setup process for local development

Write-Host "🚀 AuthCore Development Setup" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Check .NET SDK
Write-Host "Checking .NET SDK..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ .NET SDK $dotnetVersion installed" -ForegroundColor Green
} else {
    Write-Host "✗ .NET SDK not found. Please install .NET 8 SDK" -ForegroundColor Red
    exit 1
}

# Check Docker
Write-Host "Checking Docker..." -ForegroundColor Yellow
docker --version | Out-Null
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Docker is installed" -ForegroundColor Green
} else {
    Write-Host "✗ Docker not found. Please install Docker Desktop" -ForegroundColor Red
    exit 1
}

# Restore NuGet packages
Write-Host ""
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Packages restored successfully" -ForegroundColor Green
} else {
    Write-Host "✗ Failed to restore packages" -ForegroundColor Red
    exit 1
}

# Start Docker services
Write-Host ""
Write-Host "Starting Docker services..." -ForegroundColor Yellow
docker-compose up -d postgres redis
Start-Sleep -Seconds 5

# Check if services are running
$postgresRunning = docker-compose ps postgres | Select-String "Up"
$redisRunning = docker-compose ps redis | Select-String "Up"

if ($postgresRunning -and $redisRunning) {
    Write-Host "✓ Database and Redis services started" -ForegroundColor Green
} else {
    Write-Host "✗ Failed to start services. Check docker-compose logs" -ForegroundColor Red
    exit 1
}

# Apply database migrations
Write-Host ""
Write-Host "Applying database migrations..." -ForegroundColor Yellow
Set-Location src/AuthCore.Infrastructure
dotnet ef database update --startup-project ../AuthCore.API
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Database migrations applied" -ForegroundColor Green
} else {
    Write-Host "✗ Failed to apply migrations" -ForegroundColor Red
    Set-Location ../..
    exit 1
}
Set-Location ../..

# Build solution
Write-Host ""
Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build --configuration Debug
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Solution built successfully" -ForegroundColor Green
} else {
    Write-Host "✗ Build failed" -ForegroundColor Red
    exit 1
}

# Run tests
Write-Host ""
Write-Host "Running tests..." -ForegroundColor Yellow
dotnet test --no-build
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ All tests passed" -ForegroundColor Green
} else {
    Write-Host "⚠ Some tests failed" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
Write-Host "✓ Setup completed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "To start the API:" -ForegroundColor Cyan
Write-Host "  cd src/AuthCore.API" -ForegroundColor White
Write-Host "  dotnet run" -ForegroundColor White
Write-Host ""
Write-Host "Then visit: http://localhost:5000" -ForegroundColor Cyan
Write-Host ""
