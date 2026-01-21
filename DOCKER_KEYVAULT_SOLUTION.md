# Solución: Azure Key Vault con Docker

## Problema

`DefaultAzureCredential` no puede autenticarse dentro de contenedores Docker para desarrollo local porque:
- No hay `az login` ejecutado dentro del contenedor
- No hay Visual Studio credentials
- No hay Managed Identity (solo existe en Azure)
- No hay variables de entorno de Service Principal

## Solución Implementada

### 1. Key Vault Solo en Producción

El código en `Program.cs` ahora usa Key Vault **únicamente en producción**:

```csharp
if (builder.Environment.IsProduction())
{
    var keyVaultName = builder.Configuration.GetValue<string>("KeyVaultName")!;
    var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");
    
    builder.Configuration.AddAzureKeyVault(keyVaultUri, new Azure.Identity.DefaultAzureCredential(),
        new AzureKeyVaultConfigurationOptions { 
            ReloadInterval = TimeSpan.FromDays(1)
        });
}
```

### 2. Variables de Entorno en Docker

Para desarrollo local, `docker-compose.yml` ahora incluye todas las variables necesarias:

```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Development
  - Jwt__Secret=YourSuperSecretKeyThatIsAtLeast32CharactersLongForHS256Algorithm
  - Email__SmtpUser=antonio.tamez.s@gmail.com
  - Email__SmtpPassword=ocwh ucfs dfjb rtzz
  - Email__FromEmail=antonio.tamez.s@gmail.com
  # ... otras variables
```

## Cómo Funciona por Entorno

### Desarrollo Local (dotnet run)

```bash
dotnet run --project src/AuthCore.API
```

- **Environment**: Development
- **Key Vault**: ✅ Activado (si tienes `az login`)
- **Configuración**: Lee de Key Vault + appsettings.Development.json

### Desarrollo con Docker (docker-compose)

```bash
docker-compose up -d --build
```

- **Environment**: Development (definido en docker-compose.yml)
- **Key Vault**: ❌ Desactivado (no es producción)
- **Configuración**: Lee de variables de entorno del docker-compose.yml

### Producción (Azure App Service)

```bash
# Deploy a Azure
```

- **Environment**: Production (configurado en Azure)
- **Key Vault**: ✅ Activado (usa Managed Identity)
- **Configuración**: Lee de Key Vault + App Settings de Azure

## Ejecutar Ahora

```bash
# Detener contenedores actuales
docker-compose down

# Reconstruir y ejecutar
docker-compose up -d --build

# Ver logs
docker-compose logs -f api

# Verificar Swagger
# http://localhost:5000/swagger
```

## Alternativa: Usar Key Vault en Docker (Avanzado)

Si **realmente** necesitas usar Key Vault en Docker local, puedes usar un Service Principal:

### Paso 1: Crear Service Principal

```bash
az ad sp create-for-rbac --name "authcore-docker-dev" --skip-assignment
```

Guarda el output:
```json
{
  "appId": "xxx",
  "password": "yyy",
  "tenant": "zzz"
}
```

### Paso 2: Dar Permisos al Service Principal

```bash
# Obtener el objectId del Service Principal
$spObjectId = az ad sp show --id "APP_ID_DEL_PASO_1" --query id -o tsv

# Dar permisos en Key Vault
az keyvault set-policy \
  --name "prueballave" \
  --object-id $spObjectId \
  --secret-permissions get list
```

### Paso 3: Agregar Variables en docker-compose.yml

```yaml
environment:
  - AZURE_CLIENT_ID=xxx  # appId del paso 1
  - AZURE_CLIENT_SECRET=yyy  # password del paso 1
  - AZURE_TENANT_ID=zzz  # tenant del paso 1
```

### Paso 4: Remover el if de Producción

```csharp
// Cambiar en Program.cs - quitar el if
var keyVaultName = builder.Configuration.GetValue<string>("KeyVaultName")!;
var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");

builder.Configuration.AddAzureKeyVault(keyVaultUri, new Azure.Identity.DefaultAzureCredential(),
    new AzureKeyVaultConfigurationOptions { 
        ReloadInterval = TimeSpan.FromDays(1)
    });
```

**⚠️ Nota:** Esta opción es más compleja y no es necesaria para desarrollo local. Solo úsala si realmente necesitas probar Key Vault en Docker.

## Recomendación

**Para desarrollo local:**
- Usa `dotnet run` (no Docker) si quieres probar Key Vault
- Usa `docker-compose` con variables de entorno para desarrollo normal

**Para producción:**
- Key Vault con Managed Identity (ya está configurado)

## Verificación

Después de ejecutar `docker-compose up -d --build`:

```bash
# Ver logs
docker-compose logs api

# Debe mostrar:
# - info: Microsoft.Hosting.Lifetime[14]
# - Now listening on: http://[::]:80
# - Application started. Press Ctrl+C to shut down.
```

Si ves errores de Azure Key Vault, verifica que `ASPNETCORE_ENVIRONMENT=Development` está en docker-compose.yml (no Production).

## Resumen

| Entorno | Key Vault | Autenticación | Configuración |
|---------|-----------|---------------|---------------|
| **dotnet run** | ✅ | az login | Key Vault + appsettings.Development.json |
| **docker-compose** | ❌ | N/A | Variables de entorno |
| **Azure App Service** | ✅ | Managed Identity | Key Vault + App Settings |
