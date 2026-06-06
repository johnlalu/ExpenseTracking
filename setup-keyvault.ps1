#!/usr/bin/env pwsh
# Azure Key Vault Setup Script for Expense Reimbursement API
# This script creates secrets in Azure Key Vault for database connection and authentication

param(
    [Parameter(Mandatory = $true, HelpMessage = "Name of the Key Vault resource")]
    [string]$KeyVaultName,
    
    [Parameter(Mandatory = $true, HelpMessage = "Cosmos DB endpoint URI")]
    [string]$CosmosDbEndpoint,
    
    [Parameter(Mandatory = $true, HelpMessage = "Cosmos DB primary key")]
    [string]$CosmosDbPrimaryKey,
    
    [Parameter(Mandatory = $true, HelpMessage = "JWT secret key (32+ characters)")]
    [string]$JwtSecretKey,
    
    [Parameter(HelpMessage = "Application Insights instrumentation key")]
    [string]$AppInsightsKey = ""
)

Write-Host "Azure Key Vault Setup Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Validate inputs
if ($JwtSecretKey.Length -lt 32) {
    Write-Host "ERROR: JWT Secret Key must be at least 32 characters long" -ForegroundColor Red
    exit 1
}

if (-not $CosmosDbEndpoint.StartsWith("https://")) {
    Write-Host "ERROR: Cosmos DB endpoint must start with 'https://'" -ForegroundColor Red
    exit 1
}

# Check if user is logged in to Azure
try {
    $currentContext = az account show 2>$null
    if ($null -eq $currentContext) {
        Write-Host "You are not logged into Azure CLI. Running 'az login'..." -ForegroundColor Yellow
        az login
    }
}
catch {
    Write-Host "ERROR: Failed to check Azure login status" -ForegroundColor Red
    exit 1
}

# Verify Key Vault exists
Write-Host "Verifying Key Vault exists: $KeyVaultName" -ForegroundColor Yellow
try {
    $vault = az keyvault show --name $KeyVaultName 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Key Vault '$KeyVaultName' not found" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "ERROR: Failed to access Key Vault" -ForegroundColor Red
    exit 1
}

Write-Host "Key Vault verified!" -ForegroundColor Green
Write-Host ""

# Function to set a secret with error handling
function Set-KeyVaultSecret {
    param(
        [string]$Name,
        [string]$Value,
        [string]$Description
    )
    
    Write-Host "Setting secret: $Name" -ForegroundColor Cyan
    
    try {
        az keyvault secret set `
            --vault-name $KeyVaultName `
            --name $Name `
            --value $Value `
            --description $Description | Out-Null
        
        Write-Host "  ✓ Secret '$Name' set successfully" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "  ✗ Failed to set secret '$Name': $_" -ForegroundColor Red
        return $false
    }
}

# Set all secrets
$secrets = @(
    @{
        Name = "CosmosDb--EndpointUri"
        Value = $CosmosDbEndpoint
        Description = "Azure Cosmos DB endpoint URI"
    },
    @{
        Name = "CosmosDb--PrimaryKey"
        Value = $CosmosDbPrimaryKey
        Description = "Azure Cosmos DB primary key for authentication"
    },
    @{
        Name = "Jwt--SecretKey"
        Value = $JwtSecretKey
        Description = "JWT signing secret key (minimum 32 characters)"
    }
)

if (-not [string]::IsNullOrEmpty($AppInsightsKey)) {
    $secrets += @{
        Name = "ApplicationInsights--InstrumentationKey"
        Value = $AppInsightsKey
        Description = "Application Insights instrumentation key"
    }
}

Write-Host "Setting up secrets in Key Vault..." -ForegroundColor Yellow
Write-Host ""

$successCount = 0
foreach ($secret in $secrets) {
    if (Set-KeyVaultSecret -Name $secret.Name -Value $secret.Value -Description $secret.Description) {
        $successCount++
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Setup Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Key Vault: $KeyVaultName" -ForegroundColor Green
Write-Host "Secrets created: $successCount/$($secrets.Count)" -ForegroundColor Green

if ($successCount -eq $secrets.Count) {
    Write-Host ""
    Write-Host "✓ All secrets configured successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "  1. Verify secrets exist: az keyvault secret list --vault-name $KeyVaultName" -ForegroundColor White
    Write-Host "  2. Set up Access Policies for your app (Managed Identity or Service Principal)" -ForegroundColor White
    Write-Host "  3. Update appsettings.json with KeyVault URL: https://$KeyVaultName.vault.azure.net/" -ForegroundColor White
    Write-Host "  4. Deploy to Azure App Service with Managed Identity enabled" -ForegroundColor White
    exit 0
}
else {
    Write-Host ""
    Write-Host "✗ Some secrets failed to configure" -ForegroundColor Red
    exit 1
}
