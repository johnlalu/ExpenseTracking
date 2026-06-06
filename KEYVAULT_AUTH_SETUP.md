# Task 3: Key Vault Client Authentication Setup

## Overview

Your application uses **Azure.Identity** library to authenticate with Azure Key Vault. The `DefaultAzureCredential` class automatically tries multiple authentication methods in order of preference, providing flexibility across different environments.

## Authentication Methods Supported

### 1. Managed Identity (Production - Recommended)

**Best for**: Azure App Service, Azure Container Instances, Azure Functions

**How it works:**
- Azure assigns an identity to your app service
- No credentials to manage - fully automatic
- Most secure option for cloud deployments

**Setup in Program.cs** (Already implemented):
```csharp
var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions 
{ 
    ExcludeManagedIdentityCredential = false,
    ExcludeEnvironmentCredential = false,
    // ... other options
});

builder.Configuration.AddAzureKeyVault(
    new Uri(keyVaultUrl),
    credential);
```

**Steps to enable:**

1. **On Azure App Service:**
   ```bash
   az webapp identity assign \
     --resource-group expense-reimbursement-rg \
     --name your-app-service-name \
     --query principalId \
     --output tsv
   ```

2. **Grant Key Vault permissions:**
   ```bash
   az keyvault set-policy \
     --name expense-reimbursement-kv \
     --object-id <principal-id-from-step-1> \
     --secret-permissions get list
   ```

### 2. Environment Credentials (Development/CI-CD)

**Best for**: Local development, CI/CD pipelines

**How it works:**
- Reads Azure credentials from environment variables
- Used when Managed Identity is unavailable

**Environment variables needed:**
```powershell
# Windows PowerShell
$env:AZURE_CLIENT_ID = "your-client-id"
$env:AZURE_CLIENT_SECRET = "your-client-secret"
$env:AZURE_TENANT_ID = "your-tenant-id"
```

```bash
# Linux/macOS
export AZURE_CLIENT_ID="your-client-id"
export AZURE_CLIENT_SECRET="your-client-secret"
export AZURE_TENANT_ID="your-tenant-id"
```

### 3. Azure CLI Authentication

**Best for**: Local development with Azure CLI

**How it works:**
- Uses credentials from `az login`
- DefaultAzureCredential automatically uses this if available

**Prerequisite:**
```bash
az login
```

### 4. Visual Studio Authentication

**Best for**: Local development in Visual Studio

**How it works:**
- Uses your Visual Studio sign-in account
- Requires appropriate Azure permissions

**Note:** Can be excluded in production for security:
```csharp
var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions 
{ 
    ExcludeVisualStudioCredential = true  // Exclude in production
});
```

## DefaultAzureCredential Chain

The `DefaultAzureCredential` tries authentication methods in this order:

```
1. EnvironmentCredential          (AZURE_CLIENT_ID/SECRET/TENANT_ID)
2. WorkloadIdentityCredential     (Kubernetes workload identity)
3. ManagedIdentityCredential      (App Service, Container Instances, etc.)
4. SharedTokenCacheCredential     (Cached VS/CLI token)
5. VisualStudioCredential         (Visual Studio)
6. VisualStudioCodeCredential     (VS Code)
7. AzureCliCredential             (az login)
8. AzurePowerShellCredential      (PowerShell)
```

**In your Program.cs**, we're optimizing the chain to skip development-specific methods in production:

```csharp
new DefaultAzureCredentialOptions 
{ 
    ExcludeEnvironmentCredential = false,      // Keep for CI/CD
    ExcludeWorkloadIdentityCredential = false, // Keep for k8s
    ExcludeManagedIdentityCredential = false,  // Keep for App Service
    ExcludeSharedTokenCacheCredential = true,  // Skip - not needed
    ExcludeVisualStudioCredential = true,      // Skip in production
    ExcludeVisualStudioCodeCredential = true,  // Skip in production
    ExcludeAzureCliCredential = true,          // Skip - use Env or Managed
    ExcludeAzurePowerShellCredential = true    // Skip - use Env or Managed
}
```

## Development vs Production Configuration

### Development Environment

**Configuration location**: `appsettings.Development.json`
- `KeyVault.Enabled: false` (loads from local config)
- Uses local values without Key Vault
- No Azure credentials needed

```json
{
  "KeyVault": {
    "Enabled": false
  }
}
```

### Production Environment

**Configuration location**: `appsettings.json` (Production profile)
- `KeyVault.VaultUrl` configured
- Program.cs loads from Azure Key Vault
- Uses Managed Identity automatically

## Service Principal Setup (Advanced)

For scenarios requiring a Service Principal (e.g., CI/CD, local service accounts):

### Create Service Principal

```bash
az ad sp create-for-rbac \
  --name expense-api-sp \
  --role Contributor \
  --scopes /subscriptions/{subscription-id}
```

**Output:**
```json
{
  "appId": "your-client-id",
  "displayName": "expense-api-sp",
  "password": "your-client-secret",
  "tenant": "your-tenant-id"
}
```

### Assign Key Vault Permissions

```bash
az keyvault set-policy \
  --name expense-reimbursement-kv \
  --spn your-client-id \
  --secret-permissions get list
```

### Use in CI/CD Pipeline

Store as GitHub/Azure DevOps secrets and set environment variables before deployment:

```yaml
# GitHub Actions example
- name: Configure Azure credentials
  run: |
    echo "AZURE_CLIENT_ID=${{ secrets.AZURE_CLIENT_ID }}" >> $GITHUB_ENV
    echo "AZURE_CLIENT_SECRET=${{ secrets.AZURE_CLIENT_SECRET }}" >> $GITHUB_ENV
    echo "AZURE_TENANT_ID=${{ secrets.AZURE_TENANT_ID }}" >> $GITHUB_ENV
```

## Troubleshooting Authentication

### "DefaultAzureCredentialBuilder failed to instantiate"

**Cause**: No credentials available in the current environment

**Solution**:
1. For App Service: Enable Managed Identity
2. For local dev: Run `az login` or set environment variables
3. Check `ExcludeXxxCredential` settings - you may have excluded all options

### "AuthenticationFailedException: Interactive authentication is required"

**Cause**: No non-interactive authentication available

**Solution**:
1. In App Service: Verify Managed Identity is enabled
2. In CI/CD: Set `AZURE_CLIENT_ID`, `AZURE_CLIENT_SECRET`, `AZURE_TENANT_ID`
3. Locally: Run `az login`

### "Access Denied (403)"

**Cause**: Authentication successful, but principal lacks permissions

**Solution**:
1. Verify Key Vault access policy is set for the principal
2. Ensure permissions include `get` and `list` for secrets
3. Check: `az keyvault show-deleted --name expense-reimbursement-kv` to verify vault exists

### "Key Vault Not Found (404)"

**Cause**: Vault URL is incorrect or vault was deleted

**Solution**:
1. Verify vault name in `appsettings.json`
2. Check vault exists: `az keyvault list`
3. Ensure URL format: `https://<vault-name>.vault.azure.net/`

## Testing Authentication

### Local Testing

```powershell
# Test with Azure CLI
az login
dotnet run

# Should see in logs:
# "Configuration loaded from Azure Key Vault: https://..."
```

### Testing with Service Principal

```powershell
$env:AZURE_CLIENT_ID = "your-client-id"
$env:AZURE_CLIENT_SECRET = "your-client-secret"
$env:AZURE_TENANT_ID = "your-tenant-id"

dotnet run
```

### Production Testing (on App Service)

1. Deploy application to App Service
2. Check Application Insights logs for authentication details
3. Look for: "Configuration loaded from Azure Key Vault: ..."
4. Verify no authentication errors in logs

## Security Best Practices

✅ **Do:**
- Use Managed Identity on Azure services
- Rotate Service Principal credentials regularly
- Limit Key Vault permissions to minimum required (`get`, `list`)
- Store credentials in Azure Key Vault, not config files
- Use different service principals per environment
- Enable Key Vault audit logging
- Regularly review Key Vault access policies

❌ **Don't:**
- Commit credentials to version control
- Use the same credentials across environments
- Grant unnecessary permissions (e.g., `delete`, `purge`)
- Store credentials in application logs
- Use account keys instead of Managed Identity
- Disable Azure.Identity security defaults

## Next Steps

After authentication is configured:

1. ✅ Authentication is set up (this task)
2. ⬜ Program.cs is already updated (Task 4 - already complete)
3. ⬜ Test configuration loading (Task 5)

For deployment:
1. Enable Managed Identity on App Service
2. Deploy application
3. Verify logs show successful Key Vault loading
4. Test API endpoints to confirm configuration works

See [AZURE_KEYVAULT_SETUP.md](./AZURE_KEYVAULT_SETUP.md) for broader setup context.
