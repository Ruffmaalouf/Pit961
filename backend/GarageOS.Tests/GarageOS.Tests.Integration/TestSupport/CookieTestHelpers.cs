namespace GarageOS.Tests.Integration.TestSupport;

/// <summary>WebApplicationFactory's default HttpClient does not automatically persist
/// cookies across separate SendAsync calls the way a browser would -- these helpers make
/// the login -> refresh -> logout cookie chain explicit and inspectable in tests, rather
/// than hiding it behind a CookieContainer.</summary>
public static class CookieTestHelpers
{
    /// <summary>Extracts just the "name=value" pair (no attributes) for the named cookie
    /// from a response's Set-Cookie header(s), suitable for use as a request Cookie
    /// header value on a subsequent call.</summary>
    public static string? ExtractCookieValue(HttpResponseMessage response, string cookieName)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders))
        {
            return null;
        }

        foreach (var header in setCookieHeaders)
        {
            var firstSegment = header.Split(';')[0];
            var parts = firstSegment.Split('=', 2);
            if (parts.Length == 2 && parts[0].Trim() == cookieName)
            {
                return firstSegment.Trim();
            }
        }

        return null;
    }
}
