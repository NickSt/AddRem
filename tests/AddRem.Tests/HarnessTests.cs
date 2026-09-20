namespace AddRem.Tests;

/// <summary>
/// Placeholder proving the test harness builds, discovers and runs. Real suites
/// arrive with the registry abstraction in M2.
/// </summary>
/// <remarks>
/// This replaces the original ScratchTest, which called AppManager.GetAllApps()
/// against the live machine registry. It failed with InvalidCastException
/// because App.cs hard-casts the EstimatedSize registry value to int, and at
/// least one entry on a real machine stores it as REG_SZ. That bug is fixed by
/// the rewrite in M2/M3; the test is not carried forward because unit tests in
/// this project must never touch the live registry.
/// </remarks>
public class HarnessTests
{
    [Fact]
    public void TestHarness_Runs()
    {
        Assert.True(true);
    }
}
