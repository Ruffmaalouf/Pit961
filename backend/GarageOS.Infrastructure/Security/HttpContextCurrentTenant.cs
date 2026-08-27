using GarageOS.Application.Abstractions;
using GarageOS.Application.Common;
using Microsoft.AspNetCore.Http;

namespace GarageOS.Infrastructure.Security;

public sealed class HttpContextCurrentTenant : ICurrentTenant
{
    private readonly IHttpContextAccessor _accessor;

    public HttpContextCurrentTenant(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public Guid GarageId => ReadGuidClaim("garage_id");
    public Guid UserId => ReadGuidClaim("sub");
    public string Role => ReadClaim("role");

    private string ReadClaim(string type)
    {
        var user = _accessor.HttpContext?.User
            ?? throw new TenantContextUnavailableException();
        var value = user.FindFirst(type)?.Value
            ?? throw new TenantContextUnavailableException($"Missing required claim '{type}'.");
        return value;
    }

    private Guid ReadGuidClaim(string type) => Guid.Parse(ReadClaim(type));
}
