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

        public string ConvertTreeToJson()
        {
            var jsonObject = BuildObjectFromNode(this);
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            return JsonSerializer.Serialize(jsonObject, options);
        }

        private Dictionary<string, object> BuildObjectFromNode(JsonTreeNode node)
        {
            var result = new Dictionary<string, object>();

            // Add properties as string values
            if (node.Properties != null)
            {
                foreach (var prop in node.Properties)
                {
                    if (!string.IsNullOrEmpty(prop.DisplayName))
                    {
                        result[prop.DisplayName] = prop.ValueTranslation ?? string.Empty;
                    }
                }
            }

            // Add children as nested objects
            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    if (!string.IsNullOrEmpty(child.DisplayName))
                    {
                        result[child.DisplayName] = BuildObjectFromNode(child);
                    }
                }
            }

            return result;
        }
    }
}