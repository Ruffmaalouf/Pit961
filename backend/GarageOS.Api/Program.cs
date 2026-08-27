using GarageOS.Api.Endpoints;
using GarageOS.Api.Middleware;
using GarageOS.Application.Abstractions;
using GarageOS.Application.Configuration;
using GarageOS.Infrastructure.Data;
using GarageOS.Infrastructure.Data.Platform;
using GarageOS.Infrastructure.Data.Seed;
using GarageOS.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// --- Serilog -----------------------------------------------------------
// Reads its configuration from the "Serilog" section of appsettings.json /
// appsettings.Development.json (see below) rather than being hardcoded here.
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// --- Services ------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

// --- Multi-tenancy / data access (WP-3) -----------------------------------
builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<AppDbContext>((sp, options) => options
    .UseNpgsql(builder.Configuration.GetConnectionString("GarageOsDb"))
    .UseSnakeCaseNamingConvention());

builder.Services.AddDbContext<PlatformDbContext>((sp, options) => options
    .UseNpgsql(
        builder.Configuration.GetConnectionString("PlatformDb")
            ?? builder.Configuration.GetConnectionString("GarageOsDb"), // same physical DB in Phase 1
        npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history_platform"))
    .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<ICurrentTenant, HttpContextCurrentTenant>();

// Global ProblemDetails exception handling (WP-2 acceptance criterion: an
// unhandled exception returns a consistent ProblemDetails envelope).
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Strongly-typed options / configuration-binding pattern demonstration.
// Later WPs follow this same pattern for JwtOptions (WP-4) and BrandingOptions
// (WP-7) — WP-2 owns the pattern only, not those option classes themselves.
builder.Services
    .AddOptions<DemoOptions>()
    .BindConfiguration(DemoOptions.SectionName)
    .ValidateOnStart();

var app = builder.Build();

// --- Middleware pipeline ---------------------------------------------------
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapDemoEndpoints();

// Test-only diagnostic surface — never mapped in Production. "Testing" is the
// environment name GarageOS.Tests.Integration's WebApplicationFactory sets.
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.MapDiagnosticsEndpoints();
}

// Development-only seed data (WP-3 brief §11) — never Production, never Testing
// (integration tests seed their own fixtures per-test).
if (app.Environment.IsDevelopment())
{
    using var seedScope = app.Services.CreateScope();
    var seedDbContext = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DevelopmentSeeder.SeedAsync(seedDbContext);
}

app.Run();

// Exposes the implicit Program class to GarageOS.Tests.Integration's
// WebApplicationFactory<Program>.
public partial class Program;
