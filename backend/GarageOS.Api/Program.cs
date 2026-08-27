using GarageOS.Api.Endpoints;
using GarageOS.Api.Middleware;
using GarageOS.Application.Configuration;
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

app.Run();

// Exposes the implicit Program class to GarageOS.Tests.Integration's
// WebApplicationFactory<Program>.
public partial class Program;
