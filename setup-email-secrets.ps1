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

# Verificar que el usuario esta logueado
$account = az account show 2>$null
if (-not $account) {
    Write-Host "No has iniciado sesion en Azure. Ejecutando 'az login'..." -ForegroundColor Yellow
    az login
}

# Verificar que el Key Vault existe
Write-Host "`n[*] Verificando que el Key Vault existe..." -ForegroundColor Yellow
$vaultExists = az keyvault show --name $VaultName 2>$null
if (-not $vaultExists) {
    Write-Host "[ERROR] El Key Vault '$VaultName' no existe." -ForegroundColor Red
    Write-Host "Por favor verifica el nombre o crea el Key Vault primero." -ForegroundColor Yellow
    exit 1
}

Write-Host "[OK] Key Vault encontrado." -ForegroundColor Green

# Agregar secretos
Write-Host "`n[*] Agregando Email--SmtpUser..." -ForegroundColor Yellow
az keyvault secret set --vault-name $VaultName --name "Email--SmtpUser" --value $SmtpUser | Out-Null

Write-Host "[*] Agregando Email--SmtpPassword..." -ForegroundColor Yellow
az keyvault secret set --vault-name $VaultName --name "Email--SmtpPassword" --value $SmtpPassword | Out-Null

Write-Host "[*] Agregando Email--FromEmail..." -ForegroundColor Yellow
az keyvault secret set --vault-name $VaultName --name "Email--FromEmail" --value $FromEmail | Out-Null

Write-Host "`n[SUCCESS] Secretos configurados correctamente!" -ForegroundColor Green

# Listar secretos
Write-Host "`n[*] Secretos actuales en el Key Vault:" -ForegroundColor Cyan
az keyvault secret list --vault-name $VaultName --query "[].name" -o table

Write-Host "`n[*] Notas importantes:" -ForegroundColor Cyan
Write-Host "1. Asegúrate de que tu aplicación tiene permisos para leer secretos" -ForegroundColor White
Write-Host "2. Para desarrollo local, ejecuta 'az login' si aún no lo has hecho" -ForegroundColor White
Write-Host "3. Para producción, habilita Managed Identity en tu App Service" -ForegroundColor White
Write-Host "`n[*] Documentación completa: AZURE_KEYVAULT_EMAIL_SETUP.md" -ForegroundColor Yellow
