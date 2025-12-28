using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImPlot;

namespace MTGui.Graph;

/// <summary>
/// Static drawing utilities for ImPlot graphs.
/// Provides methods for drawing crosshairs, price lines, tooltips, and axis formatters.
/// </summary>
public static class GraphDrawing
{
    #region Axis Formatters
    
    /// <summary>
    /// Formatter delegate for X-axis tick labels with time values.
    /// Shows only time (HH:mm) if all visible labels are on the same day, otherwise date+time (M/d HH:mm).
    /// </summary>
    /// <remarks>
    /// The userData pointer should contain the start time ticks (long cast to void*).
    /// </remarks>
    public static readonly unsafe ImPlotFormatter XAxisTimeFormatter = (double value, byte* buff, int size, void* userData) =>
    {
        // value is seconds from the start time, userData contains the start time ticks
        var startTicks = (long)userData;
        var startTime = new DateTime(startTicks);
        var time = startTime.AddSeconds(value).ToLocalTime();
        
        // Check if the visible X-axis range spans a single day
        var plotLimits = ImPlot.GetPlotLimits();
        var visibleMinTime = startTime.AddSeconds(plotLimits.X.Min).ToLocalTime();
        var visibleMaxTime = startTime.AddSeconds(plotLimits.X.Max).ToLocalTime();
        var isSameDay = visibleMinTime.Date == visibleMaxTime.Date;
        
        // If all visible labels are on the same day, show only time; otherwise show date+time
        var format = isSameDay ? "HH:mm" : "M/d HH:mm";
        var formatted = time.ToString(format);
        var len = Math.Min(formatted.Length, size - 1);
        for (var i = 0; i < len; i++)
            buff[i] = (byte)formatted[i];
        buff[len] = 0;
        return len;
    };
    
    /// <summary>
    /// Formatter delegate for Y-axis tick labels with abbreviated notation (K, M, B).
    /// </summary>
    public static readonly unsafe ImPlotFormatter YAxisFormatter = (double value, byte* buff, int size, void* userData) =>
    {
        var formatted = FormatUtils.FormatAbbreviated(value);
        var len = Math.Min(formatted.Length, size - 1);
        for (var i = 0; i < len; i++)
            buff[i] = (byte)formatted[i];
        buff[len] = 0;
        return len;
    };
    
    #endregion
    
    #region Crosshair Drawing
    
    /// <summary>
    /// Draws crosshair lines at the current mouse position.
    /// </summary>
    /// <param name="mouseX">Mouse X position in plot coordinates.</param>
    /// <param name="mouseY">Mouse Y position in plot coordinates.</param>
    /// <param name="valueAtMouse">The Y value to display in the label.</param>
    /// <param name="style">Optional style configuration.</param>
    public static void DrawCrosshair(double mouseX, double mouseY, float valueAtMouse, GraphStyleConfig? style = null)
    {
        style ??= GraphStyleConfig.Default;
        
        var drawList = ImPlot.GetPlotDrawList();
        var plotLimits = ImPlot.GetPlotLimits();
        var plotPos = ImPlot.GetPlotPos();
        var plotSize = ImPlot.GetPlotSize();
        
        var colorU32 = ImGui.GetColorU32(ChartColors.Crosshair);
        
        // Vertical line
        var vTop = ImPlot.PlotToPixels(mouseX, plotLimits.Y.Max);
        var vBottom = ImPlot.PlotToPixels(mouseX, plotLimits.Y.Min);
        
        // Draw dashed vertical line
        var dashLength = style.CrosshairDashLength;
        var gapLength = style.CrosshairGapLength;
        var y = vTop.Y;
        while (y < vBottom.Y)
        {
            var endY = Math.Min(y + dashLength, vBottom.Y);
            drawList.AddLine(new Vector2(vTop.X, y), new Vector2(vTop.X, endY), colorU32, style.CrosshairThickness);
            y += dashLength + gapLength;
        }
        
        // Horizontal line
        var hLeft = ImPlot.PlotToPixels(plotLimits.X.Min, mouseY);
        var hRight = ImPlot.PlotToPixels(plotLimits.X.Max, mouseY);
        
        // Draw dashed horizontal line
        var x = hLeft.X;
        while (x < hRight.X)
        {
            var endX = Math.Min(x + dashLength, hRight.X);
            drawList.AddLine(new Vector2(x, hLeft.Y), new Vector2(endX, hLeft.Y), colorU32, style.CrosshairThickness);
            x += dashLength + gapLength;
        }
        
        // Draw value label on Y axis (clipped to plot area so it goes under the Y-axis)
        var valueLabel = FormatUtils.FormatAbbreviated(valueAtMouse);
        var labelSize = ImGui.CalcTextSize(valueLabel);
        var labelPos = new Vector2(hRight.X - labelSize.X - 6, hRight.Y - labelSize.Y / 2);
        
        // Background box - clipped to plot area
        var bgPadding = 3f;
        drawList.PushClipRect(plotPos, new Vector2(plotPos.X + plotSize.X, plotPos.Y + plotSize.Y), true);
        drawList.AddRectFilled(
            new Vector2(labelPos.X - bgPadding, labelPos.Y - bgPadding),
            new Vector2(labelPos.X + labelSize.X + bgPadding, labelPos.Y + labelSize.Y + bgPadding),
            ImGui.GetColorU32(ChartColors.TooltipBackground), 2f);
        drawList.AddRect(
            new Vector2(labelPos.X - bgPadding, labelPos.Y - bgPadding),
            new Vector2(labelPos.X + labelSize.X + bgPadding, labelPos.Y + labelSize.Y + bgPadding),
            ImGui.GetColorU32(ChartColors.TooltipBorder), 2f);
        
        drawList.AddText(labelPos, ImGui.GetColorU32(ChartColors.TextPrimary), valueLabel);
        drawList.PopClipRect();
    }
    
    #endregion
    
    #region Price Line Drawing
    
    /// <summary>
    /// Draws a horizontal price level line with label (trading platform style).
    /// </summary>
    /// <param name="yValue">The Y value (in plot coordinates) where the line should be drawn.</param>
    /// <param name="label">Optional label text to display on the line.</param>
    /// <param name="color">Line and label background color.</param>
    /// <param name="thickness">Line thickness. Default: 1</param>
    /// <param name="dashed">Whether to draw a dashed line. Default: false</param>
    /// <param name="style">Optional style configuration.</param>
    public static void DrawPriceLine(double yValue, string label, Vector4 color, float thickness = 1f, bool dashed = false, GraphStyleConfig? style = null)
    {
        style ??= GraphStyleConfig.Default;
        
        var drawList = ImPlot.GetPlotDrawList();
        var plotLimits = ImPlot.GetPlotLimits();
        
        // Get pixel positions
        var p1 = ImPlot.PlotToPixels(plotLimits.X.Min, yValue);
        var p2 = ImPlot.PlotToPixels(plotLimits.X.Max, yValue);
        
        var colorU32 = ImGui.GetColorU32(color);
        
        if (dashed)
        {
            // Draw dashed line
            var dashLength = style.PriceLineDashLength;
            var gapLength = style.PriceLineGapLength;
            var x = p1.X;
            while (x < p2.X)
            {
                var endX = Math.Min(x + dashLength, p2.X);
                drawList.AddLine(new Vector2(x, p1.Y), new Vector2(endX, p1.Y), colorU32, thickness);
                x += dashLength + gapLength;
            }
        }
        else
        {
            drawList.AddLine(p1, p2, colorU32, thickness);
        }
        
        // Draw price label on the right (clipped to plot area so it goes under the Y-axis)
        if (!string.IsNullOrEmpty(label))
        {
            var plotPos = ImPlot.GetPlotPos();
            var plotSize = ImPlot.GetPlotSize();
            var labelSize = ImGui.CalcTextSize(label);
            var labelPos = new Vector2(p2.X - labelSize.X - 4, p2.Y - labelSize.Y / 2);
            
            // Background for label
            var bgMin = new Vector2(labelPos.X - 4, labelPos.Y - 2);
            var bgMax = new Vector2(p2.X, labelPos.Y + labelSize.Y + 2);
            
            // Clip to plot area so label goes under the axis
            drawList.PushClipRect(plotPos, new Vector2(plotPos.X + plotSize.X, plotPos.Y + plotSize.Y), true);
            drawList.AddRectFilled(bgMin, bgMax, ImGui.GetColorU32(new Vector4(color.X, color.Y, color.Z, 0.85f)), 2f);
            drawList.AddText(labelPos, ImGui.GetColorU32(new Vector4(0.1f, 0.1f, 0.1f, 1f)), label);
            drawList.PopClipRect();
        }
    }
    
    /// <summary>
    /// Draws a current price line using the default style.
    /// </summary>
    /// <param name="yValue">The Y value (in plot coordinates) where the line should be drawn.</param>
    /// <param name="style">Optional style configuration.</param>
    public static void DrawCurrentPriceLine(double yValue, GraphStyleConfig? style = null)
    {
        style ??= GraphStyleConfig.Default;
        var label = FormatUtils.FormatAbbreviated(yValue);
        DrawPriceLine(yValue, label, ChartColors.CurrentPriceLine, style.PriceLineThickness, dashed: true, style);
    }
    
    #endregion
    
    #region Tooltip Drawing
    
    /// <summary>
    /// Draws a styled tooltip box at the given position.
    /// </summary>
    /// <param name="screenPos">Screen position for the tooltip (typically mouse position).</param>
    /// <param name="lines">Array of text lines to display.</param>
    /// <param name="accentColor">Color for the accent bar on the left side of the tooltip.</param>
    /// <param name="style">Optional style configuration.</param>
    public static void DrawTooltipBox(Vector2 screenPos, string[] lines, Vector4 accentColor, GraphStyleConfig? style = null)
    {
        style ??= GraphStyleConfig.Default;
        
        var drawList = ImPlot.GetPlotDrawList();
        
        // Calculate box size
        var maxWidth = 0f;
        var totalHeight = 0f;
        foreach (var line in lines)
        {
            var size = ImGui.CalcTextSize(line);
            maxWidth = Math.Max(maxWidth, size.X);
            totalHeight += size.Y + 2f;
        }
        
        var padding = style.TooltipPadding;
        var boxWidth = maxWidth + padding * 2 + style.TooltipAccentWidth + 1; // +1 for accent bar spacing
        var boxHeight = totalHeight + padding * 2 - 2f;
        
        // Offset to not overlap cursor
        var boxPos = new Vector2(screenPos.X + style.TooltipOffsetX, screenPos.Y - boxHeight / 2);
        
        // Background
        drawList.AddRectFilled(
            boxPos,
            new Vector2(boxPos.X + boxWidth, boxPos.Y + boxHeight),
            ImGui.GetColorU32(ChartColors.TooltipBackground), style.TooltipRounding);
        
        // Border
        drawList.AddRect(
            boxPos,
            new Vector2(boxPos.X + boxWidth, boxPos.Y + boxHeight),
            ImGui.GetColorU32(ChartColors.TooltipBorder), style.TooltipRounding, 0, 1f);
        
        // Accent bar on left
        drawList.AddRectFilled(
            new Vector2(boxPos.X, boxPos.Y),
            new Vector2(boxPos.X + style.TooltipAccentWidth, boxPos.Y + boxHeight),
            ImGui.GetColorU32(accentColor), style.TooltipRounding);
        
        // Text
        var textY = boxPos.Y + padding;
        foreach (var line in lines)
        {
            drawList.AddText(
                new Vector2(boxPos.X + padding + style.TooltipAccentWidth + 1, textY), 
                ImGui.GetColorU32(ChartColors.TextPrimary), 
                line);
            textY += ImGui.CalcTextSize(line).Y + 2f;
        }
    }
    
    /// <summary>
    /// Draws a simple single-line tooltip.
    /// </summary>
    /// <param name="screenPos">Screen position for the tooltip.</param>
    /// <param name="text">Text to display.</param>
    /// <param name="accentColor">Color for the accent bar.</param>
    /// <param name="style">Optional style configuration.</param>
    public static void DrawTooltip(Vector2 screenPos, string text, Vector4 accentColor, GraphStyleConfig? style = null)
    {
        DrawTooltipBox(screenPos, new[] { text }, accentColor, style);
    }
    
    #endregion
    
    #region Value Label Drawing
    
    /// <summary>
    /// Draws a value label at the end of a series line.
    /// </summary>
    /// <param name="plotX">X position in plot coordinates.</param>
    /// <param name="plotY">Y position in plot coordinates.</param>
    /// <param name="value">The value to display.</param>
    /// <param name="color">The series color (RGB).</param>
    /// <param name="offsetX">Horizontal offset from the point. Default: 0</param>
    /// <param name="offsetY">Vertical offset from the point. Default: 0</param>
    /// <param name="style">Optional style configuration.</param>
    public static void DrawValueLabel(double plotX, double plotY, float value, Vector3 color, float offsetX = 0f, float offsetY = 0f, GraphStyleConfig? style = null)
    {
        style ??= GraphStyleConfig.Default;
        
        var drawList = ImPlot.GetPlotDrawList();
        var pixelPos = ImPlot.PlotToPixels(plotX, plotY);
        
        // Apply offset
        var labelPos = new Vector2(pixelPos.X + offsetX, pixelPos.Y + offsetY);
        
        var label = FormatUtils.FormatAbbreviated(value);
        var labelSize = ImGui.CalcTextSize(label);
        
        var padding = style.ValueLabelPadding;
        var bgMin = new Vector2(labelPos.X - padding, labelPos.Y - labelSize.Y / 2 - padding);
        var bgMax = new Vector2(labelPos.X + labelSize.X + padding, labelPos.Y + labelSize.Y / 2 + padding);
        
        // Draw connecting line from point to label
        var lineColor = ImGui.GetColorU32(new Vector4(color.X, color.Y, color.Z, 0.6f));
        drawList.AddLine(pixelPos, new Vector2(labelPos.X, labelPos.Y), lineColor, style.ValueLabelLineThickness);
        
        // Background
        var bgColor = new Vector4(color.X * 0.3f, color.Y * 0.3f, color.Z * 0.3f, 0.9f);
        drawList.AddRectFilled(bgMin, bgMax, ImGui.GetColorU32(bgColor), style.ValueLabelRounding);
        
        // Border with series color
        var borderColor = new Vector4(color.X, color.Y, color.Z, 0.8f);
        drawList.AddRect(bgMin, bgMax, ImGui.GetColorU32(borderColor), style.ValueLabelRounding);
        
        // Text
        var textPos = new Vector2(labelPos.X, labelPos.Y - labelSize.Y / 2);
        drawList.AddText(textPos, ImGui.GetColorU32(ChartColors.TextPrimary), label);
    }
    
    /// <summary>
    /// Draws multiple value labels with automatic vertical stacking to avoid overlap.
    /// </summary>
    /// <param name="labels">List of (name, value, color) tuples to draw.</param>
    /// <param name="plotX">X position in plot coordinates (typically the rightmost point).</param>
    /// <param name="baseOffsetX">Base horizontal offset. Default: 8</param>
    /// <param name="style">Optional style configuration.</param>
    public static void DrawValueLabels(IReadOnlyList<(string Name, float Value, Vector3 Color)> labels, double plotX, float baseOffsetX = 8f, GraphStyleConfig? style = null)
    {
        style ??= GraphStyleConfig.Default;
        
        if (labels.Count == 0)
            return;
        
        var drawList = ImPlot.GetPlotDrawList();
        var plotPos = ImPlot.GetPlotPos();
        var plotSize = ImPlot.GetPlotSize();
        var lineHeight = ImGui.GetTextLineHeight();
        var rowHeight = lineHeight + style.ValueLabelPadding * 2 + 2f;
        
        // Sort by value descending for visual stacking
        var sorted = labels.OrderByDescending(l => l.Value).ToList();
        
        // Clip all value label drawing to the plot area to prevent overlap with Y-axis
        drawList.PushClipRect(plotPos, new Vector2(plotPos.X + plotSize.X, plotPos.Y + plotSize.Y), true);
        
        for (var i = 0; i < sorted.Count; i++)
        {
            var (name, value, color) = sorted[i];
            var pixelPos = ImPlot.PlotToPixels(plotX, value);
            
            // Stack labels vertically
            var labelY = pixelPos.Y + (i - sorted.Count / 2f) * rowHeight;
            
            var label = $"{name}: {FormatUtils.FormatAbbreviated(value)}";
            var labelSize = ImGui.CalcTextSize(label);
            var padding = style.ValueLabelPadding;
            
            // Calculate label position - offset to the LEFT of the data point to stay within plot area
            // Position the label so it ends at the right edge of the plot area with some margin
            var rightMargin = 8f;
            var maxLabelX = plotPos.X + plotSize.X - rightMargin;
            var labelPosX = Math.Min(pixelPos.X - labelSize.X - padding * 2 - baseOffsetX, maxLabelX - labelSize.X - padding * 2);
            
            // Ensure label doesn't go off the left side either
            labelPosX = Math.Max(labelPosX, plotPos.X + rightMargin);
            
            var labelPos = new Vector2(labelPosX + padding, labelY);
            
            var bgMin = new Vector2(labelPos.X - padding, labelPos.Y - labelSize.Y / 2 - padding);
            var bgMax = new Vector2(labelPos.X + labelSize.X + padding, labelPos.Y + labelSize.Y / 2 + padding);
            
            // Draw connecting line from data point to label
            var lineColor = ImGui.GetColorU32(new Vector4(color.X, color.Y, color.Z, 0.5f));
            var lineEndX = labelPos.X + labelSize.X + padding;
            drawList.AddLine(pixelPos, new Vector2(lineEndX, labelY), lineColor, style.ValueLabelLineThickness);
            
            // Background
            var bgColor = new Vector4(color.X * 0.2f, color.Y * 0.2f, color.Z * 0.2f, 0.9f);
            drawList.AddRectFilled(bgMin, bgMax, ImGui.GetColorU32(bgColor), style.ValueLabelRounding);
            
            // Left accent bar with series color
            var accentMin = new Vector2(bgMin.X, bgMin.Y);
            var accentMax = new Vector2(bgMin.X + 3f, bgMax.Y);
            drawList.AddRectFilled(accentMin, accentMax, ImGui.GetColorU32(new Vector4(color.X, color.Y, color.Z, 1f)), style.ValueLabelRounding);
            
            // Text
            var textPos = new Vector2(labelPos.X + 2f, labelPos.Y - labelSize.Y / 2);
            drawList.AddText(textPos, ImGui.GetColorU32(ChartColors.TextPrimary), label);
        }
        
        // Pop the clip rect we pushed earlier
        drawList.PopClipRect();
    }
    
    #endregion
}
