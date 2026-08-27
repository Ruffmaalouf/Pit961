namespace GarageOS.Tests.Integration;

/// <summary>
/// All integration tests share one real PostgreSQL database (see
/// <see cref="IntegrationTestFixture"/>), so collections in this assembly must not run
/// in parallel against it — stated explicitly here (and in xunit.runner.json's
/// parallelizeTestCollections/parallelizeAssembly: false), not left implicit, per
/// WP-2's acceptance criteria.
/// </summary>
[CollectionDefinition("Integration")]
public sealed class IntegrationCollection : ICollectionFixture<IntegrationTestFixture>;
