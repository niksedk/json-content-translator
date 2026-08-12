using CommunityToolkit.Mvvm.ComponentModel;
using JsonContentTranslator.AutoTranslate;
using System.Text.Json;

namespace JsonTreeViewEditor
{
    public partial class JsonGridItem : ObservableObject
    {
        [ObservableProperty] private string? _valueOriginal;
        [ObservableProperty] private string? _valueTranslation;

        public string DisplayName { get; set; }
        public string OriginalName { get; set; }
        public int SourceIndex { get; set; }
        public string Path { get; set; }
        public JsonElement Parent { get; set; }
        public JsonProperty JsonProperty { get; set; }
        public JsonValueKind ValueKind { get; set; }
        public JsonContentTranslator.JsonTreeNode Node { get; set; }

        // Store the original value for comparison
        public string? OriginalValue { get; private set; }

        public JsonGridItem(JsonContentTranslator.JsonTreeNode node, JsonElement element, JsonProperty prop)
        {
            DisplayName = prop.Name.CapitalizeFirstLetter();
            OriginalName = prop.Name;
            Path = $"{node.DisplayName}.{prop.Name}".ToLowerInvariant();
            Node = node;
            // GetString() only works for strings - numbers/booleans are shown (and written back) as raw json
            ValueOriginal = prop.Value.ValueKind == JsonValueKind.String ? prop.Value.GetString() : prop.Value.GetRawText();
            ValueTranslation = string.Empty;
            OriginalValue = ValueOriginal;
            JsonProperty = prop;
            Parent = element;
            ValueKind = prop.Value.ValueKind;            
        }
    }
}