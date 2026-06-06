using Microsoft.AspNetCore.Mvc;

namespace ExpenseApi.Controllers
{
    /// <summary>
    /// Health check and configuration status endpoint (development only)
    /// </summary>
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
        /// Basic health check ping endpoint
        /// </summary>
        [HttpGet("ping")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public ActionResult<object> Ping()
        {
            return Ok(new 
            { 
                status = "ok", 
                timestamp = DateTime.UtcNow,
                environment = _environment.EnvironmentName
            });
        }

        /// <summary>
        /// Configuration status check (development only)
        /// Returns which configuration sources are being used and verification of required settings
        /// </summary>
        [HttpGet("config")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public ActionResult<object> GetConfigStatus()
        {
            // Only allow in development environment for security
            if (!_environment.IsDevelopment())
            {
                _logger.LogWarning("Configuration check attempted in {Environment} environment", _environment.EnvironmentName);
                return Unauthorized(new 
                { 
                    error = "Configuration check only available in development environment" 
                });
            }

            try
            {
                // Build configuration status object
                var configStatus = new
                {
                    environment = _environment.EnvironmentName,
                    timestamp = DateTime.UtcNow,
                    keyVaultEnabled = !string.IsNullOrEmpty(_configuration["KeyVault:VaultUrl"]),
                    keyVaultUrl = _configuration["KeyVault:VaultUrl"] ?? "Not configured",
                    configuration = new
                    {
                        cosmosDb = new
                        {
                            endpointUri = new 
                            { 
                                status = _configuration["CosmosDb:EndpointUri"] != null ? "✓ Configured" : "✗ Missing",
                                value = _configuration["CosmosDb:EndpointUri"] ?? "null"
                            },
                            primaryKey = new 
                            { 
                                status = _configuration["CosmosDb:PrimaryKey"] != null ? "✓ Configured" : "✗ Missing",
                                length = _configuration["CosmosDb:PrimaryKey"]?.Length ?? 0
                            },
                            database = new 
                            { 
                                status = !string.IsNullOrEmpty(_configuration["CosmosDb:DatabaseName"]) ? "✓ Configured" : "✗ Missing",
                                value = _configuration["CosmosDb:DatabaseName"] ?? "null"
                            },
                            container = new 
                            { 
                                status = !string.IsNullOrEmpty(_configuration["CosmosDb:ContainerName"]) ? "✓ Configured" : "✗ Missing",
                                value = _configuration["CosmosDb:ContainerName"] ?? "null"
                            }
                        },
                        jwt = new
                        {
                            secretKey = new 
                            { 
                                status = _configuration["Jwt:SecretKey"] != null ? "✓ Configured" : "✗ Missing",
                                length = _configuration["Jwt:SecretKey"]?.Length ?? 0,
                                minimumLength = 32,
                                valid = (_configuration["Jwt:SecretKey"]?.Length ?? 0) >= 32
                            },
                            issuer = new 
                            { 
                                status = !string.IsNullOrEmpty(_configuration["Jwt:Issuer"]) ? "✓ Configured" : "✗ Missing",
                                value = _configuration["Jwt:Issuer"] ?? "null"
                            },
                            audience = new 
                            { 
                                status = !string.IsNullOrEmpty(_configuration["Jwt:Audience"]) ? "✓ Configured" : "✗ Missing",
                                value = _configuration["Jwt:Audience"] ?? "null"
                            },
                            expirationMinutes = new 
                            { 
                                status = !string.IsNullOrEmpty(_configuration["Jwt:ExpirationMinutes"]) ? "✓ Configured" : "✗ Missing",
                                value = _configuration["Jwt:ExpirationMinutes"] ?? "null"
                            }
                        },
                        applicationInsights = new
                        {
                            instrumentationKey = new 
                            { 
                                status = _configuration["ApplicationInsights:InstrumentationKey"] != null 
                                    ? "✓ Configured" 
                                    : "⚠ Optional - not configured",
                                length = _configuration["ApplicationInsights:InstrumentationKey"]?.Length ?? 0
                            }
                        }
                    }
                };

                _logger.LogInformation("Configuration status requested. Environment: {Environment}", _environment.EnvironmentName);
                return Ok(configStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking configuration status");
                return StatusCode(
                    StatusCodes.Status500InternalServerError, 
                    new 
                    { 
                        error = "Error checking configuration status",
                        message = ex.Message
                    }
                );
            }
        }

        /// <summary>
        /// Database connectivity check (development only)
        /// Verifies that Cosmos DB connection parameters are valid
        /// </summary>
        [HttpGet("db-check")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        public ActionResult<object> CheckDatabaseConnectivity()
        {
            if (!_environment.IsDevelopment())
            {
                return Unauthorized(new 
                { 
                    error = "Database check only available in development environment" 
                });
            }

            try
            {
                var endpointUri = _configuration["CosmosDb:EndpointUri"];
                var primaryKey = _configuration["CosmosDb:PrimaryKey"];
                var databaseName = _configuration["CosmosDb:DatabaseName"];
                var containerName = _configuration["CosmosDb:ContainerName"];

                var checks = new
                {
                    timestamp = DateTime.UtcNow,
                    endpoint = new
                    {
                        configured = !string.IsNullOrEmpty(endpointUri),
                        valid = Uri.TryCreate(endpointUri, UriKind.Absolute, out var _),
                        value = endpointUri ?? "Not configured"
                    },
                    primaryKey = new
                    {
                        configured = !string.IsNullOrEmpty(primaryKey),
                        length = primaryKey?.Length ?? 0
                    },
                    database = new
                    {
                        configured = !string.IsNullOrEmpty(databaseName),
                        value = databaseName ?? "Not configured"
                    },
                    container = new
                    {
                        configured = !string.IsNullOrEmpty(containerName),
                        value = containerName ?? "Not configured"
                    },
                    allConfigured = !string.IsNullOrEmpty(endpointUri) 
                        && !string.IsNullOrEmpty(primaryKey) 
                        && !string.IsNullOrEmpty(databaseName)
                        && !string.IsNullOrEmpty(containerName)
                };

                return Ok(checks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking database connectivity");
                return StatusCode(
                    StatusCodes.Status500InternalServerError, 
                    new 
                    { 
                        error = "Error checking database connectivity",
                        message = ex.Message
                    }
                );
            }
        }
    }
}
