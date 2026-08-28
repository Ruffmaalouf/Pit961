using GarageOS.Application.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GarageOS.Tests.Unit;

/// <summary>
/// WP-7 brief. Proves BrandingOptions binds correctly from configuration (the same
/// strongly-typed options pattern DemoOptionsBindingTests proved for WP-2), and proves
/// the JWT/Branding isolation the Owner's checklist requires: neither options class's
/// binding logic references the other's configuration section, so there is no code path
/// that could derive one from the other. See BrandingConfigPropagationTests.cs (integration)
/// for the stronger, end-to-end proof that changing Branding config never changes an
/// issued JWT's iss/aud claims.
/// </summary>
public class BrandingOptionsBindingTests
{
    [Fact]
    public void BrandingOptions_BindsAllFourFieldsFromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{BrandingOptions.SectionName}:ProductDisplayName"] = "Test Garage Co",
                [$"{BrandingOptions.SectionName}:EmailFromName"] = "Test Garage Co Support",
                [$"{BrandingOptions.SectionName}:LogoUrl"] = "https://example.test/logo.png",
                [$"{BrandingOptions.SectionName}:SupportEmail"] = "support@example.test",
            })
            .Build();

        var services = new ServiceCollection();
        services
            .AddOptions<BrandingOptions>()
            .Bind(configuration.GetSection(BrandingOptions.SectionName));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<BrandingOptions>>().Value;

        Assert.Equal("Test Garage Co", options.ProductDisplayName);
        Assert.Equal("Test Garage Co Support", options.EmailFromName);
        Assert.Equal("https://example.test/logo.png", options.LogoUrl);
        Assert.Equal("support@example.test", options.SupportEmail);
    }

    [Fact]
    public void BrandingOptions_DefaultsToEmptyStrings_WhenNotConfigured()
    {
        var configuration = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services
            .AddOptions<BrandingOptions>()
            .Bind(configuration.GetSection(BrandingOptions.SectionName));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<BrandingOptions>>().Value;

        Assert.Equal(string.Empty, options.ProductDisplayName);
        Assert.Equal(string.Empty, options.EmailFromName);
        Assert.Equal(string.Empty, options.LogoUrl);
        Assert.Equal(string.Empty, options.SupportEmail);
    }

    [Fact]
    public void BrandingOptions_BindsCleanly_WhenOnlyJwtKeysArePresent_NoBrandingKeysAtAll()
    {
        // Isolation proof (Owner checklist): BrandingOptions's binding logic does not
        // reference Jwt:* in any way -- binding still succeeds and produces defaults
        // when the configuration source contains ONLY Jwt:* keys, none of its own.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{JwtOptions.SectionName}:Issuer"] = "isolation-test-issuer",
                [$"{JwtOptions.SectionName}:Audience"] = "isolation-test-audience",
                [$"{JwtOptions.SectionName}:SigningKey"] = new string('a', 32),
            })
            .Build();

        var services = new ServiceCollection();
        services
            .AddOptions<BrandingOptions>()
            .Bind(configuration.GetSection(BrandingOptions.SectionName));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<BrandingOptions>>().Value;

        Assert.Equal(string.Empty, options.ProductDisplayName);
        Assert.Equal(string.Empty, options.EmailFromName);
    }

    [Fact]
    public void JwtOptions_BindsCleanly_WhenOnlyBrandingKeysArePresent_NoJwtKeysAtAll()
    {
        // Inverse isolation proof: JwtOptions's binding logic does not reference
        // Branding:* in any way.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{BrandingOptions.SectionName}:ProductDisplayName"] = "Isolation Test Brand",
                [$"{BrandingOptions.SectionName}:EmailFromName"] = "Isolation Test Brand",
                [$"{BrandingOptions.SectionName}:LogoUrl"] = "https://example.test/logo.png",
                [$"{BrandingOptions.SectionName}:SupportEmail"] = "support@example.test",
            })
            .Build();

        var services = new ServiceCollection();
        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<JwtOptions>>().Value;

        Assert.Equal(string.Empty, options.Issuer);
        Assert.Equal(string.Empty, options.Audience);
        Assert.Equal(string.Empty, options.SigningKey);
    }
}
