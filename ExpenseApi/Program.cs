using System.Text;
using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Azure.Identity;
using ExpenseApi;
using ExpenseApi.Data;
using ExpenseApi.Data.Repository;
using ExpenseApi.Middleware;
using ExpenseApi.Services;
using ExpenseApi.Validation;
using ExpenseApi.Models.Requests;

var builder = WebApplication.CreateBuilder(args);

// Configure Azure Key Vault integration (Production/Staging/Development)
var environment = builder.Environment;
var keyVaultUrl = builder.Configuration["KeyVault:VaultUrl"];
var keyVaultEnabled = builder.Configuration.GetValue<bool>("KeyVault:Enabled");

// Load from Key Vault if:
// - Not in Development OR explicitly enabled in Development settings
// - Key Vault URL is configured
if ((!environment.IsDevelopment() || keyVaultEnabled) && !string.IsNullOrEmpty(keyVaultUrl))
{
    try
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultUrl),
            new DefaultAzureCredential(new DefaultAzureCredentialOptions 
            { 
                ExcludeEnvironmentCredential = false,
                ExcludeWorkloadIdentityCredential = false,
                ExcludeManagedIdentityCredential = false,
                ExcludeSharedTokenCacheCredential = true,
                ExcludeVisualStudioCredential = true,
                ExcludeAzureCliCredential = true,
                ExcludeAzurePowerShellCredential = true
            }));
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Warning: Failed to load Azure Key Vault configuration: {ex.Message}");
    }
}

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// Configure JWT settings
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSettings = new AppConfig.JwtSettings();
jwtSection.Bind(jwtSettings);
builder.Services.Configure<AppConfig.JwtSettings>(jwtSection);

// Configure Cosmos DB settings
var cosmosSection = builder.Configuration.GetSection("CosmosDb");
var cosmosSettings = new AppConfig.CosmosDbSettings();
cosmosSection.Bind(cosmosSettings);
builder.Services.Configure<AppConfig.CosmosDbSettings>(cosmosSection);

// Configure basic logging
builder.Services.AddLogging(logging => 
{
    logging.ClearProviders();
    logging.AddConsole();
});

// Add Authentication with JWT Bearer
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.SecretKey ?? throw new InvalidOperationException("JWT SecretKey not configured"))),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(5),
            NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError($"JWT Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                var response = new ExpenseApi.Models.Responses.ErrorResponse
                {
                    Message = "Unauthorized - Invalid or expired token",
                    StatusCode = 401,
                    LogId = context.HttpContext.TraceIdentifier
                };
                await context.Response.WriteAsJsonAsync(response);
            }
        };
    });

builder.Services.AddAuthorization();

// Add FluentValidation validators
builder.Services.AddScoped<IValidator<RegisterRequest>, RegisterRequestValidator>();
builder.Services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();
builder.Services.AddScoped<IValidator<CreateExpenseRequest>, CreateExpenseValidator>();
builder.Services.AddScoped<IValidator<UpdateExpenseRequest>, UpdateExpenseValidator>();

// Add CORS for Angular
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",
                "https://localhost:4200",
                "https://purple-desert-02ae24b0f.7.azurestaticapps.net",
                "https://lalumondier.com",
                "https://www.lalumondier.com")
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Register application services
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Register Cosmos DB context and repositories
builder.Services.AddScoped<CosmosDbContext>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IExpenseRepository, ExpenseRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();

// Add AutoMapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseHttpsRedirection();

// Use middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Use CORS
app.UseCors("AllowAngular");

// Use authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Expense Reimbursement API starting up...");

// Log configuration source
if ((!environment.IsDevelopment() || keyVaultEnabled) && !string.IsNullOrEmpty(keyVaultUrl))
{
    logger.LogInformation("Configuration loaded from Azure Key Vault: {VaultUrl}", keyVaultUrl);
}
else if (environment.IsDevelopment())
{
    logger.LogInformation("Configuration loaded from appsettings.{Environment}.json", environment.EnvironmentName);
}

// Initialize database
logger.LogInformation("Initializing Cosmos DB database and containers...");
try
{
    using (var scope = app.Services.CreateScope())
    {
        var cosmosDbContext = scope.ServiceProvider.GetRequiredService<CosmosDbContext>();
        await cosmosDbContext.InitializeDatabaseAsync();
    }
    logger.LogInformation("Database initialization completed successfully");
}
catch (Exception ex)
{
    logger.LogError(ex, "Failed to initialize database");
    throw;
}

app.Run();
