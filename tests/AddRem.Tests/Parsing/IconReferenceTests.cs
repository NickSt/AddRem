using AddRem.Parsing;

namespace AddRem.Tests.Parsing;

public class IconReferenceTests
{
    /// <remarks>
    /// Every case here is a DisplayIcon form observed on a real machine.
    /// </remarks>
    [Theory]
    [InlineData(@"C:\Program Files\7-Zip\7zFM.exe", @"C:\Program Files\7-Zip\7zFM.exe", 0)]
    [InlineData(@"C:\Games\Vortex\Vortex.exe,0", @"C:\Games\Vortex\Vortex.exe", 0)]
    [InlineData(@"C:\Windows\System32\mstsc.exe", @"C:\Windows\System32\mstsc.exe", 0)]
    [InlineData(
        @"C:\Program Files\Git\git-for-windows.ico",
        @"C:\Program Files\Git\git-for-windows.ico",
        0
    )]
    [InlineData(@"C:\App\app.exe,3", @"C:\App\app.exe", 3)]
    public void TryParse_ReadsRealWorldForms(string raw, string expectedPath, int expectedIndex)
    {
        Assert.True(IconReference.TryParse(raw, out IconReference icon));
        Assert.Equal(expectedPath, icon.Path);
        Assert.Equal(expectedIndex, icon.Index);
    }

    /// <remarks>
    /// The form that catches people out. A negative number is a resource ID, not an
    /// index counted from the end of the file.
    /// </remarks>
    [Fact]
    public void TryParse_TreatsNegativeIndexAsResourceId()
    {
        Assert.True(
            IconReference.TryParse(
                @"C:\Users\x\OneDrive\OneDriveSetup.exe,-101",
                out IconReference icon
            )
        );

        Assert.Equal(@"C:\Users\x\OneDrive\OneDriveSetup.exe", icon.Path);
        Assert.Equal(-101, icon.Index);
        Assert.True(icon.IsResourceId);
        Assert.Equal(101, icon.ResourceId);
    }

    [Fact]
    public void TryParse_PositiveIndexIsNotAResourceId()
    {
        Assert.True(IconReference.TryParse(@"C:\App\app.exe,2", out IconReference icon));
        Assert.False(icon.IsResourceId);
        Assert.Null(icon.ResourceId);
    }

    [Theory]
    [InlineData("\"C:\\Program Files\\App\\app.exe\"", @"C:\Program Files\App\app.exe", 0)]
    [InlineData("\"C:\\Program Files\\App\\app.exe\",0", @"C:\Program Files\App\app.exe", 0)]
    [InlineData("\"C:\\Program Files\\App\\app.exe\",-5", @"C:\Program Files\App\app.exe", -5)]
    public void TryParse_HandlesQuotedPaths(string raw, string expectedPath, int expectedIndex)
    {
        Assert.True(IconReference.TryParse(raw, out IconReference icon));
        Assert.Equal(expectedPath, icon.Path);
        Assert.Equal(expectedIndex, icon.Index);
    }

    /// <remarks>
    /// Splitting on the last comma rather than the first matters: a path may contain
    /// one, and only a tail that parses as a number is an index.
    /// </remarks>
    [Fact]
    public void TryParse_PathContainingACommaWithAnIndex()
    {
        Assert.True(IconReference.TryParse(@"C:\Vendor, Inc\app.exe,1", out IconReference icon));
        Assert.Equal(@"C:\Vendor, Inc\app.exe", icon.Path);
        Assert.Equal(1, icon.Index);
    }

    [Fact]
    public void TryParse_PathContainingACommaWithNoIndex()
    {
        Assert.True(IconReference.TryParse(@"C:\Vendor, Inc\app.exe", out IconReference icon));
        Assert.Equal(@"C:\Vendor, Inc\app.exe", icon.Path);
        Assert.Equal(0, icon.Index);
    }

    [Fact]
    public void TryParse_TrailingTextThatIsNotANumberStaysPartOfThePath()
    {
        Assert.True(IconReference.TryParse(@"C:\App\app.exe,default", out IconReference icon));
        Assert.Equal(@"C:\App\app.exe,default", icon.Path);
        Assert.Equal(0, icon.Index);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(",0")]
    [InlineData("\"\"")]
    public void TryParse_RejectsValuesWithNoPath(string? raw)
    {
        Assert.False(IconReference.TryParse(raw, out IconReference icon));
        Assert.Equal(default, icon);
    }

    [Fact]
    public void TryParse_TrimsSurroundingWhitespace()
    {
        Assert.True(IconReference.TryParse(@"   C:\App\app.exe,1   ", out IconReference icon));
        Assert.Equal(@"C:\App\app.exe", icon.Path);
        Assert.Equal(1, icon.Index);
    }

    [Fact]
    public void ToString_RoundTripsToPathCommaIndex()
    {
        Assert.True(IconReference.TryParse(@"C:\App\app.exe,-101", out IconReference icon));
        Assert.Equal(@"C:\App\app.exe,-101", icon.ToString());
    }
}
