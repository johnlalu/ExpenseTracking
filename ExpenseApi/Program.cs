using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ExpenseApi;
using ExpenseApi.Data;
using ExpenseApi.Data.Repository;
using ExpenseApi.Middleware;
using ExpenseApi.Services;
using ExpenseApi.Validation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

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
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
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
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
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

// Add CORS for Angular
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
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

app.Run();
