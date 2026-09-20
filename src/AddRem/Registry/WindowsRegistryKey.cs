using Microsoft.Win32;

namespace AddRem.Registry;

/// <summary>
/// An open key on the real registry. Owns exactly one <see cref="RegistryKey"/> and
/// releases it on <see cref="Dispose"/>.
/// </summary>
internal sealed class WindowsRegistryKey : IRegistryKey
{
    private readonly RegistryKey _key;
    private bool _disposed;

    internal WindowsRegistryKey(RegistryKey key)
    {
        _key = key;
        Interlocked.Increment(ref _liveInstances);
    }

    private static int _liveInstances;

    /// <summary>
    /// Keys opened but not yet disposed. Enumerating the uninstall tree opens several
    /// hundred keys, and the obvious LINQ formulation leaks every one of them, so
    /// integration tests assert this returns to its starting value.
    /// </summary>
    internal static int LiveInstances => Volatile.Read(ref _liveInstances);

    /// <inheritdoc />
    public string Name => _key.Name;

    /// <inheritdoc />
    public string ShortName => Name[(Name.LastIndexOf('\\') + 1)..];

    /// <inheritdoc />
    public IReadOnlyList<string> GetSubKeyNames()
    {
        try
        {
            return _key.GetSubKeyNames();
        }
        catch (Exception ex) when (WindowsRegistryRoot.IsExpectedRegistryFailure(ex))
        {
            return [];
        }
    }

    /// <inheritdoc />
    public IRegistryKey? OpenSubKey(string name)
    {
        try
        {
            RegistryKey? subKey = _key.OpenSubKey(name, writable: false);
            return subKey is null ? null : new WindowsRegistryKey(subKey);
        }
        catch (Exception ex) when (WindowsRegistryRoot.IsExpectedRegistryFailure(ex))
        {
            return null;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetValueNames()
    {
        try
        {
            return _key.GetValueNames();
        }
        catch (Exception ex) when (WindowsRegistryRoot.IsExpectedRegistryFailure(ex))
        {
            return [];
        }
    }

    /// <inheritdoc />
    public object? GetValue(string name)
    {
        try
        {
            // Do not expand REG_EXPAND_SZ here. Callers that want expansion ask for it
            // explicitly, and keeping the raw form means a fixture captured on one
            // machine replays identically on another.
            return _key.GetValue(
                name,
                defaultValue: null,
                RegistryValueOptions.DoNotExpandEnvironmentNames
            );
        }
        catch (Exception ex) when (WindowsRegistryRoot.IsExpectedRegistryFailure(ex))
        {
            return null;
        }
    }

    /// <inheritdoc />
    public RegistryValueKind GetValueKind(string name)
    {
        try
        {
            return _key.GetValueKind(name);
        }
        catch (Exception ex)
            when (ex is System.IO.FileNotFoundException
                || WindowsRegistryRoot.IsExpectedRegistryFailure(ex)
            )
        {
            // GetValueKind throws rather than returning a sentinel when the value is
            // absent, which is the single most common case.
            return RegistryValueKind.Unknown;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Interlocked.Decrement(ref _liveInstances);
        _key.Dispose();
    }
}
