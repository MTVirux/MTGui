namespace MTGui.Common;

/// <summary>
/// Configuration for number formatting in widgets.
/// </summary>
public class NumberFormatConfig
{
    /// <summary>
    /// The formatting style to use.
    /// </summary>
    public NumberFormatStyle Style { get; set; } = NumberFormatStyle.Compact;
    
    /// <summary>
    /// Number of decimal places to display (0-2).
    /// Primarily used for Compact style, but may apply to other styles.
    /// </summary>
    public int DecimalPlaces { get; set; } = 2;
    
    /// <summary>
    /// Whether to use thousands separators in Standard mode.
    /// </summary>
    public bool UseThousandsSeparator { get; set; } = true;
    
    /// <summary>
    /// Creates a deep copy of this configuration.
    /// </summary>
    public NumberFormatConfig Clone() => new()
    {
        Style = Style,
        DecimalPlaces = DecimalPlaces,
        UseThousandsSeparator = UseThousandsSeparator
    };
    
    /// <summary>
    /// Copies settings from another configuration.
    /// </summary>
    public void CopyFrom(NumberFormatConfig other)
    {
        Style = other.Style;
        DecimalPlaces = other.DecimalPlaces;
        UseThousandsSeparator = other.UseThousandsSeparator;
    }
    
    /// <summary>
    /// Default configuration (Compact with 2 decimal places).
    /// </summary>
    public static NumberFormatConfig Default => new();
    
    /// <summary>
    /// Compact configuration with 2 decimal places.
    /// </summary>
    public static NumberFormatConfig Compact => new();
    
    /// <summary>
    /// Standard configuration with thousands separators.
    /// </summary>
    public static NumberFormatConfig Standard => new() { Style = NumberFormatStyle.Standard };
    
    /// <summary>
    /// Compact configuration with no decimal places.
    /// </summary>
    public static NumberFormatConfig CompactNoDecimals => new() { DecimalPlaces = 0 };
    
    /// <summary>
    /// Compact configuration with 1 decimal place.
    /// </summary>
    public static NumberFormatConfig CompactOneDecimal => new() { DecimalPlaces = 1 };
}
