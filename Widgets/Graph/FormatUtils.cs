using MTGui.Common;

namespace MTGui.Graph;

/// <summary>
/// Utility class for formatting values in a human-readable way.
/// Provides consistent formatting for large numbers with K/M/B abbreviations.
/// </summary>
/// <remarks>
/// This class is maintained for backward compatibility.
/// For new code, use <see cref="MTNumberFormatter"/> instead.
/// </remarks>
[Obsolete("Use MTNumberFormatter from MTGui.Common instead. This class is maintained for backward compatibility.")]
public static class MTFormatUtils
{
    /// <summary>
    /// Formats a numeric value with K/M/B abbreviation.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>Formatted string like "1.5M" or "500K".</returns>
    [Obsolete("Use MTNumberFormatter.FormatCompact() instead.")]
    public static string FormatAbbreviated(double value) => MTNumberFormatter.FormatCompact(value);

    /// <summary>
    /// Formats a numeric value with K/M/B abbreviation (integer overload).
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>Formatted string like "1.5M" or "500K".</returns>
    [Obsolete("Use MTNumberFormatter.FormatCompact() instead.")]
    public static string FormatAbbreviated(long value) => MTNumberFormatter.FormatCompact(value);

    /// <summary>
    /// Formats a percentage value.
    /// </summary>
    /// <param name="value">The percentage value (0-100 scale).</param>
    /// <param name="decimals">Number of decimal places.</param>
    /// <returns>Formatted string like "45.5%".</returns>
    [Obsolete("Use MTNumberFormatter.FormatPercentage() instead.")]
    public static string FormatPercentage(double value, int decimals = 1) => MTNumberFormatter.FormatPercentage(value, decimals);

    /// <summary>
    /// Formats a value with number grouping (thousands separator) and optional decimals.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <param name="epsilon">Epsilon for floating point comparisons to determine if value is an integer.</param>
    /// <returns>Formatted string with thousands separators.</returns>
    [Obsolete("Use MTNumberFormatter.FormatStandard() instead.")]
    public static string FormatWithSeparators(float value, float epsilon = 0.0001f)
    {
        return MTNumberFormatter.FormatStandard(value);
    }

    /// <summary>
    /// Formats a time duration in a human-readable way.
    /// </summary>
    /// <param name="duration">The duration to format.</param>
    /// <returns>Formatted string like "2h 30m" or "5d 12h".</returns>
    [Obsolete("Use MTNumberFormatter.FormatDuration() instead.")]
    public static string FormatDuration(TimeSpan duration) => MTNumberFormatter.FormatDuration(duration);
}
