namespace GarageOS.Application.Common;

public sealed class TenantContextUnavailableException : Exception
{
    public TenantContextUnavailableException()
        : base("Tenant context is not available on the current request.") { }

    public TenantContextUnavailableException(string message) : base(message) { }
}
