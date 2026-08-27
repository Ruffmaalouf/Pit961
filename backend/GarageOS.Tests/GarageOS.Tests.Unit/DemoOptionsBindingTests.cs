using GarageOS.Application.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GarageOS.Tests.Unit;

/// <summary>
/// WP-2 harness-proof test: confirms the strongly-typed options / configuration-binding
/// pattern (<see cref="DemoOptions"/> via <see cref="IOptions{TOptions}"/>) actually
/// binds from configuration rather than being hardcoded, and that a config-value
/// change is reflected without any code change — the exact pattern later WPs
/// (JwtOptions in WP-4, BrandingOptions in WP-7) will follow.
/// </summary>
public class DemoOptionsBindingTests
{
    [Fact]
    public void DemoOptions_BindsFromConfiguration()
    {
        var expectedMessage = $"unit-test-value-{Guid.NewGuid()}";

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{DemoOptions.SectionName}:Message"] = expectedMessage,
            })
            .Build();

        var services = new ServiceCollection();
        services
            .AddOptions<DemoOptions>()
            .Bind(configuration.GetSection(DemoOptions.SectionName));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<DemoOptions>>();

        Assert.Equal(expectedMessage, options.Value.Message);
    }

    [Fact]
    public void DemoOptions_DefaultsToEmptyMessage_WhenNotConfigured()
    {
        var configuration = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services
            .AddOptions<DemoOptions>()
            .Bind(configuration.GetSection(DemoOptions.SectionName));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<DemoOptions>>();

        Assert.Equal(string.Empty, options.Value.Message);
    }
}
