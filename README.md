# MTGui

A modular ImGui/ImPlot widget library for Dalamud plugins, providing reusable UI components with a trading platform aesthetic.

## Overview

MTGui provides high-level, configurable widgets for building plugin UIs with ImGui and ImPlot. The library follows Dalamud best practices, using `Dalamud.Bindings.ImGui` for proper ImGui integration.

## Features

### Graph Widgets

The `MTGui.Graph` namespace provides a full-featured charting solution:

- **MTGraph**: Main graph component with support for:
  - Multiple data series with individual colors
  - Time-based and index-based data
  - Interactive legend with series toggling
  - Auto-scroll (follow mode) for real-time data
  - Crosshair and tooltips
  - Multiple graph types: Area, Line, Stairs, Bars, StairsArea

- **MTGraphConfig**: Extensive configuration options including:
  - Legend position (inside or outside plot)
  - Value labels and formatting
  - Grid lines and current price indicators
  - Auto-scroll time range and position

- **GraphStyles**: Trading platform color palette with customizable styling

### Table Widgets

The `MTGui.Table` namespace provides a flexible table component:

- **MTTableWidget<TRow>**: Generic table with support for:
  - Custom column definitions with flexible widths
  - Sortable columns with persisted sort state
  - Frozen header rows
  - Alternating row colors
  - Cell content alignment (horizontal and vertical)
  - Custom cell rendering via delegates
  - Built-in settings UI for runtime customization

- **MTTableConfig**: Configuration classes including:
  - `IMTTableSettings`: Interface for table settings binding
  - `MTTableSettings`: Default settings implementation
  - `MTTableColumn`: Column definition with width, stretch, and sort preferences
  - `MTCellRenderContext`: Context passed to cell renderers
  - `MTMergedColumnGroupBase`: Base class for column merge groups (aggregate multiple columns)
  - `MTMergedRowGroupBase<TKey>`: Generic base class for row merge groups (aggregate multiple rows)

- **MTTableHelpers**: Static utilities for:
  - Aligned cell text rendering
  - Color picker with reset functionality
  - Alignment combo boxes for settings UI
  - Number formatting with compact notation

### Combo Widgets

The `MTGui.Combo` namespace provides a versatile dropdown/picker component:

- **MTComboWidget<TItem, TId>**: Generic combo with support for:
  - Single-select and multi-select modes
  - Favorite stars with toggle persistence
  - Custom icons via delegate
  - Sorting (alphabetical, by ID, or custom)
  - Hierarchical grouping (up to 3 levels)
  - Search/filter functionality
  - Bulk actions (Select All, Clear All)
  - State externalization for persistence

- **MTComboConfig**: Configuration including:
  - Placeholder and search text
  - Icon and star sizes
  - Max displayed items (for performance)
  - List height and display options

- **MTComboState<TId>**: Externalizable state for:
  - Current selection(s)
  - Favorites set
  - Sort order and group mode
  - Filter text

- **MTComboStyles**: Color constants for favorites, selection, and text

### Tree Widgets

The `MTGui.Tree` namespace provides hierarchical/tree rendering utilities:

- **MTTreeNode<TKey, TData>**: Generic tree node structure with:
  - Unique key identification
  - Display label and optional icon
  - Nested children support
  - Optional data payload
  - Custom label colors

- **MTTreeExpansionState<TKey>**: State tracker for expandable trees:
  - Track which nodes are expanded
  - Expand/collapse all functionality
  - Change notifications via events

- **MTTreeNodeConfig**: Configuration for tree rendering:
  - Default open state
  - Open on label click vs arrow only
  - Leaf node handling
  - Span full width option
  - Custom indent size

- **MTTreeHelpers**: Static utilities for:
  - Drawing tree nodes with state tracking
  - Recursive tree rendering
  - Sub-item prefixes (├ └) for table row expansion
  - Collapsible section helpers for settings UI

## Usage

### Graph Example

```csharp
using MTGui.Graph;

// Create a graph with default config
var graph = new MTGraph();

// Or with custom config
var config = new MTGraphConfig
{
    GraphType = MTGraphType.Area,
    ShowLegend = true,
    LegendPosition = MTLegendPosition.InsideTopLeft,
    AutoScrollEnabled = true,
    AutoScrollTimeValue = 1,
    AutoScrollTimeUnit = MTTimeUnit.Hours
};
var graph = new MTGraph(config);

// Render with time-series data
var series = new List<(string name, IReadOnlyList<(DateTime ts, float value)> samples)>
{
    ("Series 1", dataPoints1),
    ("Series 2", dataPoints2)
};
graph.RenderMultipleSeries(series);
```

### Table Example

```csharp
using MTGui.Table;

// Define columns
var columns = new List<MTTableColumn>
{
    new() { Header = "Name", Stretch = true },
    new() { Header = "Value", Width = 100f, PreferSortDescending = true },
    new() { Header = "Status", Width = 80f }
};

// Create table with settings
var table = new MTTableWidget<MyRowData>("MyTable");
var settings = new MTTableSettings { Sortable = true, FreezeHeader = true };
table.BindSettings(settings, () => SaveConfig());

// Render with data
table.Draw(
    columns,
    rowData,
    (row, ctx) =>
    {
        // Custom cell rendering
        switch (ctx.ColumnIndex)
        {
            case 0:
                MTTableWidget<MyRowData>.DrawAlignedText(row.Name, ctx.Settings);
                break;
            case 1:
                MTTableWidget<MyRowData>.DrawAlignedText(
                    MTTableHelpers.FormatNumber(row.Value, compact: true),
                    ctx.Settings);
                break;
            case 2:
                ImGui.TextColored(row.IsActive ? Green : Gray, row.Status);
                break;
        }
    },
    (row, colIdx) => colIdx switch // Sort key selector
    {
        0 => row.Name,
        1 => row.Value,
        2 => row.Status,
        _ => null
    }
);
```

### Combo Example

```csharp
using MTGui.Combo;

// Define your item type implementing IMTComboItem<TId>
public record MyItem(uint Id, string Name, ushort IconId, string Category) : IMTComboItem<uint>;

// Create config
var config = new MTComboConfig
{
    ComboId = "MyItemPicker",
    Placeholder = "Select item...",
    MultiSelect = true,
    ShowFavorites = true,
    ShowIcons = true,
    ShowSortToggle = true
};

// Create widget with optional external state for persistence
var state = new MTComboState<uint>();
var combo = new MTComboWidget<MyItem, uint>(config, state)
    .WithIconRenderer((item, size) => 
    {
        // Render your icon here
        var icon = textureProvider.GetFromGameIcon(item.IconId);
        if (icon.TryGetWrap(out var wrap, out _))
            ImGui.Image(wrap.Handle, size);
        else
            ImGui.Dummy(size);
    })
    .WithSecondaryText(item => item.Category)
    .WithGrouping(item => item.Category); // Optional grouping

// Set items
combo.SetItems(myItems);

// Subscribe to events
combo.SelectionChanged += id => HandleSelection(id);
combo.FavoriteToggled += (id, isFav) => SaveFavorites();

// Render
if (combo.Draw(200f))
{
    // Selection changed
}
```

### Tree Example

```csharp
using MTGui.Tree;

// Create tree nodes
var root = new MTTreeNode<int, MyData>(1, "Root Node")
{
    Children = new List<MTTreeNode<int, MyData>>
    {
        new(2, "Child 1") { Data = myData1 },
        new(3, "Child 2")
        {
            Children = new List<MTTreeNode<int, MyData>>
            {
                new(4, "Grandchild") { IsLeaf = true }
            }
        }
    }
};

// Track expansion state (persist this for user preference)
var expansionState = new MTTreeExpansionState<int>();
expansionState.StateChanged += () => SaveConfig();

// Render tree recursively
MTTreeHelpers.DrawTree(
    new[] { root },
    expansionState,
    (node, key) =>
    {
        // Custom content after each node label
        if (node.Data != null)
            ImGui.TextDisabled($" ({node.Data.Value})");
    },
    new MTTreeNodeConfig { DefaultOpen = true }
);

// Or use section helpers for settings UI
if (MTTreeHelpers.DrawSection("Advanced Settings"))
{
    // Collapsible settings content
    ImGui.Checkbox("Option 1", ref option1);
    ImGui.Checkbox("Option 2", ref option2);
    MTTreeHelpers.EndSection();
}

// For table row expansion, use sub-item rendering
MTTreeHelpers.DrawSubItem(isLast: false, () =>
{
    ImGui.Text("├ Sub-item");
});
MTTreeHelpers.DrawSubItem(isLast: true, () =>
{
    ImGui.Text("└ Last sub-item");
});
```

## Architecture

- **MTGraphDrawing**: Static utilities for crosshairs, price lines, axis formatting
- **MTGraphLegend**: Scrollable legend rendering (inside and outside plot)
- **MTGraphControls**: Interactive controls drawer for auto-scroll settings
- **MTGraphTooltips**: Styled tooltip rendering
- **MTGraphValueLabels**: Value labels at data points with collision avoidance
- **MTFormatUtils**: Number formatting with K/M/B abbreviations
- **MTTreeHelpers**: Tree node rendering with expansion tracking and sub-item prefixes

## Requirements

- Dalamud.NET.Sdk 14.0.1+
- .NET 10.0 (Windows)
- Dalamud.Bindings.ImGui
