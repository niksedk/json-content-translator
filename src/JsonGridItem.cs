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
            Path = $"{node.DisplayName}.{prop.Name}".ToLowerInvariant();
            Node = node;
            ValueOriginal = prop.Value.GetString();
            ValueTranslation = string.Empty;
            OriginalValue = ValueOriginal;
            JsonProperty = prop;
            Parent = element;
            ValueKind = prop.Value.ValueKind;            
        }
    }
}