namespace GarageOS.Application.Abstractions;

/// <summary>Outcome of a password verification attempt, deliberately NOT the same type as
/// Microsoft.AspNetCore.Identity.PasswordVerificationResult -- GarageOS.Application has no
/// ASP.NET Core dependency (matches ICurrentTenant/TenantGuard's existing pattern of
/// framework-free interfaces), so this is our own small enum. The Infrastructure-layer
/// implementation wraps Microsoft.AspNetCore.Identity.PasswordHasher&lt;TUser&gt; (WP-4
/// brief §8) and maps its result onto this one.</summary>
public enum PasswordVerifyOutcome
{
    Failed,
    Success,

    /// <summary>Password is correct, but the stored hash uses an outdated
    /// iteration-count/format -- caller should immediately re-hash and persist the new
    /// hash (lazy rehash-on-login), without forcing a password reset.</summary>
    SuccessRehashNeeded,
}

/// <summary>WP-4 brief §8. PBKDF2-HMAC-SHA256 via the in-box
/// Microsoft.AspNetCore.Identity.PasswordHasher&lt;TUser&gt; -- zero new NuGet
/// dependency, iteration count/format embedded in the stored hash.</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    PasswordVerifyOutcome Verify(string hash, string providedPassword);
}
