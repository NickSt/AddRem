using Microsoft.Win32;

namespace AddRem.Registry;

/// <summary>
/// An open registry key. This is the abstraction every part of the library reads
/// the registry through, so that enumeration and parsing can be tested against an
/// in-memory fake instead of the machine the tests happen to run on.
/// </summary>
/// <remarks>
/// <para>
/// Implementations must be forgiving. A registry value can be missing, can have a
/// different type than the one documented for it, or can be unreadable because of
/// permissions. None of those are exceptional: a single malformed vendor entry must
/// never break enumeration of the other several hundred. Accessors therefore report
/// absence rather than throwing, and <see cref="RegistryKeyExtensions"/> converts
/// values without ever throwing on a surprising type.
/// </para>
/// <para>
/// Keys own a native handle and must be disposed. Opening a subkey transfers a new
/// handle to the caller; disposing the parent does not dispose keys opened from it.
/// </para>
/// </remarks>
public interface IRegistryKey : IDisposable
{
    /// <summary>
    /// The key's full path, for example
    /// <c>HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{GUID}</c>.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The last segment of <see cref="Name"/> — for an uninstall entry this is the
    /// MSI ProductCode or the vendor's chosen key name.
    /// </summary>
    string ShortName { get; }

    /// <summary>Names of the immediate subkeys. Empty if there are none or if they cannot be read.</summary>
    IReadOnlyList<string> GetSubKeyNames();

    /// <summary>
    /// Opens an immediate subkey for reading, or returns <see langword="null"/> if it
    /// does not exist or cannot be opened. The caller owns the result and must dispose it.
    /// </summary>
    IRegistryKey? OpenSubKey(string name);

    /// <summary>Names of the values on this key. Empty if there are none or if they cannot be read.</summary>
    IReadOnlyList<string> GetValueNames();

    /// <summary>
    /// The raw value, or <see langword="null"/> if it is absent or unreadable. The
    /// runtime type follows the registry type and is not guaranteed — the same value
    /// name really does come back as <see cref="int"/> on one machine and
    /// <see cref="string"/> on another. Prefer the typed accessors in
    /// <see cref="RegistryKeyExtensions"/>.
    /// </summary>
    object? GetValue(string name);

    /// <summary>
    /// The registry type of a value, or <see cref="RegistryValueKind.Unknown"/> if it is
    /// absent or unreadable.
    /// </summary>
    RegistryValueKind GetValueKind(string name);
}
