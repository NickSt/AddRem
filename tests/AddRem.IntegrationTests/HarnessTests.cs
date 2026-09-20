namespace AddRem.IntegrationTests;

/// <summary>
/// Proves the integration-test harness builds, discovers and runs. Tests here
/// are allowed to read the real machine; tests in AddRem.Tests are not.
/// </summary>
[Trait("Category", "Integration")]
public class HarnessTests
{
    [Fact]
    public void TestHarness_RunsOnWindows()
    {
        Assert.True(OperatingSystem.IsWindows());
    }
}
