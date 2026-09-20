using AddRem.Parsing;

namespace AddRem.Tests.Parsing;

public class UninstallCommandTests
{
    private static UninstallCommand Parse(string raw)
    {
        Assert.True(UninstallCommand.TryParse(raw, out UninstallCommand? command));
        return command;
    }

    // --- splitting -------------------------------------------------------

    [Fact]
    public void TryParse_SplitsQuotedExecutableFromArguments()
    {
        UninstallCommand command = Parse("\"C:\\Program Files\\7-Zip\\Uninstall.exe\" /S");

        Assert.Equal(@"C:\Program Files\7-Zip\Uninstall.exe", command.Executable);
        Assert.Equal("/S", command.Arguments);
    }

    [Fact]
    public void TryParse_QuotedExecutableWithNoArguments()
    {
        UninstallCommand command = Parse("\"C:\\Program Files\\7-Zip\\Uninstall.exe\"");

        Assert.Equal(@"C:\Program Files\7-Zip\Uninstall.exe", command.Executable);
        Assert.Equal(string.Empty, command.Arguments);
    }

    /// <remarks>
    /// Observed on a real machine: an unquoted path containing spaces. Splitting on
    /// the first space would give "C:\Program" as the executable.
    /// </remarks>
    [Fact]
    public void TryParse_UnquotedExecutablePathContainingSpaces()
    {
        UninstallCommand command = Parse(@"C:\Program Files\AntiCheatExpert\Uninstaller.exe");

        Assert.Equal(@"C:\Program Files\AntiCheatExpert\Uninstaller.exe", command.Executable);
        Assert.Equal(string.Empty, command.Arguments);
    }

    [Fact]
    public void TryParse_UnquotedExecutableWithSpacesAndArguments()
    {
        UninstallCommand command = Parse(
            @"C:\Program Files\AntiCheatExpert\Uninstaller.exe /silent /norestart"
        );

        Assert.Equal(@"C:\Program Files\AntiCheatExpert\Uninstaller.exe", command.Executable);
        Assert.Equal("/silent /norestart", command.Arguments);
    }

    [Theory]
    [InlineData(".exe")]
    [InlineData(".com")]
    [InlineData(".bat")]
    [InlineData(".cmd")]
    [InlineData(".msi")]
    public void TryParse_RecognisesExecutableExtensions(string extension)
    {
        UninstallCommand command = Parse($@"C:\Program Files\App\setup{extension} /x");

        Assert.Equal($@"C:\Program Files\App\setup{extension}", command.Executable);
        Assert.Equal("/x", command.Arguments);
    }

    [Fact]
    public void TryParse_ExtensionMatchIsCaseInsensitive()
    {
        UninstallCommand command = Parse(@"C:\App\Setup.EXE /q");
        Assert.Equal(@"C:\App\Setup.EXE", command.Executable);
        Assert.Equal("/q", command.Arguments);
    }

    /// <remarks>
    /// Nothing recognisable: keep the whole string rather than guess. Raw is retained
    /// either way, so no information is lost.
    /// </remarks>
    [Fact]
    public void TryParse_NoRecognisableExtension_KeepsWholeStringAsExecutable()
    {
        UninstallCommand command = Parse("some opaque uninstall token");

        Assert.Equal("some opaque uninstall token", command.Executable);
        Assert.Equal(string.Empty, command.Arguments);
    }

    [Fact]
    public void TryParse_ExecutableFollowedByAQuotedScriptArgument()
    {
        UninstallCommand command = Parse("\"C:\\Windows\\py.exe\" \"C:\\App\\uninstall.py\" --yes");

        Assert.Equal(@"C:\Windows\py.exe", command.Executable);
        Assert.Equal("\"C:\\App\\uninstall.py\" --yes", command.Arguments);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParse_RejectsEmptyValues(string? raw)
    {
        Assert.False(UninstallCommand.TryParse(raw, out UninstallCommand? command));
        Assert.Null(command);
    }

    [Fact]
    public void TryParse_KeepsRawVerbatim()
    {
        const string Raw = "  \"C:\\App\\x.exe\"  /S  ";
        Assert.True(UninstallCommand.TryParse(Raw, out UninstallCommand? command));
        Assert.Equal(Raw, command.Raw);
    }

    // --- msiexec ---------------------------------------------------------

    [Theory]
    [InlineData("MsiExec.exe /X{90160000-008C-0000-1000-0000000FF1CE}")]
    [InlineData("msiexec.exe /x {90160000-008C-0000-1000-0000000FF1CE}")]
    [InlineData("msiexec /i{90160000-008C-0000-1000-0000000FF1CE}")]
    [InlineData(@"C:\Windows\System32\msiexec.exe /X{90160000-008C-0000-1000-0000000FF1CE}")]
    [InlineData("MsiExec.exe /uninstall {90160000-008C-0000-1000-0000000FF1CE}")]
    public void TryParse_ExtractsMsiProductCode(string raw)
    {
        UninstallCommand command = Parse(raw);

        Assert.True(command.IsWindowsInstaller);
        Assert.Equal(new Guid("90160000-008C-0000-1000-0000000FF1CE"), command.ProductCode);
    }

    [Fact]
    public void TryParse_MsiExecWithoutAProductCodeStillFlagsWindowsInstaller()
    {
        UninstallCommand command = Parse("msiexec.exe /x");

        Assert.True(command.IsWindowsInstaller);
        Assert.Null(command.ProductCode);
    }

    [Fact]
    public void TryParse_NonMsiExecIsNotFlagged()
    {
        UninstallCommand command = Parse(
            "\"C:\\App\\unins000.exe\" /X{90160000-008C-0000-1000-0000000FF1CE}"
        );

        Assert.False(command.IsWindowsInstaller);
        Assert.Null(command.ProductCode);
    }

    // --- silence ---------------------------------------------------------

    [Theory]
    [InlineData("/qn")]
    [InlineData("/quiet")]
    [InlineData("/S")]
    [InlineData("/s")]
    [InlineData("/silent")]
    [InlineData("/VERYSILENT")]
    [InlineData("--silent")]
    public void TryParse_DetectsSilentSwitches(string silentSwitch)
    {
        UninstallCommand command = Parse($"\"C:\\App\\unins000.exe\" {silentSwitch}");
        Assert.True(command.IsSilent);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/norestart")]
    [InlineData("/passive")]
    public void TryParse_DoesNotClaimSilenceWithoutASilentSwitch(string arguments)
    {
        UninstallCommand command = Parse($"\"C:\\App\\unins000.exe\" {arguments}".TrimEnd());
        Assert.False(command.IsSilent);
    }

    /// <remarks>
    /// "/silently" is not "/silent". Matching whole tokens rather than substrings
    /// keeps an unrelated switch from being read as a promise not to prompt.
    /// </remarks>
    [Fact]
    public void TryParse_MatchesWholeSwitchTokensOnly()
    {
        UninstallCommand command = Parse("\"C:\\App\\unins000.exe\" /silently");
        Assert.False(command.IsSilent);
    }
}
