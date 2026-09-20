using AddRem.Parsing;

namespace AddRem;

/// <summary>
/// One installed program, however it was installed and wherever it was found.
/// </summary>
/// <remarks>
/// This is the whole point of the library: a single shape that a registry uninstall
/// entry and an MSIX package both map onto, so callers stop caring which one they are
/// looking at. Anything that could not be determined is <see langword="null"/> rather
/// than an empty string, because "this program has no publisher recorded" and "this
/// program's publisher is the empty string" are different facts.
/// </remarks>
public sealed record InstalledProgram
{
    /// <summary>
    /// A stable identifier, prefixed by source and scope — for example
    /// <c>registry:hklm64:{GUID}</c> or <c>msix:Contoso.App_1.0.0.0_x64__abc</c>.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>The name a user would recognise, as Apps &amp; Features shows it.</summary>
    public required string DisplayName { get; init; }

    /// <summary>The publisher, when recorded.</summary>
    public string? Publisher { get; init; }

    /// <summary>
    /// The version exactly as stored, for display. Vendors write things like
    /// <c>2026.1 (build 3)</c> that no structured type can round-trip.
    /// </summary>
    public string? RawVersion { get; init; }

    /// <summary>The parsed version, for comparison. See <see cref="RawVersion"/> for display.</summary>
    public ProgramVersion? Version { get; init; }

    /// <summary>The install date, when one is recorded and plausible.</summary>
    public DateOnly? InstallDate { get; init; }

    /// <summary>
    /// Approximate installed size in bytes. The registry records this in kilobytes and
    /// installers are often optimistic about it, so treat it as an estimate.
    /// </summary>
    public long? EstimatedSizeBytes { get; init; }

    /// <summary>The install directory, when recorded. Most installers leave it blank.</summary>
    public string? InstallLocation { get; init; }

    /// <summary>Where the installer package was run from, when recorded.</summary>
    public string? InstallSource { get; init; }

    /// <summary>Who the program is installed for.</summary>
    public InstallScope Scope { get; init; }

    /// <summary>The architecture, best-effort. See <see cref="ProgramArchitecture"/>.</summary>
    public ProgramArchitecture Architecture { get; init; }

    /// <summary>Which source reported this program.</summary>
    public ProgramSource Source { get; init; }

    /// <summary>What installed it.</summary>
    public InstallerKind Installer { get; init; }

    /// <summary>Whether a user would see this in Apps &amp; Features.</summary>
    public ProgramVisibility Visibility { get; init; }

    /// <summary>Where the program's icon lives, when recorded.</summary>
    public IconReference? Icon { get; init; }

    /// <summary>The uninstall command, parsed. This library never runs it.</summary>
    public UninstallCommand? Uninstall { get; init; }

    /// <summary>The non-interactive uninstall command, when the vendor provided one.</summary>
    public UninstallCommand? QuietUninstall { get; init; }

    /// <summary>The modify/repair command, when the vendor provided one.</summary>
    public UninstallCommand? Modify { get; init; }

    /// <summary>
    /// <see langword="false"/> when the entry sets <c>NoRemove</c>. Such a program is
    /// still listed; its uninstall button is simply greyed out.
    /// </summary>
    public bool CanUninstall { get; init; }

    /// <summary><see langword="false"/> when the entry sets <c>NoModify</c>.</summary>
    public bool CanModify { get; init; }

    /// <summary><see langword="false"/> when the entry sets <c>NoRepair</c>.</summary>
    public bool CanRepair { get; init; }

    /// <summary>The vendor's support page.</summary>
    public Uri? HelpLink { get; init; }

    /// <summary>The vendor's product page.</summary>
    public Uri? AboutUrl { get; init; }

    /// <summary>Where the vendor publishes updates.</summary>
    public Uri? UpdateInfoUrl { get; init; }

    /// <summary>A support telephone number, on the rare entry that has one.</summary>
    public string? HelpTelephone { get; init; }

    /// <summary>Free-text comments from the installer.</summary>
    public string? Comments { get; init; }

    /// <summary>A support contact name.</summary>
    public string? Contact { get; init; }

    /// <summary>Registry specifics, when this came from the registry.</summary>
    public RegistryProgramDetails? RegistryDetails { get; init; }

    /// <summary>Package specifics, when this came from MSIX.</summary>
    public MsixProgramDetails? MsixDetails { get; init; }

    /// <summary>
    /// Every place this program was observed. More than one entry means de-duplication
    /// merged separate records — the same product in both registry views, for instance.
    /// </summary>
    public required IReadOnlyList<ProgramOrigin> Origins { get; init; }

    /// <summary>
    /// The command to prefer when uninstalling without prompting: the vendor's quiet
    /// command if there is one, otherwise a synthesised <c>msiexec</c> command for MSI
    /// products, otherwise the ordinary interactive command.
    /// </summary>
    /// <remarks>
    /// When the result is the interactive command, it is returned as-is. A silent
    /// switch is never guessed for an arbitrary vendor executable: <c>/S</c> means
    /// "silent" to NSIS and something else entirely elsewhere, and guessing wrong
    /// means an unattended reinstall or a hung installer on a user's machine.
    /// </remarks>
    public UninstallCommand? GetPreferredUninstall()
    {
        if (QuietUninstall is not null)
        {
            return QuietUninstall;
        }

        Guid? productCode = Uninstall?.ProductCode ?? RegistryDetails?.ProductCode;
        if (Installer == InstallerKind.WindowsInstaller && productCode is Guid code)
        {
            string raw = $"msiexec.exe /x {code:B} /qn /norestart";
            return new UninstallCommand
            {
                Raw = raw,
                Executable = "msiexec.exe",
                Arguments = $"/x {code:B} /qn /norestart",
                IsWindowsInstaller = true,
                ProductCode = code,
                IsSilent = true,
            };
        }

        return Uninstall;
    }
}
