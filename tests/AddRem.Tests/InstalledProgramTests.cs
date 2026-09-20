using AddRem.Parsing;

namespace AddRem.Tests;

public class InstalledProgramTests
{
    private static readonly Guid ProductCode = new("90160000-008C-0000-1000-0000000FF1CE");

    private static UninstallCommand Command(string raw)
    {
        Assert.True(UninstallCommand.TryParse(raw, out UninstallCommand? command));
        return command;
    }

    private static InstalledProgram Program(
        UninstallCommand? uninstall = null,
        UninstallCommand? quiet = null,
        InstallerKind installer = InstallerKind.Executable
    ) =>
        new()
        {
            Id = "registry:hklm64:Test",
            DisplayName = "Test",
            Uninstall = uninstall,
            QuietUninstall = quiet,
            Installer = installer,
            Origins = [],
        };

    [Fact]
    public void GetPreferredUninstall_PrefersTheVendorsQuietCommand()
    {
        UninstallCommand quiet = Command("\"C:\\App\\unins000.exe\" /VERYSILENT");
        InstalledProgram program = Program(Command("\"C:\\App\\unins000.exe\""), quiet);

        Assert.Same(quiet, program.GetPreferredUninstall());
    }

    [Fact]
    public void GetPreferredUninstall_SynthesisesMsiExecForWindowsInstallerProducts()
    {
        InstalledProgram program = Program(
            Command($"MsiExec.exe /X{ProductCode:B}"),
            quiet: null,
            installer: InstallerKind.WindowsInstaller
        );

        UninstallCommand? preferred = program.GetPreferredUninstall();

        Assert.NotNull(preferred);
        Assert.True(preferred.IsSilent);
        Assert.Equal("msiexec.exe", preferred.Executable);
        Assert.Equal(ProductCode, preferred.ProductCode);
        Assert.Contains("/qn", preferred.Arguments, StringComparison.Ordinal);
        Assert.Contains("/norestart", preferred.Arguments, StringComparison.Ordinal);
    }

    [Fact]
    public void GetPreferredUninstall_UsesProductCodeFromRegistryDetailsWhenTheCommandLacksOne()
    {
        InstalledProgram program = new()
        {
            Id = "registry:hklm64:Test",
            DisplayName = "Test",
            Installer = InstallerKind.WindowsInstaller,
            Uninstall = Command("MsiExec.exe /X"),
            RegistryDetails = new RegistryProgramDetails
            {
                KeyPath = @"HKEY_LOCAL_MACHINE\...\Uninstall\{GUID}",
                KeyName = ProductCode.ToString("B"),
                ProductCode = ProductCode,
                RawValues = new Dictionary<string, object?>(),
            },
            Origins = [],
        };

        Assert.Equal(ProductCode, program.GetPreferredUninstall()?.ProductCode);
    }

    /// <remarks>
    /// The safety rule. "/S" means silent to NSIS and something else entirely to other
    /// installers, so guessing one for an arbitrary vendor executable risks an
    /// unattended reinstall or a hung installer on a user's machine. The interactive
    /// command is returned unchanged instead.
    /// </remarks>
    [Fact]
    public void GetPreferredUninstall_NeverGuessesASilentSwitchForAVendorExecutable()
    {
        UninstallCommand interactive = Command("\"C:\\App\\unins000.exe\"");
        InstalledProgram program = Program(interactive);

        UninstallCommand? preferred = program.GetPreferredUninstall();

        Assert.NotNull(preferred);
        Assert.Same(interactive, preferred);
        Assert.False(preferred.IsSilent);
        Assert.Equal(string.Empty, preferred.Arguments);
    }

    [Fact]
    public void GetPreferredUninstall_ReturnsNullWhenThereIsNoCommandAtAll()
    {
        Assert.Null(Program().GetPreferredUninstall());
    }

    [Fact]
    public void GetPreferredUninstall_FallsBackToInteractiveForMsiWithoutAProductCode()
    {
        UninstallCommand interactive = Command("MsiExec.exe /X");
        InstalledProgram program = Program(
            interactive,
            quiet: null,
            installer: InstallerKind.WindowsInstaller
        );

        Assert.Same(interactive, program.GetPreferredUninstall());
    }
}
