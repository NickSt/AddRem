using AddRem.Registry;
using Microsoft.Win32;

namespace AddRem.Tests.Fakes;

/// <summary>
/// Fluent construction of an <see cref="InMemoryRegistryRoot"/>, so a test can state
/// the registry shape it needs in a few readable lines.
/// </summary>
/// <example>
/// <code>
/// var registry = new RegistryBuilder()
///     .Key(RegistryHive.LocalMachine, RegistryView.Registry64, $@"{RegistryPaths.Uninstall}\7-Zip")
///         .Value("DisplayName", "7-Zip 24.09 (x64)")
///         .Value("EstimatedSize", 5600, RegistryValueKind.DWord)
///     .Build();
/// </code>
/// </example>
public sealed class RegistryBuilder
{
    private readonly InMemoryRegistryRoot _root = new();
    private InMemoryKeyData? _current;

    /// <summary>Creates (or selects) a key, which subsequent <see cref="Value"/> calls apply to.</summary>
    public RegistryBuilder Key(RegistryHive hive, RegistryView view, string path)
    {
        InMemoryKeyData node = _root.GetOrAddHiveRoot(hive, view);
        foreach (string segment in InMemoryRegistryRoot.SplitPath(path))
        {
            node = node.GetOrAddSubKey(segment);
        }

        _current = node;
        return this;
    }

    /// <summary>Shorthand for a machine-scope 64-bit-view uninstall entry.</summary>
    public RegistryBuilder UninstallKey(string keyName) =>
        Key(
            RegistryHive.LocalMachine,
            RegistryView.Registry64,
            $@"{RegistryPaths.Uninstall}\{keyName}"
        );

    /// <summary>Adds a value to the current key.</summary>
    public RegistryBuilder Value(
        string name,
        object value,
        RegistryValueKind kind = RegistryValueKind.Unknown
    )
    {
        RequireCurrent().Values[name] = (
            value,
            kind == RegistryValueKind.Unknown ? InferKind(value) : kind
        );
        return this;
    }

    /// <summary>Marks the current key as existing but unreadable, the way an access-denied key behaves.</summary>
    public RegistryBuilder Unreadable()
    {
        RequireCurrent().Unreadable = true;
        return this;
    }

    /// <summary>Returns the assembled root.</summary>
    public InMemoryRegistryRoot Build() => _root;

    private InMemoryKeyData RequireCurrent() =>
        _current ?? throw new InvalidOperationException("Call Key(...) before adding values.");

    private static RegistryValueKind InferKind(object value) =>
        value switch
        {
            int => RegistryValueKind.DWord,
            long => RegistryValueKind.QWord,
            string[] => RegistryValueKind.MultiString,
            byte[] => RegistryValueKind.Binary,
            _ => RegistryValueKind.String,
        };
}
