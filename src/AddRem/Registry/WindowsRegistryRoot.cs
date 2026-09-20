using System.Security;
using Microsoft.Win32;

namespace AddRem.Registry;

/// <summary>
/// The real registry. This is the only type in the library permitted to touch
/// <see cref="RegistryKey"/> — everything else goes through <see cref="IRegistryRoot"/>
/// so it can be tested without a machine.
/// </summary>
internal sealed class WindowsRegistryRoot : IRegistryRoot
{
    /// <inheritdoc />
    public IRegistryKey? OpenSubKey(RegistryHive hive, RegistryView view, string subKeyPath)
    {
        ArgumentNullException.ThrowIfNull(subKeyPath);

        RegistryKey? baseKey = null;
        try
        {
            baseKey = RegistryKey.OpenBaseKey(hive, view);

            // The subkey carries its own handle, so the base key can be released
            // immediately; closing it does not invalidate what we just opened.
            RegistryKey? subKey = baseKey.OpenSubKey(subKeyPath, writable: false);
            return subKey is null ? null : new WindowsRegistryKey(subKey);
        }
        catch (Exception ex) when (IsExpectedRegistryFailure(ex))
        {
            // A hive that does not exist, a view unavailable on this platform, or a
            // key we are not allowed to read. None of these are exceptional: the
            // caller asked whether something is there, and the answer is no.
            return null;
        }
        finally
        {
            baseKey?.Dispose();
        }
    }

    /// <summary>
    /// Failures that mean "this key is not readable" rather than "the library is
    /// broken". Anything else is allowed to propagate.
    /// </summary>
    internal static bool IsExpectedRegistryFailure(Exception ex) =>
        ex
            is SecurityException
                or UnauthorizedAccessException
                or IOException
                or ObjectDisposedException;
}
