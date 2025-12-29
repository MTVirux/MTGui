namespace MTGui.Graph;

/// <summary>
/// Graph visualization type for time-series data.
/// </summary>
public enum MTGraphType
{
    /// <summary>Filled area chart - good for showing volume over time.</summary>
    Area = 0,
    
    /// <summary>Simple line chart.</summary>
    Line = 1,
    
    /// <summary>Step/stairs chart - shows discrete value changes.</summary>
    Stairs = 2,
    
    /// <summary>Vertical bar chart.</summary>
    Bars = 3,
    
    /// <summary>Step/stairs chart with filled area - combines stairs with shaded region.</summary>
    StairsArea = 4
}

/// <summary>
/// Unified time unit for data filtering, auto-scroll, and time range selection.
/// </summary>
public enum MTTimeUnit
{
    /// <summary>Seconds - for fine-grained auto-scroll control.</summary>
    Seconds = 0,
    
    /// <summary>Minutes.</summary>
    Minutes = 1,
    
    /// <summary>Hours.</summary>
    Hours = 2,
    
    /// <summary>Days.</summary>
    Days = 3,
    
    /// <summary>Weeks.</summary>
    Weeks = 4,
    
    /// <summary>Months - approximate (30 days).</summary>
    Months = 5,
    
    /// <summary>All time - no time limit applied.</summary>
    All = 6
}

/// <summary>
/// Specifies where the legend should be positioned in the graph.
/// </summary>
public enum MTLegendPosition
{
    /// <summary>Legend is drawn outside the graph area (to the right).</summary>
    Outside = 0,
    
    /// <summary>Legend is drawn inside the graph area (top-left corner).</summary>
    InsideTopLeft = 1,
    
    /// <summary>Legend is drawn inside the graph area (top-right corner).</summary>
    InsideTopRight = 2,
    
    /// <summary>Legend is drawn inside the graph area (bottom-left corner).</summary>
    InsideBottomLeft = 3,
    
    /// <summary>Legend is drawn inside the graph area (bottom-right corner).</summary>
    InsideBottomRight = 4
}

/// <summary>
/// Extension methods for MTTimeUnit enum.
/// </summary>
public static class MTTimeUnitExtensions
{
    /// <summary>
    /// Converts a time value and unit to total seconds.
    /// </summary>
    /// <param name="unit">The time unit.</param>
    /// <param name="value">The numeric value.</param>
    /// <returns>The total time in seconds.</returns>
    public static double ToSeconds(this MTTimeUnit unit, int value) => unit switch
    {
        MTTimeUnit.Seconds => value,
        MTTimeUnit.Minutes => value * 60,
        MTTimeUnit.Hours => value * 3600,
        MTTimeUnit.Days => value * 86400,
        MTTimeUnit.Weeks => value * 604800,
        MTTimeUnit.Months => value * 2592000, // 30 days
        MTTimeUnit.All => double.MaxValue,
        _ => 3600
    };
    
    /// <summary>
    /// Gets the display name for a time unit.
    /// </summary>
    public static string GetDisplayName(this MTTimeUnit unit) => unit switch
    {
        MTTimeUnit.Seconds => "Seconds",
        MTTimeUnit.Minutes => "Minutes",
        MTTimeUnit.Hours => "Hours",
        MTTimeUnit.Days => "Days",
        MTTimeUnit.Weeks => "Weeks",
        MTTimeUnit.Months => "Months",
        MTTimeUnit.All => "All Time",
        _ => "Unknown"
    };
    
    /// <summary>
    /// Gets the short name for a time unit (for compact UI).
    /// </summary>
    public static string GetShortName(this MTTimeUnit unit) => unit switch
    {
        MTTimeUnit.Seconds => "sec",
        MTTimeUnit.Minutes => "min",
        MTTimeUnit.Hours => "hr",
        MTTimeUnit.Days => "day",
        MTTimeUnit.Weeks => "wk",
        MTTimeUnit.Months => "mo",
        MTTimeUnit.All => "all",
        _ => "?"
    };
}

/// <summary>
/// Extension methods for MTGraphType enum.
/// </summary>
public static class MTGraphTypeExtensions
{
    /// <summary>
    /// Gets the display name for a graph type.
    /// </summary>
    public static string GetDisplayName(this MTGraphType type) => type switch
    {
        MTGraphType.Area => "Area",
        MTGraphType.Line => "Line",
        MTGraphType.Stairs => "Stairs",
        MTGraphType.Bars => "Bars",
        MTGraphType.StairsArea => "Stairs Area",
        _ => "Unknown"
    };
    
    /// <summary>
    /// Gets a description of the graph type.
    /// </summary>
    public static string GetDescription(this MTGraphType type) => type switch
    {
        MTGraphType.Area => "Filled area chart - good for showing volume over time",
        MTGraphType.Line => "Simple line chart",
        MTGraphType.Stairs => "Step chart showing discrete value changes",
        MTGraphType.Bars => "Vertical bar chart",
        MTGraphType.StairsArea => "Step chart with filled area below",
        _ => "Unknown graph type"
    };
}
