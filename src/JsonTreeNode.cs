using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Text.Json;
using JsonTreeViewEditor;
using System.Text;

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

        public string GetValue(string displayName)
        {
            var sb = new StringBuilder();
            GetTextFromNode(this, sb, displayName);
            return sb.ToString();
        }

        private void GetTextFromNode(JsonTreeNode node, StringBuilder sb, string displayName)
        {
            if (node.Properties != null)
            {
                foreach (var prop in node.Properties)
                {
                    if (sb.Length > 0)
                    {
                        return;
                    }

                    if (prop.DisplayName.Equals(displayName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        sb.Append(prop.OriginalValue);
                    }
                }
            }

            // Add children as nested objects
            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    if (sb.Length > 0)
                    {
                        return; // Stop if max length is reached
                    }

                    if (!string.IsNullOrEmpty(child.DisplayName))
                    {
                        GetTextFromNode(child, sb, displayName);
                    }
                }
            }
        }

        public string GetText(int maxLength = 2000)
        {
            var sb = new StringBuilder();
            BuildTextFromNode(this, sb, maxLength);
            return sb.ToString();
        }

        private void BuildTextFromNode(JsonTreeNode node, StringBuilder sb, int maxLength)
        {
            if (node.Properties != null)
            {
                foreach (var prop in node.Properties)
                {
                    if (sb.Length >= maxLength)
                    {
                        return; // Stop if max length is reached
                    }

                    if (!string.IsNullOrEmpty(prop.OriginalValue))
                    {
                        sb.Append($"{prop.OriginalValue} ");
                    }
                }
            }

            // Add children as nested objects
            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    if (sb.Length >= maxLength)
                    {
                        return; // Stop if max length is reached
                    }

                    if (!string.IsNullOrEmpty(child.DisplayName))
                    {
                        BuildTextFromNode(child, sb, maxLength);
                    }
                }
            }
        }

        public JsonGridItem? FindPropertyValue(JsonGridItem? selectedNodeProperty)
        {
            var found = false;
            return FindValueFromNode(this, selectedNodeProperty, ref found);
        }

        private JsonGridItem? FindValueFromNode(JsonTreeNode node, JsonGridItem? selectedNodeProperty, ref bool found)
        {
            if (node.Properties != null)
            {
                foreach (var prop in node.Properties)
                {
                    if (string.IsNullOrWhiteSpace(prop.ValueTranslation) && found)
                    {
                        return prop;
                    }

                    if (selectedNodeProperty != null && prop.Path == selectedNodeProperty.Path)
                    {
                        found = true;
                        selectedNodeProperty = null;
                    }
                }
            }

            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    var value = FindValueFromNode(child, selectedNodeProperty, ref found);
                    if (value != null)
                    {
                        return value; // Return the first found value
                    }
                }
            }

            return null;
        }
    }
}