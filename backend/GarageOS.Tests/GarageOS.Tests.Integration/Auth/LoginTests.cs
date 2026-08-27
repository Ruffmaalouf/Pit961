using System.Net;
using System.Net.Http.Json;
using GarageOS.Api.Contracts;
using GarageOS.Tests.Integration.TestSupport;

namespace GarageOS.Tests.Integration.Auth;

[Collection("Integration")]
public class LoginTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Login_CorrectCredentials_Returns200WithAccessTokenAndUserAndRefreshCookie()
    {
        await fixture.ResetDatabaseAsync();
        var (_, garage, user) = await AuthTestFixtures.SeedActiveUserAsync(fixture, garageName: "Login Test Garage");

        var client = fixture.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(user.Email, AuthTestFixtures.DefaultPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
        Assert.Equal(garage.Id, body.User.GarageId);
        Assert.Equal("Login Test Garage", body.User.GarageName);
        Assert.Equal(user.Email, body.User.Email);

        // Refresh token must be a cookie, never in the JSON body.
        var cookie = CookieTestHelpers.ExtractCookieValue(response, "garageos_refresh_token");
        Assert.NotNull(cookie);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsGenericUnauthorized()
    {
        await fixture.ResetDatabaseAsync();
        var (_, _, user) = await AuthTestFixtures.SeedActiveUserAsync(fixture);

        var client = fixture.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(user.Email, "WrongPassword123"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsIdenticalUnauthorizedAsWrongPassword()
    {
        await fixture.ResetDatabaseAsync();
        var (_, _, user) = await AuthTestFixtures.SeedActiveUserAsync(fixture);
        var client = fixture.CreateClient();

        var wrongPasswordResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(user.Email, "WrongPassword123"));
        var unknownEmailResponse = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest($"nobody+{Guid.NewGuid():N}@example.test", "WhateverPassword123"));

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPasswordResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmailResponse.StatusCode);
        var wrongPasswordBody = await wrongPasswordResponse.Content.ReadAsStringAsync();
        var unknownEmailBody = await unknownEmailResponse.Content.ReadAsStringAsync();
        Assert.Equal(wrongPasswordBody, unknownEmailBody);
    }

    [Fact]
    public async Task Login_InactiveAccount_ReturnsGenericUnauthorized()
    {
        await fixture.ResetDatabaseAsync();
        var (_, _, user) = await AuthTestFixtures.SeedActiveUserAsync(fixture, isActive: false);

        var client = fixture.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(user.Email, AuthTestFixtures.DefaultPassword));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
