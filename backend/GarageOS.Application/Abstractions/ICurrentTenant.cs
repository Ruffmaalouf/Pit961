namespace GarageOS.Application.Abstractions;

public interface ICurrentTenant
{
    Guid GarageId { get; }
    Guid UserId { get; }
    string Role { get; }
}
