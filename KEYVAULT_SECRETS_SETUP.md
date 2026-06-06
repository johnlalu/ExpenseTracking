# Task 2: Configure Connection Strings in Azure Key Vault

## Overview

This task involves populating your Azure Key Vault with the necessary secrets for the Expense Reimbursement API. These secrets include:
- Cosmos DB connection credentials
- JWT signing key
- Application Insights instrumentation key

## Prerequisites

Before proceeding, ensure you have:
1. Created an Azure Key Vault (see [AZURE_KEYVAULT_SETUP.md](./AZURE_KEYVAULT_SETUP.md) Step 1)
2. Azure CLI installed and authenticated (`az login`)
3. Appropriate permissions to create secrets in the Key Vault

## Secrets to Configure

### 1. Cosmos DB Credentials

These secrets store your Cosmos DB connection information:

| Secret Name | Value | Source |
|------------|-------|--------|
| `CosmosDb--EndpointUri` | Cosmos DB account URI | Azure Portal → Cosmos DB → Keys → URI |
| `CosmosDb--PrimaryKey` | Cosmos DB account key | Azure Portal → Cosmos DB → Keys → Primary Key |

**Example values:**
```
CosmosDb--EndpointUri = https://expense-db.documents.azure.com:443/
CosmosDb--PrimaryKey = cTVIe9Q2pK55GrZGo91hCUISGrZl0kUpBad4uH8LAduTHHU78oZy14vUiDBdhVOjDcpB2pJZBPjsACDbpM4fiw==
```

### 2. JWT Secret Key

A secure key used to sign and validate JWT tokens:

| Secret Name | Value | Notes |
|------------|-------|-------|
| `Jwt--SecretKey` | 32+ character random string | Must be at least 32 characters for security |

**Generating a secure JWT key:**

On Windows (PowerShell):
```powershell
[System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes([guid]::NewGuid().ToString() + [guid]::NewGuid().ToString()))
```

On Linux/macOS (Bash):
```bash
openssl rand -base64 32
```

### 3. Application Insights (Optional)

If using Application Insights for monitoring:

| Secret Name | Value | Source |
|------------|-------|--------|
| `ApplicationInsights--InstrumentationKey` | App Insights key | Azure Portal → Application Insights → Properties |

## Configuration Methods

### Method 1: Automated Setup (Recommended)

Use the provided PowerShell or Bash scripts to configure all secrets at once.

**Windows (PowerShell):**
```powershell
# Make the script executable
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

# Run the setup script
.\setup-keyvault.ps1 `
    -KeyVaultName "expense-reimbursement-kv" `
    -CosmosDbEndpoint "https://expense-db.documents.azure.com:443/" `
    -CosmosDbPrimaryKey "your-cosmos-db-primary-key" `
    -JwtSecretKey "your-32-character-or-longer-secret-key" `
    -AppInsightsKey "your-app-insights-key" # Optional
```

**Linux/macOS (Bash):**
```bash
chmod +x setup-keyvault.sh

./setup-keyvault.sh \
    "expense-reimbursement-kv" \
    "https://expense-db.documents.azure.com:443/" \
    "your-cosmos-db-primary-key" \
    "your-32-character-or-longer-secret-key" \
    "your-app-insights-key"  # Optional
```

### Method 2: Manual Setup via Azure Portal

1. Open your Key Vault in Azure Portal
2. Click **Secrets** in the left sidebar
3. Click **+ Generate/Import**
4. Fill in the form for each secret:
   - **Name**: `CosmosDb--EndpointUri`
   - **Value**: Your Cosmos DB endpoint URI
   - Click **Create**
5. Repeat for other secrets

### Method 3: Azure CLI Commands

Set each secret individually:

```bash
# Set Cosmos DB endpoint
az keyvault secret set \
  --vault-name expense-reimbursement-kv \
  --name "CosmosDb--EndpointUri" \
  --value "https://expense-db.documents.azure.com:443/"

# Set Cosmos DB primary key
az keyvault secret set \
  --vault-name expense-reimbursement-kv \
  --name "CosmosDb--PrimaryKey" \
  --value "your-cosmos-db-primary-key"

# Set JWT secret key
az keyvault secret set \
  --vault-name expense-reimbursement-kv \
  --name "Jwt--SecretKey" \
  --value "your-32-character-or-longer-secret-key"

# Set Application Insights key (optional)
az keyvault secret set \
  --vault-name expense-reimbursement-kv \
  --name "ApplicationInsights--InstrumentationKey" \
  --value "your-app-insights-key"
```

## Verification

After configuring secrets, verify they were created successfully:

```bash
# List all secrets
az keyvault secret list --vault-name expense-reimbursement-kv

# Retrieve a specific secret value (for verification only)
az keyvault secret show \
  --vault-name expense-reimbursement-kv \
  --name "CosmosDb--EndpointUri" \
  --query "value" -o tsv
```

## Secret Naming Convention

Note the naming convention with `--` (double dash):
- `CosmosDb--EndpointUri` (not `CosmosDb.EndpointUri`)
- `Jwt--SecretKey` (not `Jwt.SecretKey`)
- `ApplicationInsights--InstrumentationKey`

The double dash is converted to `:` by the Azure Key Vault configuration provider in .NET, matching the hierarchical structure in `appsettings.json`:
```json
{
  "CosmosDb": {
    "EndpointUri": "...",
    "PrimaryKey": "..."
  }
}
```

## Configuration Provider Mapping

The Azure Key Vault configuration provider automatically maps secret names to configuration values:

| Secret Name in Key Vault | Configuration Path | .NET Access |
|---------------------------|-------------------|------------|
| `CosmosDb--EndpointUri` | `configuration["CosmosDb:EndpointUri"]` | `cosmosSettings.EndpointUri` |
| `CosmosDb--PrimaryKey` | `configuration["CosmosDb:PrimaryKey"]` | `cosmosSettings.PrimaryKey` |
| `Jwt--SecretKey` | `configuration["Jwt:SecretKey"]` | `jwtSettings.SecretKey` |
| `ApplicationInsights--InstrumentationKey` | `configuration["ApplicationInsights:InstrumentationKey"]` | `config["ApplicationInsights:InstrumentationKey"]` |

## Testing Configuration Loading

After deployment to Azure App Service with Managed Identity:

1. Deploy the updated application
2. Check Application Insights logs for configuration loading:
   - Should see: "Configuration loaded from Azure Key Vault: https://expense-reimbursement-kv.vault.azure.net/"
   - Or if development: "Configuration loaded from appsettings.Development.json"
3. Verify the application starts successfully without configuration errors

## Troubleshooting

### "Access Denied" Error
- Verify the app's Managed Identity has `get` and `list` permissions on the Key Vault
- Check the Key Vault access policy

### "Secret Not Found"
- Verify the secret name matches exactly (case-sensitive)
- Use `az keyvault secret list` to see all available secrets
- Check for typos in the `--` separator

### Secrets Load as Null
- Verify the secret exists in Key Vault
- Check the configuration binding in `Program.cs`
- Ensure environment is not "Development" (uses local appsettings in dev)

## Security Notes

✅ **Best Practices:**
- Secrets are versioned automatically in Key Vault
- Each secret is encrypted at rest
- Access is logged for audit purposes
- Rotate secrets periodically (especially JWT key)

❌ **Never:**
- Commit secrets to version control
- Store secrets in application configuration files (except dev placeholders)
- Share Key Vault access credentials
- Log secret values in error messages

## Next Steps

After configuring all secrets:

1. ✅ Secrets are configured in Key Vault
2. ⬜ Set up authentication (Task 3)
3. ⬜ Update Program.cs to load from Key Vault (Task 4 - already done in Task 1)
4. ⬜ Test secure credential retrieval (Task 5)

See [AZURE_KEYVAULT_SETUP.md](./AZURE_KEYVAULT_SETUP.md) Step 3 for authentication setup options.
