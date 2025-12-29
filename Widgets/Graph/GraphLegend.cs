using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImPlot;

namespace MTGui.Graph;

/// <summary>
/// Legend rendering utilities for ImPlot graphs.
/// Provides methods for drawing scrollable legends both inside and outside the plot area.
/// </summary>
public static class GraphLegend
{
    #region Outside Legend (Child Window)
    
    /// <summary>
    /// Draws a scrollable legend panel outside the plot area using ImGui child window.
    /// </summary>
    /// <param name="plotId">Unique identifier for the plot (used for child window ID).</param>
    /// <param name="series">The series data to display in the legend.</param>
    /// <param name="hiddenSeries">Set of hidden series names.</param>
    /// <param name="width">Width of the legend panel.</param>
    /// <param name="height">Height of the legend panel.</param>
    /// <param name="onToggleSeries">Callback when a series visibility is toggled.</param>
    /// <param name="style">Optional style configuration.</param>
    public static void DrawScrollableLegend(
        string plotId,
        IReadOnlyList<GraphSeriesData> series,
        HashSet<string> hiddenSeries,
        float width,
        float height,
        Action<string>? onToggleSeries = null,
        GraphStyleConfig? style = null)
    {
        style ??= GraphStyleConfig.Default;
        
        // Style the legend panel with trading platform colors
        ImGui.PushStyleColor(ImGuiCol.ChildBg, ChartColors.FrameBackground);
        ImGui.PushStyleColor(ImGuiCol.Border, ChartColors.AxisLine);
        ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, ChartColors.PlotBackground);
        ImGui.PushStyleColor(ImGuiCol.ScrollbarGrab, ChartColors.GridLine);
        
        if (ImGui.BeginChild($"##{plotId}_legend", new Vector2(width, height), true))
        {
            // Sort series by last value descending
            var sortedSeries = series.OrderByDescending(s => s.PointCount > 0 ? s.YValues[s.PointCount - 1] : 0).ToList();
            
            foreach (var seriesItem in sortedSeries)
            {
                var isHidden = hiddenSeries.Contains(seriesItem.Name);
                var lastValue = seriesItem.PointCount > 0 ? (float)seriesItem.YValues[seriesItem.PointCount - 1] : 0f;
                
                // Use dimmed color for hidden series
                var displayAlpha = isHidden ? style.LegendHiddenAlpha : 1f;
                
                // Draw colored square indicator (rounded for modern look)
                var drawList = ImGui.GetWindowDrawList();
                var cursorPos = ImGui.GetCursorScreenPos();
                var indicatorSize = style.LegendIndicatorSize;
                var colorU32 = ImGui.GetColorU32(new Vector4(seriesItem.Color.X, seriesItem.Color.Y, seriesItem.Color.Z, displayAlpha));
                
                if (isHidden)
                {
                    // Draw outline only for hidden series
                    drawList.AddRect(cursorPos, new Vector2(cursorPos.X + indicatorSize, cursorPos.Y + indicatorSize), colorU32, 2f);
                }
                else
                {
                    // Draw filled rounded square for visible series
                    drawList.AddRectFilled(cursorPos, new Vector2(cursorPos.X + indicatorSize, cursorPos.Y + indicatorSize), colorU32, 2f);
                }
                
                // Make the entire row clickable
                var rowStart = cursorPos;
                
                // Advance cursor past the indicator
                ImGui.Dummy(new Vector2(indicatorSize + 4f, indicatorSize));
                ImGui.SameLine();
                
                // Draw name with appropriate text color
                var textColor = isHidden ? ChartColors.TextSecondary : ChartColors.TextPrimary;
                ImGui.PushStyleColor(ImGuiCol.Text, textColor);
                ImGui.TextUnformatted(seriesItem.Name);
                ImGui.PopStyleColor();
                
                // Make row clickable - use invisible button over the row area
                ImGui.SetCursorScreenPos(rowStart);
                if (ImGui.InvisibleButton($"##legend_toggle_{seriesItem.Name}", new Vector2(width - 16f, indicatorSize + 2f)))
                {
                    onToggleSeries?.Invoke(seriesItem.Name);
                }
                
                // Show tooltip on hover with styled content
                if (ImGui.IsItemHovered())
                {
                    var statusText = isHidden ? " (hidden)" : "";
                    ImGui.SetTooltip($"{seriesItem.Name}: {FormatUtils.FormatAbbreviated(lastValue)}{statusText}\nClick to toggle visibility");
                }
            }
        }
        ImGui.EndChild();
        ImGui.PopStyleColor(4);
    }
    
    #endregion
    
    #region Inside Legend (Plot Draw List)
    
    /// <summary>
    /// Result from drawing an inside legend, containing bounds for input blocking.
    /// </summary>
    public readonly struct InsideLegendResult
    {
        /// <summary>Minimum corner of the legend bounds.</summary>
        public readonly Vector2 BoundsMin;
        
        /// <summary>Maximum corner of the legend bounds.</summary>
        public readonly Vector2 BoundsMax;
        
        /// <summary>Whether the bounds are valid.</summary>
        public readonly bool IsValid;
        
        /// <summary>Updated scroll offset for the legend.</summary>
        public readonly float ScrollOffset;
        
        public InsideLegendResult(Vector2 boundsMin, Vector2 boundsMax, float scrollOffset)
        {
            BoundsMin = boundsMin;
            BoundsMax = boundsMax;
            IsValid = true;
            ScrollOffset = scrollOffset;
        }
        
        public static InsideLegendResult Invalid => new(Vector2.Zero, Vector2.Zero, 0f);
    }
    
    /// <summary>
    /// Draws an interactive legend inside the plot area using ImPlot's draw list.
    /// </summary>
    /// <param name="data">Prepared graph data containing series.</param>
    /// <param name="hiddenSeries">Set of hidden series names.</param>
    /// <param name="position">Legend position within the plot.</param>
    /// <param name="legendHeightPercent">Maximum legend height as percentage of plot height (10-80).</param>
    /// <param name="scrollOffset">Current scroll offset (pass result back on next frame).</param>
    /// <param name="onToggleSeries">Callback when a series visibility is toggled.</param>
    /// <param name="style">Optional style configuration.</param>
    /// <returns>Result containing legend bounds and updated scroll offset.</returns>
    public static InsideLegendResult DrawInsideLegend(
        PreparedGraphData data,
        HashSet<string> hiddenSeries,
        LegendPosition position,
        float legendHeightPercent,
        float scrollOffset,
        Action<string>? onToggleSeries = null,
        GraphStyleConfig? style = null)
    {
        style ??= GraphStyleConfig.Default;
        
        var drawList = ImPlot.GetPlotDrawList();
        var plotPos = ImPlot.GetPlotPos();
        var plotSize = ImPlot.GetPlotSize();
        
        // Calculate legend dimensions
        var padding = style.LegendPadding;
        var indicatorSize = style.LegendIndicatorSize;
        var rowHeight = style.LegendRowHeight;
        var scrollbarWidth = style.LegendScrollbarWidth;
        const float indicatorTextGap = 6f;
        
        // Measure max text width and count valid series
        var maxTextWidth = 0f;
        var validSeriesCount = 0;
        foreach (var series in data.Series)
        {
            var textSize = ImGui.CalcTextSize(series.Name);
            maxTextWidth = Math.Max(maxTextWidth, textSize.X);
            validSeriesCount++;
        }
        
        if (validSeriesCount == 0) 
            return InsideLegendResult.Invalid;
        
        // Calculate content height and max display height
        var contentHeight = validSeriesCount * rowHeight;
        var maxLegendHeight = plotSize.Y * (legendHeightPercent / 100f);
        maxLegendHeight = Math.Max(maxLegendHeight, rowHeight + padding * 2);
        var needsScrolling = contentHeight > maxLegendHeight - padding * 2;
        
        var legendWidth = padding * 2 + indicatorSize + indicatorTextGap + maxTextWidth + (needsScrolling ? scrollbarWidth + 4f : 0f);
        var legendHeight = Math.Min(padding * 2 + contentHeight, maxLegendHeight);
        
        // Clamp legend dimensions to fit within plot area with margin
        const float legendMargin = 10f;
        var maxLegendWidth = plotSize.X - legendMargin * 2;
        legendWidth = Math.Min(legendWidth, Math.Max(50f, maxLegendWidth)); // Minimum 50px width
        
        // Determine legend position and clamp to stay within plot area
        Vector2 legendPos = position switch
        {
            LegendPosition.InsideTopRight => new Vector2(plotPos.X + plotSize.X - legendWidth - legendMargin, plotPos.Y + legendMargin),
            LegendPosition.InsideBottomLeft => new Vector2(plotPos.X + legendMargin, plotPos.Y + plotSize.Y - legendHeight - legendMargin),
            LegendPosition.InsideBottomRight => new Vector2(plotPos.X + plotSize.X - legendWidth - legendMargin, plotPos.Y + plotSize.Y - legendHeight - legendMargin),
            _ => new Vector2(plotPos.X + legendMargin, plotPos.Y + legendMargin) // InsideTopLeft default
        };
        
        // Ensure legend stays within plot bounds
        legendPos.X = Math.Clamp(legendPos.X, plotPos.X + legendMargin, plotPos.X + plotSize.X - legendWidth - legendMargin);
        legendPos.Y = Math.Clamp(legendPos.Y, plotPos.Y + legendMargin, plotPos.Y + plotSize.Y - legendHeight - legendMargin);
        
        // Clip all legend drawing to the plot area to prevent overlap with axes
        drawList.PushClipRect(plotPos, new Vector2(plotPos.X + plotSize.X, plotPos.Y + plotSize.Y), true);
        
        // Draw legend background
        var bgColor = ImGui.GetColorU32(new Vector4(ChartColors.FrameBackground.X, ChartColors.FrameBackground.Y, ChartColors.FrameBackground.Z, 0.85f));
        var borderColor = ImGui.GetColorU32(ChartColors.AxisLine);
        drawList.AddRectFilled(legendPos, new Vector2(legendPos.X + legendWidth, legendPos.Y + legendHeight), bgColor, style.LegendRounding);
        drawList.AddRect(legendPos, new Vector2(legendPos.X + legendWidth, legendPos.Y + legendHeight), borderColor, style.LegendRounding);
        
        // Track mouse interactions
        var mousePos = ImGui.GetMousePos();
        var mouseInLegend = mousePos.X >= legendPos.X && mousePos.X <= legendPos.X + legendWidth &&
                           mousePos.Y >= legendPos.Y && mousePos.Y <= legendPos.Y + legendHeight;
        
        // Handle mouse wheel scrolling
        if (mouseInLegend && needsScrolling)
        {
            var wheel = ImGui.GetIO().MouseWheel;
            if (wheel != 0)
            {
                scrollOffset -= wheel * rowHeight * 2f;
            }
        }
        
        // Clamp scroll offset
        var maxScrollOffset = Math.Max(0f, contentHeight - (legendHeight - padding * 2));
        scrollOffset = Math.Clamp(scrollOffset, 0f, maxScrollOffset);
        
        // Sort series by value descending
        var sortedSeries = data.Series.OrderByDescending(s => s.PointCount > 0 ? s.YValues[s.PointCount - 1] : 0).ToList();
        
        // Calculate visible area
        var contentAreaTop = legendPos.Y + padding;
        var contentAreaBottom = legendPos.Y + legendHeight - padding;
        var contentAreaRight = legendPos.X + legendWidth - padding - (needsScrolling ? scrollbarWidth + 4f : 0f);
        
        // Draw each legend entry
        var yOffset = contentAreaTop - scrollOffset;
        foreach (var series in sortedSeries)
        {
            var rowTop = yOffset;
            var rowBottom = yOffset + rowHeight;
            
            // Skip rows outside visible area
            if (rowBottom < contentAreaTop || rowTop > contentAreaBottom)
            {
                yOffset += rowHeight;
                continue;
            }
            
            var isHidden = hiddenSeries.Contains(series.Name);
            var displayAlpha = isHidden ? style.LegendHiddenAlpha : 1f;
            
            // Check if mouse is over this row (excluding scrollbar area)
            var mouseInRow = mouseInLegend && 
                            mousePos.X <= contentAreaRight &&
                            mousePos.Y >= Math.Max(rowTop, contentAreaTop) && 
                            mousePos.Y < Math.Min(rowBottom, contentAreaBottom) &&
                            rowTop >= contentAreaTop && rowBottom <= contentAreaBottom;
            
            // Handle click to toggle visibility
            if (mouseInRow && ImGui.IsMouseClicked(0))
            {
                onToggleSeries?.Invoke(series.Name);
            }
            
            // Highlight row on hover
            if (mouseInRow)
            {
                var hoverColor = ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.1f));
                drawList.AddRectFilled(
                    new Vector2(legendPos.X + 2, Math.Max(rowTop, contentAreaTop)), 
                    new Vector2(contentAreaRight, Math.Min(rowBottom, contentAreaBottom)), 
                    hoverColor, 2f);
            }
            
            // Draw content if visible
            if (rowTop >= contentAreaTop - rowHeight && rowBottom <= contentAreaBottom + rowHeight)
            {
                var indicatorY = yOffset + (rowHeight - indicatorSize) / 2;
                if (indicatorY >= contentAreaTop - indicatorSize && indicatorY + indicatorSize <= contentAreaBottom + indicatorSize)
                {
                    var indicatorPos = new Vector2(legendPos.X + padding, indicatorY);
                    var colorU32 = ImGui.GetColorU32(new Vector4(series.Color.X, series.Color.Y, series.Color.Z, displayAlpha));
                    
                    if (isHidden)
                    {
                        drawList.AddRect(indicatorPos, new Vector2(indicatorPos.X + indicatorSize, indicatorPos.Y + indicatorSize), colorU32, 2f);
                    }
                    else
                    {
                        drawList.AddRectFilled(indicatorPos, new Vector2(indicatorPos.X + indicatorSize, indicatorPos.Y + indicatorSize), colorU32, 2f);
                    }
                    
                    // Draw series name
                    var textColor = isHidden ? ChartColors.TextSecondary : ChartColors.TextPrimary;
                    var textY = yOffset + (rowHeight - ImGui.GetTextLineHeight()) / 2;
                    if (textY >= contentAreaTop - rowHeight && textY <= contentAreaBottom)
                    {
                        var textPos = new Vector2(indicatorPos.X + indicatorSize + indicatorTextGap, textY);
                        drawList.AddText(textPos, ImGui.GetColorU32(textColor), series.Name);
                    }
                }
            }
            
            yOffset += rowHeight;
        }
        
        // Draw scrollbar if needed
        if (needsScrolling)
        {
            scrollOffset = DrawScrollbar(
                drawList,
                legendPos.X + legendWidth - padding - scrollbarWidth,
                contentAreaTop,
                contentAreaBottom,
                scrollbarWidth,
                contentHeight,
                legendHeight - padding * 2,
                scrollOffset,
                maxScrollOffset,
                style);
        }
        
        // Show tooltip (only when not over scrollbar)
        var scrollbarX = legendPos.X + legendWidth - padding - scrollbarWidth;
        var mouseOverScrollbar = needsScrolling && mousePos.X >= scrollbarX && mousePos.X <= legendPos.X + legendWidth;
        if (mouseInLegend && !mouseOverScrollbar)
        {
            ShowLegendTooltip(sortedSeries, hiddenSeries, mousePos, contentAreaTop, scrollOffset, rowHeight, needsScrolling);
        }
        
        // Pop the clip rect we pushed earlier
        drawList.PopClipRect();
        
        return new InsideLegendResult(legendPos, new Vector2(legendPos.X + legendWidth, legendPos.Y + legendHeight), scrollOffset);
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Draws a scrollbar for the inside legend and handles mouse interaction.
    /// </summary>
    /// <returns>The updated scroll offset if the user is interacting with the scrollbar.</returns>
    private static float DrawScrollbar(
        ImDrawListPtr drawList,
        float x,
        float trackTop,
        float trackBottom,
        float width,
        float contentHeight,
        float visibleHeight,
        float scrollOffset,
        float maxScrollOffset,
        GraphStyleConfig style)
    {
        var trackHeight = trackBottom - trackTop;
        
        // Track background
        var trackColor = ImGui.GetColorU32(new Vector4(0.2f, 0.2f, 0.2f, 0.5f));
        drawList.AddRectFilled(
            new Vector2(x, trackTop),
            new Vector2(x + width, trackBottom),
            trackColor, 3f);
        
        // Thumb calculations
        var visibleRatio = visibleHeight / contentHeight;
        var thumbHeight = Math.Max(20f, trackHeight * visibleRatio);
        var scrollRatio = maxScrollOffset > 0 ? scrollOffset / maxScrollOffset : 0f;
        var thumbTop = trackTop + scrollRatio * (trackHeight - thumbHeight);
        
        // Check if mouse is over the scrollbar track
        var mousePos = ImGui.GetMousePos();
        var mouseOverTrack = mousePos.X >= x && mousePos.X <= x + width &&
                            mousePos.Y >= trackTop && mousePos.Y <= trackBottom;
        var mouseOverThumb = mousePos.X >= x && mousePos.X <= x + width &&
                            mousePos.Y >= thumbTop && mousePos.Y <= thumbTop + thumbHeight;
        
        // Handle scrollbar click/drag
        if (mouseOverTrack && ImGui.IsMouseDown(0))
        {
            // Calculate new scroll position based on mouse Y
            // Map mouse Y to scroll offset (click on track jumps to that position)
            var clickableTrackHeight = trackHeight - thumbHeight;
            if (clickableTrackHeight > 0)
            {
                // Center the thumb on the mouse position
                var targetThumbTop = mousePos.Y - thumbHeight / 2f;
                targetThumbTop = Math.Clamp(targetThumbTop, trackTop, trackTop + clickableTrackHeight);
                var newScrollRatio = (targetThumbTop - trackTop) / clickableTrackHeight;
                scrollOffset = newScrollRatio * maxScrollOffset;
            }
        }
        
        // Draw thumb with hover/active highlighting
        var thumbColor = mouseOverThumb || (mouseOverTrack && ImGui.IsMouseDown(0))
            ? ImGui.GetColorU32(ChartColors.TextSecondary)  // Brighter when hovered/active
            : ImGui.GetColorU32(ChartColors.GridLine);
        
        // Recalculate thumb position with potentially updated scroll offset
        scrollRatio = maxScrollOffset > 0 ? scrollOffset / maxScrollOffset : 0f;
        thumbTop = trackTop + scrollRatio * (trackHeight - thumbHeight);
        
        drawList.AddRectFilled(
            new Vector2(x, thumbTop),
            new Vector2(x + width, thumbTop + thumbHeight),
            thumbColor, 3f);
        
        return scrollOffset;
    }
    
    /// <summary>
    /// Shows tooltip for the hovered legend entry.
    /// </summary>
    private static void ShowLegendTooltip(
        IReadOnlyList<GraphSeriesData> sortedSeries,
        HashSet<string> hiddenSeries,
        Vector2 mousePos,
        float contentAreaTop,
        float scrollOffset,
        float rowHeight,
        bool needsScrolling)
    {
        var relativeY = mousePos.Y - contentAreaTop + scrollOffset;
        var hoveredIdx = (int)(relativeY / rowHeight);
        if (hoveredIdx >= 0 && hoveredIdx < sortedSeries.Count)
        {
            var series = sortedSeries[hoveredIdx];
            var isHidden = hiddenSeries.Contains(series.Name);
            var lastValue = series.PointCount > 0 ? (float)series.YValues[series.PointCount - 1] : 0f;
            var statusText = isHidden ? " (hidden)" : "";
            var scrollHint = needsScrolling ? "\nScroll to see more" : "";
            ImGui.SetTooltip($"{series.Name}: {FormatUtils.FormatAbbreviated(lastValue)}{statusText}\nClick to toggle visibility{scrollHint}");
        }
    }
    
    /// <summary>
    /// Checks if the mouse is within the legend bounds.
    /// </summary>
    /// <param name="result">The legend result from the previous draw call.</param>
    /// <returns>True if mouse is over the legend.</returns>
    public static bool IsMouseOverLegend(InsideLegendResult result)
    {
        if (!result.IsValid) return false;
        var mousePos = ImGui.GetMousePos();
        return mousePos.X >= result.BoundsMin.X && mousePos.X <= result.BoundsMax.X &&
               mousePos.Y >= result.BoundsMin.Y && mousePos.Y <= result.BoundsMax.Y;
    }
    
    #endregion
}
