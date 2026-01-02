using Dalamud.Bindings.ImGui;
using MTGui.Common;
using MTGui.Graph;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace MTGui.Table;

/// <summary>
/// Static helper methods for table rendering that can be used by any table implementation.
/// These provide common functionality like aligned cell rendering, standard table flags, and color options.
/// </summary>
public static class MTTableHelpers
{
    /// <summary>
    /// Default color used for color pickers when no custom color is set.
    /// </summary>
    public static readonly Vector4 DefaultColor = new(0.3f, 0.3f, 0.3f, 0.5f);
    
    /// <summary>
    /// Gets the standard table flags used across the application.
    /// </summary>
    /// <param name="sortable">Whether the table should be sortable.</param>
    /// <param name="scrollable">Whether the table should have vertical scrolling.</param>
    /// <returns>Combined ImGuiTableFlags.</returns>
    public static ImGuiTableFlags GetStandardTableFlags(bool sortable = false, bool scrollable = true)
    {
        var flags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable;
        if (scrollable) flags |= ImGuiTableFlags.ScrollY;
        if (sortable) flags |= ImGuiTableFlags.Sortable;
        return flags;
    }
    
    /// <summary>
    /// Draws text in a table cell with the specified alignment.
    /// </summary>
    /// <param name="text">The text to draw.</param>
    /// <param name="hAlign">Horizontal alignment.</param>
    /// <param name="vAlign">Vertical alignment.</param>
    /// <param name="color">Optional text color.</param>
    public static void DrawAlignedCellText(
        string text,
        MTTableHorizontalAlignment hAlign,
        MTTableVerticalAlignment vAlign,
        Vector4? color = null)
    {
        var textSize = ImGui.CalcTextSize(text);
        var cellSize = ImGui.GetContentRegionAvail();
        var style = ImGui.GetStyle();
        
        float offsetX = hAlign switch
        {
            MTTableHorizontalAlignment.Center => (cellSize.X - textSize.X) * 0.5f,
            MTTableHorizontalAlignment.Right => cellSize.X - textSize.X,
            _ => 0f
        };
        
        float offsetY = vAlign switch
        {
            MTTableVerticalAlignment.Center => (style.CellPadding.Y * 2 + textSize.Y - textSize.Y) * 0.5f - style.CellPadding.Y,
            MTTableVerticalAlignment.Bottom => style.CellPadding.Y,
            _ => 0f
        };
        
        if (offsetX > 0f || offsetY != 0f)
        {
            var cursorPos = ImGui.GetCursorPos();
            ImGui.SetCursorPos(new Vector2(cursorPos.X + Math.Max(0f, offsetX), cursorPos.Y + offsetY));
        }
        
        if (color.HasValue)
        {
            ImGui.TextColored(color.Value, text);
        }
        else
        {
            ImGui.TextUnformatted(text);
        }
    }
    
    /// <summary>
    /// Draws a header cell with alignment and optional custom color.
    /// </summary>
    /// <param name="label">The header label.</param>
    /// <param name="hAlign">Horizontal alignment.</param>
    /// <param name="vAlign">Vertical alignment.</param>
    /// <param name="sortable">Whether the header should support sorting.</param>
    /// <param name="color">Optional text color.</param>
    public static void DrawAlignedHeaderCell(
        string label,
        MTTableHorizontalAlignment hAlign,
        MTTableVerticalAlignment vAlign,
        bool sortable = false,
        Vector4? color = null)
    {
        var textSize = ImGui.CalcTextSize(label);
        var cellWidth = ImGui.GetContentRegionAvail().X;
        var style = ImGui.GetStyle();
        
        // Calculate horizontal offset - GetContentRegionAvail gives actual usable space
        float offsetX = hAlign switch
        {
            MTTableHorizontalAlignment.Center => (cellWidth - textSize.X) * 0.5f,
            MTTableHorizontalAlignment.Right => cellWidth - textSize.X,
            _ => 0f
        };
        
        float offsetY = vAlign switch
        {
            MTTableVerticalAlignment.Center => (style.CellPadding.Y * 2 + textSize.Y - textSize.Y) * 0.5f - style.CellPadding.Y,
            MTTableVerticalAlignment.Bottom => style.CellPadding.Y,
            _ => 0f
        };
        
        // Store original cursor position for text rendering
        var startCursorPos = ImGui.GetCursorPos();
        
        // Render empty TableHeader to get sort arrow and click handling
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
        ImGui.TableHeader(string.Empty);
        ImGui.PopStyleVar();
        
        // Go back and render aligned text
        ImGui.SameLine();
        var afterHeaderCursor = ImGui.GetCursorPos();
        ImGui.SetCursorPos(new Vector2(
            startCursorPos.X + Math.Max(0f, offsetX),
            startCursorPos.Y + offsetY));
        
        if (color.HasValue)
        {
            ImGui.TextColored(color.Value, label);
        }
        else
        {
            ImGui.TextUnformatted(label);
        }
        
        // Restore cursor to after header for proper layout
        ImGui.SetCursorPos(afterHeaderCursor);
    }
    
    /// <summary>
    /// Applies alternating row background color.
    /// </summary>
    /// <param name="rowIndex">The current row index.</param>
    /// <param name="evenRowColor">Color for even rows (optional).</param>
    /// <param name="oddRowColor">Color for odd rows (optional).</param>
    public static void ApplyRowColor(int rowIndex, Vector4? evenRowColor, Vector4? oddRowColor)
    {
        var isEven = rowIndex % 2 == 0;
        if (isEven && evenRowColor.HasValue)
        {
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(evenRowColor.Value));
        }
        else if (!isEven && oddRowColor.HasValue)
        {
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(oddRowColor.Value));
        }
    }
    
    /// <summary>
    /// Color picker with right-click to clear functionality.
    /// </summary>
    /// <param name="label">The label/ID for the color picker.</param>
    /// <param name="color">The nullable color value. Null means use default.</param>
    /// <param name="defaultColor">The default color to show when null. Also used as the reset value.</param>
    /// <param name="tooltip">Optional tooltip to show on hover.</param>
    /// <param name="flags">ImGui color edit flags.</param>
    /// <returns>Tuple of (changed, newColor). newColor is null if right-clicked to clear.</returns>
    public static (bool changed, Vector4? newColor) ColorPickerWithClear(
        string label,
        Vector4? color,
        Vector4 defaultColor,
        string? tooltip = null,
        ImGuiColorEditFlags flags = ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaPreviewHalf)
    {
        var displayColor = color ?? defaultColor;
        var changed = false;
        Vector4? result = color;
        
        if (ImGui.ColorEdit4(label, ref displayColor, flags))
        {
            result = displayColor;
            changed = true;
        }
        
        // Right-click to clear (reset to null/default)
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            result = null;
            changed = true;
        }
        
        if (ImGui.IsItemHovered())
        {
            var hoverText = tooltip ?? "Color";
            if (color.HasValue)
            {
                hoverText += "\nRight-click to reset to default";
            }
            ImGui.SetTooltip(hoverText);
        }
        
        return (changed, result);
    }
    
    /// <summary>
    /// Draws a color option with enable/disable toggle for use in settings UI.
    /// </summary>
    /// <param name="label">The label for the color option.</param>
    /// <param name="currentColor">The current color value (null if not set).</param>
    /// <param name="setColor">Callback to set the new color value.</param>
    /// <returns>True if the color was changed.</returns>
    public static bool DrawColorOption(string label, Vector4? currentColor, Action<Vector4?> setColor)
    {
        var (changed, newColor) = ColorPickerWithClear(label, currentColor, DefaultColor, label);
        if (changed)
        {
            setColor(newColor);
        }
        return changed;
    }
    
    /// <summary>
    /// Draws alignment combo boxes for horizontal and vertical alignment.
    /// </summary>
    /// <param name="horizontalLabel">Label for horizontal alignment combo.</param>
    /// <param name="verticalLabel">Label for vertical alignment combo.</param>
    /// <param name="hAlign">Current horizontal alignment.</param>
    /// <param name="vAlign">Current vertical alignment.</param>
    /// <param name="setHAlign">Callback to set horizontal alignment.</param>
    /// <param name="setVAlign">Callback to set vertical alignment.</param>
    /// <returns>True if any alignment was changed.</returns>
    public static bool DrawAlignmentCombos(
        string horizontalLabel,
        string verticalLabel,
        MTTableHorizontalAlignment hAlign,
        MTTableVerticalAlignment vAlign,
        Action<MTTableHorizontalAlignment> setHAlign,
        Action<MTTableVerticalAlignment> setVAlign)
    {
        var changed = false;
        
        var hAlignInt = (int)hAlign;
        if (ImGui.Combo(horizontalLabel, ref hAlignInt, "Left\0Center\0Right\0"))
        {
            setHAlign((MTTableHorizontalAlignment)hAlignInt);
            changed = true;
        }
        
        var vAlignInt = (int)vAlign;
        if (ImGui.Combo(verticalLabel, ref vAlignInt, "Top\0Center\0Bottom\0"))
        {
            setVAlign((MTTableVerticalAlignment)vAlignInt);
            changed = true;
        }
        
        return changed;
    }
    
    /// <summary>
    /// Formats a number using the specified format configuration.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <param name="config">The number format configuration.</param>
    /// <returns>Formatted string.</returns>
    public static string FormatNumber(long value, NumberFormatConfig? config)
    {
        return MTNumberFormatter.Format(value, config);
    }
    
    /// <summary>
    /// Formats a number using the specified format configuration.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <param name="config">The number format configuration.</param>
    /// <returns>Formatted string.</returns>
    public static string FormatNumber(double value, NumberFormatConfig? config)
    {
        return MTNumberFormatter.Format(value, config);
    }
}
