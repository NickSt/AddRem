using AddRem;

namespace AddRem.Tests;

public class ScratchTest
{
    [Fact]
    public void RunTest()
    {
        var result = AppManager.GetAllApps();

        Assert.NotEmpty(result);
    }
}