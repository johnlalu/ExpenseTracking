# Azure Key Vault Integration Setup

## Overview
This document describes how to set up and configure Azure Key Vault for secure storage and retrieval of secrets (database connection strings, JWT signing keys, API keys, etc.) for the Expense Reimbursement Tracking application.

## Prerequisites
- Azure subscription
- Azure CLI installed (`az` command)
- .NET 10 SDK
- Appropriate Azure RBAC permissions to create Key Vault resources

## Step 1: Create Azure Key Vault Resource

### Using Azure Portal
1. Navigate to Azure Portal (https://portal.azure.com)
2. Click "Create a resource" → Search for "Key Vault"
3. Fill in the form:
   - **Resource Group**: Select or create one (e.g., `expense-reimbursement-rg`)
   - **Key Vault Name**: `expense-reimbursement-kv` (must be globally unique)
   - **Region**: Choose your region (e.g., `East US`)
   - **Pricing Tier**: Standard
4. Click "Review + create" → "Create"

### Using Azure CLI
```bash
az keyvault create \
  --resource-group expense-reimbursement-rg \
  --name expense-reimbursement-kv \
  --location eastus
```

## Step 2: Add Secrets to Key Vault

### Using Azure Portal
1. Open your Key Vault resource
2. Click "Secrets" in the left menu
3. Click "+ Generate/Import" for each secret:

| Secret Name | Value | Description |
|------------|-------|-------------|
| `CosmosDb--EndpointUri` | `https://your-cosmos-db.documents.azure.com:443/` | Cosmos DB endpoint |
| `CosmosDb--PrimaryKey` | Your Cosmos DB primary key | Cosmos DB authentication |
| `Jwt--SecretKey` | 32+ character random string | JWT signing key |
| `ApplicationInsights--InstrumentationKey` | Your App Insights key | Application Insights instrumentation |

### Using Azure CLI
```bash
az keyvault secret set \
  --vault-name expense-reimbursement-kv \
  --name "CosmosDb--EndpointUri" \
  --value "https://your-cosmos-db.documents.azure.com:443/"

az keyvault secret set \
  --vault-name expense-reimbursement-kv \
  --name "CosmosDb--PrimaryKey" \
  --value "your-cosmos-db-primary-key"

az keyvault secret set \
  --vault-name expense-reimbursement-kv \
  --name "Jwt--SecretKey" \
  --value "your-32-character-or-longer-secret-key"

az keyvault secret set \
  --vault-name expense-reimbursement-kv \
  --name "ApplicationInsights--InstrumentationKey" \
  --value "your-app-insights-key"
```

## Step 3: Set Up Authentication

### Option A: Managed Identity (Recommended for App Service)

When deploying to Azure App Service:

1. Enable System-Assigned Managed Identity on your App Service:
   ```bash
   az webapp identity assign \
     --resource-group expense-reimbursement-rg \
     --name your-app-service-name \
     --query principalId \
     --output tsv
   ```

2. Grant the Managed Identity access to Key Vault:
   ```bash
   az keyvault set-policy \
     --name expense-reimbursement-kv \
     --object-id <principal-id-from-step-1> \
     --secret-permissions get list
   ```

### Option B: Service Principal (Development)

For local development or CI/CD:

1. Create a Service Principal:
   ```bash
   az ad sp create-for-rbac \
     --name expense-api-sp \
     --role Contributor \
     --scopes /subscriptions/{subscription-id}/resourceGroups/expense-reimbursement-rg
   ```

2. Assign Key Vault access to the Service Principal:
   ```bash
   az keyvault set-policy \
     --name expense-reimbursement-kv \
     --spn <client-id-from-sp> \
     --secret-permissions get list
   ```

3. Store credentials in local environment (development only):
   - On Windows (PowerShell):
     ```powershell
     $env:AZURE_CLIENT_ID = "your-client-id"
     $env:AZURE_CLIENT_SECRET = "your-client-secret"
     $env:AZURE_TENANT_ID = "your-tenant-id"
     ```
   - On Linux/macOS:
     ```bash
     export AZURE_CLIENT_ID="your-client-id"
     export AZURE_CLIENT_SECRET="your-client-secret"
     export AZURE_TENANT_ID="your-tenant-id"
     ```

## Step 4: Code Implementation

See [Program.cs](ExpenseApi/Program.cs) for the implementation that:
- Loads Key Vault credentials from environment or Managed Identity
- Registers secrets as configuration providers
- Falls back to appsettings.json in development

## Step 5: Local Development Configuration

Create a `appsettings.Development.json` file with placeholder values (never commit real secrets):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "KeyVault": {
    "Enabled": false,
    "VaultUrl": "https://expense-reimbursement-kv.vault.azure.net/"
  },
  "Jwt": {
    "SecretKey": "dev-secret-key-32-characters-minimum-dev",
    "Issuer": "ExpenseReimbursementAPI",
    "Audience": "ExpenseReimbursementApp",
    "ExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "CosmosDb": {
    "EndpointUri": "https://localhost:8081/",
    "PrimaryKey": "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLMZA6jRo2Tp/VvMzQfsL7eMqeYvGJw1NdYXAC+",
    "DatabaseName": "ExpenseDb",
    "ContainerName": "Expenses"
  }
}
```

## Environment-Based Configuration

The application will:
1. **Development**: Load from `appsettings.Development.json` (with Cosmos DB Emulator)
2. **Staging/Production on Azure**: Use Managed Identity to load from Key Vault automatically

## Troubleshooting

### "Access Denied" Error
- Verify Managed Identity or Service Principal has `get` and `list` permissions on Key Vault
- Check that the principal is assigned the correct policy

### "Key Vault Not Found"
- Verify the Key Vault URL is correct
- Check that the Key Vault exists in the correct region and resource group

### "Secret Not Found"
- Verify the secret name matches exactly (case-sensitive for CLI)
- Ensure the secret exists in the Key Vault
- Check that the principal has permissions to list and get secrets

## Security Best Practices

✅ **Do**:
- Use Managed Identity on App Service (no credentials to manage)
- Rotate JWT signing keys periodically
- Enable Key Vault audit logging
- Use separate Key Vaults for dev, staging, and production
- Implement IP whitelisting on Key Vault if needed

❌ **Don't**:
- Store real secrets in appsettings.json (except development placeholders)
- Commit secrets to version control
- Share Service Principal credentials across environments
- Use the same Key Vault for multiple environments
- Log or expose secret values in error messages

## References
- [Azure Key Vault Documentation](https://docs.microsoft.com/en-us/azure/key-vault/)
- [Azure Identity SDK](https://github.com/Azure/azure-sdk-for-net/tree/master/sdk/identity/Azure.Identity)
- [Azure Key Vault Secrets SDK](https://github.com/Azure/azure-sdk-for-net/tree/master/sdk/keyvault/Azure.Security.KeyVault.Secrets)
- [Managed Identities in App Service](https://docs.microsoft.com/en-us/azure/app-service/overview-managed-identity)
