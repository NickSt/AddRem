using AddRem.Registry;
using AddRem.Tests.Fakes;
using Microsoft.Win32;

namespace AddRem.Tests.Registry;

/// <summary>
/// Exercises the fake against the <see cref="IRegistryRoot"/> contract. These are as
/// much a check on the contract as on the fake: if the fake and the Windows
/// implementation disagree about absence, disposal or path handling, every test built
/// on the fake is testing the wrong thing.
/// </summary>
public class InMemoryRegistryRootTests
{
    private static InMemoryRegistryRoot TwoEntries() =>
        new RegistryBuilder()
            .UninstallKey("7-Zip")
            .Value("DisplayName", "7-Zip 24.09 (x64)")
            .UninstallKey("{90160000-008C-0000-1000-0000000FF1CE}")
            .Value("DisplayName", "Microsoft Office")
            .Build();

    [Fact]
    public void OpenSubKey_ReturnsKeyThatExists()
    {
        InMemoryRegistryRoot root = TwoEntries();
        using IRegistryKey? key = root.OpenSubKey(
            RegistryHive.LocalMachine,
            RegistryView.Registry64,
            RegistryPaths.Uninstall
        );

        Assert.NotNull(key);
        Assert.Equal(2, key.GetSubKeyNames().Count);
    }

    [Fact]
    public void OpenSubKey_ReturnsNullForMissingPath_DoesNotThrow()
    {
        InMemoryRegistryRoot root = TwoEntries();
        using IRegistryKey? missing = root.OpenSubKey(
            RegistryHive.LocalMachine,
            RegistryView.Registry64,
            @"SOFTWARE\Nope\Missing"
        );

        Assert.Null(missing);
    }

    [Fact]
    public void OpenSubKey_ReturnsNullForHiveThatWasNeverPopulated()
    {
        InMemoryRegistryRoot root = TwoEntries();
        using IRegistryKey? currentUser = root.OpenSubKey(
            RegistryHive.CurrentUser,
            RegistryView.Registry64,
            RegistryPaths.Uninstall
        );

        Assert.Null(currentUser);
    }

    /// <remarks>
    /// The 32-bit and 64-bit views are genuinely different key spaces. A library that
    /// conflated them would return 32-bit entries twice, which is exactly what reading
    /// the literal Wow6432Node path alongside the 32-bit view does.
    /// </remarks>
    [Fact]
    public void OpenSubKey_ViewsAreIndependent()
    {
        InMemoryRegistryRoot root = new RegistryBuilder()
            .Key(
                RegistryHive.LocalMachine,
                RegistryView.Registry64,
                $@"{RegistryPaths.Uninstall}\Only64"
            )
            .Value("DisplayName", "Sixty Four")
            .Build();

        using IRegistryKey? sixtyFour = root.OpenSubKey(
            RegistryHive.LocalMachine,
            RegistryView.Registry64,
            RegistryPaths.Uninstall
        );
        using IRegistryKey? thirtyTwo = root.OpenSubKey(
            RegistryHive.LocalMachine,
            RegistryView.Registry32,
            RegistryPaths.Uninstall
        );

        Assert.NotNull(sixtyFour);
        Assert.Null(thirtyTwo);
    }

    [Fact]
    public void Name_IsFullPath_And_ShortName_IsLastSegment()
    {
        InMemoryRegistryRoot root = TwoEntries();
        using IRegistryKey? key = root.OpenSubKey(
            RegistryHive.LocalMachine,
            RegistryView.Registry64,
            $@"{RegistryPaths.Uninstall}\7-Zip"
        );

        Assert.NotNull(key);
        Assert.Equal($@"HKEY_LOCAL_MACHINE\{RegistryPaths.Uninstall}\7-Zip", key.Name);
        Assert.Equal("7-Zip", key.ShortName);
    }

    [Fact]
    public void OpenSubKey_OnKey_ReturnsChild()
    {
        InMemoryRegistryRoot root = TwoEntries();
        using IRegistryKey? parent = root.OpenSubKey(
            RegistryHive.LocalMachine,
            RegistryView.Registry64,
            RegistryPaths.Uninstall
        );
        Assert.NotNull(parent);

        using IRegistryKey? child = parent.OpenSubKey("7-Zip");
        Assert.NotNull(child);
        Assert.Equal("7-Zip 24.09 (x64)", child.GetStringValue("DisplayName"));
    }

    [Fact]
    public void OpenSubKey_OnKey_ReturnsNullForMissingChild()
    {
        InMemoryRegistryRoot root = TwoEntries();
        using IRegistryKey? parent = root.OpenSubKey(
            RegistryHive.LocalMachine,
            RegistryView.Registry64,
            RegistryPaths.Uninstall
        );
        Assert.NotNull(parent);
        Assert.Null(parent.OpenSubKey("NotInstalled"));
    }

    /// <remarks>
    /// An access-denied key exists but yields nothing. Enumeration must treat that as
    /// "no data here" and move on, not as a failure.
    /// </remarks>
    [Fact]
    public void UnreadableKey_YieldsNothing_AndDoesNotThrow()
    {
        InMemoryRegistryRoot root = new RegistryBuilder()
            .UninstallKey("Locked")
            .Value("DisplayName", "Should not be visible")
            .Unreadable()
            .Build();

        using IRegistryKey? key = root.OpenSubKey(
            RegistryHive.LocalMachine,
            RegistryView.Registry64,
            $@"{RegistryPaths.Uninstall}\Locked"
        );

        Assert.NotNull(key);
        Assert.Empty(key.GetValueNames());
        Assert.Empty(key.GetSubKeyNames());
        Assert.Null(key.GetStringValue("DisplayName"));
    }

    [Fact]
    public void Disposing_EveryOpenedKey_ReturnsLiveCountToZero()
    {
        InMemoryRegistryRoot root = TwoEntries();

        IRegistryKey parent = root.OpenSubKey(
            RegistryHive.LocalMachine,
            RegistryView.Registry64,
            RegistryPaths.Uninstall
        )!;

        foreach (string name in parent.GetSubKeyNames())
        {
            using IRegistryKey? child = parent.OpenSubKey(name);
            Assert.NotNull(child);
        }

        Assert.Equal(1, root.LiveKeyCount);
        parent.Dispose();
        Assert.Equal(0, root.LiveKeyCount);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        InMemoryRegistryRoot root = TwoEntries();
        IRegistryKey key = root.OpenSubKey(
            RegistryHive.LocalMachine,
            RegistryView.Registry64,
            RegistryPaths.Uninstall
        )!;

        key.Dispose();
        key.Dispose();

        Assert.Equal(0, root.LiveKeyCount);
    }
}
