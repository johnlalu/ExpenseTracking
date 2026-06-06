#!/bin/bash
# Azure Key Vault Setup Script for Expense Reimbursement API (Bash version)
# This script creates secrets in Azure Key Vault for database connection and authentication

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Function to print colored output
print_info() {
    echo -e "${CYAN}$1${NC}"
}

print_success() {
    echo -e "${GREEN}$1${NC}"
}

print_error() {
    echo -e "${RED}$1${NC}"
}

print_warning() {
    echo -e "${YELLOW}$1${NC}"
}

# Print header
print_info "Azure Key Vault Setup Script"
print_info "========================================"
echo ""

# Check if required arguments are provided
if [ $# -lt 3 ]; then
    print_error "ERROR: Missing required arguments"
    echo ""
    echo "Usage: $0 <KeyVaultName> <CosmosDbEndpoint> <CosmosDbPrimaryKey> [JwtSecretKey] [AppInsightsKey]"
    echo ""
    echo "Arguments:"
    echo "  KeyVaultName           Name of the Azure Key Vault"
    echo "  CosmosDbEndpoint       Cosmos DB endpoint URI (https://...)"
    echo "  CosmosDbPrimaryKey     Cosmos DB primary key"
    echo "  JwtSecretKey          JWT secret key (32+ characters, optional - will prompt if not provided)"
    echo "  AppInsightsKey        Application Insights key (optional)"
    echo ""
    exit 1
fi

KEY_VAULT_NAME="$1"
COSMOS_DB_ENDPOINT="$2"
COSMOS_DB_PRIMARY_KEY="$3"
JWT_SECRET_KEY="${4:-}"
APP_INSIGHTS_KEY="${5:-}"

# If JWT key not provided, generate one
if [ -z "$JWT_SECRET_KEY" ]; then
    print_warning "JWT Secret Key not provided. Generating a random 32-character key..."
    JWT_SECRET_KEY=$(openssl rand -base64 24 | tr -d '\n/')
    print_info "Generated JWT Secret Key: $JWT_SECRET_KEY"
    echo ""
fi

# Validate inputs
if [ ${#JWT_SECRET_KEY} -lt 32 ]; then
    print_error "ERROR: JWT Secret Key must be at least 32 characters long"
    exit 1
fi

if [[ ! "$COSMOS_DB_ENDPOINT" =~ ^https:// ]]; then
    print_error "ERROR: Cosmos DB endpoint must start with 'https://'"
    exit 1
fi

# Check if user is logged in to Azure
print_warning "Checking Azure CLI login..."
if ! az account show &>/dev/null; then
    print_warning "You are not logged into Azure CLI. Running 'az login'..."
    az login
fi

# Verify Key Vault exists
print_warning "Verifying Key Vault exists: $KEY_VAULT_NAME"
if ! az keyvault show --name "$KEY_VAULT_NAME" &>/dev/null; then
    print_error "ERROR: Key Vault '$KEY_VAULT_NAME' not found"
    exit 1
fi

print_success "Key Vault verified!"
echo ""

# Function to set a secret with error handling
set_keyvault_secret() {
    local name=$1
    local value=$2
    local description=$3
    
    print_info "Setting secret: $name"
    
    if az keyvault secret set \
        --vault-name "$KEY_VAULT_NAME" \
        --name "$name" \
        --value "$value" \
        --description "$description" &>/dev/null; then
        print_success "  ✓ Secret '$name' set successfully"
        return 0
    else
        print_error "  ✗ Failed to set secret '$name'"
        return 1
    fi
}

# Set all secrets
print_warning "Setting up secrets in Key Vault..."
echo ""

success_count=0
total_count=0

# Cosmos DB secrets
((total_count++))
if set_keyvault_secret "CosmosDb--EndpointUri" "$COSMOS_DB_ENDPOINT" "Azure Cosmos DB endpoint URI"; then
    ((success_count++))
fi

((total_count++))
if set_keyvault_secret "CosmosDb--PrimaryKey" "$COSMOS_DB_PRIMARY_KEY" "Azure Cosmos DB primary key for authentication"; then
    ((success_count++))
fi

# JWT secret
((total_count++))
if set_keyvault_secret "Jwt--SecretKey" "$JWT_SECRET_KEY" "JWT signing secret key (minimum 32 characters)"; then
    ((success_count++))
fi

# Application Insights (optional)
if [ -n "$APP_INSIGHTS_KEY" ]; then
    ((total_count++))
    if set_keyvault_secret "ApplicationInsights--InstrumentationKey" "$APP_INSIGHTS_KEY" "Application Insights instrumentation key"; then
        ((success_count++))
    fi
fi

echo ""
print_info "========================================"
print_info "Setup Summary"
print_info "========================================"
print_success "Key Vault: $KEY_VAULT_NAME"
print_success "Secrets created: $success_count/$total_count"

if [ "$success_count" -eq "$total_count" ]; then
    echo ""
    print_success "✓ All secrets configured successfully!"
    echo ""
    print_warning "Next steps:"
    echo "  1. Verify secrets exist: az keyvault secret list --vault-name $KEY_VAULT_NAME"
    echo "  2. Set up Access Policies for your app (Managed Identity or Service Principal)"
    echo "  3. Update appsettings.json with KeyVault URL: https://$KEY_VAULT_NAME.vault.azure.net/"
    echo "  4. Deploy to Azure App Service with Managed Identity enabled"
    exit 0
else
    echo ""
    print_error "✗ Some secrets failed to configure"
    exit 1
fi
