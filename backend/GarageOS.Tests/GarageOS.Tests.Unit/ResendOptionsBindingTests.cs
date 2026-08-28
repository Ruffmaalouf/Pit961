using GarageOS.Application.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GarageOS.Tests.Unit;

/// <summary>
/// WP-6 brief. Proves ResendOptions binds correctly from configuration (same strongly-
/// typed options pattern as JwtOptions/BrandingOptions), that it carries no embedded
/// default/fallback API key, and the isolation half of the same proof
/// BrandingOptionsBindingTests already established for Branding/Jwt: ResendOptions'
/// binding logic does not reference Jwt:*/Branding:* in any way.
/// </summary>
public class ResendOptionsBindingTests
{
    [Fact]
    public void ResendOptions_BindsFromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{ResendOptions.SectionName}:ApiKey"] = "re_test_isolation_key",
                [$"{ResendOptions.SectionName}:FromAddress"] = "no-reply@example.test",
            })
            .Build();

        var services = new ServiceCollection();
        services
            .AddOptions<ResendOptions>()
            .Bind(configuration.GetSection(ResendOptions.SectionName));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ResendOptions>>().Value;

        Assert.Equal("re_test_isolation_key", options.ApiKey);
        Assert.Equal("no-reply@example.test", options.FromAddress);
    }

    [Fact]
    public void ResendOptions_DefaultApiKey_IsEmptyString_NotARealLookingKey()
    {
        // The class itself carries no embedded default/fallback key -- the C#-level
        // complement to scripts/ci/check-no-resend-outside-service.sh, which scans
        // configuration/source files rather than this class's own default-value behavior.
        var configuration = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services
            .AddOptions<ResendOptions>()
            .Bind(configuration.GetSection(ResendOptions.SectionName));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ResendOptions>>().Value;

        Assert.Equal(string.Empty, options.ApiKey);
        Assert.Equal(string.Empty, options.FromAddress);
    }

    [Fact]
    public void ResendOptions_BindsCleanly_WhenOnlyJwtAndBrandingKeysArePresent_NoResendKeysAtAll()
    {
        // Isolation proof: ResendOptions's binding logic does not reference Jwt:*/
        // Branding:* in any way -- binding still succeeds and produces defaults when the
        // configuration source contains ONLY other sections' keys, none of its own.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{JwtOptions.SectionName}:Issuer"] = "isolation-test-issuer",
                [$"{JwtOptions.SectionName}:Audience"] = "isolation-test-audience",
                [$"{JwtOptions.SectionName}:SigningKey"] = new string('a', 32),
                [$"{BrandingOptions.SectionName}:ProductDisplayName"] = "Isolation Test Brand",
                [$"{BrandingOptions.SectionName}:EmailFromName"] = "Isolation Test Brand",
            })
            .Build();

        var services = new ServiceCollection();
        services
            .AddOptions<ResendOptions>()
            .Bind(configuration.GetSection(ResendOptions.SectionName));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ResendOptions>>().Value;

        Assert.Equal(string.Empty, options.ApiKey);
        Assert.Equal(string.Empty, options.FromAddress);
    }

    [Fact]
    public void JwtAndBrandingOptions_BindCleanly_WhenOnlyResendKeysArePresent_NoOwnKeysAtAll()
    {
        // Inverse isolation proof: neither JwtOptions nor BrandingOptions's binding
        // logic references Resend:* in any way.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{ResendOptions.SectionName}:ApiKey"] = "re_test_isolation_key",
                [$"{ResendOptions.SectionName}:FromAddress"] = "no-reply@example.test",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddOptions<JwtOptions>().Bind(configuration.GetSection(JwtOptions.SectionName));
        services.AddOptions<BrandingOptions>().Bind(configuration.GetSection(BrandingOptions.SectionName));

        using var provider = services.BuildServiceProvider();
        var jwtOptions = provider.GetRequiredService<IOptions<JwtOptions>>().Value;
        var brandingOptions = provider.GetRequiredService<IOptions<BrandingOptions>>().Value;

        Assert.Equal(string.Empty, jwtOptions.Issuer);
        Assert.Equal(string.Empty, jwtOptions.SigningKey);
        Assert.Equal(string.Empty, brandingOptions.ProductDisplayName);
        Assert.Equal(string.Empty, brandingOptions.EmailFromName);
    }
}
