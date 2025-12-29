namespace MTGui.Combo;

/// <summary>
/// Sort order options for combo items.
/// </summary>
public enum MTComboSortOrder
{
    /// <summary>Sort alphabetically by name.</summary>
    Alphabetical,
    /// <summary>Sort by numeric ID.</summary>
    ById,
    /// <summary>Custom sort order (using provided comparer).</summary>
    Custom
}

/// <summary>
/// Display mode for combo groups.
/// </summary>
public enum MTComboGroupDisplayMode
{
    /// <summary>Show as flat list without grouping.</summary>
    Flat,
    /// <summary>Show with collapsible group headers.</summary>
    Grouped
}
