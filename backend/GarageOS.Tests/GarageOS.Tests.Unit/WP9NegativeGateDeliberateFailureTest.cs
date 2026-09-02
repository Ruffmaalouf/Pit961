using Xunit;

namespace GarageOS.Tests.Unit;

/// <summary>
/// WP-9 negative-gate proof (temporary, never merged to main): a deliberately
/// failing test to prove CI actually blocks a red build. Removed immediately
/// after the CI failure is confirmed. See KNOWN_ISSUES.md / PROGRESS.md WP-9
/// entry for the record of this proof.
/// </summary>
public class WP9NegativeGateDeliberateFailureTest
{
    [Fact]
    public void DeliberateFailure_ProvesCiBlocksRedBuild()
    {
        Assert.True(false, "WP-9 negative-gate proof: this test is meant to fail.");
    }
}
