using System.Globalization;

namespace AddRem.Parsing;

/// <summary>
/// A parsed program version: up to four numeric components plus whatever trailing
/// text the vendor appended.
/// </summary>
/// <remarks>
/// <para>
/// <c>DisplayVersion</c> is free text. Real values include <c>12.0.40664</c>,
/// <c>1.4.40.0</c>, <c>26.163.0823.0004</c>, <c>2026.1 (build 3)</c>,
/// <c>1.0-beta2</c> and <c>v3.2</c>. This type takes the leading dotted numeric run
/// and keeps the rest as <see cref="Suffix"/>, so ordering works on the part that
/// means something and nothing is silently discarded.
/// </para>
/// <para>
/// The original text is deliberately not stored here — <c>InstalledProgram.RawVersion</c>
/// keeps that. Otherwise <c>1.0</c> and <c>1.0.0.0</c> would compare as different
/// values despite being the same version.
/// </para>
/// </remarks>
public readonly record struct ProgramVersion(
    int Major,
    int Minor,
    int Build,
    int Revision,
    string? Suffix
) : IComparable<ProgramVersion>
{
    /// <summary>
    /// Parses a version string. Returns <see langword="false"/> when there is no
    /// leading number at all, which is the only case that carries no information.
    /// </summary>
    public static bool TryParse(string? raw, out ProgramVersion version)
    {
        version = default;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        ReadOnlySpan<char> text = raw.AsSpan().Trim();

        // A leading "v" is common and carries no meaning.
        if (text.Length > 1 && (text[0] == 'v' || text[0] == 'V') && char.IsAsciiDigit(text[1]))
        {
            text = text[1..];
        }

        Span<int> components = [0, 0, 0, 0];
        int componentCount = 0;
        int position = 0;

        while (componentCount < 4 && position < text.Length && char.IsAsciiDigit(text[position]))
        {
            int start = position;
            while (position < text.Length && char.IsAsciiDigit(text[position]))
            {
                position++;
            }

            if (
                !int.TryParse(
                    text[start..position],
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out int component
                )
            )
            {
                // A run of digits too long for an int. Treat the whole thing as
                // unparseable rather than silently truncating it.
                return false;
            }

            components[componentCount++] = component;

            // Continue only if there is room for another component and a dot is
            // followed by a digit. "1.0." ends here, and so does the fifth component
            // of "1.2.3.4.5" — its separating dot must stay in the suffix.
            if (
                componentCount < 4
                && position + 1 < text.Length
                && text[position] == '.'
                && char.IsAsciiDigit(text[position + 1])
            )
            {
                position++;
            }
            else
            {
                break;
            }
        }

        if (componentCount == 0)
        {
            return false;
        }

        string? suffix = position < text.Length ? text[position..].Trim().ToString() : null;

        version = new ProgramVersion(
            components[0],
            components[1],
            components[2],
            components[3],
            string.IsNullOrEmpty(suffix) ? null : suffix
        );

        return true;
    }

    /// <summary>
    /// Unpacks the numeric <c>Version</c> value some installers write instead of a
    /// <c>DisplayVersion</c> string. Windows Installer packs it as
    /// <c>major &lt;&lt; 24 | minor &lt;&lt; 16 | build</c>.
    /// </summary>
    public static ProgramVersion FromPackedMsiVersion(int packed) =>
        new((packed >> 24) & 0xFF, (packed >> 16) & 0xFF, packed & 0xFFFF, 0, null);

    /// <summary>
    /// Orders by the numeric components, then by <see cref="Suffix"/> as an ordinal
    /// tie-break with "no suffix" first.
    /// </summary>
    /// <remarks>
    /// The suffix tie-break is lexical, not semantic. Registry versions are not
    /// semver, so this does not attempt to know that <c>-rc2</c> precedes a release.
    /// </remarks>
    public int CompareTo(ProgramVersion other)
    {
        int result = Major.CompareTo(other.Major);
        if (result != 0)
        {
            return result;
        }

        result = Minor.CompareTo(other.Minor);
        if (result != 0)
        {
            return result;
        }

        result = Build.CompareTo(other.Build);
        if (result != 0)
        {
            return result;
        }

        result = Revision.CompareTo(other.Revision);
        if (result != 0)
        {
            return result;
        }

        return string.CompareOrdinal(Suffix ?? string.Empty, other.Suffix ?? string.Empty);
    }

    /// <summary>Compares two versions.</summary>
    public static bool operator <(ProgramVersion left, ProgramVersion right) =>
        left.CompareTo(right) < 0;

    /// <summary>Compares two versions.</summary>
    public static bool operator >(ProgramVersion left, ProgramVersion right) =>
        left.CompareTo(right) > 0;

    /// <summary>Compares two versions.</summary>
    public static bool operator <=(ProgramVersion left, ProgramVersion right) =>
        left.CompareTo(right) <= 0;

    /// <summary>Compares two versions.</summary>
    public static bool operator >=(ProgramVersion left, ProgramVersion right) =>
        left.CompareTo(right) >= 0;

    /// <summary>Renders the four components, plus the suffix when there is one.</summary>
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Major}.{Minor}.{Build}.{Revision}{Suffix}");
}
