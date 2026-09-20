using Microsoft.Win32;

namespace AddRem.Registry;

/// <summary>
/// Entry point to the registry: opens a key by hive, view and path.
/// </summary>
/// <remarks>
/// The view is an explicit parameter rather than something callers encode in the
/// path. Reading the 32-bit view means passing <see cref="RegistryView.Registry32"/>,
/// never appending <c>Wow6432Node</c> to the path — a 64-bit process can reach the
/// same keys both ways, and mixing the two silently double-counts every 32-bit entry.
/// </remarks>
public interface IRegistryRoot
{
    /// <summary>
    /// Opens a key for reading, or returns <see langword="null"/> if the hive, view or
    /// path does not exist or cannot be opened. Absence is not an error and must not
    /// throw. The caller owns the result and must dispose it.
    /// </summary>
    IRegistryKey? OpenSubKey(RegistryHive hive, RegistryView view, string subKeyPath);
}
