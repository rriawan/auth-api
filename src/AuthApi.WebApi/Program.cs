using AuthApi.Application;
using AuthApi.Infrastructure;
using AuthApi.Infrastructure.Persistence;
using AuthApi.WebApi.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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

// 4. Controllers & Built-in .NET 10 OpenAPI
builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "Centralized Auth API (AuthN & AuthZ)";
        document.Info.Version = "v1";
        document.Info.Description =
            "Layanan terpusat Authentication (SSO User Pool, JWT RS256 + JWKS, Refresh Token Rotation) & Authorization (Role + Permission per ClientApp).";
        return Task.CompletedTask;
    });
});

// 5. Health Checks (API & MSSQL Database Connectivity)
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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Centralized Auth API — Scalar UI");
    });
}

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
