using System.Globalization;
using Microsoft.Win32;

namespace AddRem.Registry;

/// <summary>
/// Typed, non-throwing accessors over <see cref="IRegistryKey"/>.
/// </summary>
/// <remarks>
/// Registry values do not reliably have the type their documentation claims. On a
/// real machine <c>EstimatedSize</c> is a <c>REG_DWORD</c> on hundreds of uninstall
/// entries and a <c>REG_SZ</c> on at least one, and <c>InstallDate</c> is sometimes
/// <c>yyyyMMdd</c> and sometimes a Unix timestamp. Every accessor here coerces what
/// it is given and returns <see langword="null"/> (or <see langword="false"/>) when
/// it cannot, so a single odd entry cannot abort an enumeration.
/// </remarks>
public static class RegistryKeyExtensions
{
    /// <summary>
    /// Reads a string value. Surrounding whitespace is trimmed, and a value that is
    /// empty or entirely whitespace is reported as <see langword="null"/> — callers
    /// care about "has a display name", not "has an empty display name".
    /// </summary>
    /// <remarks>
    /// A <c>REG_MULTI_SZ</c> value is joined with <c>"; "</c>. Binary values have no
    /// sensible string form and are reported as <see langword="null"/>.
    /// </remarks>
    public static string? GetStringValue(this IRegistryKey key, string name)
    {
        ArgumentNullException.ThrowIfNull(key);

        return key.GetValue(name) switch
        {
            string s => NullIfBlank(s),
            string[] parts => NullIfBlank(
                string.Join("; ", parts.Where(p => !string.IsNullOrWhiteSpace(p)))
            ),
            byte[] => null,
            null => null,
            var other => NullIfBlank(Convert.ToString(other, CultureInfo.InvariantCulture)),
        };
    }

    /// <summary>
    /// Reads a 32-bit integer value, accepting a <c>REG_DWORD</c>, a <c>REG_QWORD</c>
    /// that fits, or a string holding a decimal or <c>0x</c>-prefixed hex number.
    /// </summary>
    public static int? GetInt32Value(this IRegistryKey key, string name) =>
        GetInt64Value(key, name) is long value && value >= int.MinValue && value <= int.MaxValue
            ? (int)value
            : null;

    /// <summary>
    /// Reads a 64-bit integer value, accepting a <c>REG_QWORD</c>, a <c>REG_DWORD</c>,
    /// or a string holding a decimal or <c>0x</c>-prefixed hex number.
    /// </summary>
    public static long? GetInt64Value(this IRegistryKey key, string name)
    {
        ArgumentNullException.ThrowIfNull(key);

        return key.GetValue(name) switch
        {
            int i => i,
            long l => l,
            uint u => u,
            ulong ul when ul <= long.MaxValue => (long)ul,
            string s => ParseInt64(s),
            _ => null,
        };
    }

    /// <summary>
    /// Reads a boolean flag. Registry flags are <c>REG_DWORD</c> values where any
    /// non-zero number means true, but some installers write them as strings, so
    /// <c>"1"</c>, <c>"true"</c> and <c>"yes"</c> are accepted too. A missing or
    /// unreadable value is <see langword="false"/>, which is the right default for
    /// every flag this library reads (<c>SystemComponent</c>, <c>NoRemove</c>,
    /// <c>WindowsInstaller</c>).
    /// </summary>
    public static bool GetFlagValue(this IRegistryKey key, string name)
    {
        ArgumentNullException.ThrowIfNull(key);

        object? raw = key.GetValue(name);
        if (raw is string text)
        {
            string trimmed = text.Trim();
            if (
                trimmed.Equals("true", StringComparison.OrdinalIgnoreCase)
                || trimmed.Equals("yes", StringComparison.OrdinalIgnoreCase)
            )
            {
                return true;
            }
        }

        return GetInt64Value(key, name) is long value && value != 0;
    }

    /// <summary>
    /// Reads an absolute URI. Vendors put plenty of non-URI text in <c>HelpLink</c>
    /// and <c>URLInfoAbout</c> (phone numbers, bare host names, empty strings), all of
    /// which are reported as <see langword="null"/> rather than thrown over.
    /// </summary>
    public static Uri? GetUriValue(this IRegistryKey key, string name) =>
        GetStringValue(key, name) is string text
        && Uri.TryCreate(text, UriKind.Absolute, out Uri? uri)
            ? uri
            : null;

    /// <summary>
    /// The registry type of a value, for callers that need to distinguish a genuinely
    /// absent value from one that is present but unusable.
    /// </summary>
    public static RegistryValueKind GetKindOrUnknown(this IRegistryKey key, string name)
    {
        ArgumentNullException.ThrowIfNull(key);
        return key.GetValueKind(name);
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static long? ParseInt64(string raw)
    {
        string text = raw.Trim();
        if (text.Length == 0)
        {
            return null;
        }

        bool negative = text[0] == '-';
        string digits = negative || text[0] == '+' ? text[1..] : text;

        if (digits.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return long.TryParse(
                digits[2..],
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out long hex
            )
                ? negative
                    ? -hex
                    : hex
                : null;
        }

        return long.TryParse(
            text,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out long value
        )
            ? value
            : null;
    }
}
