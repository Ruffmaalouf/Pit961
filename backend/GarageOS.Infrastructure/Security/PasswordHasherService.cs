using GarageOS.Application.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace GarageOS.Infrastructure.Security;

/// <summary>
/// WP-4 brief §8. Thin wrapper around the in-box
/// Microsoft.AspNetCore.Identity.PasswordHasher&lt;TUser&gt; (PBKDF2-HMAC-SHA256,
/// 100,000 iterations, 128-bit salt) -- zero new NuGet dependency, iteration count/format
/// embedded in the stored hash, enabling lazy rehash-on-login. The generic type parameter
/// is never actually used by PasswordHasher's Hash/Verify overloads (a long-standing
/// PasswordHasher&lt;TUser&gt; API quirk -- it exists for extensibility hooks GarageOS
/// doesn't use), so `object` is a fine placeholder rather than pulling GarageOS.Domain's
/// User entity into this Identity-flavored generic parameter.
/// </summary>
public sealed class PasswordHasherService : IPasswordHasher
{
    private readonly PasswordHasher<object> _inner = new();
    private static readonly object DummyUser = new();

    public string Hash(string password) => _inner.HashPassword(DummyUser, password);

    public PasswordVerifyOutcome Verify(string hash, string providedPassword)
    {
        var result = _inner.VerifyHashedPassword(DummyUser, hash, providedPassword);
        return result switch
        {
            PasswordVerificationResult.Success => PasswordVerifyOutcome.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordVerifyOutcome.SuccessRehashNeeded,
            _ => PasswordVerifyOutcome.Failed,
        };
    }
}
