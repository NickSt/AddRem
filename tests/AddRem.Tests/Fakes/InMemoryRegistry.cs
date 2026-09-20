using AddRem.Registry;
using Microsoft.Win32;

namespace AddRem.Tests.Fakes;

/// <summary>
/// An in-memory <see cref="IRegistryRoot"/>. Unit tests run against this so they are
/// deterministic on any machine and never depend on what happens to be installed.
/// </summary>
public sealed class InMemoryRegistryRoot : IRegistryRoot
{
    private readonly Dictionary<(RegistryHive Hive, RegistryView View), InMemoryKeyData> _hives =
    [];

    /// <summary>Number of keys handed out that have not been disposed. Tests assert this returns to zero.</summary>
    public int LiveKeyCount { get; private set; }

    internal InMemoryKeyData GetOrAddHiveRoot(RegistryHive hive, RegistryView view)
    {
        if (!_hives.TryGetValue((hive, view), out InMemoryKeyData? root))
        {
            root = new InMemoryKeyData(HiveRootName(hive));
            _hives[(hive, view)] = root;
        }

        return root;
    }

    /// <inheritdoc />
    public IRegistryKey? OpenSubKey(RegistryHive hive, RegistryView view, string subKeyPath)
    {
        if (!_hives.TryGetValue((hive, view), out InMemoryKeyData? node))
        {
            return null;
        }

        foreach (string segment in SplitPath(subKeyPath))
        {
            if (!node.SubKeys.TryGetValue(segment, out InMemoryKeyData? child))
            {
                return null;
            }

            node = child;
        }

        return Open(node);
    }

    internal IRegistryKey Open(InMemoryKeyData data)
    {
        LiveKeyCount++;
        return new InMemoryRegistryKey(this, data);
    }

    internal void OnKeyDisposed() => LiveKeyCount--;

    /// <summary>Registry paths are backslash-separated.</summary>
    internal const char PathSeparator = '\\';

    internal static IEnumerable<string> SplitPath(string path) =>
        path.Split(
            PathSeparator,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );

    private static string HiveRootName(RegistryHive hive) =>
        hive switch
        {
            RegistryHive.LocalMachine => "HKEY_LOCAL_MACHINE",
            RegistryHive.CurrentUser => "HKEY_CURRENT_USER",
            RegistryHive.Users => "HKEY_USERS",
            RegistryHive.ClassesRoot => "HKEY_CLASSES_ROOT",
            _ => hive.ToString(),
        };
}

/// <summary>Backing store for one fake key: its children and its values.</summary>
internal sealed class InMemoryKeyData(string name)
{
    public string Name { get; } = name;

    public Dictionary<string, InMemoryKeyData> SubKeys { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, (object Value, RegistryValueKind Kind)> Values { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Simulates a key that exists but cannot be read, as an access-denied key does.</summary>
    public bool Unreadable { get; set; }

    public InMemoryKeyData GetOrAddSubKey(string name)
    {
        if (!SubKeys.TryGetValue(name, out InMemoryKeyData? child))
        {
            child = new InMemoryKeyData($@"{Name}\{name}");
            SubKeys[name] = child;
        }

        return child;
    }
}

internal sealed class InMemoryRegistryKey(InMemoryRegistryRoot root, InMemoryKeyData data)
    : IRegistryKey
{
    private bool _disposed;

    public string Name => data.Name;

    public string ShortName => Name[(Name.LastIndexOf(InMemoryRegistryRoot.PathSeparator) + 1)..];

    public IReadOnlyList<string> GetSubKeyNames() => data.Unreadable ? [] : [.. data.SubKeys.Keys];

    public IRegistryKey? OpenSubKey(string name) =>
        !data.Unreadable && data.SubKeys.TryGetValue(name, out InMemoryKeyData? child)
            ? root.Open(child)
            : null;

    public IReadOnlyList<string> GetValueNames() => data.Unreadable ? [] : [.. data.Values.Keys];

    public object? GetValue(string name) =>
        !data.Unreadable
        && data.Values.TryGetValue(name, out (object Value, RegistryValueKind Kind) v)
            ? v.Value
            : null;

    public RegistryValueKind GetValueKind(string name) =>
        !data.Unreadable
        && data.Values.TryGetValue(name, out (object Value, RegistryValueKind Kind) v)
            ? v.Kind
            : RegistryValueKind.Unknown;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        root.OnKeyDisposed();
    }
}
