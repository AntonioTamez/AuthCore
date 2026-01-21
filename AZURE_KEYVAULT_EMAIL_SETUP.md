# Azure Key Vault - Configuración de Email

Esta guía explica cómo configurar los secretos de Email en Azure Key Vault para AuthCore.

## Contexto

Tu aplicación ya tiene configurado Azure Key Vault en `Program.cs`:

```csharp
var keyVaultName = builder.Configuration.GetValue<string>("KeyVaultName")!;
var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");

builder.Configuration.AddAzureKeyVault(keyVaultUri, new Azure.Identity.DefaultAzureCredential(),
    new AzureKeyVaultConfigurationOptions { 
        ReloadInterval = TimeSpan.FromMinutes(1)
    });
```

## Secretos Necesarios

Para que el servicio de Email funcione, necesitas agregar estos secretos en tu Key Vault:

| Configuración en appsettings.json | Nombre en Key Vault | Valor de Ejemplo |
|----------------------------------|---------------------|------------------|
| `Email:SmtpUser` | `Email--SmtpUser` | `?` |
| `Email:SmtpPassword` | `Email--SmtpPassword` | `?` |
| `Email:FromEmail` | `Email--FromEmail` | `?` |

**Nota Importante:** Azure Key Vault NO permite el carácter `:` en los nombres de secretos. Por eso se usa `--` (doble guión) en lugar de `:`. ASP.NET Core automáticamente convierte `--` a `:` al leer la configuración.

## Opción 1: Agregar Secretos con Azure CLI (Recomendado)

### 1. Login a Azure

```bash
az login
```

### 2. Agregar Secretos

```bash
# Reemplaza 'prueballave' con el nombre de tu Key Vault (valor de KeyVaultName en appsettings.json)
$vaultName = "prueballave"

# Email SMTP User
az keyvault secret set `
    --vault-name $vaultName `
    --name "Email--SmtpUser" `
    --value "?"

# Email SMTP Password (App Password de Gmail)
az keyvault secret set `
    --vault-name $vaultName `
    --name "Email--SmtpPassword" `
    --value "?"

# Email From (correo que aparece como remitente)
az keyvault secret set `
    --vault-name $vaultName `
    --name "Email--FromEmail" `
    --value "?"
```

### 3. Verificar Secretos

```bash
# Listar todos los secretos
az keyvault secret list --vault-name $vaultName --query "[].name" -o table

# Ver un secreto específico (muestra el valor)
az keyvault secret show --vault-name $vaultName --name "Email--SmtpUser" --query "value" -o tsv
```

## Opción 2: Agregar Secretos desde Azure Portal

### 1. Abrir Key Vault

1. Ve a [portal.azure.com](https://portal.azure.com)
2. Busca tu Key Vault: `prueballave`
3. Click en tu Key Vault

### 2. Crear Secretos

1. En el menú izquierdo, click en **Secrets**
2. Click en **+ Generate/Import**
3. Configuración para cada secreto:

**Secreto 1: Email--SmtpUser**
- Upload options: **Manual**
- Name: `Email--SmtpUser`
- Value: `?`
- Content type: (dejar vacío)
- Enabled: **Yes**
- Click **Create**

**Secreto 2: Email--SmtpPassword**
- Upload options: **Manual**
- Name: `Email--SmtpPassword`
- Value: `?`
- Content type: (dejar vacío)
- Enabled: **Yes**
- Click **Create**

**Secreto 3: Email--FromEmail**
- Upload options: **Manual**
- Name: `Email--FromEmail`
- Value: `?`
- Content type: (dejar vacío)
- Enabled: **Yes**
- Click **Create**

## Opción 3: Script PowerShell Automatizado

Guarda este script como `setup-email-secrets.ps1`:

```powershell
# Script para configurar secretos de Email en Azure Key Vault
param(
    [Parameter(Mandatory=$true)]
    [string]$VaultName,
    
    [Parameter(Mandatory=$true)]
    [string]$SmtpUser,
    
    [Parameter(Mandatory=$true)]
    [string]$SmtpPassword,
    
    [Parameter(Mandatory=$true)]
    [string]$FromEmail
)

Write-Host "[*] Configurando secretos de Email en Key Vault: $VaultName" -ForegroundColor Cyan

# Verificar que el usuario está logueado
$account = az account show 2>$null
if (-not $account) {
    Write-Host "No has iniciado sesión en Azure. Ejecutando 'az login'..." -ForegroundColor Yellow
    az login
}

# Agregar secretos
Write-Host "`n[*] Agregando Email--SmtpUser..." -ForegroundColor Yellow
az keyvault secret set --vault-name $VaultName --name "Email--SmtpUser" --value $SmtpUser

Write-Host "[*] Agregando Email--SmtpPassword..." -ForegroundColor Yellow
az keyvault secret set --vault-name $VaultName --name "Email--SmtpPassword" --value $SmtpPassword

Write-Host "[*] Agregando Email--FromEmail..." -ForegroundColor Yellow
az keyvault secret set --vault-name $VaultName --name "Email--FromEmail" --value $FromEmail

Write-Host "`n[SUCCESS] Secretos configurados correctamente!" -ForegroundColor Green

# Listar secretos
Write-Host "`n[*] Secretos en el Key Vault:" -ForegroundColor Cyan
az keyvault secret list --vault-name $VaultName --query "[].name" -o table
```

Ejecutar el script:

```powershell
.\setup-email-secrets.ps1 `
    -VaultName "prueballave" `
    -SmtpUser "?" `
    -SmtpPassword "?" `
    -FromEmail "?"
```

## Verificación

### 1. Verificar que la Aplicación Tiene Acceso

Tu aplicación usa `DefaultAzureCredential()` que intenta autenticar en este orden:

1. **Environment Credentials** (variables de entorno)
2. **Managed Identity** (cuando está en Azure App Service)
3. **Visual Studio Credentials**
4. **Azure CLI Credentials** (si hiciste `az login`)
5. **Azure PowerShell Credentials**

Para desarrollo local, asegúrate de estar logueado:

```bash
az login
```

### 2. Probar la Configuración

Ejecuta tu aplicación localmente:

```bash
dotnet run --project src/AuthCore.API
```

Si todo está bien configurado:
- La aplicación se conectará al Key Vault
- Obtendrá los secretos automáticamente
- Los valores estarán disponibles en `IConfiguration`

### 3. Verificar en Logs

Cuando la aplicación inicie, deberías ver en los logs que se conectó al Key Vault. Si hay errores de autenticación, verás mensajes como:

```
Azure.Identity.CredentialUnavailableException: DefaultAzureCredential failed to retrieve a token
```

Si ves ese error, verifica:
- Que ejecutaste `az login`
- Que tu usuario tiene permisos en el Key Vault
- Que el nombre del Key Vault es correcto en `appsettings.json`

## Permisos Necesarios

Tu cuenta de Azure necesita estos permisos en el Key Vault:

- **Secret: Get** (leer secretos)
- **Secret: List** (listar secretos)

### Dar Permisos con Azure CLI

```bash
# Obtener tu Object ID (identificador de usuario)
$userId = az ad signed-in-user show --query id -o tsv

# Dar permisos al Key Vault
az keyvault set-policy `
    --name "prueballave" `
    --object-id $userId `
    --secret-permissions get list
```

### Dar Permisos desde Azure Portal

1. Ve a tu Key Vault en Azure Portal
2. Click en **Access policies**
3. Click **+ Add Access Policy**
4. Secret permissions: Selecciona **Get** y **List**
5. Select principal: Busca tu cuenta de usuario
6. Click **Add**
7. Click **Save** (importante)

## Configuración para Producción (Azure App Service)

Cuando despliegues a Azure App Service, necesitas:

### 1. Habilitar Managed Identity

```bash
az webapp identity assign `
    --name "tu-app-name" `
    --resource-group "tu-resource-group"
```

### 2. Dar Permisos al App Service

```bash
# Obtener el principalId del App Service
$appPrincipalId = az webapp identity show `
    --name "tu-app-name" `
    --resource-group "tu-resource-group" `
    --query principalId -o tsv

# Dar permisos
az keyvault set-policy `
    --name "prueballave" `
    --object-id $appPrincipalId `
    --secret-permissions get list
```

## Secretos Adicionales (Opcional)

Si quieres, también puedes mover otros valores sensibles al Key Vault:

### JWT Secret

```bash
az keyvault secret set `
    --vault-name "prueballave" `
    --name "Jwt--Secret" `
    --value "?"
```

### OAuth Credentials

```bash
# Google OAuth
az keyvault secret set --vault-name "prueballave" --name "OAuth--Google--ClientId" --value "tu-client-id"
az keyvault secret set --vault-name "prueballave" --name "OAuth--Google--ClientSecret" --value "tu-client-secret"

# GitHub OAuth
az keyvault secret set --vault-name "prueballave" --name "OAuth--GitHub--ClientId" --value "tu-client-id"
az keyvault secret set --vault-name "prueballave" --name "OAuth--GitHub--ClientSecret" --value "tu-client-secret"
```

### Connection Strings

```bash
# PostgreSQL
az keyvault secret set `
    --vault-name "prueballave" `
    --name "ConnectionStrings--DefaultConnection" `
    --value "Host=your-host;Database=authcore;Username=user;Password=password"

# Redis
az keyvault secret set `
    --vault-name "prueballave" `
    --name "ConnectionStrings--Redis" `
    --value "your-redis-connection-string"
```

## Troubleshooting

### Error: "Azure.RequestFailedException: The user, group or application does not have secrets get permission"

**Solución:** Ejecuta el comando para dar permisos (ver sección "Permisos Necesarios")

### Error: "Secret not found"

**Solución:** Verifica que el nombre del secreto usa `--` en lugar de `:`. Ejemplo: `Email--SmtpUser` NO `Email:SmtpUser`

### Error: "DefaultAzureCredential failed to retrieve a token"

**Solución:** 
1. Ejecuta `az login`
2. Verifica que el nombre del Key Vault es correcto
3. Verifica que tienes permisos en el Key Vault

### La aplicación no usa los valores del Key Vault

**Solución:**
1. Verifica que `KeyVaultName` en `appsettings.json` tiene el nombre correcto
2. Verifica que la sección de Azure Key Vault en `Program.cs` está descomentada
3. Verifica que el `ReloadInterval` no está causando demoras (puedes removerlo para testing)

## Testing

### Probar Envío de Email con Password Reset

```bash
# Endpoint para solicitar reset de password
curl -X POST http://localhost:5000/api/auth/password-reset/request \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "tenantDomain": "example.com"
  }'
```

Si todo funciona, deberías recibir un email en la cuenta configurada.

## Resumen

1. ✅ Agrega los 3 secretos en Azure Key Vault con nombres que usan `--`
2. ✅ Asegúrate de tener permisos (Get + List)
3. ✅ Ejecuta `az login` para desarrollo local
4. ✅ La aplicación obtiene los secretos automáticamente
5. ✅ Los valores ya NO están en `appsettings.json` (más seguro)

## Ventajas de Usar Key Vault

- 🔐 **Seguridad**: Credenciales no están en el código
- 🔄 **Rotación**: Puedes cambiar passwords sin redeployar
- 📝 **Auditoría**: Azure registra quién accede a los secretos
- 🌍 **Multi-entorno**: Diferentes Key Vaults para Dev/Prod
- 👥 **Compartir**: Múltiples apps pueden usar los mismos secretos
