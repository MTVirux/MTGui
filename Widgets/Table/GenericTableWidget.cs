using Dalamud.Bindings.ImGui;
using MTGui.Tree;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace MTGui.Table;

/// <summary>
/// A generic, reusable table widget with customizable columns, sorting, and styling.
/// This widget handles table structure, sorting, and row styling, while delegating
/// cell content rendering to the caller via delegates.
/// </summary>
/// <typeparam name="TRow">The type of data for each row.</typeparam>
public class GenericTableWidget<TRow>
{
    private readonly string _tableId;
    private readonly string _noDataText;
    
    // Settings binding
    private IGenericTableSettings? _boundSettings;
    private Action? _onSettingsChanged;
    private string _settingsName = "Table Settings";
    
    // Sort state tracking
    private bool _sortInitialized = false;
    
    /// <summary>
    /// Gets whether this widget has bound settings.
    /// </summary>
    public bool HasSettings => _boundSettings != null;
    
    /// <summary>
    /// Gets the display name for settings.
    /// </summary>
    public string SettingsName => _settingsName;
    
    /// <summary>
    /// Delegate for rendering a cell's content.
    /// </summary>
    /// <param name="row">The row data.</param>
    /// <param name="context">The cell render context with row/column indices.</param>
    public delegate void CellRenderer(TRow row, CellRenderContext context);
    
    /// <summary>
    /// Delegate for getting a sortable value from a row for a specific column.
    /// Return IComparable (string, int, float, DateTime, etc.) for sorting.
    /// </summary>
    /// <param name="row">The row data.</param>
    /// <param name="columnIndex">The column index.</param>
    /// <returns>A comparable value for sorting, or null if not sortable.</returns>
    public delegate IComparable? SortKeySelector(TRow row, int columnIndex);
    
    /// <summary>
    /// Creates a new GenericTableWidget.
    /// </summary>
    /// <param name="tableId">Unique ID for ImGui table identification.</param>
    /// <param name="noDataText">Text to display when there is no data.</param>
    public GenericTableWidget(string tableId, string noDataText = "No data available.")
    {
        _tableId = tableId;
        _noDataText = noDataText;
    }
    
    /// <summary>
    /// Binds this widget to a settings object for automatic synchronization.
    /// </summary>
    /// <param name="settings">The settings object implementing IGenericTableSettings.</param>
    /// <param name="onSettingsChanged">Callback when settings are changed (e.g., to trigger config save).</param>
    /// <param name="settingsName">Display name for the settings section.</param>
    public void BindSettings(
        IGenericTableSettings settings,
        Action? onSettingsChanged = null,
        string settingsName = "Table Settings")
    {
        _boundSettings = settings;
        _onSettingsChanged = onSettingsChanged;
        _settingsName = settingsName;
    }
    
    /// <summary>
    /// Draws the table.
    /// </summary>
    /// <param name="columns">Column definitions.</param>
    /// <param name="rows">Row data.</param>
    /// <param name="cellRenderer">Delegate to render each cell's content.</param>
    /// <param name="sortKeySelector">Optional delegate to get sort keys. If null, sorting uses row order.</param>
    /// <param name="settings">Optional settings override. If null, uses bound settings.</param>
    /// <param name="height">Optional explicit height. If 0, uses available height.</param>
    public void Draw(
        IReadOnlyList<GenericTableColumn> columns,
        IReadOnlyList<TRow> rows,
        CellRenderer cellRenderer,
        SortKeySelector? sortKeySelector = null,
        IGenericTableSettings? settings = null,
        float height = 0f)
    {
        settings ??= _boundSettings ?? new GenericTableSettings();
        
        if (columns.Count == 0)
        {
            ImGui.TextUnformatted("No columns defined.");
            return;
        }
        
        if (rows.Count == 0)
        {
            ImGui.TextUnformatted(_noDataText);
            return;
        }
        
        var flags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY;
        if (settings.Sortable) flags |= ImGuiTableFlags.Sortable;
        
        var tableHeight = height > 0 ? height : ImGui.GetContentRegionAvail().Y;
        
        if (!ImGui.BeginTable(_tableId, columns.Count, flags, new Vector2(0, tableHeight)))
            return;
        
        try
        {
            // Setup columns
            for (int i = 0; i < columns.Count; i++)
            {
                var column = columns[i];
                var colFlags = column.Flags;
                
                // Apply default sort to saved column
                if (i == settings.SortColumnIndex)
                {
                    colFlags |= settings.SortAscending 
                        ? ImGuiTableColumnFlags.DefaultSort 
                        : ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.PreferSortDescending;
                }
                else if (column.PreferSortDescending)
                {
                    colFlags |= ImGuiTableColumnFlags.PreferSortDescending;
                }
                
                // Apply width/stretch flags
                if (column.Stretch)
                {
                    colFlags |= ImGuiTableColumnFlags.WidthStretch;
                }
                else if (column.Width > 0)
                {
                    colFlags |= ImGuiTableColumnFlags.WidthFixed;
                }
                
                ImGui.TableSetupColumn(column.Header, colFlags, column.Width);
            }
            
            if (settings.FreezeHeader)
            {
                ImGui.TableSetupScrollFreeze(0, 1);
            }
            
            // Apply header color if set
            if (settings.HeaderColor.HasValue)
            {
                ImGui.PushStyleColor(ImGuiCol.TableHeaderBg, settings.HeaderColor.Value);
            }
            
            // Draw custom header row with alignment support
            ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
            for (int i = 0; i < columns.Count; i++)
            {
                ImGui.TableNextColumn();
                DrawAlignedHeaderCell(
                    columns[i].Header,
                    settings.HeaderHorizontalAlignment,
                    settings.HeaderVerticalAlignment,
                    settings.Sortable,
                    columns[i].HeaderColor);
            }
            
            if (settings.HeaderColor.HasValue)
            {
                ImGui.PopStyleColor();
            }
            
            // Handle sorting
            var sortedRows = GetSortedRows(rows, sortKeySelector, settings);
            
            // Draw data rows
            for (int rowIdx = 0; rowIdx < sortedRows.Count; rowIdx++)
            {
                var row = sortedRows[rowIdx];
                ImGui.TableNextRow();
                
                // Apply row background color based on even/odd
                var isEven = rowIdx % 2 == 0;
                if (settings.UseAlternatingRowColors)
                {
                    if (isEven && settings.EvenRowColor.HasValue)
                    {
                        ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(settings.EvenRowColor.Value));
                    }
                    else if (!isEven && settings.OddRowColor.HasValue)
                    {
                        ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(settings.OddRowColor.Value));
                    }
                }
                
                // Render each cell
                for (int colIdx = 0; colIdx < columns.Count; colIdx++)
                {
                    ImGui.TableNextColumn();
                    
                    var context = new CellRenderContext
                    {
                        RowIndex = rowIdx,
                        ColumnIndex = colIdx,
                        Settings = settings
                    };
                    
                    cellRenderer(row, context);
                }
            }
        }
        finally
        {
            ImGui.EndTable();
        }
    }
    
    private List<TRow> GetSortedRows(
        IReadOnlyList<TRow> rows,
        SortKeySelector? sortKeySelector,
        IGenericTableSettings settings)
    {
        if (!settings.Sortable || sortKeySelector == null)
            return rows.ToList();
        
        // Check for sort specs - update settings when user changes sort
        var sortSpecs = ImGui.TableGetSortSpecs();
        if (sortSpecs.SpecsDirty)
        {
            if (_sortInitialized && sortSpecs.SpecsCount > 0)
            {
                var spec = sortSpecs.Specs;
                settings.SortColumnIndex = spec.ColumnIndex;
                settings.SortAscending = spec.SortDirection == ImGuiSortDirection.Ascending;
                _onSettingsChanged?.Invoke();
            }
            _sortInitialized = true;
            sortSpecs.SpecsDirty = false;
        }
        
        var sortColumnIndex = settings.SortColumnIndex;
        var sortAscending = settings.SortAscending;
        
        // Sort the rows using the sort key selector
        var sorted = rows.ToList();
        sorted.Sort((a, b) =>
        {
            var keyA = sortKeySelector(a, sortColumnIndex);
            var keyB = sortKeySelector(b, sortColumnIndex);
            
            if (keyA == null && keyB == null) return 0;
            if (keyA == null) return sortAscending ? -1 : 1;
            if (keyB == null) return sortAscending ? 1 : -1;
            
            var result = keyA.CompareTo(keyB);
            return sortAscending ? result : -result;
        });
        
        return sorted;
    }
    
    /// <summary>
    /// Draws a header cell with alignment and optional color.
    /// </summary>
    private static void DrawAlignedHeaderCell(
        string label,
        TableHorizontalAlignment hAlign,
        TableVerticalAlignment vAlign,
        bool sortable,
        Vector4? color)
    {
        var textSize = ImGui.CalcTextSize(label);
        var cellSize = ImGui.GetContentRegionAvail();
        var style = ImGui.GetStyle();
        
        // Reserve space for sort arrow if sortable
        const float sortArrowWidth = 20f;
        var effectiveCellWidth = sortable ? cellSize.X - sortArrowWidth : cellSize.X;
        
        // Calculate horizontal offset
        float offsetX = hAlign switch
        {
            TableHorizontalAlignment.Center => (effectiveCellWidth - textSize.X) * 0.5f,
            TableHorizontalAlignment.Right => effectiveCellWidth - textSize.X,
            _ => 0f
        };
        
        // Calculate vertical offset
        float offsetY = vAlign switch
        {
            TableVerticalAlignment.Center => (style.CellPadding.Y * 2 + textSize.Y - textSize.Y) * 0.5f - style.CellPadding.Y,
            TableVerticalAlignment.Bottom => style.CellPadding.Y,
            _ => 0f
        };
        
        if (offsetX > 0f || offsetY != 0f)
        {
            var cursorPos = ImGui.GetCursorPos();
            ImGui.SetCursorPos(new Vector2(cursorPos.X + Math.Max(0f, offsetX), cursorPos.Y + offsetY));
        }
        
        if (color.HasValue)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, color.Value);
        }
        
        ImGui.TableHeader(label);
        
        if (color.HasValue)
        {
            ImGui.PopStyleColor();
        }
    }
    
    #region Settings UI
    
    /// <summary>
    /// Draws the settings UI for this table widget.
    /// </summary>
    /// <returns>True if any setting was changed.</returns>
    public bool DrawSettings()
    {
        if (_boundSettings == null) return false;
        
        var changed = false;
        var settings = _boundSettings;
        
        // Table options
        var sortable = settings.Sortable;
        if (ImGui.Checkbox("Enable sorting", ref sortable))
        {
            settings.Sortable = sortable;
            changed = true;
        }
        
        var freezeHeader = settings.FreezeHeader;
        if (ImGui.Checkbox("Freeze header row", ref freezeHeader))
        {
            settings.FreezeHeader = freezeHeader;
            changed = true;
        }
        
        var useAlternatingColors = settings.UseAlternatingRowColors;
        if (ImGui.Checkbox("Use alternating row colors", ref useAlternatingColors))
        {
            settings.UseAlternatingRowColors = useAlternatingColors;
            changed = true;
        }
        
        ImGui.Spacing();
        if (TreeHelpers.DrawSection("Data Column Alignment", true))
        {
            // Data horizontal alignment
            var hAlign = (int)settings.DataHorizontalAlignment;
            if (ImGui.Combo("Data Horizontal", ref hAlign, "Left\0Center\0Right\0"))
            {
                settings.DataHorizontalAlignment = (TableHorizontalAlignment)hAlign;
                changed = true;
            }
        
            // Data vertical alignment
            var vAlign = (int)settings.DataVerticalAlignment;
            if (ImGui.Combo("Data Vertical", ref vAlign, "Top\0Center\0Bottom\0"))
            {
                settings.DataVerticalAlignment = (TableVerticalAlignment)vAlign;
                changed = true;
            }
            TreeHelpers.EndSection();
        }
        
        ImGui.Spacing();
        if (TreeHelpers.DrawSection("Header Row Alignment"))
        {
            // Header horizontal alignment
            var headerHAlign = (int)settings.HeaderHorizontalAlignment;
            if (ImGui.Combo("Header Horizontal", ref headerHAlign, "Left\0Center\0Right\0"))
            {
                settings.HeaderHorizontalAlignment = (TableHorizontalAlignment)headerHAlign;
                changed = true;
            }
        
            // Header vertical alignment
            var headerVAlign = (int)settings.HeaderVerticalAlignment;
            if (ImGui.Combo("Header Vertical", ref headerVAlign, "Top\0Center\0Bottom\0"))
            {
                settings.HeaderVerticalAlignment = (TableVerticalAlignment)headerVAlign;
                changed = true;
            }
            TreeHelpers.EndSection();
        }
        
        ImGui.Spacing();
        if (TreeHelpers.DrawSection("Row Colors"))
        {
            // Header color
            changed |= TableHelpers.DrawColorOption("Header", settings.HeaderColor, c => settings.HeaderColor = c);
        
            // Even row color
            changed |= TableHelpers.DrawColorOption("Even Rows", settings.EvenRowColor, c => settings.EvenRowColor = c);
        
            // Odd row color
            changed |= TableHelpers.DrawColorOption("Odd Rows", settings.OddRowColor, c => settings.OddRowColor = c);
            TreeHelpers.EndSection();
        }
        
        if (changed)
        {
            _onSettingsChanged?.Invoke();
        }
        
        return changed;
    }
    
    #endregion
    
    #region Helper Methods for Cell Rendering
    
    /// <summary>
    /// Helper method to draw text with alignment in a cell.
    /// Call this from your cell renderer delegate for aligned text.
    /// </summary>
    public static void DrawAlignedText(
        string text,
        TableHorizontalAlignment hAlign,
        TableVerticalAlignment vAlign,
        Vector4? color = null)
    {
        TableHelpers.DrawAlignedCellText(text, hAlign, vAlign, color);
    }
    
    /// <summary>
    /// Helper method to draw text using settings alignment.
    /// </summary>
    public static void DrawAlignedText(string text, IGenericTableSettings settings, Vector4? color = null)
    {
        TableHelpers.DrawAlignedCellText(text, settings.DataHorizontalAlignment, settings.DataVerticalAlignment, color);
    }
    
    #endregion
}
