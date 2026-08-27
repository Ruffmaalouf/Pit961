using System.Security.Cryptography;
using System.Text;

namespace GarageOS.Application.Common;

/// <summary>
/// Shared opaque-token mechanics for both RefreshToken and PasswordResetToken (WP-4 brief
/// §9/§10): a 256-bit CSPRNG-generated raw token, base64url-encoded for transport, with
/// only its SHA-256 hash ever persisted. SHA-256 (not PBKDF2/BCrypt) is deliberate --
/// this is already-maximal-entropy random data, so a slow adaptive hash buys nothing
/// while costing CPU on every refresh/reset attempt. This is NOT password hashing --
/// see IPasswordHasher for that, which uses PBKDF2 because passwords are
/// human-choosable, non-uniform-entropy input.
/// </summary>
public static class OpaqueTokenGenerator
{
    private const int RawTokenBytes = 32; // 256 bits

    public static (string RawToken, string TokenHash) Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(RawTokenBytes);
        var rawToken = Base64UrlEncode(bytes);
        return (rawToken, Hash(rawToken));
    }

    public static string Hash(string rawToken)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hashBytes); // lowercase-independent, fixed-length, index-friendly
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
