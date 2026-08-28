namespace GarageOS.Tests.Unit.Architecture;

using System.Reflection;
using GarageOS.Api.Controllers;
using GarageOS.Application.Configuration;
using Microsoft.Extensions.Options;

/// <summary>
/// WP-7 brief §4, structural leak-prevention layer 2 (see ConfigController's own doc
/// comment). Proves by reflection -- not by reading the source and trusting it stays that
/// way -- that ConfigController's constructor takes EXACTLY one dependency,
/// <see cref="IOptions{TOptions}"/> of <see cref="BrandingOptions"/>, and nothing else.
/// It is therefore structurally incapable of reaching IConfiguration, JwtOptions,
/// PasswordResetOptions, a connection string, or any other options/config type: there is
/// no object graph reachable from this controller that contains them. A future edit that
/// adds a second constructor parameter -- even an apparently harmless one like
/// IConfiguration for "just one more setting" -- fails this test immediately, forcing a
/// deliberate, reviewed decision rather than a silent widening of what this
/// [AllowAnonymous] endpoint can reach.
/// </summary>
public class ConfigControllerDependencySurfaceTests
{
    [Fact]
    public void ConfigController_HasExactlyOneConstructor()
    {
        var constructors = typeof(ConfigController).GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        Assert.Single(constructors);
    }

    [Fact]
    public void ConfigController_SoleConstructor_TakesExactlyOneParameter_IOptionsOfBrandingOptions()
    {
        var constructor = typeof(ConfigController).GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();
        var parameters = constructor.GetParameters();

        Assert.Single(parameters);
        Assert.Equal(typeof(IOptions<BrandingOptions>), parameters[0].ParameterType);
    }

    [Fact]
    public void ConfigController_HasNoFieldsOfAnyOtherOptionsOrConfigurationType()
    {
        // Belt-and-suspenders beyond the constructor-surface checks above: even a field
        // assigned some other way (not through the constructor) would show up here.
        var fields = typeof(ConfigController).GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

        foreach (var field in fields)
        {
            var fieldTypeName = field.FieldType.FullName ?? field.FieldType.Name;
            Assert.True(
                field.FieldType == typeof(BrandingOptions),
                $"ConfigController has an unexpected field '{field.Name}' of type '{fieldTypeName}' -- " +
                "this controller's object graph must contain nothing but BrandingOptions " +
                "(WP-7 brief §4 structural leak-prevention layer 2).");
        }
    }
}
