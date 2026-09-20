using AddRem.Parsing;

namespace AddRem.Tests.Parsing;

public class InstallDateParserTests
{
    private static readonly DateOnly Today = new(2026, 9, 19);

    /// <remarks>
    /// The documented format. Most entries on a real machine use it.
    /// </remarks>
    [Theory]
    [InlineData("20260615", 2026, 6, 15)]
    [InlineData("19900101", 1990, 1, 1)]
    [InlineData("20001231", 2000, 12, 31)]
    public void TryParse_ReadsDocumentedYyyyMmDd(string raw, int year, int month, int day)
    {
        Assert.True(InstallDateParser.TryParse(raw, Today, out DateOnly date));
        Assert.Equal(new DateOnly(year, month, day), date);
    }

    /// <remarks>
    /// Observed on a real machine alongside the documented format: an installer wrote
    /// a Unix timestamp instead. Ten digits cannot collide with yyyyMMdd, which is
    /// always eight.
    /// </remarks>
    [Fact]
    public void TryParse_ReadsUnixSecondsWrittenByInstallersThatIgnoredTheSpec()
    {
        Assert.True(InstallDateParser.TryParse("1725416004", Today, out DateOnly date));
        Assert.Equal(new DateOnly(2024, 9, 4), date);
    }

    [Fact]
    public void TryParse_ReadsUnixMilliseconds()
    {
        Assert.True(InstallDateParser.TryParse("1725416004000", Today, out DateOnly date));
        Assert.Equal(new DateOnly(2024, 9, 4), date);
    }

    [Theory]
    [InlineData("2026-06-15")]
    [InlineData("2026/06/15")]
    public void TryParse_ReadsIsoLikeForms(string raw)
    {
        Assert.True(InstallDateParser.TryParse(raw, Today, out DateOnly date));
        Assert.Equal(new DateOnly(2026, 6, 15), date);
    }

    [Fact]
    public void TryParse_ReadsInvariantSlashForm()
    {
        Assert.True(InstallDateParser.TryParse("6/15/2026", Today, out DateOnly date));
        Assert.Equal(new DateOnly(2026, 6, 15), date);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a date")]
    [InlineData("0")]
    [InlineData("99999999")]
    public void TryParse_RejectsUnusableValues(string? raw)
    {
        Assert.False(InstallDateParser.TryParse(raw, Today, out DateOnly date));
        Assert.Equal(default, date);
    }

    /// <remarks>
    /// A date decoded from the wrong format is worse than no date, so implausible
    /// results are rejected. 19700101 is what an empty or zero timestamp decodes to.
    /// </remarks>
    [Theory]
    [InlineData("19700101")]
    [InlineData("19891231")]
    [InlineData("16010101")]
    public void TryParse_RejectsImplausiblyOldDates(string raw)
    {
        Assert.False(InstallDateParser.TryParse(raw, Today, out _));
    }

    [Theory]
    [InlineData("20280101")]
    [InlineData("21000101")]
    public void TryParse_RejectsImplausiblyFutureDates(string raw)
    {
        Assert.False(InstallDateParser.TryParse(raw, Today, out _));
    }

    /// <remarks>
    /// Clock skew is real, so a little into the future is allowed; a lot is not.
    /// </remarks>
    [Fact]
    public void TryParse_AllowsUpToAYearAhead()
    {
        Assert.True(InstallDateParser.TryParse("20270601", Today, out DateOnly date));
        Assert.Equal(new DateOnly(2027, 6, 1), date);
    }

    [Fact]
    public void TryParse_TrimsSurroundingWhitespace()
    {
        Assert.True(InstallDateParser.TryParse("  20260615  ", Today, out DateOnly date));
        Assert.Equal(new DateOnly(2026, 6, 15), date);
    }

    /// <remarks>
    /// <para>
    /// Parsing must not depend on the machine's culture, or the same registry would
    /// yield different dates on different machines. An ambiguous slash date pins the
    /// interpretation: invariant culture reads month first, so 3/4 is March 4th and
    /// never April 3rd.
    /// </para>
    /// <para>
    /// The obvious version of this test — switch to de-DE and re-parse — cannot be
    /// written here, because the library sets InvariantGlobalization and constructing
    /// a named culture throws. That setting is itself part of the guarantee.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData("3/4/2026", 2026, 3, 4)]
    [InlineData("6/15/2026", 2026, 6, 15)]
    [InlineData("12/1/2026", 2026, 12, 1)]
    public void TryParse_ReadsSlashDatesAsMonthFirst(string raw, int year, int month, int day)
    {
        Assert.True(InstallDateParser.TryParse(raw, Today, out DateOnly date));
        Assert.Equal(new DateOnly(year, month, day), date);
    }

    [Fact]
    public void TryParse_PublicOverload_UsesRealClock()
    {
        Assert.True(InstallDateParser.TryParse("20200101", out DateOnly date));
        Assert.Equal(new DateOnly(2020, 1, 1), date);
    }
}
