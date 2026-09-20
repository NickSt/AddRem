namespace AddRem;

/// <summary>Where the library learned about a program.</summary>
[Flags]
public enum ProgramSource
{
    /// <summary>No source.</summary>
    None = 0,

    /// <summary>An uninstall registry key, whether written by an MSI or an EXE installer.</summary>
    Registry = 1,

    /// <summary>An MSIX / Store / UWP package.</summary>
    Msix = 2,

    /// <summary>Every source.</summary>
    All = Registry | Msix,
}

/// <summary>Who a program is installed for.</summary>
[Flags]
public enum InstallScope
{
    /// <summary>Scope could not be determined.</summary>
    Unknown = 0,

    /// <summary>Installed for every user on the machine (<c>HKEY_LOCAL_MACHINE</c>).</summary>
    Machine = 1,

    /// <summary>Installed for the user running this code (<c>HKEY_CURRENT_USER</c>).</summary>
    CurrentUser = 2,

    /// <summary>
    /// Installed for a different user profile. Discovering these requires
    /// administrative rights, so they are never included unless asked for.
    /// </summary>
    OtherUser = 4,

    /// <summary>Every scope.</summary>
    All = Machine | CurrentUser | OtherUser,
}

/// <summary>
/// The architecture a program was built for.
/// </summary>
/// <remarks>
/// For registry entries this is inferred from which view the entry was found in, which
/// is a strong hint rather than a fact — a 32-bit installer can register under the
/// 64-bit view. It is reported as <see cref="Unknown"/> when the signals conflict.
/// </remarks>
public enum ProgramArchitecture
{
    /// <summary>Not known.</summary>
    Unknown = 0,

    /// <summary>32-bit x86.</summary>
    X86,

    /// <summary>64-bit x86.</summary>
    X64,

    /// <summary>32-bit ARM.</summary>
    Arm,

    /// <summary>64-bit ARM.</summary>
    Arm64,

    /// <summary>Architecture-independent, as MSIX packages can be.</summary>
    Neutral,
}

/// <summary>What installed the program, which determines how it can be removed.</summary>
public enum InstallerKind
{
    /// <summary>Not known.</summary>
    Unknown = 0,

    /// <summary>Windows Installer (MSI). Removable by ProductCode.</summary>
    WindowsInstaller,

    /// <summary>A vendor executable installer, with its own uninstall command.</summary>
    Executable,

    /// <summary>An MSIX package, removed through the packaging APIs.</summary>
    Msix,
}

/// <summary>
/// Whether a program is one a user would see in Settings &gt; Apps &amp; Features.
/// </summary>
/// <remarks>
/// This is reported on every program rather than used to silently drop rows, because
/// the rules that decide it are only partly documented by Microsoft. A caller that
/// disagrees can ask for everything and apply its own policy.
/// </remarks>
[Flags]
public enum ProgramVisibility
{
    /// <summary>Shown in Apps &amp; Features.</summary>
    Visible = 1,

    /// <summary>
    /// Present in the registry but not shown — a system component, an update, or a
    /// child entry rolled into a parent product.
    /// </summary>
    Hidden = 2,

    /// <summary>Both.</summary>
    All = Visible | Hidden,
}
