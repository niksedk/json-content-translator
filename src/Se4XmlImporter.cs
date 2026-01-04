using JsonTreeViewEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace JsonContentTranslator;

public static class Se4XmlImporter
{
    private static List<string> _mappingLog = new List<string>();
    private static List<string> _unmatchedLog = new List<string>();

    public static void ImportSe4Xml(string se4XmlfilePath, System.Collections.ObjectModel.ObservableCollection<JsonTreeNode> jsonTree)
    {
        _mappingLog.Clear();
        _unmatchedLog.Clear();

        var xmlDoc = XDocument.Load(se4XmlfilePath);
        var root = xmlDoc.Root;

        if (root == null)
            return;

        // Build a flat list of all JSON properties for easier searching
        var allJsonProperties = GetAllJsonProperties(jsonTree);

        _mappingLog.Add("=== SE4 XML Import Mapping Log ===\n");
        _mappingLog.Add($"Total JSON properties available: {allJsonProperties.Count}\n");

        // Process each element in the XML recursively
        int matchCount = 0;
        ProcessXmlElementFlat(root, allJsonProperties, "", ref matchCount);

        _mappingLog.Add($"\n=== Summary ===");
        _mappingLog.Add($"Total matches found: {matchCount}");
        _mappingLog.Add($"Total unmatched: {_unmatchedLog.Count}");

        // Save mapping log
        var logPath = Path.Combine(Path.GetDirectoryName(se4XmlfilePath), "import_mapping_log.txt");
        File.WriteAllText(logPath, string.Join("\n", _mappingLog));

        // Save unmatched log
        if (_unmatchedLog.Count > 0)
        {
            var unmatchedPath = Path.Combine(Path.GetDirectoryName(se4XmlfilePath), "import_unmatched.txt");
            File.WriteAllText(unmatchedPath, string.Join("\n", _unmatchedLog));
        }

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(logPath) { UseShellExecute = true });
    }

    private static void ProcessXmlElementFlat(XElement element, List<JsonPropertyInfo> allJsonProperties, string path, ref int matchCount)
    {
        foreach (var childElement in element.Elements())
        {
            var elementName = childElement.Name.LocalName;
            var elementValue = childElement.Value;
            var currentPath = string.IsNullOrEmpty(path) ? elementName : $"{path}.{elementName}";

            // If element has no child elements, it's a leaf node with a value
            if (!childElement.HasElements && !string.IsNullOrWhiteSpace(elementValue))
            {
                var match = FindBestMatch(elementName, currentPath, elementValue, allJsonProperties);

                if (match != null)
                {
                    // Convert XML shortcut format (&) to JSON shortcut format (_)
                    var convertedValue = ConvertShortcutFormat(elementValue);
                    match.Property.ValueTranslation = convertedValue;
                    matchCount++;
                    _mappingLog.Add($"✓ MATCHED: {currentPath}");
                    _mappingLog.Add($"  XML: '{elementName}' = '{TruncateValue(elementValue)}'");
                    _mappingLog.Add($"  JSON: {match.FullPath}");
                    _mappingLog.Add($"  Converted: '{TruncateValue(convertedValue)}'");
                    _mappingLog.Add($"  Match Score: {match.Score}\n");
                }
                else
                {
                    _unmatchedLog.Add($"✗ NO MATCH: {currentPath} = '{TruncateValue(elementValue)}'");
                }
            }
            else if (childElement.HasElements)
            {
                // Recurse into child elements
                ProcessXmlElementFlat(childElement, allJsonProperties, currentPath, ref matchCount);
            }
        }
    }

    private static MatchResult FindBestMatch(string xmlName, string xmlPath, string xmlValue, List<JsonPropertyInfo> allJsonProperties)
    {
        var candidates = new List<MatchResult>();

        foreach (var jsonProp in allJsonProperties)
        {
            int score = CalculateMatchScore(xmlName, xmlPath, xmlValue, jsonProp);

            if (score > 0)
            {
                candidates.Add(new MatchResult
                {
                    Property = jsonProp.Property,
                    FullPath = jsonProp.FullPath,
                    Score = score
                });
            }
        }

        // Return the best match
        return candidates.OrderByDescending(c => c.Score).FirstOrDefault();
    }

    private static int CalculateMatchScore(string xmlName, string xmlPath, string xmlValue, JsonPropertyInfo jsonProp)
    {
        int score = 0;

        var normalizedXmlName = NormalizeName(xmlName);
        var normalizedJsonName = NormalizeName(jsonProp.Property.DisplayName);

        // Exact match (highest priority)
        if (string.Equals(normalizedXmlName, normalizedJsonName, StringComparison.OrdinalIgnoreCase))
        {
            score += 1000;
        }

        // Match with DotDotDot variations
        if (normalizedJsonName.EndsWith("DotDotDot", StringComparison.OrdinalIgnoreCase))
        {
            var jsonBase = normalizedJsonName.Substring(0, normalizedJsonName.Length - 9);
            if (string.Equals(normalizedXmlName, jsonBase, StringComparison.OrdinalIgnoreCase))
            {
                score += 900;
            }
        }

        // Match XML names that might have "..." written as "DotDotDot"
        if (normalizedXmlName.EndsWith("DotDotDot", StringComparison.OrdinalIgnoreCase))
        {
            var xmlBase = normalizedXmlName.Substring(0, normalizedXmlName.Length - 9);
            if (string.Equals(xmlBase, normalizedJsonName, StringComparison.OrdinalIgnoreCase))
            {
                score += 900;
            }
        }

        // Partial name match
        if (normalizedJsonName.Contains(normalizedXmlName, StringComparison.OrdinalIgnoreCase) ||
            normalizedXmlName.Contains(normalizedJsonName, StringComparison.OrdinalIgnoreCase))
        {
            score += 500;
        }

        // Path similarity bonus (if parent names match)
        var xmlPathParts = xmlPath.Split('.');
        var jsonPathParts = jsonProp.FullPath.Split('.');

        int matchingPathSegments = 0;
        for (int i = 0; i < Math.Min(xmlPathParts.Length - 1, jsonPathParts.Length - 1); i++)
        {
            if (string.Equals(NormalizeName(xmlPathParts[i]), NormalizeName(jsonPathParts[i]), StringComparison.OrdinalIgnoreCase))
            {
                matchingPathSegments++;
            }
        }
        score += matchingPathSegments * 100;

        // Value similarity bonus (if original values are similar)
        if (!string.IsNullOrWhiteSpace(jsonProp.Property.OriginalValue))
        {
            if (string.Equals(xmlValue, jsonProp.Property.OriginalValue, StringComparison.OrdinalIgnoreCase))
            {
                score += 200;
            }
            else if (xmlValue.Contains(jsonProp.Property.OriginalValue, StringComparison.OrdinalIgnoreCase) ||
                     jsonProp.Property.OriginalValue.Contains(xmlValue, StringComparison.OrdinalIgnoreCase))
            {
                score += 50;
            }
        }

        return score;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return string.Empty;

        // Remove & and _ characters used for shortcuts
        return name.Replace("&", "").Replace("_", "").Trim();
    }

    private static string ConvertShortcutFormat(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // Convert XML shortcut format (&) to JSON shortcut format (_)
        return value.Replace("&", "_");
    }

    private static string TruncateValue(string value, int maxLength = 60)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        if (value.Length <= maxLength)
            return value;

        return value.Substring(0, maxLength) + "...";
    }

    private static List<JsonPropertyInfo> GetAllJsonProperties(System.Collections.ObjectModel.ObservableCollection<JsonTreeNode> nodes)
    {
        var result = new List<JsonPropertyInfo>();
        CollectJsonProperties(nodes, "", result);
        return result;
    }

    private static void CollectJsonProperties(System.Collections.ObjectModel.ObservableCollection<JsonTreeNode> nodes, string parentPath, List<JsonPropertyInfo> result)
    {
        foreach (var node in nodes)
        {
            var nodePath = string.IsNullOrEmpty(parentPath) ? node.DisplayName : $"{parentPath}.{node.DisplayName}";

            // Add all properties from this node
            if (node.Properties != null)
            {
                foreach (var property in node.Properties)
                {
                    result.Add(new JsonPropertyInfo
                    {
                        Property = property,
                        FullPath = $"{nodePath}.{property.DisplayName}",
                        ParentPath = nodePath
                    });
                }
            }

            // Recurse into children
            if (node.Children != null && node.Children.Count > 0)
            {
                CollectJsonProperties(node.Children, nodePath, result);
            }
        }
    }

    private class JsonPropertyInfo
    {
        public JsonGridItem Property { get; set; }
        public string FullPath { get; set; }
        public string ParentPath { get; set; }
    }

    private class MatchResult
    {
        public JsonGridItem Property { get; set; }
        public string FullPath { get; set; }
        public int Score { get; set; }
    }
}