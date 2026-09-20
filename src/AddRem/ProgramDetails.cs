using Microsoft.Win32;

namespace AddRem;

/// <summary>
/// One place a program was observed. A program found in more than one place — the same
/// MSI product registered in both registry views, say — carries several of these after
/// de-duplication.
/// </summary>
/// <param name="Source">Which source reported it.</param>
/// <param name="Scope">Which scope it was found in.</param>
/// <param name="View">The registry view, for registry sources.</param>
/// <param name="Locator">The full registry key path, or the MSIX package full name.</param>
public readonly record struct ProgramOrigin(
    ProgramSource Source,
    InstallScope Scope,
    RegistryView View,
    string Locator
);

/// <summary>
/// Registry facts that do not belong on the unified model but are worth keeping, so a
/// caller never has to reopen the key to answer a question this library could have.
/// </summary>
public sealed record RegistryProgramDetails
{
    /// <summary>Full path of the uninstall key.</summary>
    public required string KeyPath { get; init; }

    /// <summary>The key's own name — an MSI ProductCode, or a name the vendor chose.</summary>
    public required string KeyName { get; init; }

    /// <summary>The ProductCode, when <see cref="KeyName"/> is a well-formed GUID.</summary>
    public Guid? ProductCode { get; init; }

    /// <summary>The <c>WindowsInstaller</c> flag.</summary>
    public bool IsWindowsInstaller { get; init; }

    /// <summary>
    /// The <c>SystemComponent</c> flag. By far the most common reason an entry is
    /// hidden — it accounts for the large majority of uninstall keys on a developer
    /// machine.
    /// </summary>
    public bool SystemComponent { get; init; }

    /// <summary>The <c>ParentKeyName</c> value, when this entry rolls up into another.</summary>
    public string? ParentKeyName { get; init; }

    /// <summary>The <c>ParentDisplayName</c> value, when this entry rolls up into another.</summary>
    public string? ParentDisplayName { get; init; }

    /// <summary>
    /// The <c>ReleaseType</c> value — <c>Security Update</c>, <c>Hotfix</c> and friends
    /// mark patches rather than products.
    /// </summary>
    public string? ReleaseType { get; init; }

    /// <summary>The <c>BundleProviderKey</c> value, set by bundle installers such as Burn.</summary>
    public string? BundleProviderKey { get; init; }

    /// <summary>The <c>Language</c> value.</summary>
    public string? Language { get; init; }

    /// <summary>
    /// Every value read from the key, unparsed. The escape hatch for callers who need
    /// something this library does not model.
    /// </summary>
    public required IReadOnlyDictionary<string, object?> RawValues { get; init; }
}

/// <summary>
/// MSIX package facts that do not belong on the unified model.
/// </summary>
/// <remarks>
/// This record lives in the core library so the model is complete, but nothing here
/// depends on the packaging APIs. Populating it is the job of the separate
/// <c>AddRem.Msix</c> package, which alone carries the Windows SDK projection.
/// </remarks>
public sealed record MsixProgramDetails
{
    /// <summary>The package full name, which uniquely identifies an installed package.</summary>
    public required string PackageFullName { get; init; }

    /// <summary>The package family name, stable across versions.</summary>
    public required string PackageFamilyName { get; init; }

    /// <summary>The package name.</summary>
    public required string PackageName { get; init; }

    /// <summary>The publisher's X.500 identity string.</summary>
    public string? PublisherId { get; init; }

    /// <summary>A framework package other apps depend on, not something a user installed.</summary>
    public bool IsFramework { get; init; }

    /// <summary>A resource package (a language or scale variant) rather than an app.</summary>
    public bool IsResourcePackage { get; init; }

    /// <summary>A bundle rather than a single package.</summary>
    public bool IsBundle { get; init; }

    /// <summary>Side-loaded for development rather than installed normally.</summary>
    public bool IsDevelopmentMode { get; init; }

    /// <summary>How the package was signed — <c>Store</c>, <c>System</c>, <c>Developer</c> and so on.</summary>
    public string? SignatureKind { get; init; }
}
