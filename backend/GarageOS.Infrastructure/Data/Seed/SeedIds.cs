namespace GarageOS.Infrastructure.Data.Seed;

/// <summary>Well-known fixed GUIDs for development seed data (WP-3 brief §11).
/// Keyed off fixed constants so re-running the seeder in dev is idempotent.</summary>
public static class SeedIds
{
    public static readonly Guid PerformanceAutoGarageAccount = new("00000000-0000-0000-0001-000000000001");
    public static readonly Guid PerformanceAutoGarage = new("00000000-0000-0000-0001-000000000002");

    public static readonly Guid UserRalph = new("00000000-0000-0000-0002-000000000001");
    public static readonly Guid UserSarahKhalil = new("00000000-0000-0000-0002-000000000002");
    public static readonly Guid UserAhmedHassan = new("00000000-0000-0000-0002-000000000003");
    public static readonly Guid UserHassanAli = new("00000000-0000-0000-0002-000000000004");
    public static readonly Guid UserMaya = new("00000000-0000-0000-0002-000000000005");

    public static readonly Guid CustomerJohnSmith = new("00000000-0000-0000-0003-000000000001");
    public static readonly Guid CustomerNourKhalil = new("00000000-0000-0000-0003-000000000002");
    public static readonly Guid CustomerWalidFares = new("00000000-0000-0000-0003-000000000003");
    public static readonly Guid CustomerRaniaSaade = new("00000000-0000-0000-0003-000000000004");
    public static readonly Guid CustomerKarimAbouZeid = new("00000000-0000-0000-0003-000000000005");
    public static readonly Guid CustomerElieNassar = new("00000000-0000-0000-0003-000000000006");

    public static readonly Guid VehicleBmw328i = new("00000000-0000-0000-0004-000000000001");
    public static readonly Guid VehicleMercedesC300 = new("00000000-0000-0000-0004-000000000002");
    public static readonly Guid VehicleBmwX5 = new("00000000-0000-0000-0004-000000000003");
    public static readonly Guid VehicleGolfGti = new("00000000-0000-0000-0004-000000000004");
    public static readonly Guid VehicleAudiA4 = new("00000000-0000-0000-0004-000000000005");
    public static readonly Guid VehicleWranglerRubicon = new("00000000-0000-0000-0004-000000000006");
}
