# MTGui

A modular ImGui/ImPlot widget library for Dalamud plugins, providing reusable UI components with a trading platform aesthetic.

## Overview

MTGui provides high-level, configurable widgets for building plugin UIs with ImGui and ImPlot. The library follows Dalamud best practices, using `Dalamud.Bindings.ImGui` for proper ImGui integration.

## Features

### Graph Widgets

The `MTGui.Graph` namespace provides a full-featured charting solution:

- **ImPlotGraph**: Main graph component with support for:
  - Multiple data series with individual colors
  - Time-based and index-based data
  - Interactive legend with series toggling
  - Auto-scroll (follow mode) for real-time data
  - Crosshair and tooltips
  - Multiple graph types: Area, Line, Stairs, Bars, StairsArea

- **GraphConfig**: Extensive configuration options including:
  - Legend position (inside or outside plot)
  - Value labels and formatting
  - Grid lines and current price indicators
  - Auto-scroll time range and position

- **GraphStyles**: Trading platform color palette with customizable styling

## Usage

```csharp
using MTGui.Graph;

// Create a graph with default config
var graph = new ImPlotGraph();

// Or with custom config
var config = new ImPlotGraphConfig
{
    GraphType = GraphType.Area,
    ShowLegend = true,
    LegendPosition = LegendPosition.InsideTopLeft,
    AutoScrollEnabled = true,
    AutoScrollTimeValue = 1,
    AutoScrollTimeUnit = TimeUnit.Hours
};
var graph = new ImPlotGraph(config);

// Render with time-series data
var series = new List<(string name, IReadOnlyList<(DateTime ts, float value)> samples)>
{
    ("Series 1", dataPoints1),
    ("Series 2", dataPoints2)
};
graph.RenderMultipleSeries(series);
```

## Architecture

- **GraphDrawing**: Static utilities for crosshairs, price lines, axis formatting
- **GraphLegend**: Scrollable legend rendering (inside and outside plot)
- **GraphControls**: Interactive controls drawer for auto-scroll settings
- **GraphTooltips**: Styled tooltip rendering
- **GraphValueLabels**: Value labels at data points with collision avoidance
- **FormatUtils**: Number formatting with K/M/B abbreviations

## Requirements

- Dalamud.NET.Sdk 14.0.1+
- .NET 10.0 (Windows)
- Dalamud.Bindings.ImGui
