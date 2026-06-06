# Azure Key Vault - Quick Reference Commands

## Common Azure CLI Commands

### Key Vault Management

```bash
# Create Key Vault
az keyvault create \
  --resource-group <rg-name> \
  --name <vault-name> \
  --location <region>

# List all Key Vaults
az keyvault list

# Show Key Vault details
az keyvault show --name <vault-name>

# Delete Key Vault (soft delete)
az keyvault delete --name <vault-name>

# Purge deleted Key Vault (permanent)
az keyvault purge --name <vault-name>
```

### Secret Management

```bash
# Create/Update secret
az keyvault secret set \
  --vault-name <vault-name> \
  --name <secret-name> \
  --value <secret-value>

# Get secret value
az keyvault secret show \
  --vault-name <vault-name> \
  --name <secret-name> \
  --query "value" \
  --output tsv

# List all secrets
az keyvault secret list --vault-name <vault-name>

# Show secret metadata
az keyvault secret show \
  --vault-name <vault-name> \
  --name <secret-name>

# Delete secret (soft delete)
az keyvault secret delete \
  --vault-name <vault-name> \
  --name <secret-name>

# List deleted secrets
az keyvault secret list-deleted --vault-name <vault-name>

# Restore deleted secret
az keyvault secret recover \
  --vault-name <vault-name> \
  --name <secret-name>

# Purge secret permanently
az keyvault secret purge \
  --vault-name <vault-name> \
  --name <secret-name>
```

### Access Policies

```bash
# Set access policy for user/principal
az keyvault set-policy \
  --name <vault-name> \
  --object-id <principal-id> \
  --secret-permissions get list

# Remove access policy
az keyvault delete-policy \
  --name <vault-name> \
  --object-id <principal-id>

# List access policies
az keyvault show \
  --name <vault-name> \
  --query "properties.accessPolicies"
```

## App Service Integration

### Enable Managed Identity

```bash
# Enable system-assigned identity
az webapp identity assign \
  --resource-group <rg-name> \
  --name <app-service-name> \
  --query principalId \
  --output tsv

# Show identity details
az webapp identity show \
  --resource-group <rg-name> \
  --name <app-service-name>
```

### Grant Key Vault Access

```bash
# Grant permissions using principal ID
az keyvault set-policy \
  --name <vault-name> \
  --object-id <principal-id> \
  --secret-permissions get list

# For Service Principal
az keyvault set-policy \
  --name <vault-name> \
  --spn <client-id> \
  --secret-permissions get list
```

## Service Principal Setup

```bash
# Create Service Principal
az ad sp create-for-rbac \
  --name <sp-name> \
  --role Contributor \
  --scopes /subscriptions/<subscription-id>

# List Service Principals
az ad sp list

# Show Service Principal details
az ad sp show --id <service-principal-id>

# Delete Service Principal
az ad sp delete --id <service-principal-id>
```

## Azure Authentication

```bash
# Login to Azure
az login

# Login with specific tenant
az login --tenant <tenant-id>

# Show current account
az account show

# List all accounts
az account list

# Set default subscription
az account set --subscription <subscription-id>

# Get subscription ID
az account show --query "id" --output tsv

# Get tenant ID
az account show --query "tenantId" --output tsv
```

## Secrets Configuration (Quick Commands)

### Set Cosmos DB Secrets

```bash
# Set endpoint
az keyvault secret set \
  --vault-name expense-reimbursement-kv \
  --name "CosmosDb--EndpointUri" \
  --value "https://your-cosmos-db.documents.azure.com:443/"

# Set primary key
az keyvault secret set \
  --vault-name expense-reimbursement-kv \
  --name "CosmosDb--PrimaryKey" \
  --value "<your-cosmos-db-primary-key>"
```

### Set JWT Secret

```bash
az keyvault secret set \
  --vault-name expense-reimbursement-kv \
  --name "Jwt--SecretKey" \
  --value "<your-32-character-or-longer-secret-key>"
```

### Set Application Insights

```bash
az keyvault secret set \
  --vault-name expense-reimbursement-kv \
  --name "ApplicationInsights--InstrumentationKey" \
  --value "<your-app-insights-instrumentation-key>"
```

## Testing Commands

### Verify Secret Retrieval

```bash
# Test all required secrets exist
for secret in "CosmosDb--EndpointUri" "CosmosDb--PrimaryKey" "Jwt--SecretKey"
do
  echo "Testing $secret..."
  az keyvault secret show \
    --vault-name expense-reimbursement-kv \
    --name "$secret" \
    --query "value" \
    --output tsv > /dev/null && echo "✓ Found" || echo "✗ Missing"
done
```

### Test Application Health

```bash
# Ping endpoint
curl http://localhost:5000/health/ping

# Check configuration (development only)
curl http://localhost:5000/health/config | jq

# Check database config
curl http://localhost:5000/health/db-check | jq
```

## Environment Variables

### Set for Development (PowerShell)

```powershell
# Azure CLI credentials
$env:AZURE_CLIENT_ID = "your-client-id"
$env:AZURE_CLIENT_SECRET = "your-client-secret"
$env:AZURE_TENANT_ID = "your-tenant-id"

# ASP.NET environment
$env:ASPNETCORE_ENVIRONMENT = "Development"
```

### Set for Development (Bash)

```bash
# Azure CLI credentials
export AZURE_CLIENT_ID="your-client-id"
export AZURE_CLIENT_SECRET="your-client-secret"
export AZURE_TENANT_ID="your-tenant-id"

# ASP.NET environment
export ASPNETCORE_ENVIRONMENT="Development"
```

## Useful Queries

### Find Key Vault by Resource Group

```bash
az keyvault list --resource-group <rg-name>
```

### Get All Secrets with Metadata

```bash
az keyvault secret list \
  --vault-name <vault-name> \
  --query "[].{name:name, created:attributes.created, updated:attributes.updated}"
```

### Find which App Service uses a Key Vault

```bash
az resource list \
  --query "[?contains(appSettings.KeyVault.VaultUrl, '<vault-name>')]"
```

## PowerShell Automation

### Run Setup Script

```powershell
.\setup-keyvault.ps1 `
  -KeyVaultName "expense-reimbursement-kv" `
  -CosmosDbEndpoint "https://cosmos-db.documents.azure.com:443/" `
  -CosmosDbPrimaryKey "your-key" `
  -JwtSecretKey "your-32-char-key"
```

### Quick Secret Setter

```powershell
function Set-KeyVaultSecrets {
    param(
        [string]$KeyVaultName,
        [hashtable]$Secrets
    )
    
    foreach ($secret in $Secrets.GetEnumerator()) {
        Write-Host "Setting $($secret.Name)..." -ForegroundColor Cyan
        az keyvault secret set `
            --vault-name $KeyVaultName `
            --name $secret.Name `
            --value $secret.Value
    }
}

# Usage:
$secrets = @{
    "CosmosDb--EndpointUri" = "https://..."
    "CosmosDb--PrimaryKey" = "..."
    "Jwt--SecretKey" = "..."
}

Set-KeyVaultSecrets -KeyVaultName "expense-reimbursement-kv" -Secrets $secrets
```

## Troubleshooting Commands

### Check Identity Permissions

```bash
# Get Managed Identity object ID
PRINCIPAL_ID=$(az webapp identity show \
  --resource-group <rg> \
  --name <app-name> \
  --query principalId \
  --output tsv)

# Check access policy
az keyvault show \
  --name <vault-name> \
  --query "properties.accessPolicies[?objectId=='$PRINCIPAL_ID']"
```

### List Access Logs

```bash
az monitor activity-log list \
  --resource-group <rg> \
  --resource <vault-name> \
  --resource-type "Microsoft.KeyVault/vaults"
```

### Test Azure Authentication

```bash
# Verify current login
az account show

# List available credentials
az account list

# Re-authenticate if needed
az login --use-device-code
```

## Cosmos DB Integration

### Get Cosmos DB Connection Details

```bash
# Get Cosmos DB account details
az cosmosdb show \
  --resource-group <rg> \
  --name <account-name>

# Get connection string
az cosmosdb keys list \
  --resource-group <rg> \
  --name <account-name> \
  --query "primaryMasterKey" \
  --output tsv

# Get endpoint URI
az cosmosdb show \
  --resource-group <rg> \
  --name <account-name> \
  --query "documentEndpoint" \
  --output tsv
```

## Useful Aliases (Bash)

```bash
# Add to ~/.bashrc or ~/.zshrc

# Key Vault shortcuts
alias kv-list='az keyvault list'
alias kv-secrets='az keyvault secret list --vault-name'
alias kv-get='az keyvault secret show --vault-name'

# Azure shortcuts
alias az-login='az login'
alias az-account='az account show'
alias az-sub='az account list --query "[].{name:name, id:id}"'

# Examples:
# kv-secrets expense-reimbursement-kv
# kv-get expense-reimbursement-kv --name "CosmosDb--EndpointUri"
```

---

## Quick Reference Table

| Task | Command |
|------|---------|
| Create Key Vault | `az keyvault create --resource-group <rg> --name <name> --location <region>` |
| Set Secret | `az keyvault secret set --vault-name <vault> --name <name> --value <value>` |
| Get Secret | `az keyvault secret show --vault-name <vault> --name <name>` |
| List Secrets | `az keyvault secret list --vault-name <vault>` |
| Grant Access | `az keyvault set-policy --name <vault> --object-id <id> --secret-permissions get list` |
| Enable Managed Identity | `az webapp identity assign --resource-group <rg> --name <app>` |
| Login to Azure | `az login` |
| Check Account | `az account show` |
| Test Health | `curl http://localhost:5000/health/ping` |
| View Config | `curl http://localhost:5000/health/config` |

---

**Last Updated**: May 30, 2026
**Vault Name**: expense-reimbursement-kv
**Region**: East US (customizable)
