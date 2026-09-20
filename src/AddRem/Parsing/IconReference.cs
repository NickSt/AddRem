using System.Globalization;

namespace AddRem.Parsing;

/// <summary>
/// Where a program's icon lives: a file, plus which icon within it.
/// </summary>
/// <remarks>
/// <para>
/// This is a descriptor, not an image. Resolving it to pixels needs either
/// <c>System.Drawing</c> or a P/Invoke to <c>PrivateExtractIcons</c>, and WPF, WinUI
/// and Avalonia can all load a <c>path,index</c> pair directly — so the core library
/// stays dependency-free and hands back the reference.
/// </para>
/// <para>
/// Observed <c>DisplayIcon</c> forms: a bare path with no index, <c>...\Vortex.exe,0</c>,
/// <c>...\OneDriveSetup.exe,-101</c>, and <c>...\git-for-windows.ico</c>. The negative
/// form is the one that catches people out — it is a resource ID, not an index counted
/// from the end.
/// </para>
/// </remarks>
public readonly record struct IconReference(string Path, int Index)
{
    /// <summary>
    /// <see langword="true"/> when <see cref="Index"/> is negative, meaning it names a
    /// resource ID rather than an ordinal position within the file.
    /// </summary>
    public bool IsResourceId => Index < 0;

    /// <summary>
    /// The resource ID when <see cref="IsResourceId"/>, otherwise <see langword="null"/>.
    /// </summary>
    public int? ResourceId => Index < 0 ? -Index : null;

    /// <summary>
    /// Parses a <c>DisplayIcon</c> value. Returns <see langword="false"/> when there is
    /// no usable path.
    /// </summary>
    public static bool TryParse(string? displayIcon, out IconReference icon)
    {
        icon = default;

        if (string.IsNullOrWhiteSpace(displayIcon))
        {
            return false;
        }

        string text = displayIcon.Trim();

        // A fully quoted path, optionally followed by an index: "C:\a\b.exe",0
        if (text.StartsWith('"'))
        {
            int closingQuote = text.IndexOf('"', 1);
            if (closingQuote > 1)
            {
                string quotedPath = text[1..closingQuote];
                string remainder = text[(closingQuote + 1)..].TrimStart();
                int quotedIndex =
                    remainder.StartsWith(',') && TryParseIndex(remainder[1..], out int parsed)
                        ? parsed
                        : 0;

                return Create(quotedPath, quotedIndex, out icon);
            }
        }

        // Unquoted. Split on the LAST comma, because paths may legitimately contain
        // one; only treat the tail as an index if it actually parses as a number.
        int lastComma = text.LastIndexOf(',');
        if (lastComma >= 0 && TryParseIndex(text[(lastComma + 1)..], out int index))
        {
            return Create(text[..lastComma], index, out icon);
        }

        return Create(text, 0, out icon);
    }

    private static bool Create(string path, int index, out IconReference icon)
    {
        string trimmed = path.Trim().Trim('"');
        if (trimmed.Length == 0)
        {
            icon = default;
            return false;
        }

        icon = new IconReference(trimmed, index);
        return true;
    }

    private static bool TryParseIndex(string text, out int index) =>
        int.TryParse(
            text.Trim(),
            NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture,
            out index
        );

    /// <summary>Renders the reference back to <c>path,index</c> form.</summary>
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Path},{Index}");
}
