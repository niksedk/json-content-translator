using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Text.Json;

namespace JsonTreeViewEditor
{
    public partial class JsonGridItem : ObservableObject
    {
        [ObservableProperty] private string? _value;
        [ObservableProperty] private bool _isDirty;

        public string DisplayName { get; set; }
        public JsonElement Parent { get; internal set; }
        public JsonProperty JsonProperty { get; internal set; }
        public JsonValueKind ValueKind { get; internal set; }

        // Store the original value for comparison
        public string? OriginalValue { get; private set; }

        // Event to notify when value changes
        public event EventHandler<JsonGridItem>? ValueChanged;

        public JsonGridItem(JsonElement element, JsonProperty prop)
        {
            DisplayName = prop.Name;
            Value = prop.Value.GetString();
            OriginalValue = Value;
            JsonProperty = prop;
            Parent = element;
            ValueKind = prop.Value.ValueKind;            
        }

        partial void OnValueChanged(string? value)
        {
            IsDirty = value != OriginalValue;
            ValueChanged?.Invoke(this, this);
        }

        public void MarkAsClean()
        {
            OriginalValue = Value;
            IsDirty = false;
        }
    }
}