using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImPlot;

namespace MTGui.Graph;

/// <summary>
/// A reusable ImPlot graph widget for displaying numerical sample data.
/// Renders using ImPlot with a trading platform (Binance-style) aesthetic.
/// </summary>
/// <remarks>
/// This is the main graph component that orchestrates all the rendering utilities
/// from GraphDrawing, GraphLegend, and GraphControls.
/// </remarks>
public class ImPlotGraph
{
    #region Fields
    
    private readonly ImPlotGraphConfig _config;
    
    /// <summary>
    /// Set of series names that are currently hidden.
    /// </summary>
    private readonly HashSet<string> _hiddenSeries = new();
    
    /// <summary>
    /// Scroll offset for the inside legend.
    /// </summary>
    private float _insideLegendScrollOffset = 0f;
    
    /// <summary>
    /// Whether the controls drawer is currently open.
    /// </summary>
    private bool _controlsDrawerOpen = false;
    
    /// <summary>
    /// Cached legend bounds from the previous frame for input blocking.
    /// </summary>
    private GraphLegend.InsideLegendResult _cachedLegendResult;
    
    /// <summary>
    /// Cached controls drawer bounds from the previous frame.
    /// </summary>
    private GraphControls.ControlsDrawerResult _cachedControlsResult;
    
    // === Array pooling ===
    private readonly Dictionary<string, double[]> _pooledXArrays = new();
    private readonly Dictionary<string, double[]> _pooledYArrays = new();
    
    // === PreparedGraphData caching ===
    private PreparedGraphData? _cachedPreparedData;
    private IReadOnlyList<(string name, IReadOnlyList<(DateTime ts, float value)> samples)>? _lastSeriesData;
    private IReadOnlyList<(string name, IReadOnlyList<(DateTime ts, float value)> samples, Vector4? color)>? _lastSeriesDataWithColors;
    private int _lastHiddenSeriesHash;
    private bool _lastAutoScrollEnabled;
    private DateTime _lastPreparedDataTime;
    
    #endregion
    
    #region Events
    
    /// <summary>
    /// Event raised when auto-scroll settings change via the controls drawer.
    /// </summary>
    public event Action<bool, int, TimeUnit, float>? OnAutoScrollSettingsChanged;
    
    #endregion
    
    #region Properties
    
    /// <summary>
    /// Gets the configuration for this graph.
    /// </summary>
    public ImPlotGraphConfig Config => _config;
    
    /// <summary>
    /// Gets or sets whether the mouse is over an overlay element (legend, controls drawer).
    /// When true, plot inputs are disabled to prevent unintended panning/zooming.
    /// </summary>
    public bool IsMouseOverOverlay { get; private set; }
    
    #endregion
    
    #region Constructors
    
    /// <summary>
    /// Creates a new ImPlotGraph with default configuration.
    /// </summary>
    public ImPlotGraph() : this(new ImPlotGraphConfig()) { }
    
    /// <summary>
    /// Creates a new ImPlotGraph with custom configuration.
    /// </summary>
    /// <param name="config">The graph configuration.</param>
    public ImPlotGraph(ImPlotGraphConfig config)
    {
        _config = config ?? new ImPlotGraphConfig();
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Renders the graph with the provided samples using ImPlot.
    /// </summary>
    /// <param name="samples">The sample data to plot.</param>
    public void Render(IReadOnlyList<float> samples)
    {
        if (samples == null || samples.Count == 0)
        {
            DrawNoDataMessage();
            return;
        }
        
        var preparedData = PrepareIndexBasedData(samples);
        DrawGraph(preparedData);
    }
    
    /// <summary>
    /// Renders multiple data series overlaid on the same graph with time-aligned data.
    /// </summary>
    /// <param name="series">List of data series with names and timestamped values.</param>
    public void RenderMultipleSeries(IReadOnlyList<(string name, IReadOnlyList<(DateTime ts, float value)> samples)> series)
    {
        if (series == null || series.Count == 0 || series.All(s => s.samples == null || s.samples.Count == 0))
        {
            DrawNoDataMessage();
            return;
        }
        
        PreparedGraphData data;
        var needsRecompute = NeedsPreparedDataRecompute(series);
        
        if (needsRecompute)
        {
            data = PrepareTimeBasedData(series);
            _cachedPreparedData = data;
            _lastSeriesData = series;
            _lastHiddenSeriesHash = ComputeHiddenSeriesHash();
            _lastAutoScrollEnabled = _config.AutoScrollEnabled;
            _lastPreparedDataTime = DateTime.UtcNow;
        }
        else
        {
            data = _cachedPreparedData!;
            
            if (_config.AutoScrollEnabled)
            {
                UpdateAutoScrollLimits(data);
            }
        }
        
        DrawGraph(data);
    }
    
    /// <summary>
    /// Renders multiple data series with custom colors.
    /// </summary>
    /// <param name="series">List of data series with names, timestamped values, and optional colors.</param>
    public void RenderMultipleSeries(IReadOnlyList<(string name, IReadOnlyList<(DateTime ts, float value)> samples, Vector4? color)> series)
    {
        if (series == null || series.Count == 0 || series.All(s => s.samples == null || s.samples.Count == 0))
        {
            DrawNoDataMessage();
            return;
        }
        
        PreparedGraphData data;
        var needsRecompute = NeedsPreparedDataRecomputeWithColors(series);
        
        if (needsRecompute)
        {
            data = PrepareTimeBasedDataWithColors(series);
            _cachedPreparedData = data;
            _lastSeriesDataWithColors = series;
            _lastSeriesData = null;
            _lastHiddenSeriesHash = ComputeHiddenSeriesHash();
            _lastAutoScrollEnabled = _config.AutoScrollEnabled;
            _lastPreparedDataTime = DateTime.UtcNow;
        }
        else
        {
            data = _cachedPreparedData!;
            
            if (_config.AutoScrollEnabled)
            {
                UpdateAutoScrollLimits(data);
            }
        }
        
        DrawGraph(data);
    }
    
    /// <summary>
    /// Updates the Y-axis bounds without recreating the widget.
    /// </summary>
    public void UpdateBounds(float minValue, float maxValue)
    {
        _config.MinValue = minValue;
        _config.MaxValue = maxValue;
    }
    
    /// <summary>
    /// Toggles the visibility of a series by name.
    /// </summary>
    public void ToggleSeriesVisibility(string seriesName)
    {
        if (!_hiddenSeries.Add(seriesName))
        {
            _hiddenSeries.Remove(seriesName);
        }
    }
    
    /// <summary>
    /// Shows a series by name.
    /// </summary>
    public void ShowSeries(string seriesName)
    {
        _hiddenSeries.Remove(seriesName);
    }
    
    /// <summary>
    /// Hides a series by name.
    /// </summary>
    public void HideSeries(string seriesName)
    {
        _hiddenSeries.Add(seriesName);
    }
    
    /// <summary>
    /// Checks if a series is currently hidden.
    /// </summary>
    public bool IsSeriesHidden(string seriesName) => _hiddenSeries.Contains(seriesName);
    
    /// <summary>
    /// Clears the data cache, forcing a full recompute on the next render.
    /// </summary>
    public void ClearCache()
    {
        _cachedPreparedData = null;
        _lastSeriesData = null;
        _lastSeriesDataWithColors = null;
    }
    
    #endregion
    
    #region Core Graph Rendering
    
    /// <summary>
    /// Core graph drawing method.
    /// </summary>
    private unsafe void DrawGraph(PreparedGraphData data)
    {
        try
        {
            var avail = ImGui.GetContentRegionAvail();
            
            // Reserve space for outside legend if needed
            var useOutsideLegend = _config.ShowLegend && 
                                   _config.LegendPosition == LegendPosition.Outside && 
                                   data.HasMultipleSeriesTotal;
            var legendWidth = useOutsideLegend ? _config.LegendWidth : 0f;
            var legendPadding = useOutsideLegend ? 5f : 0f;
            var plotWidth = Math.Max(1f, avail.X - legendWidth - legendPadding);
            var plotSize = new Vector2(plotWidth, Math.Max(1f, avail.Y));

            // Configure plot flags
            var plotFlags = ImPlotFlags.NoTitle | ImPlotFlags.NoLegend | ImPlotFlags.NoMenus | 
                           ImPlotFlags.NoBoxSelect | ImPlotFlags.NoMouseText | ImPlotFlags.Crosshairs;
            
            // Check if mouse is over overlay from previous frame
            IsMouseOverOverlay = GraphLegend.IsMouseOverLegend(_cachedLegendResult) || 
                                GraphControls.IsMouseOverDrawer(_cachedControlsResult);
            
            if (IsMouseOverOverlay)
            {
                plotFlags |= ImPlotFlags.NoInputs;
            }
            
            // Apply styling
            ChartColors.PushChartStyle(_config.Style);
            
            // Set axis limits
            var plotCondition = _config.AutoScrollEnabled ? ImPlotCond.Always : ImPlotCond.Once;
            ImPlot.SetNextAxesLimits(data.XMin, data.XMax, data.YMin, data.YMax, plotCondition);

            var plotId = data.HasMultipleSeries ? $"##{_config.PlotId}_multi" : $"##{_config.PlotId}";
            
            if (ImPlot.BeginPlot(plotId, plotSize, plotFlags))
            {
                // Configure axes
                var xAxisFlags = data.IsTimeBased && _config.ShowXAxisTimestamps 
                    ? ImPlotAxisFlags.None 
                    : ImPlotAxisFlags.NoTickLabels;
                var yAxisFlags = ImPlotAxisFlags.Opposite;
                
                if (!_config.ShowGridLines)
                {
                    xAxisFlags |= ImPlotAxisFlags.NoGridLines;
                    yAxisFlags |= ImPlotAxisFlags.NoGridLines;
                }
                
                ImPlot.SetupAxes("", "", xAxisFlags, yAxisFlags);
                
                // Format axes
                if (data.IsTimeBased && _config.ShowXAxisTimestamps)
                {
                    ImPlot.SetupAxisFormat(ImAxis.X1, GraphDrawing.XAxisTimeFormatter, (void*)data.StartTime.Ticks);
                }
                ImPlot.SetupAxisFormat(ImAxis.Y1, GraphDrawing.YAxisFormatter);
                
                // Constrain axes
                ImPlot.SetupAxisLimitsConstraints(ImAxis.X1, 0, double.MaxValue);
                ImPlot.SetupAxisLimitsConstraints(ImAxis.Y1, 0, double.MaxValue);
                
                // Plot dummy points for auto-fit padding
                var dummyX = stackalloc double[2] { 0, data.XMax };
                var dummyY = stackalloc double[2] { data.YMin > 0 ? data.YMin : 0, data.YMax };
                ImPlot.SetNextMarkerStyle(ImPlotMarker.None);
                ImPlot.SetNextLineStyle(new Vector4(0, 0, 0, 0), 0);
                ImPlot.PlotLine("##padding", dummyX, dummyY, 2);
                
                // Draw each series
                foreach (var series in data.Series)
                {
                    if (!series.Visible) continue;
                    DrawSeries(series, data);
                }
                
                // Draw current price line for single series
                if (_config.ShowCurrentPriceLine && !data.HasMultipleSeries)
                {
                    var lastVisibleSeries = data.Series.LastOrDefault(s => s.Visible);
                    if (lastVisibleSeries != null && lastVisibleSeries.PointCount > 0)
                    {
                        var currentValue = lastVisibleSeries.YValues[lastVisibleSeries.PointCount - 1];
                        GraphDrawing.DrawCurrentPriceLine(currentValue, _config.Style);
                    }
                }
                
                // Draw hover effects
                if (_config.ShowCrosshair && ImPlot.IsPlotHovered())
                {
                    DrawHoverEffects(data);
                }
                
                // Draw inside legend if applicable
                if (_config.ShowLegend && _config.LegendPosition != LegendPosition.Outside && data.HasMultipleSeriesTotal)
                {
                    _cachedLegendResult = GraphLegend.DrawInsideLegend(
                        data,
                        _hiddenSeries,
                        _config.LegendPosition,
                        _config.LegendHeightPercent,
                        _insideLegendScrollOffset,
                        ToggleSeriesVisibility,
                        _config.Style);
                    _insideLegendScrollOffset = _cachedLegendResult.ScrollOffset;
                }
                else
                {
                    _cachedLegendResult = GraphLegend.InsideLegendResult.Invalid;
                }
                
                // Draw controls drawer
                if (_config.ShowControlsDrawer)
                {
                    _cachedControlsResult = GraphControls.DrawControlsDrawer(
                        _controlsDrawerOpen,
                        _config.AutoScrollEnabled,
                        _config.AutoScrollTimeValue,
                        _config.AutoScrollTimeUnit,
                        _config.AutoScrollNowPosition,
                        _config.Style);
                    
                    // Update state from result
                    _controlsDrawerOpen = _cachedControlsResult.IsOpen;
                    if (_cachedControlsResult.SettingsChanged)
                    {
                        _config.AutoScrollEnabled = _cachedControlsResult.AutoScrollEnabled;
                        _config.AutoScrollTimeValue = _cachedControlsResult.AutoScrollTimeValue;
                        _config.AutoScrollTimeUnit = _cachedControlsResult.AutoScrollTimeUnit;
                        _config.AutoScrollNowPosition = _cachedControlsResult.AutoScrollNowPosition;
                        
                        OnAutoScrollSettingsChanged?.Invoke(
                            _config.AutoScrollEnabled,
                            _config.AutoScrollTimeValue,
                            _config.AutoScrollTimeUnit,
                            _config.AutoScrollNowPosition);
                    }
                }
                else
                {
                    _cachedControlsResult = GraphControls.ControlsDrawerResult.Invalid;
                }
                
                // Draw value labels
                if (_config.ShowValueLabel)
                {
                    DrawValueLabels(data);
                }
                
                ImPlot.EndPlot();
            }
            
            ChartColors.PopChartStyle();
            
            // Draw outside legend
            if (useOutsideLegend)
            {
                ImGui.SameLine();
                GraphLegend.DrawScrollableLegend(
                    _config.PlotId,
                    data.Series,
                    _hiddenSeries,
                    _config.LegendWidth,
                    avail.Y,
                    ToggleSeriesVisibility,
                    _config.Style);
            }
        }
        catch (Exception ex)
        {
            ImGui.TextUnformatted($"Error rendering graph: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Draws a single series on the plot.
    /// </summary>
    private unsafe void DrawSeries(GraphSeriesData series, PreparedGraphData data)
    {
        var colorVec4 = new Vector4(series.Color.X, series.Color.Y, series.Color.Z, 1f);
        ImPlot.SetNextLineStyle(colorVec4, _config.Style.LineWeight);
        
        fixed (double* xPtr = series.XValues)
        fixed (double* yPtr = series.YValues)
        {
            var count = series.PointCount;
            
            switch (_config.GraphType)
            {
                case GraphType.Line:
                    ImPlot.PlotLine(series.Name, xPtr, yPtr, count);
                    break;
                    
                case GraphType.Stairs:
                    ImPlot.PlotStairs(series.Name, xPtr, yPtr, count);
                    break;
                    
                case GraphType.Bars:
                    var barWidth = data.HasMultipleSeries 
                        ? data.TotalTimeSpan / count * 0.8 / data.Series.Count(s => s.Visible)
                        : 0.67;
                    ImPlot.SetNextFillStyle(colorVec4);
                    ImPlot.PlotBars(series.Name, xPtr, yPtr, count, barWidth);
                    break;
                    
                case GraphType.Area:
                default:
                    var fillAlpha = data.HasMultipleSeries ? _config.Style.MultiSeriesFillAlpha : _config.Style.FillAlpha + 0.25f;
                    ImPlot.SetNextFillStyle(new Vector4(series.Color.X, series.Color.Y, series.Color.Z, fillAlpha));
                    ImPlot.PlotShaded($"{series.Name}##shaded", xPtr, yPtr, count, 0.0);
                    ImPlot.SetNextLineStyle(colorVec4, _config.Style.LineWeight);
                    ImPlot.PlotLine(series.Name, xPtr, yPtr, count);
                    break;
            }
        }
    }
    
    /// <summary>
    /// Draws crosshair and tooltip when hovering over the plot.
    /// </summary>
    private void DrawHoverEffects(PreparedGraphData data)
    {
        var mousePos = ImPlot.GetPlotMousePos();
        var mouseX = mousePos.X;
        var mouseY = mousePos.Y;
        
        // Find nearest point across all visible series
        string nearestSeriesName = string.Empty;
        float nearestValue = 0f;
        var nearestColor = new Vector3(1f, 1f, 1f);
        var minYDistance = double.MaxValue;
        var foundPoint = false;
        
        foreach (var series in data.Series)
        {
            if (!series.Visible || series.PointCount == 0) continue;
            
            var idx = BinarySearchNearestX(series.XValues, series.PointCount, mouseX);
            
            if (idx >= 0 && idx < series.PointCount)
            {
                var value = (float)series.YValues[idx];
                var yDistance = Math.Abs(mouseY - value);
                
                if (yDistance < minYDistance)
                {
                    minYDistance = yDistance;
                    nearestSeriesName = series.Name;
                    nearestValue = value;
                    nearestColor = series.Color;
                    foundPoint = true;
                }
            }
        }
        
        if (foundPoint)
        {
            GraphDrawing.DrawCrosshair(mouseX, mouseY, nearestValue, _config.Style);
            
            // Draw tooltip for multi-series
            if (data.HasMultipleSeries)
            {
                var screenPos = ImPlot.PlotToPixels(mouseX, mouseY);
                var lines = new List<string>();
                
                // Add time if time-based
                if (data.IsTimeBased)
                {
                    var time = data.StartTime.AddSeconds(mouseX).ToLocalTime();
                    lines.Add(time.ToString("g"));
                }
                
                // Add value for nearest series
                lines.Add($"{nearestSeriesName}: {FormatUtils.FormatAbbreviated(nearestValue)}");
                
                GraphDrawing.DrawTooltipBox(screenPos, lines.ToArray(), new Vector4(nearestColor.X, nearestColor.Y, nearestColor.Z, 1f), _config.Style);
            }
        }
    }
    
    /// <summary>
    /// Draws current value labels at the latest point of each visible series.
    /// Labels are colored to match their series and auto-adjust to prevent overlap.
    /// </summary>
    private void DrawValueLabels(PreparedGraphData data)
    {
        var seriesData = new List<(string Name, double LastX, double LastY, Vector3 Color)>();
        
        foreach (var series in data.Series)
        {
            if (!series.Visible || series.PointCount == 0) continue;
            
            var lastX = series.XValues[series.PointCount - 1];
            var lastY = series.YValues[series.PointCount - 1];
            seriesData.Add((series.Name, lastX, lastY, series.Color));
        }
        
        if (seriesData.Count > 0)
        {
            GraphDrawing.DrawCurrentValueLabels(seriesData, _config.Style);
        }
    }
    
    /// <summary>
    /// Displays the "no data" message.
    /// </summary>
    private void DrawNoDataMessage()
    {
        ImGui.PushStyleColor(ImGuiCol.Text, ChartColors.TextSecondary);
        ImGui.TextUnformatted(_config.NoDataText);
        ImGui.PopStyleColor();
    }
    
    #endregion
    
    #region Data Preparation
    
    /// <summary>
    /// Prepares index-based sample data for rendering.
    /// </summary>
    private PreparedGraphData PrepareIndexBasedData(IReadOnlyList<float> samples)
    {
        var xValues = new double[samples.Count];
        var yValues = new double[samples.Count];
        for (var i = 0; i < samples.Count; i++)
        {
            xValues[i] = i;
            yValues[i] = samples[i];
        }
        
        var isBullish = samples.Count < 2 || samples[^1] >= samples[0];
        var color = isBullish 
            ? new Vector3(ChartColors.Bullish.X, ChartColors.Bullish.Y, ChartColors.Bullish.Z)
            : new Vector3(ChartColors.Bearish.X, ChartColors.Bearish.Y, ChartColors.Bearish.Z);
        
        var series = new List<GraphSeriesData>
        {
            new()
            {
                Name = "Value",
                XValues = xValues,
                YValues = yValues,
                PointCount = samples.Count,
                Color = color,
                Visible = true
            }
        };
        
        var (yMin, yMax) = CalculateYBounds(series, 0, double.MaxValue);
        var xDataMax = (double)samples.Count;
        var xPadding = Math.Max(xDataMax * 0.05, 1.0);
        
        return new PreparedGraphData
        {
            Series = series,
            XMin = 0,
            XMax = xDataMax + xPadding,
            YMin = yMin,
            YMax = yMax,
            IsTimeBased = false,
            StartTime = DateTime.MinValue,
            TotalTimeSpan = xDataMax
        };
    }
    
    /// <summary>
    /// Prepares time-based multi-series data for rendering.
    /// </summary>
    private PreparedGraphData PrepareTimeBasedData(
        IReadOnlyList<(string name, IReadOnlyList<(DateTime ts, float value)> samples)> seriesData)
    {
        var (globalMinTime, totalTimeSpan) = CalculateTimeRange(seriesData);
        var colors = ChartColors.GetSeriesColors(seriesData.Count);
        
        var series = new List<GraphSeriesData>();
        for (var i = 0; i < seriesData.Count; i++)
        {
            var (name, samples) = seriesData[i];
            if (samples == null || samples.Count == 0) continue;
            
            var pointCount = samples.Count + 1;
            var xValues = GetOrCreatePooledArray(_pooledXArrays, name, pointCount);
            var yValues = GetOrCreatePooledArray(_pooledYArrays, name, pointCount);
            
            for (var j = 0; j < samples.Count; j++)
            {
                xValues[j] = (samples[j].ts - globalMinTime).TotalSeconds;
                yValues[j] = samples[j].value;
            }
            
            xValues[samples.Count] = totalTimeSpan;
            yValues[samples.Count] = samples[^1].value;
            
            series.Add(new GraphSeriesData
            {
                Name = name,
                XValues = xValues,
                YValues = yValues,
                PointCount = pointCount,
                Color = colors[i],
                Visible = !_hiddenSeries.Contains(name)
            });
        }
        
        var (xMin, xMax) = CalculateXLimits(totalTimeSpan);
        var (yMin, yMax) = CalculateYBounds(series, xMin, xMax);
        
        return new PreparedGraphData
        {
            Series = series,
            XMin = xMin,
            XMax = xMax,
            YMin = yMin,
            YMax = yMax,
            IsTimeBased = true,
            StartTime = globalMinTime,
            TotalTimeSpan = totalTimeSpan
        };
    }
    
    /// <summary>
    /// Prepares time-based multi-series data with custom colors.
    /// </summary>
    private PreparedGraphData PrepareTimeBasedDataWithColors(
        IReadOnlyList<(string name, IReadOnlyList<(DateTime ts, float value)> samples, Vector4? color)> seriesData)
    {
        var globalMinTime = DateTime.MaxValue;
        var globalMaxTime = DateTime.UtcNow;
        
        foreach (var (_, samples, _) in seriesData)
        {
            if (samples == null || samples.Count == 0) continue;
            if (samples[0].ts < globalMinTime) globalMinTime = samples[0].ts;
        }
        
        if (globalMinTime == DateTime.MaxValue)
            globalMinTime = DateTime.UtcNow.AddHours(-1);
        
        var totalTimeSpan = Math.Max(1, (globalMaxTime - globalMinTime).TotalSeconds);
        var defaultColors = ChartColors.GetSeriesColors(seriesData.Count);
        
        var series = new List<GraphSeriesData>();
        var defaultColorIndex = 0;
        
        for (var i = 0; i < seriesData.Count; i++)
        {
            var (name, samples, customColor) = seriesData[i];
            if (samples == null || samples.Count == 0) continue;
            
            var pointCount = samples.Count + 1;
            var xValues = GetOrCreatePooledArray(_pooledXArrays, name, pointCount);
            var yValues = GetOrCreatePooledArray(_pooledYArrays, name, pointCount);
            
            for (var j = 0; j < samples.Count; j++)
            {
                xValues[j] = (samples[j].ts - globalMinTime).TotalSeconds;
                yValues[j] = samples[j].value;
            }
            
            xValues[samples.Count] = totalTimeSpan;
            yValues[samples.Count] = samples[^1].value;
            
            var color = customColor.HasValue 
                ? new Vector3(customColor.Value.X, customColor.Value.Y, customColor.Value.Z)
                : defaultColors[defaultColorIndex++ % defaultColors.Length];
            
            series.Add(new GraphSeriesData
            {
                Name = name,
                XValues = xValues,
                YValues = yValues,
                PointCount = pointCount,
                Color = color,
                Visible = !_hiddenSeries.Contains(name)
            });
        }
        
        var (xMin, xMax) = CalculateXLimits(totalTimeSpan);
        var (yMin, yMax) = CalculateYBounds(series, xMin, xMax);
        
        return new PreparedGraphData
        {
            Series = series,
            XMin = xMin,
            XMax = xMax,
            YMin = yMin,
            YMax = yMax,
            IsTimeBased = true,
            StartTime = globalMinTime,
            TotalTimeSpan = totalTimeSpan
        };
    }
    
    #endregion
    
    #region Helper Methods
    
    private (DateTime globalMinTime, double totalTimeSpan) CalculateTimeRange(
        IReadOnlyList<(string name, IReadOnlyList<(DateTime ts, float value)> samples)> seriesData)
    {
        var globalMinTime = DateTime.MaxValue;
        var globalMaxTime = DateTime.UtcNow;
        
        foreach (var (_, samples) in seriesData)
        {
            if (samples == null || samples.Count == 0) continue;
            if (samples[0].ts < globalMinTime) globalMinTime = samples[0].ts;
        }
        
        if (globalMinTime == DateTime.MaxValue)
            globalMinTime = DateTime.UtcNow.AddHours(-1);
        
        var totalTimeSpan = Math.Max(1, (globalMaxTime - globalMinTime).TotalSeconds);
        return (globalMinTime, totalTimeSpan);
    }
    
    private (double xMin, double xMax) CalculateXLimits(double totalTimeSpan)
    {
        if (_config.AutoScrollEnabled)
        {
            var timeRangeSeconds = _config.GetAutoScrollTimeRangeSeconds();
            var nowFraction = _config.AutoScrollNowPosition / 100f;
            var leftPortion = timeRangeSeconds * nowFraction;
            var rightPortion = timeRangeSeconds * (1f - nowFraction);
            return (totalTimeSpan - leftPortion, totalTimeSpan + rightPortion);
        }
        
        return (0, totalTimeSpan + Math.Max(totalTimeSpan * 0.05, 1.0));
    }
    
    private (double yMin, double yMax) CalculateYBounds(
        IReadOnlyList<GraphSeriesData> series, 
        double xMinVisible, 
        double xMaxVisible)
    {
        var dataMin = double.MaxValue;
        var dataMax = double.MinValue;
        
        foreach (var s in series)
        {
            if (!s.Visible) continue;
            
            double? lastValueBeforeRange = null;
            
            for (var i = 0; i < s.PointCount; i++)
            {
                var x = s.XValues[i];
                var y = s.YValues[i];
                
                if (_config.AutoScrollEnabled)
                {
                    if (x < xMinVisible)
                    {
                        lastValueBeforeRange = y;
                        continue;
                    }
                    if (x > xMaxVisible) continue;
                }
                
                if (y < dataMin) dataMin = y;
                if (y > dataMax) dataMax = y;
            }
            
            if (_config.AutoScrollEnabled && lastValueBeforeRange.HasValue)
            {
                if (lastValueBeforeRange.Value < dataMin) dataMin = lastValueBeforeRange.Value;
                if (lastValueBeforeRange.Value > dataMax) dataMax = lastValueBeforeRange.Value;
            }
        }
        
        if (dataMin == double.MaxValue || dataMax == double.MinValue)
        {
            dataMin = 0;
            dataMax = 100;
        }
        
        var dataRange = dataMax - dataMin;
        if (dataRange < _config.FloatEpsilon)
        {
            dataRange = Math.Max(dataMax * 0.1, 1.0);
        }
        
        var yMin = Math.Max(0, dataMin - dataRange * 0.15);
        var yMax = dataMax + dataRange * 0.15;
        
        if (Math.Abs(yMax - yMin) < _config.FloatEpsilon)
        {
            yMax = yMin + 1;
        }
        
        return (yMin, yMax);
    }
    
    private void UpdateAutoScrollLimits(PreparedGraphData data)
    {
        var (xMin, xMax) = CalculateXLimits(data.TotalTimeSpan);
        data.XMin = xMin;
        data.XMax = xMax;
        
        var (yMin, yMax) = CalculateYBounds(data.Series.ToList(), xMin, xMax);
        data.YMin = yMin;
        data.YMax = yMax;
    }
    
    private bool NeedsPreparedDataRecompute(
        IReadOnlyList<(string name, IReadOnlyList<(DateTime ts, float value)> samples)> series)
    {
        if (_cachedPreparedData == null || _lastSeriesData == null)
            return true;
        
        if (!ReferenceEquals(series, _lastSeriesData))
            return true;
        
        var currentHiddenHash = ComputeHiddenSeriesHash();
        if (currentHiddenHash != _lastHiddenSeriesHash)
            return true;
        
        if (_config.AutoScrollEnabled != _lastAutoScrollEnabled)
            return true;
        
        return false;
    }
    
    private bool NeedsPreparedDataRecomputeWithColors(
        IReadOnlyList<(string name, IReadOnlyList<(DateTime ts, float value)> samples, Vector4? color)> series)
    {
        if (_cachedPreparedData == null || _lastSeriesDataWithColors == null)
            return true;
        
        if (!ReferenceEquals(series, _lastSeriesDataWithColors))
            return true;
        
        var currentHiddenHash = ComputeHiddenSeriesHash();
        if (currentHiddenHash != _lastHiddenSeriesHash)
            return true;
        
        if (_config.AutoScrollEnabled != _lastAutoScrollEnabled)
            return true;
        
        return false;
    }
    
    private int ComputeHiddenSeriesHash()
    {
        var hash = _hiddenSeries.Count;
        foreach (var name in _hiddenSeries)
        {
            hash = HashCode.Combine(hash, name);
        }
        return hash;
    }
    
    private static double[] GetOrCreatePooledArray(Dictionary<string, double[]> pool, string key, int requiredSize)
    {
        if (pool.TryGetValue(key, out var existing) && existing.Length >= requiredSize)
        {
            return existing;
        }
        
        var newArray = new double[requiredSize + 16];
        pool[key] = newArray;
        return newArray;
    }
    
    private static int BinarySearchNearestX(double[] xValues, int count, double target)
    {
        if (count == 0) return -1;
        
        if (target < xValues[0]) return -1;
        if (target >= xValues[count - 1]) return count - 1;
        
        var left = 0;
        var right = count - 1;
        
        while (left < right)
        {
            var mid = left + (right - left + 1) / 2;
            if (xValues[mid] <= target)
            {
                left = mid;
            }
            else
            {
                right = mid - 1;
            }
        }
        
        return left;
    }
    
    #endregion
}
