namespace MTGui.Graph;

/// <summary>
/// Utility class for formatting values in a human-readable way.
/// Provides consistent formatting for large numbers with K/M/B abbreviations.
/// </summary>
public static class MTFormatUtils
{
    /// <summary>
    /// Formats a numeric value with K/M/B abbreviation.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>Formatted string like "1.5M" or "500K".</returns>
    public static string FormatAbbreviated(double value)
    {
        return value switch
        {
            >= 1_000_000_000 => $"{value / 1_000_000_000:0.##}B",
            >= 1_000_000 => $"{value / 1_000_000:0.##}M",
            >= 1_000 => $"{value / 1_000:0.##}K",
            _ => $"{value:0.##}"
        };
    }

    /// <summary>
    /// Formats a numeric value with K/M/B abbreviation (integer overload).
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>Formatted string like "1.5M" or "500K".</returns>
    public static string FormatAbbreviated(long value) => FormatAbbreviated((double)value);

    /// <summary>
    /// Formats a percentage value.
    /// </summary>
    /// <param name="value">The percentage value (0-100 scale).</param>
    /// <param name="decimals">Number of decimal places.</param>
    /// <returns>Formatted string like "45.5%".</returns>
    public static string FormatPercentage(double value, int decimals = 1) => $"{value.ToString($"F{decimals}")}%";

    /// <summary>
    /// Formats a value with number grouping (thousands separator) and optional decimals.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <param name="epsilon">Epsilon for floating point comparisons to determine if value is an integer.</param>
    /// <returns>Formatted string with thousands separators.</returns>
    public static string FormatWithSeparators(float value, float epsilon = 0.0001f)
    {
        if (Math.Abs(value - Math.Truncate(value)) < epsilon)
            return ((long)value).ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
        return value.ToString("N2", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Formats a time duration in a human-readable way.
    /// </summary>
    /// <param name="duration">The duration to format.</param>
    /// <returns>Formatted string like "2h 30m" or "5d 12h".</returns>
    public static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalDays >= 1)
            return $"{(int)duration.TotalDays}d {duration.Hours}h";
        if (duration.TotalHours >= 1)
            return $"{(int)duration.TotalHours}h {duration.Minutes}m";
        if (duration.TotalMinutes >= 1)
            return $"{(int)duration.TotalMinutes}m {duration.Seconds}s";
        return $"{duration.Seconds}s";
    }
}
