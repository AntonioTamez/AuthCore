# AuthCore - Multi-Tenant Authentication System

A production-ready, reusable authentication and authorization system built with .NET 8, PostgreSQL, and Redis. Implements JWT authentication, OAuth2, role-based access control (RBAC), and multi-tenancy.

## 🚀 Features

- ✅ **JWT Authentication** - Access & refresh tokens with secure token management
- ✅ **Multi-Tenancy** - Isolated tenant data with domain-based routing
- ✅ **Role-Based Authorization** - Flexible RBAC with granular permissions
- ✅ **OAuth2 Integration** - Google and GitHub authentication
- ✅ **Password Management** - Secure hashing (PBKDF2) with password reset functionality
- ✅ **Rate Limiting** - IP-based rate limiting to prevent abuse
- ✅ **Redis Caching** - Session and token caching for performance
- ✅ **PostgreSQL Database** - Robust relational database with EF Core
- ✅ **Swagger Documentation** - Interactive API documentation
- ✅ **Docker Support** - Full containerization with Docker Compose
- ✅ **Unit Tests** - 80%+ code coverage with xUnit

## 📋 Tech Stack

| Technology | Version | Purpose |
|------------|---------|---------|
| .NET | 8.0 | Web API Framework |
| PostgreSQL | 16 | Primary Database |
| Redis | 7.x | Caching & Session Store |
| Entity Framework Core | 8.0 | ORM |
| JWT Bearer | 8.0 | Authentication |
| Swashbuckle | 6.5.0 | API Documentation |
| xUnit | Latest | Unit Testing |

## 🏗️ Architecture

```
AuthCore/
├── src/
│   ├── AuthCore.API/           # Web API Layer
│   │   ├── Controllers/        # API Controllers
│   │   ├── Program.cs          # Application startup
│   │   └── appsettings.json    # Configuration
│   ├── AuthCore.Core/          # Domain Layer
│   │   ├── Entities/           # Domain models
│   │   ├── DTOs/               # Data transfer objects
│   │   └── Interfaces/         # Service contracts
│   └── AuthCore.Infrastructure/ # Infrastructure Layer
│       ├── Data/               # DbContext & Configurations
│       └── Services/           # Service implementations
├── tests/
│   └── AuthCore.Tests/         # Unit & Integration Tests
├── docker-compose.yml          # Docker orchestration
└── Dockerfile                  # API container definition
```

## 🛠️ Prerequisites

- .NET 8 SDK ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
- Docker Desktop ([Download](https://www.docker.com/products/docker-desktop))
- PostgreSQL 16+ (optional if using Docker)
- Redis 7+ (optional if using Docker)

## 🚀 Quick Start

### Option 1: Docker Compose (Recommended)

1. **Clone the repository**
```bash
git clone https://github.com/yourusername/authcore.git
cd authcore
```

2. **Start all services**
```bash
docker-compose up -d --build
```

3. **Apply database migrations**
```bash
docker-compose exec api dotnet ef database update --project src/AuthCore.Infrastructure
```

4. **Access the API**
- Swagger UI: http://localhost:5000
- API Base URL: http://localhost:5000/api

### Option 2: Local Development

1. **Install dependencies**
```bash
dotnet restore
```

2. **Update connection strings** in `src/AuthCore.API/appsettings.json`
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=authcore;Username=your_user;Password=your_password",
    "Redis": "localhost:6379"
  }
}
```

3. **Create database and apply migrations**
```bash
cd src/AuthCore.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../AuthCore.API
dotnet ef database update --startup-project ../AuthCore.API
```

4. **Run the API**
```bash
cd src/AuthCore.API
dotnet run
```

5. **Access the API**
- Swagger UI: https://localhost:7000 or http://localhost:5000
- API Documentation: https://localhost:7000/swagger

## 📚 API Endpoints

### Authentication

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/api/auth/register` | Register new user | No |
| POST | `/api/auth/login` | Login user | No |
| POST | `/api/auth/refresh` | Refresh access token | No |
| POST | `/api/auth/logout` | Revoke refresh token | Yes |
| GET | `/api/auth/me` | Get current user info | Yes |
| POST | `/api/auth/password-reset/request` | Request password reset | No |
| POST | `/api/auth/password-reset/confirm` | Confirm password reset | No |

### OAuth2

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/oauth/google` | Login with Google |
| GET | `/api/oauth/google/callback` | Google callback |
| GET | `/api/oauth/github` | Login with GitHub |
| GET | `/api/oauth/github/callback` | GitHub callback |

## 💡 Usage Examples

### Register a New User

```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecurePass123!",
    "firstName": "John",
    "lastName": "Doe",
    "tenantDomain": "acme"
  }'
```

### Login

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecurePass123!",
    "tenantDomain": "acme"
  }'
```

**Response:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "base64_encoded_refresh_token",
  "expiresAt": "2026-01-15T20:00:00Z",
  "user": {
    "id": "guid",
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "tenantDomain": "acme",
    "roles": ["User"],
    "permissions": ["users.read"]
  }
}
```

### Access Protected Endpoint

```bash
curl -X GET http://localhost:5000/api/auth/me \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

### Refresh Token

```bash
curl -X POST http://localhost:5000/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "YOUR_REFRESH_TOKEN"
  }'
```

## 🔐 Security Features

- **Password Hashing**: PBKDF2 with SHA256, 100,000 iterations
- **JWT Tokens**: HS256 algorithm with configurable expiration
- **Refresh Tokens**: Cryptographically secure random tokens stored in database
- **Rate Limiting**: 
  - Global: 60 requests/minute
  - Login: 5 attempts/minute
  - Register: 3 attempts/hour
- **Multi-tenant Isolation**: Data segregation by tenant
- **Token Revocation**: Blacklist tokens via Redis cache

## ⚙️ Configuration

### JWT Settings (appsettings.json)

```json
{
  "Jwt": {
    "Secret": "your-secret-key-min-32-chars",
    "Issuer": "AuthCore",
    "Audience": "AuthCore.Client",
    "AccessTokenExpirationMinutes": "15",
    "RefreshTokenExpirationDays": "7"
  }
}
```

### OAuth2 Configuration

1. **Google OAuth2**
   - Create credentials at [Google Cloud Console](https://console.cloud.google.com)
   - Add to `appsettings.json`:
   ```json
   {
     "OAuth": {
       "Google": {
         "ClientId": "your-google-client-id",
         "ClientSecret": "your-google-client-secret"
       }
     }
   }
   ```

2. **GitHub OAuth2**
   - Create OAuth App at [GitHub Settings](https://github.com/settings/developers)
   - Add to `appsettings.json`:
   ```json
   {
     "OAuth": {
       "GitHub": {
         "ClientId": "your-github-client-id",
         "ClientSecret": "your-github-client-secret"
       }
     }
   }
   ```

## 🧪 Testing

Run all tests:
```bash
dotnet test
```

Run with coverage:
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## 📦 Database Migrations

### Create a new migration
```bash
dotnet ef migrations add MigrationName --project src/AuthCore.Infrastructure --startup-project src/AuthCore.API
```

### Apply migrations
```bash
dotnet ef database update --project src/AuthCore.Infrastructure --startup-project src/AuthCore.API
```

### Rollback migration
```bash
dotnet ef database update PreviousMigrationName --project src/AuthCore.Infrastructure --startup-project src/AuthCore.API
```

## 🐳 Docker Commands

```bash
# Build and start all services
docker-compose up -d --build

# View logs
docker-compose logs -f api

# Stop all services
docker-compose down

# Reset database (remove volumes)
docker-compose down -v

# Restart a specific service
docker-compose restart api

# Execute commands in container
docker-compose exec api dotnet ef database update
```

## 📊 Default Roles & Permissions

| Role | Permissions |
|------|-------------|
| **Admin** | users.read, users.create, users.update, users.delete, roles.read, roles.manage |
| **User** | users.read |

## 🔄 Integration Guide

### As a NuGet Package

1. Package the Core and Infrastructure projects
2. Reference in your application
3. Configure services in Program.cs:

```csharp
builder.Services.AddAuthCore(builder.Configuration);
```

### As a Microservice

Deploy AuthCore as a standalone authentication service and integrate via HTTP:

```csharp
var client = new HttpClient { BaseAddress = new Uri("https://authcore.yourdomain.com") };
var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
```

## 🚢 Deployment

### Render.com (Free Tier)

1. Create new Web Service
2. Connect GitHub repository
3. Set build command: `docker build -t authcore .`
4. Add environment variables
5. Deploy

### Railway.app

1. Create new project from GitHub
2. Add PostgreSQL and Redis plugins
3. Configure environment variables
4. Deploy automatically

### Azure App Service

1. Create App Service (B1 tier)
2. Create Azure Database for PostgreSQL
3. Create Azure Cache for Redis
4. Deploy via GitHub Actions or Azure DevOps

## 💰 Monetization Opportunities

- **SaaS Model**: $9-49/month per tenant
- **White-Label Licensing**: $299-999 one-time
- **Integration Consulting**: $500-2,000 per project
- **Premium Support**: $99/month

## 🤝 Contributing

1. Fork the repository
2. Create feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Open Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

- Documentation: [Wiki](https://github.com/yourusername/authcore/wiki)
- Issues: [GitHub Issues](https://github.com/yourusername/authcore/issues)
- Email: support@authcore.dev

## 🗺️ Roadmap

- [ ] Two-Factor Authentication (2FA)
- [ ] TOTP/SMS support
- [ ] Biometric authentication
- [ ] SAML/SSO support
- [ ] Admin dashboard UI
- [ ] Audit logging
- [ ] API key management
- [ ] Webhook notifications

## ⭐ Acknowledgments

Built with .NET 8, Entity Framework Core, PostgreSQL, and Redis.

---

**Made with ❤️ for the developer community**
