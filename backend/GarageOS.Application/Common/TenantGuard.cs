using GarageOS.Application.Abstractions;

namespace GarageOS.Application.Common;

public static class TenantGuard
{
    public static void EnsureOwned(Guid resourceGarageId, ICurrentTenant currentTenant)
    {
        if (resourceGarageId != currentTenant.GarageId)
        {
            throw new TenantOwnershipException();
        }
    }
}
