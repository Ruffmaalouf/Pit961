using System.Net;
using System.Net.Http.Json;
using GarageOS.Api.Contracts;
using GarageOS.Tests.Integration.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Tests.Integration.Auth;

/// <summary>WP-4 brief §11: 5 consecutive failed logins locks the account for 15
/// minutes; a locked-out attempt (even with the correct password) must return the SAME
/// generic 401 as any other failure -- revealing "account locked" would itself be an
/// enumeration/timing signal (brief §12).</summary>
[Collection("Integration")]
public class AccountLockoutTests(IntegrationTestFixture fixture)
{
    private const string WrongPassword = "TotallyWrongPassword1";

    [Fact]
    public async Task Login_FiveConsecutiveFailures_LocksAccountAndSixthAttemptFailsEvenWithCorrectPassword()
    {
        await fixture.ResetDatabaseAsync();
        var (_, _, user) = await AuthTestFixtures.SeedActiveUserAsync(fixture);
        var client = fixture.CreateClient();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(user.Email, WrongPassword));
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // Direct DB proof the 5th failure actually set a lockout, not just returned 401
        // for the ordinary wrong-password reason.
        await using (var db = fixture.CreateAppDbContext(new FakeCurrentTenant()))
        {
            var row = await db.Users.IgnoreQueryFilters().SingleAsync(u => u.Id == user.Id);
            Assert.Equal(5, row.FailedLoginAttempts);
            Assert.NotNull(row.LockoutEndAt);
            Assert.True(row.LockoutEndAt > DateTimeOffset.UtcNow);
        }

        // Sixth attempt, this time with the CORRECT password -- still fails identically
        // while locked out.
        var sixthAttempt = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(user.Email, AuthTestFixtures.DefaultPassword));
        Assert.Equal(HttpStatusCode.Unauthorized, sixthAttempt.StatusCode);
    }

    [Fact]
    public async Task Login_SuccessfulLoginBelowThreshold_ResetsFailedAttemptCounter()
    {
        await fixture.ResetDatabaseAsync();
        var (_, _, user) = await AuthTestFixtures.SeedActiveUserAsync(fixture);
        var client = fixture.CreateClient();

        // Two failures, then a success -- below the 5-attempt lockout threshold.
        await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(user.Email, WrongPassword));
        await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(user.Email, WrongPassword));

        await using (var db = fixture.CreateAppDbContext(new FakeCurrentTenant()))
        {
            var midway = await db.Users.IgnoreQueryFilters().SingleAsync(u => u.Id == user.Id);
            Assert.Equal(2, midway.FailedLoginAttempts);
        }

        var success = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(user.Email, AuthTestFixtures.DefaultPassword));
        Assert.Equal(HttpStatusCode.OK, success.StatusCode);

        await using var dbAfter = fixture.CreateAppDbContext(new FakeCurrentTenant());
        var after = await dbAfter.Users.IgnoreQueryFilters().SingleAsync(u => u.Id == user.Id);
        Assert.Equal(0, after.FailedLoginAttempts);
        Assert.Null(after.LockoutEndAt);
    }
}
