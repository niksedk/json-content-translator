using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Encodings.Web;
using JsonTreeViewEditor;
using System.Text;

namespace JsonContentTranslator
{
    public class JsonTreeNode
    {
        public string DisplayName { get; set; }
        public string OriginalName { get; set; }
        public int SourceIndex { get; set; }
        public ObservableCollection<JsonTreeNode> Children { get; set; }
        public List<JsonGridItem> Properties { get; set; }
        public JsonElement Element { get; internal set; }
        public JsonValueKind ValueKind { get; internal set; }

        // Raw json of a non-editable scalar (array elements that are not objects), so it can be written back unchanged
        public string? RawValue { get; internal set; }

        public JsonTreeNode()
        {
            DisplayName = string.Empty;
            OriginalName = string.Empty;
            Dictionary<string, object> properties = new();
            Children = new ObservableCollection<JsonTreeNode>();
            Properties = new List<JsonGridItem>();
        }

        public string ConvertTreeToJson()
        {
            var jsonObject = BuildNode(this);
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            };
            return JsonSerializer.Serialize(jsonObject, options);
        }

        private static string FormatKey(string original, string fallback)
        {
            return string.IsNullOrEmpty(original) ? fallback : original;
        }

        private JsonNode? BuildNode(JsonTreeNode node)
        {
            if (node.ValueKind == JsonValueKind.Array)
            {
                return BuildArrayFromNode(node);
            }

            if (node.ValueKind != JsonValueKind.Object &&
                node.ValueKind != JsonValueKind.Undefined &&
                !string.IsNullOrEmpty(node.RawValue))
            {
                return JsonNode.Parse(node.RawValue);
            }

            return BuildObjectFromNode(node);
        }

        private static JsonNode? BuildValue(JsonGridItem prop)
        {
            var value = prop.ValueTranslation ?? string.Empty;
            if (prop.ValueKind == JsonValueKind.String)
            {
                return JsonValue.Create(value);
            }

            // Numbers and booleans are not translated - write them back with their original json type
            var raw = string.IsNullOrWhiteSpace(value) ? prop.OriginalValue : value;
            try
            {
                return JsonNode.Parse(raw ?? string.Empty);
            }
            catch (JsonException)
            {
                return JsonValue.Create(value);
            }
        }

        private JsonArray BuildArrayFromNode(JsonTreeNode node)
        {
            var result = new JsonArray();

            var children = node.Children ?? new ObservableCollection<JsonTreeNode>();
            foreach (var child in children.OrderBy(c => c.SourceIndex))
            {
                result.Add(BuildNode(child));
            }

            return result;
        }

        private JsonObject BuildObjectFromNode(JsonTreeNode node)
        {
            var result = new JsonObject();

            var properties = node.Properties ?? new List<JsonGridItem>();
            var children = node.Children ?? new ObservableCollection<JsonTreeNode>();

            // Values and nested objects are written back in the order they appeared in the source json
            var entries = properties
                .Where(p => !string.IsNullOrEmpty(p.DisplayName))
                .Select(p => new
                {
                    p.SourceIndex,
                    Key = FormatKey(p.OriginalName, p.DisplayName),
                    Value = BuildValue(p),
                })
                .Concat(children
                    .Where(c => !string.IsNullOrEmpty(c.DisplayName))
                    .Select(c => new
                    {
                        c.SourceIndex,
                        Key = FormatKey(c.OriginalName, c.DisplayName),
                        Value = BuildNode(c),
                    }))
                .OrderBy(e => e.SourceIndex);

            foreach (var entry in entries)
            {
                result[entry.Key] = entry.Value;
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