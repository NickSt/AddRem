using AddRem.Parsing;

namespace AddRem.Tests.Parsing;

public class ProgramVersionTests
{
    /// <remarks>
    /// Every case in this table is a shape observed in a real DisplayVersion value.
    /// </remarks>
    [Theory]
    [InlineData("12.0.40664", 12, 0, 40664, 0, null)]
    [InlineData("1.4.40.0", 1, 4, 40, 0, null)]
    [InlineData("26.163.0823.0004", 26, 163, 823, 4, null)]
    [InlineData("3", 3, 0, 0, 0, null)]
    [InlineData("3.2", 3, 2, 0, 0, null)]
    [InlineData("v3.2", 3, 2, 0, 0, null)]
    [InlineData("V3.2", 3, 2, 0, 0, null)]
    [InlineData("  1.2.3  ", 1, 2, 3, 0, null)]
    [InlineData("2026.1 (build 3)", 2026, 1, 0, 0, "(build 3)")]
    [InlineData("1.0-beta2", 1, 0, 0, 0, "-beta2")]
    [InlineData("1.2.3.4.5", 1, 2, 3, 4, ".5")]
    [InlineData("1.0.", 1, 0, 0, 0, ".")]
    [InlineData("10.0.19041.1", 10, 0, 19041, 1, null)]
    public void TryParse_ParsesRealWorldVersions(
        string raw,
        int major,
        int minor,
        int build,
        int revision,
        string? suffix
    )
    {
        Assert.True(ProgramVersion.TryParse(raw, out ProgramVersion version));
        Assert.Equal(new ProgramVersion(major, minor, build, revision, suffix), version);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("unknown")]
    [InlineData("beta")]
    [InlineData("v")]
    [InlineData(".1.2")]
    public void TryParse_RejectsValuesWithNoLeadingNumber(string? raw)
    {
        Assert.False(ProgramVersion.TryParse(raw, out ProgramVersion version));
        Assert.Equal(default, version);
    }

    /// <remarks>
    /// A digit run too long for an int is rejected rather than truncated — reporting
    /// a wrong version is worse than reporting none.
    /// </remarks>
    [Fact]
    public void TryParse_RejectsComponentTooLargeForInt32()
    {
        Assert.False(ProgramVersion.TryParse("99999999999.0", out _));
    }

    [Fact]
    public void FromPackedMsiVersion_UnpacksMajorMinorBuild()
    {
        // 3.14.1000 packed as major<<24 | minor<<16 | build
        int packed = (3 << 24) | (14 << 16) | 1000;
        Assert.Equal(
            new ProgramVersion(3, 14, 1000, 0, null),
            ProgramVersion.FromPackedMsiVersion(packed)
        );
    }

    [Fact]
    public void FromPackedMsiVersion_HandlesZero()
    {
        Assert.Equal(new ProgramVersion(0, 0, 0, 0, null), ProgramVersion.FromPackedMsiVersion(0));
    }

    [Theory]
    [InlineData("1.0", "2.0")]
    [InlineData("1.0", "1.1")]
    [InlineData("1.0.0", "1.0.1")]
    [InlineData("1.0.0.0", "1.0.0.1")]
    [InlineData("1.9", "1.10")]
    [InlineData("2.0", "10.0")]
    public void CompareTo_OrdersNumerically(string lower, string higher)
    {
        Assert.True(ProgramVersion.TryParse(lower, out ProgramVersion a));
        Assert.True(ProgramVersion.TryParse(higher, out ProgramVersion b));

        Assert.True(a < b);
        Assert.True(b > a);
        Assert.True(a <= b);
        Assert.True(b >= a);
    }

    /// <remarks>
    /// Missing components are zero, so these are the same version written two ways.
    /// Not storing the raw text on this type is what makes that true.
    /// </remarks>
    [Fact]
    public void Equality_IgnoresHowManyComponentsWereWritten()
    {
        Assert.True(ProgramVersion.TryParse("1.0", out ProgramVersion shortForm));
        Assert.True(ProgramVersion.TryParse("1.0.0.0", out ProgramVersion longForm));

        Assert.Equal(shortForm, longForm);
        Assert.Equal(0, shortForm.CompareTo(longForm));
    }

    [Fact]
    public void CompareTo_UsesSuffixOnlyAsTieBreak_WithNoSuffixFirst()
    {
        Assert.True(ProgramVersion.TryParse("1.0", out ProgramVersion plain));
        Assert.True(ProgramVersion.TryParse("1.0-beta", out ProgramVersion suffixed));

        Assert.True(plain < suffixed);
    }

    [Fact]
    public void ToString_RendersAllFourComponents()
    {
        Assert.True(ProgramVersion.TryParse("1.2", out ProgramVersion version));
        Assert.Equal("1.2.0.0", version.ToString());
    }

    [Fact]
    public void ToString_IncludesSuffix()
    {
        Assert.True(ProgramVersion.TryParse("1.0-beta2", out ProgramVersion version));
        Assert.Equal("1.0.0.0-beta2", version.ToString());
    }
}
