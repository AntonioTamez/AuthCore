# AuthCore Setup Guide

This guide will help you set up and run AuthCore locally or in production.

## Table of Contents

1. [Local Development Setup](#local-development-setup)
2. [Database Setup](#database-setup)
3. [Redis Configuration](#redis-configuration)
4. [OAuth2 Configuration](#oauth2-configuration)
5. [Docker Setup](#docker-setup)
6. [Troubleshooting](#troubleshooting)

## Local Development Setup

### Step 1: Install Prerequisites

1. **Install .NET 8 SDK**
   ```bash
   # Windows (using winget)
   winget install Microsoft.DotNet.SDK.8
   
   # macOS (using homebrew)
   brew install dotnet@8
   
   # Linux (Ubuntu/Debian)
   wget https://dot.net/v1/dotnet-install.sh
   chmod +x dotnet-install.sh
   ./dotnet-install.sh --channel 8.0
   ```

2. **Install PostgreSQL**
   ```bash
   # Windows - Download installer from postgresql.org
   
   # macOS
   brew install postgresql@16
   brew services start postgresql@16
   
   # Linux (Ubuntu/Debian)
   sudo apt update
   sudo apt install postgresql-16
   sudo systemctl start postgresql
   ```

3. **Install Redis**
   ```bash
   # Windows - Use WSL or download from GitHub
   
   # macOS
   brew install redis
   brew services start redis
   
   # Linux (Ubuntu/Debian)
   sudo apt install redis-server
   sudo systemctl start redis
   ```

### Step 2: Clone and Restore

```bash
git clone https://github.com/yourusername/authcore.git
cd authcore
dotnet restore
```

### Step 3: Configure Application

1. **Copy and edit appsettings**
   ```bash
   cd src/AuthCore.API
   cp appsettings.json appsettings.Development.json
   ```

2. **Edit `appsettings.Development.json`**
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=authcore;Username=postgres;Password=yourpassword",
       "Redis": "localhost:6379"
     },
     "Jwt": {
       "Secret": "GenerateASecureSecretKeyHereMinimum32Characters!",
       "Issuer": "AuthCore",
       "Audience": "AuthCore.Client",
       "AccessTokenExpirationMinutes": "15",
       "RefreshTokenExpirationDays": "7"
     }
   }
   ```

## Database Setup

### Create Database User

```sql
-- Connect to PostgreSQL
psql -U postgres

-- Create user and database
CREATE USER authcore_user WITH PASSWORD 'your_secure_password';
CREATE DATABASE authcore OWNER authcore_user;

-- Grant privileges
GRANT ALL PRIVILEGES ON DATABASE authcore TO authcore_user;

-- Exit
\q
```

### Apply Migrations

```bash
# Install EF Core tools (if not already installed)
dotnet tool install --global dotnet-ef

# Navigate to infrastructure project
cd src/AuthCore.Infrastructure

# Create initial migration
dotnet ef migrations add InitialCreate --startup-project ../AuthCore.API

# Apply migration to database
dotnet ef database update --startup-project ../AuthCore.API
```

### Verify Database

```bash
# Connect to database
psql -U authcore_user -d authcore

# List tables
\dt

# You should see:
# - Tenants
# - Users
# - Roles
# - Permissions
# - UserRoles
# - RolePermissions
# - RefreshTokens

# Check seeded roles
SELECT * FROM "Roles";

# Exit
\q
```

## Redis Configuration

### Test Redis Connection

```bash
# Connect to Redis CLI
redis-cli

# Test connection
PING
# Should return: PONG

# Check info
INFO server

# Exit
quit
```

### Configure Redis for Production

Edit `redis.conf`:
```conf
# Bind to specific interface
bind 127.0.0.1

# Set password
requirepass your_strong_redis_password

# Enable persistence
appendonly yes

# Set max memory
maxmemory 256mb
maxmemory-policy allkeys-lru
```

Update connection string:
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379,password=your_strong_redis_password"
  }
}
```

## OAuth2 Configuration

### Google OAuth2

1. **Create Project**
   - Go to [Google Cloud Console](https://console.cloud.google.com)
   - Create new project or select existing
   - Enable Google+ API

2. **Create OAuth2 Credentials**
   - Navigate to "APIs & Services" > "Credentials"
   - Click "Create Credentials" > "OAuth client ID"
   - Application type: Web application
   - Authorized redirect URIs:
     - `http://localhost:5000/api/oauth/google/callback`
     - `https://yourdomain.com/api/oauth/google/callback`

3. **Update Configuration**
   ```json
   {
     "OAuth": {
       "Google": {
         "ClientId": "your-client-id.apps.googleusercontent.com",
         "ClientSecret": "your-client-secret"
       }
     }
   }
   ```

### GitHub OAuth2

1. **Create OAuth App**
   - Go to [GitHub Settings](https://github.com/settings/developers)
   - Click "New OAuth App"
   - Application name: AuthCore
   - Homepage URL: `http://localhost:5000`
   - Authorization callback URL: `http://localhost:5000/api/oauth/github/callback`

2. **Update Configuration**
   ```json
   {
     "OAuth": {
       "GitHub": {
         "ClientId": "your_github_client_id",
         "ClientSecret": "your_github_client_secret"
       }
     }
   }
   ```

## Docker Setup

### Install Docker Desktop

- **Windows**: [Download Docker Desktop](https://www.docker.com/products/docker-desktop)
- **macOS**: `brew install --cask docker`
- **Linux**: Follow [official guide](https://docs.docker.com/engine/install/)

### Start with Docker Compose

```bash
# Build and start all services
docker-compose up -d --build

# Check status
docker-compose ps

# View logs
docker-compose logs -f

# View specific service logs
docker-compose logs -f api
docker-compose logs -f postgres
docker-compose logs -f redis
```

### Apply Migrations in Docker

```bash
# Execute migration command in API container
docker-compose exec api dotnet ef database update --project /src/src/AuthCore.Infrastructure

# Or shell into container and run manually
docker-compose exec api bash
cd /src/src/AuthCore.Infrastructure
dotnet ef database update --startup-project ../AuthCore.API
exit
```

### Access Services

- **API**: http://localhost:5000
- **Swagger UI**: http://localhost:5000
- **PostgreSQL**: localhost:5432
- **Redis**: localhost:6379

### Connect to Database in Docker

```bash
# Using docker-compose
docker-compose exec postgres psql -U authcore_user -d authcore

# Or using psql directly
psql -h localhost -p 5432 -U authcore_user -d authcore
```

## Environment Variables

### Development (.env file)

Create `.env` file in project root:
```env
ASPNETCORE_ENVIRONMENT=Development
POSTGRES_PASSWORD=your_secure_password
REDIS_PASSWORD=your_redis_password
JWT_SECRET=YourSuperSecretKeyThatIsAtLeast32CharactersLong
GOOGLE_CLIENT_ID=your-google-client-id
GOOGLE_CLIENT_SECRET=your-google-client-secret
GITHUB_CLIENT_ID=your-github-client-id
GITHUB_CLIENT_SECRET=your-github-client-secret
```

### Production Environment Variables

For production deployment, set these environment variables:

```bash
# Database
ConnectionStrings__DefaultConnection=Host=prod-db;Database=authcore;Username=user;Password=pass

# Redis
ConnectionStrings__Redis=prod-redis:6379,password=redispass

# JWT
Jwt__Secret=ProductionSecretKey32CharsMinimum
Jwt__Issuer=AuthCore
Jwt__Audience=AuthCore.Client

# OAuth (if using)
OAuth__Google__ClientId=prod-google-client-id
OAuth__Google__ClientSecret=prod-google-secret
OAuth__GitHub__ClientId=prod-github-client-id
OAuth__GitHub__ClientSecret=prod-github-secret
```

## Troubleshooting

### Database Connection Issues

**Problem**: Can't connect to PostgreSQL

**Solutions**:
```bash
# Check if PostgreSQL is running
sudo systemctl status postgresql  # Linux
brew services list  # macOS

# Check PostgreSQL logs
sudo tail -f /var/log/postgresql/postgresql-16-main.log  # Linux
tail -f /usr/local/var/log/postgres.log  # macOS

# Test connection
psql -h localhost -p 5432 -U authcore_user -d authcore

# Check firewall
sudo ufw allow 5432/tcp  # Linux
```

### Redis Connection Issues

**Problem**: Can't connect to Redis

**Solutions**:
```bash
# Check if Redis is running
redis-cli ping

# Check Redis logs
sudo tail -f /var/log/redis/redis-server.log  # Linux
tail -f /usr/local/var/log/redis.log  # macOS

# Restart Redis
sudo systemctl restart redis  # Linux
brew services restart redis  # macOS
```

### Migration Issues

**Problem**: Migration fails

**Solutions**:
```bash
# Remove last migration
dotnet ef migrations remove --project src/AuthCore.Infrastructure --startup-project src/AuthCore.API

# Drop database and recreate
dotnet ef database drop --project src/AuthCore.Infrastructure --startup-project src/AuthCore.API
dotnet ef database update --project src/AuthCore.Infrastructure --startup-project src/AuthCore.API

# Check connection string
echo $ConnectionStrings__DefaultConnection
```

### Docker Issues

**Problem**: Container fails to start

**Solutions**:
```bash
# Check logs
docker-compose logs api

# Rebuild without cache
docker-compose build --no-cache
docker-compose up -d

# Remove all containers and volumes
docker-compose down -v
docker-compose up -d --build

# Check container status
docker ps -a
```

### Port Conflicts

**Problem**: Port already in use

**Solutions**:
```bash
# Find process using port
# Windows
netstat -ano | findstr :5000
taskkill /PID <PID> /F

# Linux/macOS
lsof -ti:5000 | xargs kill -9

# Or change port in docker-compose.yml
ports:
  - "5001:80"  # Changed from 5000:80
```

## Running the Application

### Development Mode

```bash
cd src/AuthCore.API
dotnet run
```

Access at: https://localhost:7000 or http://localhost:5000

### Production Mode

```bash
cd src/AuthCore.API
dotnet run --configuration Release
```

### Watch Mode (Auto-reload)

```bash
cd src/AuthCore.API
dotnet watch run
```

## Next Steps

1. Review the [README.md](README.md) for API documentation
2. Import the Postman collection for testing
3. Explore the Swagger UI at http://localhost:5000
4. Check the [Integration Guide](INTEGRATION.md) for using AuthCore in your projects

## Support

If you encounter issues not covered here:

1. Check [GitHub Issues](https://github.com/yourusername/authcore/issues)
2. Review application logs: `docker-compose logs -f api`
3. Enable detailed logging in `appsettings.json`:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Debug",
         "Microsoft.AspNetCore": "Information"
       }
     }
   }
   ```
