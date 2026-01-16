#!/bin/bash
# AuthCore Development Setup Script
# Automates the setup process for local development

echo "🚀 AuthCore Development Setup"
echo "================================"
echo ""

# Check .NET SDK
echo "Checking .NET SDK..."
if command -v dotnet &> /dev/null; then
    dotnet_version=$(dotnet --version)
    echo "✓ .NET SDK $dotnet_version installed"
else
    echo "✗ .NET SDK not found. Please install .NET 8 SDK"
    exit 1
fi

# Check Docker
echo "Checking Docker..."
if command -v docker &> /dev/null; then
    echo "✓ Docker is installed"
else
    echo "✗ Docker not found. Please install Docker"
    exit 1
fi

# Restore NuGet packages
echo ""
echo "Restoring NuGet packages..."
if dotnet restore; then
    echo "✓ Packages restored successfully"
else
    echo "✗ Failed to restore packages"
    exit 1
fi

# Start Docker services
echo ""
echo "Starting Docker services..."
docker-compose up -d postgres redis
sleep 5

# Check if services are running
if docker-compose ps | grep -q "postgres.*Up" && docker-compose ps | grep -q "redis.*Up"; then
    echo "✓ Database and Redis services started"
else
    echo "✗ Failed to start services. Check docker-compose logs"
    exit 1
fi

# Apply database migrations
echo ""
echo "Applying database migrations..."
cd src/AuthCore.Infrastructure || exit
if dotnet ef database update --startup-project ../AuthCore.API; then
    echo "✓ Database migrations applied"
else
    echo "✗ Failed to apply migrations"
    cd ../..
    exit 1
fi
cd ../..

# Build solution
echo ""
echo "Building solution..."
if dotnet build --configuration Debug; then
    echo "✓ Solution built successfully"
else
    echo "✗ Build failed"
    exit 1
fi

# Run tests
echo ""
echo "Running tests..."
if dotnet test --no-build; then
    echo "✓ All tests passed"
else
    echo "⚠ Some tests failed"
fi

echo ""
echo "================================"
echo "✓ Setup completed successfully!"
echo ""
echo "To start the API:"
echo "  cd src/AuthCore.API"
echo "  dotnet run"
echo ""
echo "Then visit: http://localhost:5000"
echo ""
