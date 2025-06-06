using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Text.Json;
using JsonTreeViewEditor;

namespace JsonContentTranslator
{
    public class JsonTreeNode
    {
        public string DisplayName { get; set; }
        public ObservableCollection<JsonTreeNode> Children { get; set; }
        public List<JsonGridItem> Properties { get; set; }
        public JsonElement Element { get; internal set; }
        public JsonValueKind ValueKind { get; internal set; }

        public JsonTreeNode()
        {
            DisplayName = string.Empty;
            Dictionary<string, object> properties = new();
            Children = new ObservableCollection<JsonTreeNode>();
            Properties = new List<JsonGridItem>();
        }
    }
}