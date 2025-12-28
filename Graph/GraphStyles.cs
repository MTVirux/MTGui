using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImPlot;

namespace MTGui.Graph;

/// <summary>
/// Trading platform color palette for graph styling.
/// Provides vibrant, professional colors inspired by financial charting platforms.
/// </summary>
public static class ChartColors
{
    #region Background Colors
    
    /// <summary>Plot background color - dark charcoal.</summary>
    public static readonly Vector4 PlotBackground = new(0.08f, 0.09f, 0.10f, 1f);
    
    /// <summary>Frame background color - slightly darker than plot.</summary>
    public static readonly Vector4 FrameBackground = new(0.06f, 0.07f, 0.08f, 1f);
    
    #endregion
    
    #region Grid and Axis Colors
    
    /// <summary>Grid line color with transparency.</summary>
    public static readonly Vector4 GridLine = new(0.18f, 0.20f, 0.22f, 0.6f);
    
    /// <summary>Axis line color.</summary>
    public static readonly Vector4 AxisLine = new(0.25f, 0.28f, 0.30f, 1f);
    
    #endregion
    
    #region Price Movement Colors
    
    /// <summary>Bullish/positive trend color - bright green.</summary>
    public static readonly Vector4 Bullish = new(0.20f, 0.90f, 0.40f, 1f);
    
    /// <summary>Bullish fill color - top of gradient.</summary>
    public static readonly Vector4 BullishFillTop = new(0.20f, 0.90f, 0.40f, 0.60f);
    
    /// <summary>Bullish fill color - bottom of gradient (very transparent).</summary>
    public static readonly Vector4 BullishFillBottom = new(0.20f, 0.90f, 0.40f, 0.05f);
    
    /// <summary>Bearish/negative trend color - bright red.</summary>
    public static readonly Vector4 Bearish = new(1.0f, 0.25f, 0.25f, 1f);
    
    /// <summary>Bearish fill color - top of gradient.</summary>
    public static readonly Vector4 BearishFillTop = new(1.0f, 0.25f, 0.25f, 0.60f);
    
    /// <summary>Bearish fill color - bottom of gradient (very transparent).</summary>
    public static readonly Vector4 BearishFillBottom = new(1.0f, 0.25f, 0.25f, 0.05f);
    
    /// <summary>Neutral/no change color - bright yellow.</summary>
    public static readonly Vector4 Neutral = new(1.0f, 0.85f, 0.0f, 1f);
    
    #endregion
    
    #region Crosshair and Tooltip Colors
    
    /// <summary>Crosshair color with transparency.</summary>
    public static readonly Vector4 Crosshair = new(0.55f, 0.58f, 0.62f, 0.8f);
    
    /// <summary>Tooltip background color.</summary>
    public static readonly Vector4 TooltipBackground = new(0.12f, 0.14f, 0.16f, 0.95f);
    
    /// <summary>Tooltip border color.</summary>
    public static readonly Vector4 TooltipBorder = new(0.30f, 0.32f, 0.35f, 1f);
    
    #endregion
    
    #region Current Price Line
    
    /// <summary>Current price horizontal line color - bright yellow.</summary>
    public static readonly Vector4 CurrentPriceLine = new(1.0f, 0.85f, 0.0f, 0.9f);
    
    #endregion
    
    #region Text Colors
    
    /// <summary>Primary text color - bright white/gray.</summary>
    public static readonly Vector4 TextPrimary = new(0.90f, 0.92f, 0.94f, 1f);
    
    /// <summary>Secondary text color - dimmed gray.</summary>
    public static readonly Vector4 TextSecondary = new(0.55f, 0.58f, 0.62f, 1f);
    
    #endregion
    
    #region Series Color Palette
    
    /// <summary>
    /// Default series color palette for multi-series graphs.
    /// Vibrant, distinguishable colors for up to 8 series.
    /// </summary>
    public static readonly Vector3[] SeriesPalette =
    {
        new(1.0f, 0.25f, 0.25f),   // Bright Red
        new(0.25f, 0.50f, 1.0f),   // Bright Blue
        new(0.20f, 0.90f, 0.40f),  // Bright Green
        new(0.75f, 0.30f, 0.90f),  // Bright Purple
        new(1.0f, 0.45f, 0.70f),   // Bright Pink
        new(1.0f, 0.85f, 0.0f),    // Bright Yellow
        new(1.0f, 0.55f, 0.0f),    // Bright Orange
        new(0.0f, 0.85f, 0.85f),   // Bright Cyan
    };
    
    #endregion
    
    #region Style Push/Pop Methods
    
    /// <summary>
    /// Number of ImPlot style colors pushed by PushChartStyle.
    /// </summary>
    private const int StyleColorCount = 5;
    
    /// <summary>
    /// Number of ImPlot style variables pushed by PushChartStyle.
    /// </summary>
    private const int StyleVarCount = 2;
    
    /// <summary>
    /// Applies trading platform style colors to the plot.
    /// Must be called before BeginPlot and matched with PopChartStyle after EndPlot.
    /// </summary>
    /// <param name="style">Optional style configuration for customization.</param>
    public static void PushChartStyle(GraphStyleConfig? style = null)
    {
        style ??= GraphStyleConfig.Default;
        
        // Plot frame and background
        ImPlot.PushStyleColor(ImPlotCol.Bg, PlotBackground);
        ImPlot.PushStyleColor(ImPlotCol.FrameBg, FrameBackground);
        
        // Line styling - bullish green by default
        ImPlot.PushStyleColor(ImPlotCol.Line, Bullish);
        ImPlot.PushStyleColor(ImPlotCol.Fill, BullishFillTop);
        
        // Crosshair
        ImPlot.PushStyleColor(ImPlotCol.Crosshairs, Crosshair);
        
        // Style variables
        ImPlot.PushStyleVar(ImPlotStyleVar.LineWeight, style.LineWeight);
        ImPlot.PushStyleVar(ImPlotStyleVar.FillAlpha, style.FillAlpha);
    }
    
    /// <summary>
    /// Pops the trading platform style colors.
    /// Must be called after EndPlot to match a previous PushChartStyle call.
    /// </summary>
    public static void PopChartStyle()
    {
        ImPlot.PopStyleVar(StyleVarCount);
        ImPlot.PopStyleColor(StyleColorCount);
    }
    
    /// <summary>
    /// Gets colors for the specified number of series.
    /// </summary>
    /// <param name="count">Number of series.</param>
    /// <returns>Array of colors cycling through the palette.</returns>
    public static Vector3[] GetSeriesColors(int count)
    {
        var result = new Vector3[count];
        for (var i = 0; i < count; i++)
            result[i] = SeriesPalette[i % SeriesPalette.Length];
        return result;
    }
    
    #endregion
}

/// <summary>
/// Style configuration for ImPlot graphs.
/// Contains all configurable style values that can be customized.
/// </summary>
public class GraphStyleConfig
{
    #region Line and Fill Styles
    
    /// <summary>Line weight/thickness for plot lines. Default: 2.0</summary>
    public float LineWeight { get; set; } = 2f;
    
    /// <summary>Fill alpha for area charts. Default: 0.35</summary>
    public float FillAlpha { get; set; } = 0.35f;
    
    /// <summary>Fill alpha for multi-series area charts. Default: 0.55</summary>
    public float MultiSeriesFillAlpha { get; set; } = 0.55f;
    
    #endregion
    
    #region Crosshair Styles
    
    /// <summary>Crosshair dash length in pixels. Default: 4</summary>
    public float CrosshairDashLength { get; set; } = 4f;
    
    /// <summary>Crosshair gap length in pixels. Default: 3</summary>
    public float CrosshairGapLength { get; set; } = 3f;
    
    /// <summary>Crosshair line thickness. Default: 1</summary>
    public float CrosshairThickness { get; set; } = 1f;
    
    #endregion
    
    #region Price Line Styles
    
    /// <summary>Current price line dash length. Default: 6</summary>
    public float PriceLineDashLength { get; set; } = 6f;
    
    /// <summary>Current price line gap length. Default: 4</summary>
    public float PriceLineGapLength { get; set; } = 4f;
    
    /// <summary>Current price line thickness. Default: 1.5</summary>
    public float PriceLineThickness { get; set; } = 1.5f;
    
    #endregion
    
    #region Tooltip Styles
    
    /// <summary>Tooltip padding in pixels. Default: 8</summary>
    public float TooltipPadding { get; set; } = 8f;
    
    /// <summary>Tooltip corner rounding. Default: 4</summary>
    public float TooltipRounding { get; set; } = 4f;
    
    /// <summary>Tooltip accent bar width. Default: 3</summary>
    public float TooltipAccentWidth { get; set; } = 3f;
    
    /// <summary>Tooltip offset from cursor X. Default: 12</summary>
    public float TooltipOffsetX { get; set; } = 12f;
    
    #endregion
    
    #region Legend Styles
    
    /// <summary>Legend indicator size (colored square). Default: 10</summary>
    public float LegendIndicatorSize { get; set; } = 10f;
    
    /// <summary>Legend row height. Default: 18</summary>
    public float LegendRowHeight { get; set; } = 18f;
    
    /// <summary>Legend padding. Default: 8</summary>
    public float LegendPadding { get; set; } = 8f;
    
    /// <summary>Legend scrollbar width. Default: 6</summary>
    public float LegendScrollbarWidth { get; set; } = 6f;
    
    /// <summary>Legend corner rounding. Default: 4</summary>
    public float LegendRounding { get; set; } = 4f;
    
    /// <summary>Alpha for hidden series in legend. Default: 0.35</summary>
    public float LegendHiddenAlpha { get; set; } = 0.35f;
    
    #endregion
    
    #region Value Label Styles
    
    /// <summary>Value label background padding. Default: 4</summary>
    public float ValueLabelPadding { get; set; } = 4f;
    
    /// <summary>Value label corner rounding. Default: 3</summary>
    public float ValueLabelRounding { get; set; } = 3f;
    
    /// <summary>Value label connecting line thickness. Default: 1.5</summary>
    public float ValueLabelLineThickness { get; set; } = 1.5f;
    
    #endregion
    
    #region Controls Drawer Styles
    
    /// <summary>Toggle button width. Default: 24</summary>
    public float ToggleButtonWidth { get; set; } = 24f;
    
    /// <summary>Toggle button height. Default: 20</summary>
    public float ToggleButtonHeight { get; set; } = 20f;
    
    /// <summary>Controls drawer width. Default: 160</summary>
    public float DrawerWidth { get; set; } = 160f;
    
    /// <summary>Controls drawer padding. Default: 8</summary>
    public float DrawerPadding { get; set; } = 8f;
    
    /// <summary>Controls drawer row height. Default: 22</summary>
    public float DrawerRowHeight { get; set; } = 22f;
    
    /// <summary>Controls checkbox size. Default: 14</summary>
    public float DrawerCheckboxSize { get; set; } = 14f;
    
    #endregion
    
    /// <summary>
    /// Creates a default style configuration.
    /// </summary>
    public static GraphStyleConfig Default => new();
}
