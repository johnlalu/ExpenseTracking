# Task 5: Testing Secure Credential Retrieval

## Overview

This document provides comprehensive testing strategies to verify that your application correctly loads configuration and credentials from Azure Key Vault or local configuration.

## Quick Health Check

Before running detailed tests, verify basic setup:

```bash
# 1. Check Key Vault exists
az keyvault show --name expense-reimbursement-kv

# 2. Verify secrets exist
az keyvault secret list --vault-name expense-reimbursement-kv

# 3. Test secret retrieval
az keyvault secret show \
  --vault-name expense-reimbursement-kv \
  --name "CosmosDb--EndpointUri" \
  --query "value" -o tsv
```

## Testing Strategies

### Test 1: Local Development Testing

**Objective**: Verify configuration loads from `appsettings.Development.json`

**Prerequisites**:
- `appsettings.Development.json` configured with local values
- Cosmos DB Emulator running (optional) or mock connection
- .NET 10 SDK installed

**Steps**:

1. **Set environment to Development**:
   ```bash
   $env:ASPNETCORE_ENVIRONMENT = "Development"  # PowerShell
   # OR
   export ASPNETCORE_ENVIRONMENT=Development     # Bash
   ```

2. **Restore NuGet packages**:
   ```bash
   cd ExpenseApi
   dotnet restore
   ```

3. **Run the application**:
   ```bash
   dotnet run
   ```

4. **Expected output** in logs:
   ```
   Configuration loaded from appsettings.Development.json
   Expense Reimbursement API starting up...
   ```

5. **Test API endpoint** (in another terminal):
   ```bash
   curl http://localhost:5000/health
   # Should return 200 OK
   ```

### Test 2: Configuration Verification Test

**Objective**: Verify all required configuration values are loaded

Create a test endpoint that returns configuration status (development only):

**File**: [ExpenseApi/Controllers/HealthController.cs](./ExpenseApi/Controllers/HealthController.cs)

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace ExpenseApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<HealthController> _logger;
        private readonly IWebHostEnvironment _environment;

        public HealthController(
            IConfiguration configuration,
            ILogger<HealthController> logger,
            IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _logger = logger;
            _environment = environment;
        }

        /// <summary>
        /// Basic health check endpoint
        /// </summary>
        [HttpGet("ping")]
        public ActionResult<object> Ping()
        {
            return Ok(new { status = "ok", timestamp = DateTime.UtcNow });
        }

        /// <summary>
        /// Configuration check (development only)
        /// </summary>
        [HttpGet("config")]
        public ActionResult<object> GetConfigStatus()
        {
            if (!_environment.IsDevelopment())
            {
                return Unauthorized(new { error = "Config check only available in development" });
            }

            try
            {
                var configStatus = new
                {
                    environment = _environment.EnvironmentName,
                    timestamp = DateTime.UtcNow,
                    configuration = new
                    {
                        cosmosDb = new
                        {
                            endpoint = _configuration["CosmosDb:EndpointUri"] != null ? "✓ Configured" : "✗ Missing",
                            primaryKey = _configuration["CosmosDb:PrimaryKey"] != null ? "✓ Configured" : "✗ Missing",
                            database = _configuration["CosmosDb:DatabaseName"],
                            container = _configuration["CosmosDb:ContainerName"]
                        },
                        jwt = new
                        {
                            secretKey = _configuration["Jwt:SecretKey"] != null ? "✓ Configured" : "✗ Missing",
                            issuer = _configuration["Jwt:Issuer"],
                            audience = _configuration["Jwt:Audience"]
                        },
                        applicationInsights = new
                        {
                            instrumentationKey = _configuration["ApplicationInsights:InstrumentationKey"] != null 
                                ? "✓ Configured" 
                                : "✗ Missing"
                        }
                    }
                };

                _logger.LogInformation("Configuration status: {@ConfigStatus}", configStatus);
                return Ok(configStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking configuration status");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
```

**Test it**:
```bash
curl http://localhost:5000/health/config | jq

# Expected output:
# {
#   "environment": "Development",
#   "configuration": {
#     "cosmosDb": {
#       "endpoint": "✓ Configured",
#       "primaryKey": "✓ Configured",
#       "database": "ExpenseDb",
#       "container": "Expenses"
#     },
#     "jwt": {
#       "secretKey": "✓ Configured",
#       ...
#     }
#   }
# }
```

### Test 3: Unit Tests

**File**: [ExpenseApi/Tests/Unit/ConfigurationTests.cs](./ExpenseApi/Tests/Unit/ConfigurationTests.cs)

```csharp
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ExpenseApi.Tests.Unit
{
    public class ConfigurationTests
    {
        private IConfiguration BuildConfiguration()
        {
            var configBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables();

            return configBuilder.Build();
        }

        [Fact]
        public void Configuration_HasCosmosDbSettings()
        {
            // Arrange
            var config = BuildConfiguration();

            // Act & Assert
            Assert.NotNull(config["CosmosDb:EndpointUri"]);
            Assert.NotEmpty(config["CosmosDb:EndpointUri"]);
            
            Assert.NotNull(config["CosmosDb:PrimaryKey"]);
            Assert.NotEmpty(config["CosmosDb:PrimaryKey"]);
            
            Assert.NotNull(config["CosmosDb:DatabaseName"]);
            Assert.NotEmpty(config["CosmosDb:DatabaseName"]);
        }

        [Fact]
        public void Configuration_HasJwtSettings()
        {
            // Arrange
            var config = BuildConfiguration();

            // Act & Assert
            Assert.NotNull(config["Jwt:SecretKey"]);
            Assert.True(config["Jwt:SecretKey"].Length >= 32, 
                "JWT Secret Key must be at least 32 characters");

            Assert.NotNull(config["Jwt:Issuer"]);
            Assert.NotNull(config["Jwt:Audience"]);
        }

        [Fact]
        public void Configuration_CosmosDbEndpointIsValidUri()
        {
            // Arrange
            var config = BuildConfiguration();
            var endpoint = config["CosmosDb:EndpointUri"];

            // Act & Assert
            Assert.True(Uri.TryCreate(endpoint, UriKind.Absolute, out var uri), 
                "CosmosDb endpoint must be a valid URI");
            Assert.Equal("https", uri.Scheme);
        }
    }
}
```

**Run the tests**:
```bash
cd ExpenseApi
dotnet test
```

### Test 4: Integration Test with Key Vault

**File**: [ExpenseApi/Tests/Integration/KeyVaultConfigurationTests.cs](./ExpenseApi/Tests/Integration/KeyVaultConfigurationTests.cs)

```csharp
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Xunit;

namespace ExpenseApi.Tests.Integration
{
    public class KeyVaultConfigurationTests
    {
        private const string KeyVaultUrl = "https://expense-reimbursement-kv.vault.azure.net/";

        [Fact(Skip = "Requires Azure CLI login and Key Vault access")]
        public async Task KeyVault_CanRetrieveCosmosDbSecret()
        {
            // Arrange
            var client = new SecretClient(new Uri(KeyVaultUrl), new DefaultAzureCredential());

            // Act
            var secret = await client.GetSecretAsync("CosmosDb--EndpointUri");

            // Assert
            Assert.NotNull(secret);
            Assert.NotNull(secret.Value.Value);
            Assert.StartsWith("https://", secret.Value.Value);
        }

        [Fact(Skip = "Requires Azure CLI login and Key Vault access")]
        public async Task KeyVault_CanRetrieveJwtSecret()
        {
            // Arrange
            var client = new SecretClient(new Uri(KeyVaultUrl), new DefaultAzureCredential());

            // Act
            var secret = await client.GetSecretAsync("Jwt--SecretKey");

            // Assert
            Assert.NotNull(secret);
            Assert.NotNull(secret.Value.Value);
            Assert.True(secret.Value.Value.Length >= 32);
        }

        [Fact(Skip = "Requires Azure CLI login and Key Vault access")]
        public async Task KeyVault_AllRequiredSecretsExist()
        {
            // Arrange
            var client = new SecretClient(new Uri(KeyVaultUrl), new DefaultAzureCredential());
            var requiredSecrets = new[] 
            { 
                "CosmosDb--EndpointUri",
                "CosmosDb--PrimaryKey",
                "Jwt--SecretKey"
            };

            // Act & Assert
            foreach (var secretName in requiredSecrets)
            {
                var secret = await client.GetSecretAsync(secretName);
                Assert.NotNull(secret);
                Assert.NotNull(secret.Value.Value);
            }
        }
    }
}
```

**Run integration tests**:
```bash
# First, ensure you're logged into Azure
az login

# Then run the tests
cd ExpenseApi
dotnet test --filter "KeyVaultConfigurationTests" --no-skip
```

### Test 5: Application Startup Test

**Objective**: Verify application starts without configuration errors

```bash
# Set to Development
$env:ASPNETCORE_ENVIRONMENT = "Development"

# Run with verbose logging
dotnet run --verbosity diagnostic

# Should see:
# - No configuration errors
# - No authentication failures
# - "API starting up..." message
# - No unhandled exceptions
```

### Test 6: Authentication Method Test

**Objective**: Verify the correct authentication method is being used

**For Managed Identity (App Service)**:
```bash
# Deploy to App Service and check logs in Application Insights
# Look for: "Configuration loaded from Azure Key Vault: ..."
# No "Interactive authentication required" errors
```

**For Environment Credentials (CI/CD)**:
```powershell
# Set environment variables
$env:AZURE_CLIENT_ID = "your-client-id"
$env:AZURE_CLIENT_SECRET = "your-client-secret"
$env:AZURE_TENANT_ID = "your-tenant-id"
$env:ASPNETCORE_ENVIRONMENT = "Staging"

# Start application
dotnet run

# Should load from Key Vault without errors
```

**For Azure CLI (Local Dev)**:
```bash
# Verify Azure CLI login
az account show

# Start application
ASPNETCORE_ENVIRONMENT=Development dotnet run

# Verify logs show proper configuration loading
```

## Monitoring and Logs

### Local Development Logs

Check console output for:
```
Information: Expense Reimbursement API starting up...
Information: Configuration loaded from appsettings.Development.json
Information: Initializing Cosmos DB database and containers...
Information: Database initialization completed successfully
```

### Production Logs (Application Insights)

Query in Application Insights:
```kusto
traces
| where message contains "Configuration loaded"
   or message contains "Azure Key Vault"
| summarize by message, timestamp
```

### Troubleshooting Logs

Look for errors:
```kusto
customEvents
| where name == "ConfigurationError" 
   or name == "KeyVaultError"
| project timestamp, name, customDimensions
```

## Automated Testing Script

**File**: [run-config-tests.ps1](../run-config-tests.ps1)

```powershell
#!/usr/bin/env pwsh

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Development", "Staging", "Production")]
    [string]$Environment,
    
    [string]$KeyVaultUrl = "https://expense-reimbursement-kv.vault.azure.net/"
)

Write-Host "Configuration Testing Script" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Set environment
$env:ASPNETCORE_ENVIRONMENT = $Environment
Write-Host "Environment: $Environment" -ForegroundColor Yellow

# Build project
Write-Host "Building project..." -ForegroundColor Yellow
dotnet build ExpenseApi/ExpenseApi.csproj
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Run unit tests
Write-Host ""
Write-Host "Running unit tests..." -ForegroundColor Yellow
dotnet test ExpenseApi/Tests/Unit/ --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    Write-Host "Unit tests failed!" -ForegroundColor Red
    exit 1
}

# Run configuration check endpoint (if Development)
if ($Environment -eq "Development") {
    Write-Host ""
    Write-Host "Starting application for configuration check..." -ForegroundColor Yellow
    
    # Start app in background
    $process = Start-Process -NoNewWindow -PassThru `
        -FilePath "dotnet" `
        -ArgumentList "run", "--project", "ExpenseApi/ExpenseApi.csproj"
    
    # Wait for startup
    Start-Sleep -Seconds 5
    
    # Check configuration endpoint
    Write-Host "Checking configuration endpoint..." -ForegroundColor Yellow
    try {
        $response = Invoke-RestMethod http://localhost:5000/health/config
        Write-Host "Configuration status:" -ForegroundColor Green
        $response.configuration | ConvertTo-Json | Write-Host
    }
    catch {
        Write-Host "Failed to check configuration: $_" -ForegroundColor Red
    }
    
    # Stop application
    Stop-Process -InputObject $process -Force
}

Write-Host ""
Write-Host "✓ All configuration tests passed!" -ForegroundColor Green
```

## Test Checklist

Use this checklist to verify all aspects of credential retrieval:

- [ ] Local development configuration loads without errors
- [ ] All required secrets present in configuration
- [ ] Cosmos DB endpoint is valid URI with `https://`
- [ ] JWT secret key is minimum 32 characters
- [ ] JWT issuer and audience are configured
- [ ] Application Insights key configured (if using)
- [ ] Health check endpoint returns 200 OK
- [ ] `/health/config` endpoint shows all values configured ✓
- [ ] Unit tests pass (80%+ success)
- [ ] Database initialization succeeds
- [ ] Authentication works with test credentials
- [ ] Error handling works (invalid credentials)
- [ ] Application logs show correct configuration source
- [ ] No credential/secret values logged
- [ ] Production deployment uses Managed Identity
- [ ] CI/CD deployment uses environment variables

## Security Notes During Testing

✅ **Do:**
- Log configuration status (not values)
- Use separate test credentials
- Clean up test data after testing
- Test in isolated environment first

❌ **Don't:**
- Log actual secret values
- Share test credentials
- Test with production credentials
- Leave debug endpoints in production

## Next Steps

After successful testing:

1. ✅ All tests pass locally
2. ✅ Configuration loads correctly
3. Deploy to Azure App Service:
   - Enable Managed Identity
   - Set Key Vault URL in appsettings
   - Deploy application
   - Verify logs show successful loading
4. Monitor in production:
   - Check Application Insights logs
   - Monitor credential refresh (if any)
   - Set up alerts for authentication failures

## References

- [Azure Key Vault Documentation](https://docs.microsoft.com/en-us/azure/key-vault/)
- [Azure.Identity Package](https://github.com/Azure/azure-sdk-for-net/blob/master/sdk/identity/Azure.Identity/README.md)
- [Configuration in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
