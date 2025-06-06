using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using JsonContentTranslator;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace JsonTreeViewEditor
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty] private ObservableCollection<JsonTreeNode> _jsonTree = new();
        [ObservableProperty] private JsonTreeNode? _selectedNode;
        [ObservableProperty] private ObservableCollection<JsonGridItem> _nodeProperties = new();
        [ObservableProperty] private JsonGridItem? _selectedNodeProperty;
        [ObservableProperty] private bool _isTextBoxEnabled;

        private JsonDocument? _document;
        private string? _fileName;
        private JsonDocumentManager? _documentManager;

        public TreeView JsonTreeView { get; internal set; }

        public MainWindowViewModel()
        {

        }

        partial void OnSelectedNodeChanged(JsonTreeNode? value)
        {
            // Save any pending changes before switching nodes
            if (_documentManager != null && HasPendingChanges())
            {
                ApplyPendingChangesToDocument();
            }

            NodeProperties = new ObservableCollection<JsonGridItem>(
                value?.Properties ?? new List<JsonGridItem>());

            // Subscribe to value changes for the new properties
            foreach (var item in NodeProperties)
            {
                item.ValueChanged += OnJsonItemValueChanged;
            }
        }

        public void LoadJson(string filePath)
        {
            _fileName = filePath;
            var json = File.ReadAllText(filePath);
            _document = JsonDocument.Parse(json);
            _documentManager = new JsonDocumentManager(_document);
            var root = ParseJson(_document.RootElement, "Root");
            JsonTree.Clear();
            JsonTree.Add(root);
            ExpandAll(); 
        }

        public void SaveJson(string filePath)
        {
            // Apply any pending changes first
            SaveChanges();

            if (_document == null || string.IsNullOrEmpty(filePath))
            {
                return;
            }

            using var stream = File.Create(filePath);
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
            _document.WriteTo(writer);
        }

        private JsonTreeNode ParseJson(JsonElement element, string name)
        {
            var node = new JsonTreeNode
            {
                DisplayName = name,
                Element = element,
                ValueKind = element.ValueKind,
            };

            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var prop in element.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Undefined ||
                            prop.Value.ValueKind == JsonValueKind.Null)
                        {
                            continue; // Skip undefined/null properties
                        }

                        if (prop.Value.ValueKind == JsonValueKind.String ||
                            prop.Value.ValueKind == JsonValueKind.Number ||
                            prop.Value.ValueKind == JsonValueKind.True ||
                            prop.Value.ValueKind == JsonValueKind.False)
                        {
                            var gridItem = new JsonGridItem(element, prop);
                            node.Properties.Add(gridItem);
                        }

                        if (prop.Value.ValueKind == JsonValueKind.Object)
                        {
                            var child = ParseJson(prop.Value, prop.Name);
                            node.Children.Add(child);
                        }
                    }
                    break;
                case JsonValueKind.Array:
                    int index = 0;
                    foreach (var item in element.EnumerateArray())
                    {
                        var child = ParseJson(item, $"[{index++}]");
                        node.Children.Add(child);
                    }
                    break;
                default:
                    break;
            }

            return node;
        }

        private void OnJsonItemValueChanged(object? sender, JsonGridItem item)
        {
            if (item.IsDirty && _documentManager != null)
            {
                // Just register the change, don't apply immediately
                var propertyPath = BuildPropertyPath(item);
                _documentManager.RegisterChange(propertyPath, item.Value);
            }
        }

        private string BuildPropertyPath(JsonGridItem item)
        {
            // For now, simple implementation
            return item.DisplayName;
        }

        private bool HasPendingChanges()
        {
            return NodeProperties.Any(p => p.IsDirty);
        }

        private void ApplyPendingChangesToDocument()
        {
            if (_documentManager == null) return;

            // Apply changes to get updated document
            _document = _documentManager.ApplyChanges();

            // Mark all current properties as clean
            foreach (var item in NodeProperties)
            {
                item.MarkAsClean();
            }
        }

        // Call this method to apply all pending changes and rebuild tree
        public void SaveChanges()
        {
            if (_documentManager == null) return;

            // Apply pending changes
            ApplyPendingChangesToDocument();

            // Now rebuild the tree with the updated document
            RebuildTree();
        }

        private void RebuildTree()
        {
            if (_document == null) return;

            // Remember the currently selected node path
            var selectedPath = SelectedNode?.DisplayName;

            var root = ParseJson(_document.RootElement, "Root");
            JsonTree.Clear();
            JsonTree.Add(root);

            // Try to restore selection
            if (!string.IsNullOrEmpty(selectedPath))
            {
                var newSelectedNode = FindNodeByPath(selectedPath);
                if (newSelectedNode != null)
                {
                    SelectedNode = newSelectedNode;
                }
            }
        }

        private JsonTreeNode? FindNodeByPath(string displayName)
        {
            return JsonTree.FirstOrDefault()?.FindChild(displayName);
        }

        // Remove auto-save on property selection change
        partial void OnSelectedNodePropertyChanged(JsonGridItem? value)
        {
            // Don't auto-save here - let user control when to save
            // SaveChanges();
        }

        // Add explicit methods for user to call
        public void ApplyChanges()
        {
            ApplyPendingChangesToDocument();
        }

        public void RefreshTree()
        {
            SaveChanges();
        }

        internal void OnDataGridSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            IsTextBoxEnabled = SelectedNodeProperty != null;
        }

        public void ExpandAll()
        {
            Dispatcher.UIThread.Post(() =>
            {
                var allTreeViewItems = FindAllTreeViewItems(JsonTreeView);
                foreach (var item in allTreeViewItems)
                {
                    item.IsExpanded = true;
                }
            }, DispatcherPriority.Background);
        }

        private IEnumerable<TreeViewItem> FindAllTreeViewItems(Control parent)
        {
            var result = new List<TreeViewItem>();
            if (parent is TreeViewItem tvi)
            {
                result.Add(tvi);
            }

            foreach (var child in parent.GetLogicalDescendants())
            {
                if (child is TreeViewItem treeViewItem)
                {
                    result.Add(treeViewItem);
                }
            }

            return result;
        }
    }
}

// Extension method for JsonTreeNode to help find children
public static class JsonTreeNodeExtensions
{
    public static JsonTreeNode? FindChild(this JsonTreeNode node, string displayName)
    {
        if (node.DisplayName == displayName)
            return node;

        foreach (var child in node.Children)
        {
            var found = child.FindChild(displayName);
            if (found != null)
                return found;
        }

        return null;
    }
}