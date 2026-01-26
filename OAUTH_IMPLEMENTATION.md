# OAuth Implementation - AuthCore

## Implementación Completa de Google y GitHub OAuth

### Cambios Realizados

#### 1. Nuevo DTO: `OAuthLoginRequest`
**Ubicación:** `src/AuthCore.Core/DTOs/OAuthLoginRequest.cs`

```csharp
public class OAuthLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty; // "Google" or "GitHub"
    public string TenantDomain { get; set; } = string.Empty;
}
```

#### 2. Nuevo Método en `IAuthService`
**Ubicación:** `src/AuthCore.Core/Interfaces/IAuthService.cs`

```csharp
Task<AuthResponse> OAuthLoginAsync(OAuthLoginRequest request, string ipAddress);
```

#### 3. Implementación en `AuthService`
**Ubicación:** `src/AuthCore.Infrastructure/Services/AuthService.cs`

El método `OAuthLoginAsync` realiza:

1. **Validación del email** (requerido de OAuth provider)
2. **Búsqueda de usuario existente** por email
3. **Si el usuario NO existe:**
   - Obtiene o crea el Tenant basado en `tenantDomain`
   - Extrae nombre y apellido del nombre completo
   - Crea nuevo usuario con:
     - `PasswordHash` vacío (no usa password)
     - `EmailConfirmed = true` (OAuth verifica el email)
     - `IsActive = true`
   - Asigna rol "User" por defecto
4. **Si el usuario YA existe:**
   - Verifica que esté activo
   - Confirma el email automáticamente si no estaba confirmado
5. **Genera tokens JWT** (access + refresh)
6. **Retorna `AuthResponse`** con tokens y datos del usuario

#### 4. OAuthController Actualizado
**Ubicación:** `src/AuthCore.API/Controllers/OAuthController.cs`

**Cambios principales:**
- Inyecta `IAuthService`
- Acepta parámetro opcional `tenantDomain`
- Integra con `AuthService.OAuthLoginAsync`
- Retorna `AuthResponse` con tokens JWT
- Manejo completo de errores

---

## Flujo de Autenticación OAuth

### Google Login

```
1. Usuario navega a: GET /api/oauth/google?tenantDomain=example.com
2. Redirect a Google para autenticación
3. Usuario se autentica en Google
4. Google redirect a: GET /api/oauth/google/callback?tenantDomain=example.com
5. AuthCore:
   - Extrae email y nombre de Google claims
   - Llama a AuthService.OAuthLoginAsync
   - Crea usuario si no existe
   - Genera JWT tokens
   - Retorna AuthResponse con tokens
```

### GitHub Login

```
1. Usuario navega a: GET /api/oauth/github?tenantDomain=example.com
2. Redirect a GitHub para autenticación
3. Usuario se autentica en GitHub
4. GitHub redirect a: GET /api/oauth/github/callback?tenantDomain=example.com
5. AuthCore:
   - Extrae email y nombre de GitHub claims
   - Llama a AuthService.OAuthLoginAsync
   - Crea usuario si no existe
   - Genera JWT tokens
   - Retorna AuthResponse con tokens
```

---

## Cómo Usar

### 1. Configurar Credenciales OAuth

#### Google OAuth
1. Ve a [Google Cloud Console](https://console.cloud.google.com)
2. Crea o selecciona un proyecto
3. Habilita Google+ API
4. Crea credenciales OAuth 2.0
5. Agregar URIs autorizados:
   - `http://localhost:5000` (desarrollo)
   - `https://your-domain.com` (producción)
6. Agregar URIs de redirección:
   - `http://localhost:5000/signin-google`
   - `https://your-domain.com/signin-google`

#### GitHub OAuth
1. Ve a [GitHub Developer Settings](https://github.com/settings/developers)
2. Click en "New OAuth App"
3. Configuración:
   - Application name: AuthCore
   - Homepage URL: `http://localhost:5000`
   - Authorization callback URL: `http://localhost:5000/signin-github`

### 2. Configurar en `appsettings.json` o Key Vault

```json
{
  "OAuth": {
    "Google": {
      "ClientId": "your-google-client-id",
      "ClientSecret": "your-google-client-secret"
    },
    "GitHub": {
      "ClientId": "your-github-client-id",
      "ClientSecret": "your-github-client-secret"
    }
  }
}
```

O en Azure Key Vault:
```bash
az keyvault secret set --vault-name "your-vault" --name "OAuth--Google--ClientId" --value "your-client-id"
az keyvault secret set --vault-name "your-vault" --name "OAuth--Google--ClientSecret" --value "your-client-secret"
az keyvault secret set --vault-name "your-vault" --name "OAuth--GitHub--ClientId" --value "your-client-id"
az keyvault secret set --vault-name "your-vault" --name "OAuth--GitHub--ClientSecret" --value "your-client-secret"
```

### 3. Ejecutar la Aplicación

```bash
dotnet run --project src/AuthCore.API
```

### 4. Probar OAuth Login

#### En el Navegador:

**Google:**
```
http://localhost:5000/api/oauth/google?tenantDomain=mycompany.com
```

**GitHub:**
```
http://localhost:5000/api/oauth/github?tenantDomain=mycompany.com
```

**Nota:** Si no proporcionas `tenantDomain`, se usará el dominio del email (ej: si el email es `user@gmail.com`, el tenant será `gmail.com`).

---

## Response Example

Después de autenticación exitosa, recibes:

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "ABC123XYZ...",
  "expiresAt": "2026-01-30T10:00:00Z",
  "user": {
    "id": "guid",
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "tenantDomain": "example.com",
    "roles": ["User"],
    "permissions": ["read:own_profile", "update:own_profile"]
  }
}
```

---

## Frontend Integration

### React Example

```javascript
// Iniciar OAuth flow
const handleGoogleLogin = () => {
  const tenantDomain = 'mycompany.com';
  window.location.href = `http://localhost:5000/api/oauth/google?tenantDomain=${tenantDomain}`;
};

// En la página de callback (opcional)
// OAuth controller ya maneja el callback automáticamente
```

### Angular Example

```typescript
// Iniciar OAuth flow
loginWithGoogle() {
  const tenantDomain = 'mycompany.com';
  window.location.href = `http://localhost:5000/api/oauth/google?tenantDomain=${tenantDomain}`;
}
```

### Almacenar Tokens

Después del callback, AuthCore retorna los tokens. Puedes:

1. **Opción 1:** Redirigir a un frontend URL con tokens como query params
2. **Opción 2:** Usar un popup window y postMessage
3. **Opción 3:** Guardar en cookies (más seguro)

**Ejemplo modificando el callback:**

```csharp
// En OAuthController.cs - GoogleCallback
// En lugar de: return Ok(authResponse);
// Puedes redirigir:
var redirectUrl = $"http://localhost:3000/oauth/callback?token={authResponse.AccessToken}&refresh={authResponse.RefreshToken}";
return Redirect(redirectUrl);
```

---

## Diferencias con Login Normal

| Característica | OAuth Login | Login Normal |
|---------------|-------------|---------------|
| **Password** | No requerido | Requerido |
| **Email Verification** | Automático | Manual con email |
| **User Creation** | Automática | Registro separado |
| **Provider** | Google/GitHub | AuthCore |
| **Tokens JWT** | ✅ Generados | ✅ Generados |

---

## Seguridad

### ✅ Implementado

1. **Email verification automática** - OAuth providers verifican el email
2. **Tenant isolation** - Usuarios asociados a tenants
3. **JWT tokens** - Access + Refresh tokens
4. **Role-based access** - Rol "User" por defecto
5. **IP tracking** - Se registra IP en refresh tokens

### ⚠️ Consideraciones

1. **Usuarios OAuth no tienen password** - Si quieren login normal después, deben usar password reset
2. **Email único por tenant** - Un email solo puede existir una vez por tenant
3. **Provider trust** - Confiamos en Google/GitHub para verificar identidad

---

## Troubleshooting

### Error: "Email not provided by Google/GitHub"

**Solución:** Asegúrate de solicitar el scope `email` en la configuración OAuth.

En `Program.cs`, verifica:
```csharp
.AddGoogle(options =>
{
    options.ClientId = configuration["OAuth:Google:ClientId"];
    options.ClientSecret = configuration["OAuth:Google:ClientSecret"];
    options.Scope.Add("email"); // ✅ Asegúrate que está
    options.Scope.Add("profile");
});
```

### Error: "redirect_uri_mismatch"

**Solución:** Los URIs en Google/GitHub Console deben coincidir exactamente con tu app.

**Google Console URIs:**
- Authorized redirect URIs: `http://localhost:5000/signin-google`

**GitHub OAuth App:**
- Authorization callback URL: `http://localhost:5000/signin-github`

### Usuario se crea pero no tiene roles

**Solución:** Asegúrate de tener el rol "User" en la base de datos.

```sql
-- Verificar roles
SELECT * FROM "Roles";

-- Crear rol si no existe
INSERT INTO "Roles" ("Id", "Name", "Description", "IsActive", "CreatedAt")
VALUES (gen_random_uuid(), 'User', 'Default user role', true, NOW());
```

### Tokens no se están generando

**Solución:** Verifica que `Jwt:Secret` esté configurado y sea >= 32 caracteres.

```json
{
  "Jwt": {
    "Secret": "YourSuperSecretKeyThatIsAtLeast32CharactersLongForHS256Algorithm"
  }
}
```

---

## Testing

### 1. Test con cURL (no funciona directamente)

OAuth requiere un navegador porque Google/GitHub redireccionan al usuario. No puedes hacer OAuth con cURL directamente.

### 2. Test en Navegador

```
http://localhost:5000/api/oauth/google?tenantDomain=test.com
```

### 3. Test Automático (Unit Tests)

Puedes crear unit tests para `OAuthLoginAsync`:

```csharp
[Fact]
public async Task OAuthLoginAsync_CreatesNewUser_WhenUserDoesNotExist()
{
    // Arrange
    var request = new OAuthLoginRequest
    {
        Email = "newuser@gmail.com",
        Name = "New User",
        Provider = "Google",
        TenantDomain = "gmail.com"
    };

    // Act
    var response = await _authService.OAuthLoginAsync(request, "127.0.0.1");

    // Assert
    Assert.NotNull(response);
    Assert.NotEmpty(response.AccessToken);
    Assert.Equal("newuser@gmail.com", response.User.Email);
}
```

---

## Endpoints Disponibles

### Google OAuth
- **Inicio:** `GET /api/oauth/google?tenantDomain={domain}`
- **Callback:** `GET /api/oauth/google/callback` (automático)

### GitHub OAuth
- **Inicio:** `GET /api/oauth/github?tenantDomain={domain}`
- **Callback:** `GET /api/oauth/github/callback` (automático)

---

## Próximos Pasos (Opcional)

### 1. Mejorar la Experiencia de Usuario

Redirigir a frontend después de callback:

```csharp
public async Task<IActionResult> GoogleCallback(string? tenantDomain = null)
{
    // ... código actual ...
    
    // Redirigir a frontend con tokens
    var frontendUrl = _configuration["AppSettings:FrontendUrl"] ?? "http://localhost:3000";
    return Redirect($"{frontendUrl}/auth/callback?token={authResponse.AccessToken}");
}
```

### 2. Usar Cookies en lugar de tokens en URL

Más seguro para SPAs:

```csharp
// Guardar tokens en cookies HttpOnly
Response.Cookies.Append("access_token", authResponse.AccessToken, new CookieOptions
{
    HttpOnly = true,
    Secure = true,
    SameSite = SameSiteMode.Strict,
    Expires = authResponse.ExpiresAt
});

return Redirect(frontendUrl);
```

### 3. Agregar más OAuth Providers

- Microsoft
- Facebook
- Twitter/X
- LinkedIn

---

## Resumen

✅ **OAuth Google y GitHub completamente funcionales**

**Características:**
- ✅ Crea usuarios automáticamente
- ✅ Genera JWT tokens (access + refresh)
- ✅ Integra con sistema de tenants
- ✅ Asigna roles por defecto
- ✅ Email verificado automáticamente
- ✅ Compatible con flujo normal de login/registro

**Para usar:**
1. Configura credenciales OAuth en Google/GitHub
2. Agrega ClientId y ClientSecret a appsettings.json
3. Navega a `/api/oauth/google` o `/api/oauth/github`
4. Usa los tokens JWT retornados para autenticación
