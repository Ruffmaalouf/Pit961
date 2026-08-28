using System.Text;
using System.Threading.RateLimiting;
using GarageOS.Api.Endpoints;
using GarageOS.Api.Middleware;
using GarageOS.Application.Abstractions;
using GarageOS.Application.Auth;
using GarageOS.Application.Common;
using GarageOS.Application.Estimates;
using GarageOS.Application.Configuration;
using GarageOS.Infrastructure.BackgroundJobs;
using GarageOS.Infrastructure.Data;
using GarageOS.Infrastructure.Data.Auth;
using GarageOS.Infrastructure.Data.Platform;
using GarageOS.Infrastructure.Data.Provisioning;
using GarageOS.Infrastructure.Data.Estimates;
using GarageOS.Infrastructure.Data.Seed;
using GarageOS.Infrastructure.Email;
using GarageOS.Infrastructure.Security;
using GarageOS.Infrastructure.Security.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
builder.Services.AddScoped<IAccountProvisioningService, AccountProvisioningService>();

// Global ProblemDetails exception handling (WP-2 acceptance criterion: an
// unhandled exception returns a consistent ProblemDetails envelope).
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Strongly-typed options / configuration-binding pattern demonstration (WP-2).
builder.Services
    .AddOptions<DemoOptions>()
    .BindConfiguration(DemoOptions.SectionName)
    .ValidateOnStart();

// --- Authentication / JWT / Authorization (WP-4) ---------------------------
// JwtOptions: mandatory ValidateOnStart -- a missing/short SigningKey crashes the app at
// boot in EVERY environment, never a silent default-key fallback (WP-4 brief §3).
builder.Services
    .AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer), "Jwt:Issuer is required.")
    .Validate(o => !string.IsNullOrWhiteSpace(o.Audience), "Jwt:Audience is required.")
    .Validate(
        o => Encoding.UTF8.GetByteCount(o.SigningKey ?? string.Empty) >= 32,
        "Jwt:SigningKey must be at least 32 bytes (256 bits). Generate it with a CSPRNG " +
        "(e.g. `openssl rand -base64 32`) -- never a human-chosen passphrase. See " +
        "JwtOptions.cs and README.md for the per-environment provisioning story.")
    .ValidateOnStart();

builder.Services
    .AddOptions<PasswordResetOptions>()
    .BindConfiguration(PasswordResetOptions.SectionName)
    .ValidateOnStart();

// JwtOptions is needed synchronously here (before the DI container is built) to
// configure AddJwtBearer's TokenValidationParameters -- read directly from
// IConfiguration rather than through IOptions<JwtOptions>, since the signing key never
// changes at runtime and this avoids the extra IPostConfigureOptions<JwtBearerOptions>
// indirection just to reach a value builder.Configuration already has.
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtIssuer = jwtSection["Issuer"] ?? string.Empty;
var jwtAudience = jwtSection["Audience"] ?? string.Empty;
var jwtSigningKey = jwtSection["SigningKey"] ?? string.Empty;
var jwtKeyId = jwtSection["KeyId"] ?? "1";
var jwtPreviousSigningKey = jwtSection["PreviousSigningKey"];
var jwtPreviousKeyId = jwtSection["PreviousKeyId"];
var jwtPreviousSigningKeyValidUntil = jwtSection["PreviousSigningKeyValidUntil"] is { } validUntilRaw
    && DateTimeOffset.TryParse(validUntilRaw, out var parsedValidUntil)
        ? parsedValidUntil
        : (DateTimeOffset?)null;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // JwtBearerOptions.MapInboundClaims defaults to true, which silently remaps
        // "sub"->ClaimTypes.NameIdentifier, "role"->ClaimTypes.Role, etc. before
        // HttpContext.User is populated. Left at default, HttpContextCurrentTenant's
        // literal FindFirst("sub")/FindFirst("role") reads would throw
        // TenantContextUnavailableException on EVERY authenticated request (WP-4 brief
        // §6 critical finding).
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30), // minimized from the 5-minute default
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidateIssuerSigningKey = true,
            // Resolver restricted to the configured current/previous key by kid -- not a
            // wildcard "any key we know about" resolver. Previous key only accepted while
            // PreviousSigningKeyValidUntil is still in the future (WP-4 brief §4/§16).
            IssuerSigningKeyResolver = (_, _, kid, _) =>
            {
                var keys = new List<SecurityKey>();
                if (!string.IsNullOrEmpty(jwtSigningKey) && (kid is null || kid == jwtKeyId))
                {
                    keys.Add(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)) { KeyId = jwtKeyId });
                }
                if (!string.IsNullOrEmpty(jwtPreviousSigningKey)
                    && jwtPreviousSigningKeyValidUntil is { } validUntil && validUntil > DateTimeOffset.UtcNow
                    && kid == jwtPreviousKeyId)
                {
                    keys.Add(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtPreviousSigningKey)) { KeyId = jwtPreviousKeyId });
                }
                return keys;
            },
        };
    });

// Two mutual-exclusion policies (WP-4 brief §6). "GarageTenant" gates every tenant-scoped
// endpoint (including /me) -- a validly-authenticated platform-admin token would otherwise
// fall through to application code and crash into HttpContextCurrentTenant.GarageId
// throwing, surfacing as a generic 500 instead of a clean 403. "PlatformAdminOnly" is
// attached to zero controller actions in Phase 1 (brief §0: no live platform-admin
// route) -- it exists so the mutual-exclusion assertion is testable directly via
// IAuthorizationService.AuthorizeAsync.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("GarageTenant", policy => policy.Requirements.Add(new GarageTenantRequirement()));
    options.AddPolicy("PlatformAdminOnly", policy => policy.Requirements.Add(new PlatformAdminRequirement()));
    // WP-5 brief §3: resource-based, never attached via a bare [Authorize(Policy=...)]
    // attribute -- see DiscountLimitRequirement.cs / EstimateApprovalThresholdRequirement.cs
    // doc comments and AuthorizationAttributeMisuseTests for why. Invoked explicitly via
    // IAuthorizationService.AuthorizeAsync(user, resource, policyName) from inside
    // AspNetBusinessRuleAuthorizer.
    options.AddPolicy("DiscountLimit", policy => policy.Requirements.Add(new DiscountLimitRequirement()));
    options.AddPolicy("EstimateApprovalThreshold", policy => policy.Requirements.Add(new EstimateApprovalThresholdRequirement()));
});
builder.Services.AddSingleton<IAuthorizationHandler, GarageTenantHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, PlatformAdminHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, DiscountLimitHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, EstimateApprovalThresholdHandler>();

// WP-5 brief §4 -- Application stays framework-free, same split as
// ICurrentTenant/HttpContextCurrentTenant above.
builder.Services.AddScoped<IBusinessRuleAuthorizer, AspNetBusinessRuleAuthorizer>();
builder.Services.AddScoped<IEstimateMutationRepository, EstimateMutationRepository>();
builder.Services.AddScoped<EstimateDiscountService>();
builder.Services.AddScoped<EstimateApprovalService>();

// Sliding-window rate limiters, partitioned by remote IP (WP-4 brief §15). Real-IP
// resolution behind a reverse proxy (X-Forwarded-For trust) is deferred per Decision #5
// -- correct for direct-connection dev/CI/staging; needs ForwardedHeadersOptions once a
// hosting provider is chosen.
//
// Permit/window values are configuration-driven (bound below with production-safe
// fallback defaults), NOT hardcoded -- discovered necessary because
// GarageOS.Tests.Integration's IntegrationTestFixture shares ONE WebApplicationFactory
// (and therefore one in-memory rate-limiter partition store) across the entire
// "Integration" xunit collection; every TestServer request reports the same loopback
// RemoteIpAddress, so all of WP-4's functional login/refresh/forgot-password/
// reset-password tests would otherwise compete for a single production-sized (e.g. 5/min)
// budget and spuriously 429. appsettings.Testing.json sets generous test-only limits;
// Development/Production get the fallback defaults below when "RateLimiting" is absent.
// RateLimitingTests.cs proves the mechanism itself still enforces a tight limit, using
// its own isolated WebApplicationFactory instance (not this shared fixture).
//
// Local functions: declared here, ahead of their uses below -- local function
// declarations have whole-block scope in C#, but keeping them textually first reads more
// naturally in a top-level-statements file.
static int ReadRateLimitSetting(IConfiguration configuration, string policy, string key, int fallback) =>
    int.TryParse(configuration[$"RateLimiting:{policy}:{key}"], out var parsed) ? parsed : fallback;

static void AddAuthRateLimitPolicy(RateLimiterOptions options, string policyName, int permitLimit, TimeSpan window) =>
    options.AddPolicy(policyName, context => RateLimitPartition.GetSlidingWindowLimiter(
        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = window,
            SegmentsPerWindow = 4,
            QueueLimit = 0,
        }));

// IMPORTANT: builder.Configuration (a ConfigurationManager) is read INSIDE this
// AddRateLimiter callback deliberately, not into top-level `var`s beforehand. Services.
// Configure<T>-style registrations (which AddRateLimiter uses under the hood) invoke
// their callback lazily -- only once RateLimiterOptions is first resolved, which happens
// well after builder.Build(). Reading eagerly, before Build(), would silently miss any
// test-only configuration override applied via WebApplicationFactory's
// ConfigureAppConfiguration (RateLimitingTests.cs), the same reason the DbContext
// registrations below read builder.Configuration from inside a lambda rather than into a
// pre-computed variable.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        // WriteAsJsonAsync's contentType parameter must be passed explicitly -- setting
        // Response.ContentType beforehand and then calling the (options, cancellationToken)
        // overload gets silently overwritten back to "application/json; charset=utf-8"
        // (RateLimitingTests.cs caught this as a real behavioral gap, not just a missing
        // assertion: every OTHER error path in this app, e.g. GlobalExceptionHandler,
        // returns a genuine application/problem+json envelope).
        await context.HttpContext.Response.WriteAsJsonAsync(
            new ProblemDetails { Status = 429, Title = "Too many requests. Please try again later." },
            options: (System.Text.Json.JsonSerializerOptions?)null,
            contentType: "application/problem+json",
            cancellationToken: ct);
    };

    var configuration = builder.Configuration;
    AddAuthRateLimitPolicy(options, "auth-login",
        ReadRateLimitSetting(configuration, "AuthLogin", "PermitLimit", 5),
        TimeSpan.FromSeconds(ReadRateLimitSetting(configuration, "AuthLogin", "WindowSeconds", 60)));
    AddAuthRateLimitPolicy(options, "auth-refresh",
        ReadRateLimitSetting(configuration, "AuthRefresh", "PermitLimit", 20),
        TimeSpan.FromSeconds(ReadRateLimitSetting(configuration, "AuthRefresh", "WindowSeconds", 60)));
    AddAuthRateLimitPolicy(options, "auth-forgot-password",
        ReadRateLimitSetting(configuration, "AuthForgotPassword", "PermitLimit", 3),
        TimeSpan.FromSeconds(ReadRateLimitSetting(configuration, "AuthForgotPassword", "WindowSeconds", 600)));
    AddAuthRateLimitPolicy(options, "auth-reset-password",
        ReadRateLimitSetting(configuration, "AuthResetPassword", "PermitLimit", 5),
        TimeSpan.FromSeconds(ReadRateLimitSetting(configuration, "AuthResetPassword", "WindowSeconds", 600)));
});

// --- Auth service registrations (WP-4) --------------------------------------
// AuthService (Application layer) takes plain JwtOptions/PasswordResetOptions -- not
// IOptions<T> -- to keep GarageOS.Application free of a Microsoft.Extensions.Options
// package reference (matches its existing framework-free pattern). The AddOptions<T>()
// .ValidateOnStart() registrations above still run the mandatory boot-time validation;
// these two just project out .Value for direct constructor injection.
builder.Services.AddScoped(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<JwtOptions>>().Value);
builder.Services.AddScoped(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PasswordResetOptions>>().Value);
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasherService>();
builder.Services.AddScoped<IUserAuthLookupRepository, UserAuthLookupRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
builder.Services.AddScoped<AuthService>();

// IEmailService is architecturally WP-6's (see IEmailService.cs governance comment) --
// NoOpEmailService is an explicitly-temporary stub until WP-6 lands (WP-4 brief §14).
builder.Services.AddScoped<IEmailService, NoOpEmailService>();

// Bounded in-process queue + background consumer implementing the anti-enumeration
// mechanism (WP-4 brief §13). Singleton queue (wraps one Channel for the process
// lifetime); the background service resolves scoped services (AuthService/AppDbContext)
// per item via IServiceScopeFactory.
builder.Services.AddSingleton<IPasswordResetRequestQueue, PasswordResetRequestQueue>();
builder.Services.AddHostedService<PasswordResetRequestBackgroundService>();

var app = builder.Build();

// --- Middleware pipeline ---------------------------------------------------
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

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
    var seedProvisioning = seedScope.ServiceProvider.GetRequiredService<IAccountProvisioningService>();
    await DevelopmentSeeder.SeedAsync(seedDbContext, seedProvisioning);
}

app.Run();

// Exposes the implicit Program class to GarageOS.Tests.Integration's
// WebApplicationFactory<Program>.
public partial class Program;
