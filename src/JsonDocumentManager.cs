using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

public class JsonDocumentManager
{
    private JsonDocument _document;
    private readonly Dictionary<string, object?> _pendingChanges = new();

    public JsonDocumentManager(JsonDocument document)
    {
        _document = document;
    }

    public JsonDocument Document => _document;

    public void RegisterChange(string propertyPath, object? newValue)
    {
        _pendingChanges[propertyPath] = newValue;
    }

    public bool HasPendingChanges => _pendingChanges.Count > 0;

    public JsonDocument ApplyChanges()
    {
        if (!_pendingChanges.Any())
            return _document;

        // Convert current document to a mutable dictionary
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        var jsonText = _document.RootElement.GetRawText();
        var jsonObject = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonText);

        if (jsonObject == null)
        {
            _pendingChanges.Clear();
            return _document;
        }

        // Convert to object dictionary for easier manipulation
        var mutableObject = ConvertToMutableDictionary(jsonObject);

        // Apply all pending changes
        foreach (var change in _pendingChanges)
        {
            ApplyChangeToObject(mutableObject, change.Key, change.Value);
        }

        // Create new JsonDocument
        var newJsonText = JsonSerializer.Serialize(mutableObject, new JsonSerializerOptions { WriteIndented = true });
        var newDocument = JsonDocument.Parse(newJsonText);

        // Replace the old document
        _document.Dispose();
        _document = newDocument;

        // Clear pending changes
        _pendingChanges.Clear();

        return _document;
    }

    private Dictionary<string, object?> ConvertToMutableDictionary(Dictionary<string, JsonElement> source)
    {
        var result = new Dictionary<string, object?>();

        foreach (var kvp in source)
        {
            result[kvp.Key] = ConvertJsonElement(kvp.Value);
        }

        return result;
    }

    private object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt32(out int intVal) ? intVal : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Object => ConvertToMutableDictionary(
                JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(element.GetRawText()) ?? new()
            ),
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToArray(),
            _ => element.GetRawText()
        };
    }

    private void ApplyChangeToObject(Dictionary<string, object?> obj, string propertyPath, object? newValue)
    {
        var parts = propertyPath.Split('.');
        var current = obj;

        // Navigate to the parent object
        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (current.TryGetValue(parts[i], out var value) && value is Dictionary<string, object?> nestedObj)
            {
                current = nestedObj;
            }
            else
            {
                // Create nested object if it doesn't exist
                var newObj = new Dictionary<string, object?>();
                current[parts[i]] = newObj;
                current = newObj;
            }
        }

        // Set the final value, converting string to appropriate type based on original
        var finalKey = parts[parts.Length - 1];
        current[finalKey] = ConvertStringToAppropriateType(newValue?.ToString(), current.GetValueOrDefault(finalKey));
    }

    private object? ConvertStringToAppropriateType(string? value, object? originalValue)
    {
        if (value == null) return null;

        // Try to maintain the original type
        return originalValue switch
        {
            int => int.TryParse(value, out int intVal) ? intVal : value,
            double => double.TryParse(value, out double doubleVal) ? doubleVal : value,
            bool => bool.TryParse(value, out bool boolVal) ? boolVal : value,
            _ => value
        };
    }
}