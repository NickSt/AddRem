using System.Globalization;

namespace AddRem.Parsing;

/// <summary>
/// Reads the <c>InstallDate</c> value, which is documented as <c>yyyyMMdd</c> and is
/// not reliably that in practice.
/// </summary>
/// <remarks>
/// On a single real machine this value appears both as <c>20260615</c> and as
/// <c>1725416004</c> — a Unix timestamp written by an installer that did not read the
/// specification. Formats are tried most-specific first, and anything that decodes to
/// an implausible date is rejected rather than reported, since a wrong date is worse
/// than no date.
/// </remarks>
public static class InstallDateParser
{
    /// <summary>Installs before this are assumed to be corrupt data, not history.</summary>
    private static readonly DateOnly EarliestPlausible = new(1990, 1, 1);

    /// <summary>Parses an install date, or returns <see langword="false"/>.</summary>
    public static bool TryParse(string? raw, out DateOnly date) =>
        TryParse(raw, DateOnly.FromDateTime(DateTime.UtcNow), out date);

    /// <summary>
    /// Parses an install date against a caller-supplied "today", so the
    /// implausible-future rule can be tested deterministically.
    /// </summary>
    internal static bool TryParse(string? raw, DateOnly today, out DateOnly date)
    {
        date = default;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        string text = raw.Trim();
        DateOnly latestPlausible = today.AddYears(1);

        if (TryParseCandidate(text, out DateOnly candidate) && IsPlausible(candidate))
        {
            date = candidate;
            return true;
        }

        return false;

        bool IsPlausible(DateOnly value) => value >= EarliestPlausible && value <= latestPlausible;
    }

    private static bool TryParseCandidate(string text, out DateOnly date)
    {
        date = default;

        // The documented form, and by far the most common.
        if (
            DateOnly.TryParseExact(
                text,
                "yyyyMMdd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date
            )
        )
        {
            return true;
        }

        // Unix timestamps. Ten digits is seconds, thirteen is milliseconds; both are
        // unambiguous against yyyyMMdd, which is always eight.
        if (long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out long epoch))
        {
            if (text.Length == 10)
            {
                date = DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(epoch).UtcDateTime);
                return true;
            }

            if (text.Length == 13)
            {
                date = DateOnly.FromDateTime(
                    DateTimeOffset.FromUnixTimeMilliseconds(epoch).UtcDateTime
                );
                return true;
            }
        }

        string[] formats = ["yyyy-MM-dd", "yyyy/MM/dd", "M/d/yyyy", "d/M/yyyy", "MM/dd/yyyy"];
        if (
            DateOnly.TryParseExact(
                text,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date
            )
        )
        {
            return true;
        }

        // Last resort, invariant only. Parsing with the machine's culture would make
        // the same registry produce different dates on different machines.
        if (
            DateTime.TryParse(
                text,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime parsed
            )
        )
        {
            date = DateOnly.FromDateTime(parsed);
            return true;
        }

        return false;
    }
}
