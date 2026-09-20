using AddRem.Registry;
using AddRem.Tests.Fakes;
using Microsoft.Win32;

namespace AddRem.Tests.Registry;

public class RegistryKeyExtensionsTests
{
    private static IRegistryKey KeyWith(
        string name,
        object value,
        RegistryValueKind kind = RegistryValueKind.Unknown
    )
    {
        InMemoryRegistryRoot root = new RegistryBuilder()
            .UninstallKey("Test")
            .Value(name, value, kind)
            .Build();
        return root.OpenSubKey(
            RegistryHive.LocalMachine,
            RegistryView.Registry64,
            $@"{RegistryPaths.Uninstall}\Test"
        )!;
    }

    private static IRegistryKey EmptyKey()
    {
        InMemoryRegistryRoot root = new RegistryBuilder().UninstallKey("Test").Build();
        return root.OpenSubKey(
            RegistryHive.LocalMachine,
            RegistryView.Registry64,
            $@"{RegistryPaths.Uninstall}\Test"
        )!;
    }

    // --- GetStringValue -------------------------------------------------

    [Fact]
    public void GetStringValue_ReadsString()
    {
        using IRegistryKey key = KeyWith("DisplayName", "7-Zip 24.09 (x64)");
        Assert.Equal("7-Zip 24.09 (x64)", key.GetStringValue("DisplayName"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void GetStringValue_TreatsBlankAsAbsent(string blank)
    {
        using IRegistryKey key = KeyWith("DisplayName", blank);
        Assert.Null(key.GetStringValue("DisplayName"));
    }

    [Fact]
    public void GetStringValue_TrimsSurroundingWhitespace()
    {
        using IRegistryKey key = KeyWith("Publisher", "  Contoso Ltd.  ");
        Assert.Equal("Contoso Ltd.", key.GetStringValue("Publisher"));
    }

    [Fact]
    public void GetStringValue_JoinsMultiString()
    {
        using IRegistryKey key = KeyWith(
            "Tags",
            new[] { "one", "", "two" },
            RegistryValueKind.MultiString
        );
        Assert.Equal("one; two", key.GetStringValue("Tags"));
    }

    [Fact]
    public void GetStringValue_ReturnsNullForBinary()
    {
        using IRegistryKey key = KeyWith("Blob", new byte[] { 1, 2, 3 }, RegistryValueKind.Binary);
        Assert.Null(key.GetStringValue("Blob"));
    }

    [Fact]
    public void GetStringValue_ReturnsNullWhenMissing()
    {
        using IRegistryKey key = EmptyKey();
        Assert.Null(key.GetStringValue("NotThere"));
    }

    // --- GetInt32Value / GetInt64Value ----------------------------------

    [Fact]
    public void GetInt32Value_ReadsDWord()
    {
        using IRegistryKey key = KeyWith("EstimatedSize", 5600, RegistryValueKind.DWord);
        Assert.Equal(5600, key.GetInt32Value("EstimatedSize"));
    }

    /// <remarks>
    /// This is the bug the old code shipped: EstimatedSize is a DWORD on hundreds of
    /// uninstall entries and a REG_SZ on at least one real machine, and a hard
    /// (int) cast threw InvalidCastException part way through enumeration.
    /// </remarks>
    [Fact]
    public void GetInt32Value_ReadsEstimatedSizeStoredAsString()
    {
        using IRegistryKey key = KeyWith("EstimatedSize", "5600", RegistryValueKind.String);
        Assert.Equal(5600, key.GetInt32Value("EstimatedSize"));
    }

    [Theory]
    [InlineData("0x1F4", 500)]
    [InlineData("0X1f4", 500)]
    [InlineData("-42", -42)]
    [InlineData("  17  ", 17)]
    public void GetInt32Value_ParsesStringForms(string stored, int expected)
    {
        using IRegistryKey key = KeyWith("Value", stored, RegistryValueKind.String);
        Assert.Equal(expected, key.GetInt32Value("Value"));
    }

    [Theory]
    [InlineData("not a number")]
    [InlineData("")]
    [InlineData("12.5")]
    public void GetInt32Value_ReturnsNullForUnparseableString(string stored)
    {
        using IRegistryKey key = KeyWith("Value", stored, RegistryValueKind.String);
        Assert.Null(key.GetInt32Value("Value"));
    }

    [Fact]
    public void GetInt32Value_ReturnsNullWhenQWordDoesNotFit()
    {
        using IRegistryKey key = KeyWith("Big", long.MaxValue, RegistryValueKind.QWord);
        Assert.Null(key.GetInt32Value("Big"));
        Assert.Equal(long.MaxValue, key.GetInt64Value("Big"));
    }

    [Fact]
    public void GetInt64Value_ReadsDWordAndQWord()
    {
        using IRegistryKey dword = KeyWith("A", 7, RegistryValueKind.DWord);
        using IRegistryKey qword = KeyWith("A", 7L, RegistryValueKind.QWord);
        Assert.Equal(7L, dword.GetInt64Value("A"));
        Assert.Equal(7L, qword.GetInt64Value("A"));
    }

    [Fact]
    public void GetInt32Value_ReturnsNullForBinary()
    {
        using IRegistryKey key = KeyWith("Blob", new byte[] { 1 }, RegistryValueKind.Binary);
        Assert.Null(key.GetInt32Value("Blob"));
    }

    // --- GetFlagValue ---------------------------------------------------

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(-1, true)]
    [InlineData(0, false)]
    public void GetFlagValue_ReadsDWordAsNonZero(int stored, bool expected)
    {
        using IRegistryKey key = KeyWith("SystemComponent", stored, RegistryValueKind.DWord);
        Assert.Equal(expected, key.GetFlagValue("SystemComponent"));
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("true", true)]
    [InlineData("TRUE", true)]
    [InlineData("yes", true)]
    [InlineData("0", false)]
    [InlineData("false", false)]
    [InlineData("nonsense", false)]
    public void GetFlagValue_ReadsStringForms(string stored, bool expected)
    {
        using IRegistryKey key = KeyWith("SystemComponent", stored, RegistryValueKind.String);
        Assert.Equal(expected, key.GetFlagValue("SystemComponent"));
    }

    [Fact]
    public void GetFlagValue_MissingValueIsFalse()
    {
        using IRegistryKey key = EmptyKey();
        Assert.False(key.GetFlagValue("SystemComponent"));
    }

    // --- GetUriValue ----------------------------------------------------

    [Fact]
    public void GetUriValue_ReadsAbsoluteUri()
    {
        using IRegistryKey key = KeyWith("HelpLink", "https://example.com/help");
        Assert.Equal(new Uri("https://example.com/help"), key.GetUriValue("HelpLink"));
    }

    [Theory]
    [InlineData("1-800-555-0100")]
    [InlineData("example.com")]
    [InlineData("/relative/path")]
    [InlineData("")]
    public void GetUriValue_ReturnsNullForNonAbsoluteUri(string stored)
    {
        using IRegistryKey key = KeyWith("HelpLink", stored);
        Assert.Null(key.GetUriValue("HelpLink"));
    }

    // --- kind -----------------------------------------------------------

    [Fact]
    public void GetKindOrUnknown_ReportsStoredKind()
    {
        using IRegistryKey key = KeyWith("EstimatedSize", "5600", RegistryValueKind.String);
        Assert.Equal(RegistryValueKind.String, key.GetKindOrUnknown("EstimatedSize"));
    }

    [Fact]
    public void GetKindOrUnknown_ReportsUnknownWhenMissing()
    {
        using IRegistryKey key = EmptyKey();
        Assert.Equal(RegistryValueKind.Unknown, key.GetKindOrUnknown("NotThere"));
    }
}
