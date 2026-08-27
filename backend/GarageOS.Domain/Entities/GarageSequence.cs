using GarageOS.Domain.Common;

namespace GarageOS.Domain.Entities;

public class GarageSequence : ITenantOwned
{
    public Guid GarageId { get; set; }
    public long NextJobNumber { get; set; } = 1;
    public long NextInvoiceNumber { get; set; } = 1;
}
