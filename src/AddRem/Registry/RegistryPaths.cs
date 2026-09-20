namespace AddRem.Registry;

/// <summary>
/// Registry paths the library reads.
/// </summary>
public static class RegistryPaths
{
    /// <summary>
    /// The uninstall key, relative to a hive root. Present under
    /// <c>HKEY_LOCAL_MACHINE</c> for per-machine installs and
    /// <c>HKEY_CURRENT_USER</c> for per-user installs.
    /// </summary>
    /// <remarks>
    /// There is deliberately no <c>Wow6432Node</c> constant here. The 32-bit entries
    /// are reached by opening this same path through
    /// <see cref="Microsoft.Win32.RegistryView.Registry32"/>. Reading both the literal
    /// <c>Wow6432Node</c> path and the 32-bit view returns the same keys twice.
    /// </remarks>
    public const string Uninstall = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
}
