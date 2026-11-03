using System.Threading.RateLimiting;
using AuthApi.Application;
using AuthApi.Application.Common.Models;
using AuthApi.Infrastructure;
using AuthApi.Infrastructure.Persistence;
using AuthApi.WebApi.Controllers;
using AuthApi.WebApi.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. ProblemDetails (RFC 9457) & Global Exception Handling
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// 2. Application & Infrastructure Layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// 3. Authentication (JWT RS256) & Authorization
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services.ConfigureOptions<JwtBearerOptionsSetup>();
builder.Services.AddAuthorization();

// 4. Controlled CORS Policy
var allowedOrigins = builder.Configuration
    .GetSection("AllowedCorsOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("ConfiguredOrigins", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .WithHeaders("Authorization", "Content-Type", AuthController.ClientIdHeaderName, AuthController.ApiKeyHeaderName)
                .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                .AllowCredentials();
        }
        else if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                .WithHeaders("Authorization", "Content-Type", AuthController.ClientIdHeaderName, AuthController.ApiKeyHeaderName)
                .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS");
        }
    });
});

// 5. Built-in Rate Limiting Berbasis IP untuk Endpoint Publik Sensitif
var securitySettings = builder.Configuration
    .GetSection(SecuritySettings.SectionName)
    .Get<SecuritySettings>() ?? new SecuritySettings();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too Many Requests",
            Detail = "Terlalu banyak permintaan dari alamat IP ini. Silakan tunggu beberapa saat sebelum mencoba kembali.",
            Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}"
        };
        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        problem.Extensions["timestampUtc"] = DateTime.UtcNow;

        await context.HttpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
    };

    options.AddPolicy(RateLimitPolicies.PublicAuth, httpContext =>
    {
        var ipKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ipKey,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = securitySettings.PublicEndpointRateLimitPermitCount > 0
                    ? securitySettings.PublicEndpointRateLimitPermitCount
                    : 15,
                Window = TimeSpan.FromSeconds(
                    securitySettings.PublicEndpointRateLimitWindowSeconds > 0
                        ? securitySettings.PublicEndpointRateLimitWindowSeconds
                        : 60),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
});

// 6. Controllers & Built-in .NET 10 OpenAPI
builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "Centralized Auth API (AuthN & AuthZ)";
        document.Info.Version = "v1";
        document.Info.Description =
            "Layanan terpusat Authentication (SSO User Pool, JWT RS256 + JWKS, Refresh Token Rotation) & Authorization (Role + Permission per ClientApp).";

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Masukkan Access Token JWT RS256 (tanpa kata 'Bearer ')."
        };
        document.Components.SecuritySchemes["ClientId"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = AuthController.ClientIdHeaderName,
            Description = "Kode unik aplikasi terdaftar (contoh: auth-admin atau erp-core)."
        };
        document.Components.SecuritySchemes["ClientApiKey"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = AuthController.ApiKeyHeaderName,
            Description = "Raw API Key milik aplikasi terdaftar."
        };

        return Task.CompletedTask;
    });
});

// 7. Health Checks (API & MSSQL Database Connectivity)
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("AuthApi service is running."))
    .AddCheck<DatabaseHealthCheck>("mssql");

var app = builder.Build();

// Mendukung eksekusi Database Seeder via CLI (`dotnet run --project src/AuthApi.WebApi -- --seed`)
// atau konfigurasi eksplisit BootstrapAdmin:SeedOnStartup = true (default false sesuai aturan operasi database).
var runSeedFromCli = args.Any(a => string.Equals(a, "--seed", StringComparison.OrdinalIgnoreCase));
var seedOnStartup = app.Configuration.GetValue<bool>("BootstrapAdmin:SeedOnStartup");

if (runSeedFromCli || seedOnStartup)
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();

    if (runSeedFromCli)
    {
        return;
    }
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseCors("ConfiguredOrigins");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Centralized Auth API — Scalar UI");
    });
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var payload = new
        {
            status = report.Status.ToString(),
            timestampUtc = DateTime.UtcNow,
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            entries = report.Entries.ToDictionary(
                e => e.Key,
                e => new
                {
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    durationMs = e.Value.Duration.TotalMilliseconds
                })
        };
        await context.Response.WriteAsJsonAsync(payload);
    }
});

app.MapControllers();

app.Run();

public partial class Program;
