namespace GarageOS.Application.Common;

public sealed class TenantOwnershipException : Exception
{
    public TenantOwnershipException() : base("Resource does not belong to the current tenant.") { }
}
