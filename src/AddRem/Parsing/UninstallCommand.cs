using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace AddRem.Parsing;

/// <summary>
/// A parsed <c>UninstallString</c>, <c>QuietUninstallString</c> or <c>ModifyPath</c>.
/// </summary>
/// <remarks>
/// <para>
/// This library reports uninstall commands; it never runs them. Splitting the command
/// is still worth doing, because the raw string cannot be handed to a process launcher
/// as-is and callers should not each reinvent the quoting rules.
/// </para>
/// <para>
/// Parsing is pure — it never probes the file system to decide where the executable
/// ends, so the same string parses identically on any machine.
/// </para>
/// </remarks>
public sealed partial record UninstallCommand
{
    /// <summary>Extensions that end an unquoted executable path.</summary>
    private static readonly string[] ExecutableExtensions =
    [
        ".exe",
        ".com",
        ".bat",
        ".cmd",
        ".msi",
    ];

    /// <summary>Switches that mean the command will not prompt.</summary>
    private static readonly string[] SilentSwitches =
    [
        "/qn",
        "/quiet",
        "/silent",
        "/verysilent",
        "/s",
        "-s",
        "--silent",
    ];

    /// <summary>The value exactly as stored in the registry.</summary>
    public required string Raw { get; init; }

    /// <summary>The executable, with quotes removed.</summary>
    public required string Executable { get; init; }

    /// <summary>Everything after the executable, trimmed. Empty when there are no arguments.</summary>
    public required string Arguments { get; init; }

    /// <summary>
    /// <see langword="true"/> when the executable is <c>msiexec</c>, in which case
    /// <see cref="ProductCode"/> is usually available.
    /// </summary>
    public bool IsWindowsInstaller { get; init; }

    /// <summary>
    /// The MSI ProductCode from an <c>msiexec /x{GUID}</c> command, when present.
    /// </summary>
    public Guid? ProductCode { get; init; }

    /// <summary>
    /// <see langword="true"/> when the arguments already contain a recognised silent
    /// switch. This reports what the command says; it never adds one.
    /// </summary>
    public bool IsSilent { get; init; }

    /// <summary>
    /// Parses a command string. Returns <see langword="false"/> only when there is
    /// nothing to parse.
    /// </summary>
    public static bool TryParse(string? raw, [NotNullWhen(true)] out UninstallCommand? command)
    {
        command = null;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        string text = raw.Trim();
        (string executable, string arguments) = Split(text);

        if (executable.Length == 0)
        {
            return false;
        }

        bool isMsiExec = IsMsiExec(executable);

        command = new UninstallCommand
        {
            Raw = raw,
            Executable = executable,
            Arguments = arguments,
            IsWindowsInstaller = isMsiExec,
            ProductCode = isMsiExec ? ExtractProductCode(arguments) : null,
            IsSilent = HasSilentSwitch(arguments),
        };

        return true;
    }

    private static (string Executable, string Arguments) Split(string text)
    {
        if (text.StartsWith('"'))
        {
            int closingQuote = text.IndexOf('"', 1);
            return closingQuote > 0
                ? (text[1..closingQuote], text[(closingQuote + 1)..].Trim())
                : (text.Trim('"'), string.Empty);
        }

        // Unquoted, and the path may contain spaces:
        //     C:\Program Files\AntiCheatExpert\Uninstaller.exe
        // Walk the space-separated tokens and stop at the first one whose accumulated
        // path ends in an executable extension.
        string[] tokens = text.Split(' ');
        for (int i = 0; i < tokens.Length; i++)
        {
            string candidate = string.Join(' ', tokens[..(i + 1)]);
            if (
                ExecutableExtensions.Any(ext =>
                    candidate.EndsWith(ext, StringComparison.OrdinalIgnoreCase)
                )
            )
            {
                return (candidate, string.Join(' ', tokens[(i + 1)..]).Trim());
            }
        }

        // No recognisable extension. The common case here is an extensionless launcher
        // invoked by name — "msiexec /x{GUID}" is written that way constantly — so
        // split at the first space when what follows looks like a switch. Anything
        // else is left whole rather than guessed at; Raw is kept either way.
        int firstSpace = text.IndexOf(' ', StringComparison.Ordinal);
        if (firstSpace > 0)
        {
            string remainder = text[(firstSpace + 1)..].TrimStart();
            if (remainder.StartsWith('/') || remainder.StartsWith('-'))
            {
                return (text[..firstSpace], remainder);
            }
        }

        return (text, string.Empty);
    }

    private static bool IsMsiExec(string executable)
    {
        string fileName = executable;

        int lastSeparator = fileName.LastIndexOfAny(['\\', '/']);
        if (lastSeparator >= 0)
        {
            fileName = fileName[(lastSeparator + 1)..];
        }

        return fileName.Equals("msiexec", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("msiexec.exe", StringComparison.OrdinalIgnoreCase);
    }

    /// <remarks>
    /// Only the braced GUID form is recognised, because that is what msiexec command
    /// lines actually contain. The 32-character compressed form appears as a key name
    /// under <c>Installer\Products</c>, which this library does not read.
    /// </remarks>
    private static Guid? ExtractProductCode(string arguments) =>
        ProductCodePattern().Match(arguments) is { Success: true } match
        && Guid.TryParse(match.Groups["guid"].Value, out Guid productCode)
            ? productCode
            : null;

    private static bool HasSilentSwitch(string arguments)
    {
        if (arguments.Length == 0)
        {
            return false;
        }

        foreach (
            string token in arguments.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            )
        )
        {
            if (SilentSwitches.Contains(token, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    [GeneratedRegex(
        @"/[xXiI]\s*(?<guid>\{[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}\})"
            + @"|/uninstall\s+(?<guid>\{[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}\})",
        RegexOptions.ExplicitCapture
    )]
    private static partial Regex ProductCodePattern();
}
