using AddRem.Registry;
using Microsoft.Win32;

namespace AddRem.IntegrationTests.Registry;

/// <summary>
/// Exercises <see cref="RegistryRoots.Windows"/> against the real machine. These
/// assert shape and behaviour, never specific installed software, so they hold on any
/// Windows box including a bare CI runner.
/// </summary>
[Trait("Category", "Integration")]
public class WindowsRegistryRootTests
{
    private static readonly IRegistryRoot Registry = RegistryRoots.Windows;

    [Fact]
    public void OpensMachineUninstallKey_InBothViews()
    {
        using IRegistryKey? sixtyFour = Registry.OpenSubKey(
            RegistryHive.LocalMachine,
            RegistryView.Registry64,
            RegistryPaths.Uninstall
        );
        using IRegistryKey? thirtyTwo = Registry.OpenSubKey(
            RegistryHive.LocalMachine,
            RegistryView.Registry32,
            RegistryPaths.Uninstall
        );

        Assert.NotNull(sixtyFour);
        Assert.NotNull(thirtyTwo);
        Assert.NotEmpty(sixtyFour.GetSubKeyNames());
        Assert.NotEmpty(thirtyTwo.GetSubKeyNames());
    }

    /// <remarks>
    /// The 64-bit and 32-bit views are distinct key spaces. If this ever finds them
    /// identical, the view parameter is being ignored and every 32-bit program would
    /// be reported twice.
    /// </remarks>
    [Fact]
    public void MachineViews_AreDistinctKeySpaces()
    {
        using IRegistryKey? sixtyFour = Registry.OpenSubKey(
            RegistryHive.LocalMachine,
            RegistryView.Registry64,
            RegistryPaths.Uninstall
        );
        using IRegistryKey? thirtyTwo = Registry.OpenSubKey(
            RegistryHive.LocalMachine,
            RegistryView.Registry32,
            RegistryPaths.Uninstall
        );

        Assert.NotNull(sixtyFour);
        Assert.NotNull(thirtyTwo);

        Assert.NotEqual(
            sixtyFour.GetSubKeyNames().OrderBy(n => n, StringComparer.OrdinalIgnoreCase),
            thirtyTwo.GetSubKeyNames().OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public void MissingKey_ReturnsNull_DoesNotThrow()
    {
        using IRegistryKey? missing = Registry.OpenSubKey(
            RegistryHive.LocalMachine,
            RegistryView.Registry64,
            @"SOFTWARE\AddRem\DefinitelyNotInstalled\" + Guid.NewGuid().ToString("N")
        );

        Assert.Null(missing);
    }

    [Fact]
    public void Name_IsFullPath_And_ShortName_IsLastSegment()
    {
        using IRegistryKey? key = Registry.OpenSubKey(
            RegistryHive.LocalMachine,
            RegistryView.Registry64,
            RegistryPaths.Uninstall
        );

        Assert.NotNull(key);
        Assert.StartsWith("HKEY_LOCAL_MACHINE", key.Name, StringComparison.Ordinal);
        Assert.Equal("Uninstall", key.ShortName);
    }

    /// <remarks>
    /// Reads every uninstall entry on the machine. The point is that nothing throws:
    /// real registries contain values with unexpected types, unreadable keys, and
    /// vendor entries with no values at all.
    /// </remarks>
    [Fact]
    public void ReadingEveryUninstallEntry_NeverThrows()
    {
        int entriesRead = 0;

        foreach ((RegistryHive hive, RegistryView view) in AllScopes())
        {
            using IRegistryKey? root = Registry.OpenSubKey(hive, view, RegistryPaths.Uninstall);
            if (root is null)
            {
                continue;
            }

            foreach (string name in root.GetSubKeyNames())
            {
                using IRegistryKey? entry = root.OpenSubKey(name);
                if (entry is null)
                {
                    continue;
                }

                // Touch everything a real caller would.
                _ = entry.GetStringValue("DisplayName");
                _ = entry.GetStringValue("Publisher");
                _ = entry.GetStringValue("DisplayVersion");
                _ = entry.GetInt32Value("EstimatedSize");
                _ = entry.GetInt64Value("EstimatedSize");
                _ = entry.GetFlagValue("SystemComponent");
                _ = entry.GetFlagValue("WindowsInstaller");
                _ = entry.GetUriValue("HelpLink");
                _ = entry.GetValueNames();
                entriesRead++;
            }
        }

        Assert.True(entriesRead > 0, "Expected at least one uninstall entry on a real machine.");
    }

    /// <remarks>
    /// The original implementation opened every subkey inside a LINQ Select and
    /// disposed none of them, leaking one handle per installed program on every call.
    /// </remarks>
    [Fact]
    public void EnumeratingAndDisposing_LeaksNoKeys()
    {
        int before = WindowsRegistryKey.LiveInstances;

        using (
            IRegistryKey? root = Registry.OpenSubKey(
                RegistryHive.LocalMachine,
                RegistryView.Registry64,
                RegistryPaths.Uninstall
            )
        )
        {
            Assert.NotNull(root);
            foreach (string name in root.GetSubKeyNames())
            {
                using IRegistryKey? entry = root.OpenSubKey(name);
                _ = entry?.GetStringValue("DisplayName");
            }
        }

        Assert.Equal(before, WindowsRegistryKey.LiveInstances);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        int before = WindowsRegistryKey.LiveInstances;

        IRegistryKey? key = Registry.OpenSubKey(
            RegistryHive.LocalMachine,
            RegistryView.Registry64,
            RegistryPaths.Uninstall
        );
        Assert.NotNull(key);

        key.Dispose();
        key.Dispose();

        Assert.Equal(before, WindowsRegistryKey.LiveInstances);
    }

    [Fact]
    public void CurrentUserUninstallKey_IsReadableWhenPresent()
    {
        using IRegistryKey? key = Registry.OpenSubKey(
            RegistryHive.CurrentUser,
            RegistryView.Registry64,
            RegistryPaths.Uninstall
        );

        // A freshly imaged machine may genuinely have no per-user installs, so the
        // key's absence is acceptable; being present but unreadable is not.
        if (key is not null)
        {
            _ = key.GetSubKeyNames();
        }
    }

    private static IEnumerable<(RegistryHive Hive, RegistryView View)> AllScopes() =>
        [
            (RegistryHive.LocalMachine, RegistryView.Registry64),
            (RegistryHive.LocalMachine, RegistryView.Registry32),
            (RegistryHive.CurrentUser, RegistryView.Registry64),
        ];
}
