using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImPlot;

namespace MTGui.Graph;

/// <summary>
/// Static utilities for drawing tooltips on ImPlot graphs.
/// </summary>
public static class GraphTooltips
{
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
}
