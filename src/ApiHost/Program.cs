// ====================================================================
// VERIXORA – ApiHost / Program.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   The composition root of the entire VERIXORA backend.  This is
//   the ONLY executable project in the solution.  It wires up the
//   dependency injection container, configures all middleware, and
//   starts the HTTP server.  All module Presentation projects are
//   discovered and registered here.
//
//   WHY A SEPARATE HOST PROJECT:
//     - The host knows about all modules, but modules don't know
//       about each other (modular monolith pattern).
//     - Cross‑cutting concerns (logging, tracing, rate limiting,
//       health checks, encryption) are configured once, here.
//     - The host is the single place to change the deployment
//       topology (e.g., add a new module, enable HTTPS, etc.).
//
//   MIDDLEWARE ORDER (top to bottom = outer to inner):
//     1. Serilog request logging    – logs every HTTP request
//     2. Rate limiter               – blocks excessive requests
//     3. Health checks              – /health endpoint
//     4. Controllers                – API endpoints (innermost)
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **Top‑level statements** (C# 9+):
//    - No `Main` method or `class Program`.  The compiler generates
//      them automatically.  This keeps the host file clean.
//
// 2. **WebApplication.CreateBuilder(args)**:
//    - Creates a builder with default ASP.NET Core settings
//      (appsettings.json, environment variables, logging).
//
// 3. **builder.Services** (IServiceCollection):
//    - The dependency injection container.  Every `Add*` call
//      registers a service so it can be injected into controllers
//      and handlers.
//
// 4. **builder.Host.UseSerilog()**:
//    - Replaces the default ASP.NET Core logging with Serilog,
//      which provides structured, queryable logs.
//
// 5. **AddApiVersioning()**:
//    - Enables URL‑based versioning (/api/v1/..., /api/v2/...).
//      Old versions are kept alive so mobile clients don't break.
//
// 6. **AddSwaggerGen()**:
//    - Generates OpenAPI documentation from XML comments.
//      Developers can test endpoints at /swagger.
//
// 7. **AddControllers().AddApplicationPart()**:
//    - Scans the Identity.Presentation assembly for [ApiController]
//      classes and registers them.  Without this, the host would
//      not discover the AuthController.
//
// 8. **AddVerixoraEncryption()**:
//    - Registers IEncryptionService, IKeyProvider, IAadProvider,
//      and binds EncryptionOptions from configuration.  Required
//      by the EF Core EncryptionConverter for column‑level
//      encryption.
//
// 9. **Middleware pipeline** (app.Use...):
//    - `UseSerilogRequestLogging()` – logs each request.
//    - `UseRateLimiter()` – enforces ADR‑020 limits.
//    - `MapHealthChecks("/health")` – exposes /health for
//      Kubernetes / load balancer probes.
//    - `MapControllers()` – maps HTTP routes to controller actions.
// ====================================================================

using BuildingBlocks.Infrastructure.Encryption;
using BuildingBlocks.Infrastructure.HealthChecks;
using BuildingBlocks.Infrastructure.RateLimiting;
using BuildingBlocks.Infrastructure.Tracing;
using Identity.Application;
using Identity.Infrastructure;
using Serilog;

// ================================================================
// Step 1: Create the application builder
// ================================================================
// WebApplication.CreateBuilder reads appsettings.json, environment
// variables, and command‑line arguments to configure the host.
var builder = WebApplication.CreateBuilder(args);

// ================================================================
// Step 2: Configure structured logging (Serilog)
// ================================================================
// Serilog replaces the default console logger with structured
// logging.  Every log entry includes TenantId, UserId, and
// CorrelationId (via the enricher registered in BuildingBlocks).
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)   // read settings from appsettings
    .Enrich.FromLogContext()                     // push properties from code
    .WriteTo.Console());                         // output to console (Docker/stdout)

// ================================================================
// Step 3: API versioning (ADR‑015)
// ================================================================
// All routes are prefixed with /api/v1/.  When we introduce
// breaking changes, we add /api/v2/ and keep v1 for one release.
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;   // adds "api-supported-versions" header
});

// ================================================================
// Step 4: Swagger / OpenAPI (ADR‑034)
// ================================================================
// Swagger reads XML documentation from all Presentation projects.
// In production, Swagger is disabled (see middleware pipeline).
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Find all XML doc files generated by Presentation projects.
    var xmlFiles = Directory.GetFiles(
        AppContext.BaseDirectory, "*.Presentation.xml", SearchOption.AllDirectories);
    foreach (var xmlFile in xmlFiles)
        options.IncludeXmlComments(xmlFile);
});

// ================================================================
// Step 5: Global rate limiting (ADR‑020)
// ================================================================
// Enforces 100 req/min/user, 200 req/min/IP, 500 req/min/API key.
// Unlock endpoints have an additional burst limit (see BuildingBlocks).
builder.Services.AddVerixoraRateLimiting();

// ================================================================
// Step 6: Health checks (for Kubernetes / load balancer)
// ================================================================
// The /health endpoint reports database and MQTT broker status.
builder.Services.AddVerixoraHealthChecks(
    mqttBrokerAddress: builder.Configuration["Mqtt:BrokerAddress"]);

// ================================================================
// Step 7: OpenTelemetry distributed tracing (ADR‑021)
// ================================================================
// Every incoming HTTP request creates a trace span.  The unlock
// pipeline's 200ms p95 SLA is measurable because each step is
// instrumented.
builder.Services.AddVerixoraTracing();

// ================================================================
// Step 8: Register the Identity module
// ================================================================
// AddIdentityApplication() – registers MediatR handlers, validators,
//   and SharedKernel pipeline behaviours.
// AddIdentityInfrastructure(config) – registers the DbContext,
//   repositories, and service implementations (JWT, password hasher).
builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure(builder.Configuration);

// ================================================================
// Step 9: Register encryption services (needed by EF Core converters)
// ================================================================
builder.Services.AddVerixoraEncryption(builder.Configuration);

// ================================================================
// Step 10: Discover controllers in module Presentation projects
// ================================================================
// Without AddApplicationPart, the host would only scan its own
// assembly for controllers.  We explicitly tell it to look in
// Identity.Presentation for the AuthController.
builder.Services.AddControllers()
    .AddApplicationPart(typeof(Identity.Presentation.Controllers.AuthController).Assembly);

// ================================================================
// Step 11: Build the application
// ================================================================
var app = builder.Build();

// ================================================================
// Step 12: Configure the HTTP request pipeline (middleware)
// ================================================================

// --- Development‑only tools ---
if (app.Environment.IsDevelopment())
{
    // Swagger UI at /swagger – interactive API documentation.
    app.UseSwagger();
    app.UseSwaggerUI();
}

// --- Production middleware (applies in all environments) ---

// Log every HTTP request with its status code and duration.
app.UseSerilogRequestLogging();

// Apply rate limiting policies (enforces ADR‑020 limits).
app.UseRateLimiter();

// Map the /health endpoint for infrastructure probes.
app.MapHealthChecks("/health");

// Initialise the static encryption service for EF Core value converters.
// The service itself is registered above; here we grab the singleton
// and store it in a static field so the parameterless converter can use it.
BuildingBlocks.Infrastructure.Encryption.EncryptionConverter.EncryptionService =
    app.Services.GetRequiredService<IEncryptionService>();

// Map controller routes (e.g., /api/v1/auth/register).
app.MapControllers();

// ================================================================
// Step 13: Start the server
// ================================================================
app.Run();
