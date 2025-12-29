using Dalamud.Bindings.ImGui;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace MTGui.Tree;

/// <summary>
/// Static helper methods for rendering tree structures in ImGui.
/// </summary>
public static class MTTreeHelpers
{
    /// <summary>
    /// Draws a tree node with standard flags based on configuration.
    /// </summary>
    /// <param name="label">The label to display.</param>
    /// <param name="config">Node configuration.</param>
    /// <param name="id">Optional unique ID suffix for the node.</param>
    /// <returns>True if the node is open and children should be rendered.</returns>
    public static bool DrawTreeNode(string label, MTTreeNodeConfig? config = null, string? id = null)
    {
        config ??= new MTTreeNodeConfig();
        var flags = GetTreeNodeFlags(config);
        
        var fullLabel = id != null ? $"{label}##{id}" : label;
        var isOpen = ImGui.TreeNodeEx(fullLabel, flags);
        
        // For leaf nodes with NoTreePushOnOpen, we don't need to pop
        if (config.IsLeaf)
            return false;
        
        return isOpen;
    }
    
    /// <summary>
    /// Draws a tree node that tracks its own expansion state.
    /// </summary>
    /// <typeparam name="TKey">The type of the node key.</typeparam>
    /// <param name="key">The unique key for this node.</param>
    /// <param name="label">The label to display.</param>
    /// <param name="expansionState">The expansion state tracker.</param>
    /// <param name="config">Node configuration.</param>
    /// <returns>True if the node is open and children should be rendered.</returns>
    public static bool DrawTreeNodeWithState<TKey>(
        TKey key,
        string label,
        MTTreeExpansionState<TKey> expansionState,
        MTTreeNodeConfig? config = null) where TKey : notnull
    {
        config ??= new MTTreeNodeConfig();
        var flags = GetTreeNodeFlags(config);
        
        // Set open state based on our tracking
        if (expansionState.IsExpanded(key))
            flags |= ImGuiTreeNodeFlags.DefaultOpen;
        
        var fullLabel = $"{label}##{key}";
        var wasOpen = expansionState.IsExpanded(key);
        var isOpen = ImGui.TreeNodeEx(fullLabel, flags);
        
        // Sync state if it changed via ImGui interaction
        if (isOpen != wasOpen && !config.IsLeaf)
            expansionState.SetExpanded(key, isOpen);
        
        if (config.IsLeaf)
            return false;
        
        return isOpen;
    }
    
    /// <summary>
    /// Draws a complete tree structure recursively.
    /// </summary>
    /// <typeparam name="TKey">The type of node keys.</typeparam>
    /// <typeparam name="TData">The type of node data.</typeparam>
    /// <param name="nodes">The root nodes to render.</param>
    /// <param name="expansionState">Optional expansion state tracker.</param>
    /// <param name="nodeRenderer">Optional custom renderer for node content. Called after the node label.</param>
    /// <param name="config">Default node configuration.</param>
    public static void DrawTree<TKey, TData>(
        IEnumerable<MTTreeNode<TKey, TData>> nodes,
        MTTreeExpansionState<TKey>? expansionState = null,
        Action<MTTreeNode<TKey, TData>>? nodeRenderer = null,
        MTTreeNodeConfig? config = null) where TKey : notnull
    {
        config ??= new MTTreeNodeConfig();
        
        foreach (var node in nodes)
        {
            DrawTreeNodeRecursive(node, expansionState, nodeRenderer, config);
        }
    }
    
    private static void DrawTreeNodeRecursive<TKey, TData>(
        MTTreeNode<TKey, TData> node,
        MTTreeExpansionState<TKey>? expansionState,
        Action<MTTreeNode<TKey, TData>>? nodeRenderer,
        MTTreeNodeConfig config) where TKey : notnull
    {
        var nodeConfig = config with { IsLeaf = !node.HasChildren };
        
        var flags = GetTreeNodeFlags(nodeConfig);
        
        // Apply expansion state if tracked
        if (expansionState != null && expansionState.IsExpanded(node.Key))
            flags |= ImGuiTreeNodeFlags.DefaultOpen;
        
        // Apply icon if present
        var label = node.Icon != null ? $"{node.Icon} {node.Label}" : node.Label;
        
        // Apply color if present
        if (node.LabelColor.HasValue)
            ImGui.PushStyleColor(ImGuiCol.Text, node.LabelColor.Value);
        
        var isOpen = ImGui.TreeNodeEx($"{label}##{node.Key}", flags);
        
        if (node.LabelColor.HasValue)
            ImGui.PopStyleColor();
        
        // Track expansion state
        if (expansionState != null && node.HasChildren)
        {
            var wasOpen = expansionState.IsExpanded(node.Key);
            if (isOpen != wasOpen)
                expansionState.SetExpanded(node.Key, isOpen);
        }
        
        // Custom rendering
        nodeRenderer?.Invoke(node);
        
        // Render children if open
        if (isOpen && node.HasChildren)
        {
            foreach (var child in node.Children)
            {
                DrawTreeNodeRecursive(child, expansionState, nodeRenderer, config);
            }
            ImGui.TreePop();
        }
    }
    
    /// <summary>
    /// Converts a MTTreeNodeConfig to ImGui flags.
    /// </summary>
    public static ImGuiTreeNodeFlags GetTreeNodeFlags(MTTreeNodeConfig config)
    {
        var flags = ImGuiTreeNodeFlags.None;
        
        if (config.DefaultOpen)
            flags |= ImGuiTreeNodeFlags.DefaultOpen;
        
        if (!config.OpenOnLabelClick)
            flags |= ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;
        
        if (config.SpanFullWidth)
            flags |= ImGuiTreeNodeFlags.SpanAvailWidth;
        
        if (config.IsLeaf)
        {
            flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
            if (config.ShowBulletForLeaf)
                flags |= ImGuiTreeNodeFlags.Bullet;
        }
        
        return flags;
    }
    
    /// <summary>
    /// Draws an indented sub-item with a tree-style prefix (├ or └).
    /// Useful for rendering expanded sub-rows in tables.
    /// </summary>
    /// <param name="label">The label to display.</param>
    /// <param name="isLast">Whether this is the last item in the list.</param>
    /// <param name="indentSize">The indent size in pixels.</param>
    /// <param name="color">Optional text color.</param>
    public static void DrawSubItem(string label, bool isLast, float indentSize = 16f, Vector4? color = null)
    {
        ImGui.Indent(indentSize);
        var prefix = isLast ? "└ " : "├ ";
        
        if (color.HasValue)
            ImGui.TextColored(color.Value, $"{prefix}{label}");
        else
            ImGui.TextUnformatted($"{prefix}{label}");
        
        ImGui.Unindent(indentSize);
    }
    
    /// <summary>
    /// Draws a collapsible section header. Returns true if the section is open.
    /// This is a simplified wrapper for common settings/section patterns.
    /// </summary>
    /// <param name="label">The section label.</param>
    /// <param name="defaultOpen">Whether the section is open by default.</param>
    /// <param name="id">Optional unique ID suffix.</param>
    /// <returns>True if the section is open and content should be rendered.</returns>
    public static bool DrawSection(string label, bool defaultOpen = false, string? id = null)
    {
        var flags = ImGuiTreeNodeFlags.None;
        if (defaultOpen)
            flags |= ImGuiTreeNodeFlags.DefaultOpen;
        
        var fullLabel = id != null ? $"{label}###{id}" : label;
        return ImGui.TreeNodeEx(fullLabel, flags);
    }
    
    /// <summary>
    /// Ends a collapsible section opened with DrawSection.
    /// Only call this if DrawSection returned true.
    /// </summary>
    public static void EndSection()
    {
        ImGui.TreePop();
    }
    
    /// <summary>
    /// Draws a collapsible header section (CollapsingHeader style).
    /// Unlike DrawSection, this does NOT require a matching EndSection call.
    /// </summary>
    /// <param name="label">The section label.</param>
    /// <param name="defaultOpen">Whether the section is open by default.</param>
    /// <param name="id">Optional unique ID suffix.</param>
    /// <returns>True if the section is open and content should be rendered.</returns>
    public static bool DrawCollapsingSection(string label, bool defaultOpen = true, string? id = null)
    {
        var flags = ImGuiTreeNodeFlags.None;
        if (defaultOpen)
            flags |= ImGuiTreeNodeFlags.DefaultOpen;
        
        var fullLabel = id != null ? $"{label}###{id}" : label;
        return ImGui.CollapsingHeader(fullLabel, flags);
    }
    
    /// <summary>
    /// Draws a collapsible header section with automatic indentation for content.
    /// </summary>
    /// <param name="label">The section label.</param>
    /// <param name="defaultOpen">Whether the section is open by default.</param>
    /// <param name="contentRenderer">The content to render when open.</param>
    /// <param name="indentContent">Whether to indent the content.</param>
    public static void DrawCollapsingSectionWithContent(
        string label,
        bool defaultOpen,
        Action contentRenderer,
        bool indentContent = true)
    {
        if (DrawCollapsingSection(label, defaultOpen))
        {
            if (indentContent)
                ImGui.Indent();
            
            contentRenderer();
            
            if (indentContent)
                ImGui.Unindent();
        }
    }
}
