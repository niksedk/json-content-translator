using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JsonContentTranslator;
using JsonContentTranslator.AutoTranslate;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JsonTreeViewEditor
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty] private ObservableCollection<JsonTreeNode> _jsonTree = new();
        [ObservableProperty] private JsonTreeNode? _selectedNode;
        [ObservableProperty] private ObservableCollection<JsonGridItem> _nodeProperties = new();
        [ObservableProperty] private ObservableCollection<JsonGridItem> _selectedNodesProperties = new();
        [ObservableProperty] private JsonGridItem? _selectedNodeProperty;
        [ObservableProperty] private bool _isTextBoxEnabled;
        [ObservableProperty] private bool _isLoaded;
        [ObservableProperty] private bool _isNotLoaded;
        [ObservableProperty] private bool _isTranslating;
        [ObservableProperty] private ObservableCollection<TranslationPair> _sourceLanguages;
        [ObservableProperty] private TranslationPair _selectedSourceLanguage;
        [ObservableProperty] private ObservableCollection<TranslationPair> _targetLanguages;
        [ObservableProperty] private TranslationPair _selectedTargetLanguage;

        public Window? Window { get; set; }
        public TreeView JsonTreeView { get; internal set; }
        public DataGrid JsonDataGrid { get; internal set; }
        public string Title { get; internal set; }

        private string? _baseFileName;
        private string? _translationFileName;
        private Dictionary<string, JsonGridItem> _lookupBaseDictionary;
        private Dictionary<string, JsonGridItem> _lookupTranslationDictionary;
        private IAutoTranslator _autoTranslator;

        public MainWindowViewModel()
        {
            _autoTranslator = new GoogleTranslateV1();

            _lookupBaseDictionary = new Dictionary<string, JsonGridItem>();
            _lookupTranslationDictionary = new Dictionary<string, JsonGridItem>();

            SourceLanguages = new ObservableCollection<TranslationPair>(GoogleTranslateV1.GetTranslationPairs());
            SelectedSourceLanguage = SourceLanguages.FirstOrDefault(x => x.Code == "en") ?? SourceLanguages[0];

            TargetLanguages = new ObservableCollection<TranslationPair>(GoogleTranslateV1.GetTranslationPairs());
            SelectedTargetLanguage = TargetLanguages[0];

            JsonTreeView = new TreeView();
            JsonDataGrid = new DataGrid();

            Title = "Json Content Translator 1.0";
            IsNotLoaded = true;
        }

        [RelayCommand]
        public void SelectNextBlankPropertyValue()
        {
            var node = JsonTree.FirstOrDefault();
            if (node == null)
            {
                return;
            }

            var property = node.FindPropertyValue(SelectedNodeProperty);
            if (property == null)
            {
                return;
            }

            SelectedNode = property.Node;
            SelectedNodeProperty = property;

            Dispatcher.UIThread.Invoke(() =>
            {
                JsonTreeView.ScrollIntoView(SelectedNode);
                //JsonTreeView.ExpandSubTree();
            });

        }

        [RelayCommand]
        public void TransferBaseForSelected()
        {
            var selectedItems = JsonDataGrid.SelectedItems.OfType<JsonGridItem>().ToList();
            if (selectedItems.Count == 0 || SelectedSourceLanguage == null || SelectedTargetLanguage == null)
            {
                return;
            }

            foreach (var item in selectedItems)
            {
                item.ValueTranslation = item.ValueOriginal;
            }
        }


        [RelayCommand]
        public async Task TranslateSelectedItems()
        {
            var selectedItems = JsonDataGrid.SelectedItems.OfType<JsonGridItem>().ToList();
            if (selectedItems.Count == 0 || SelectedSourceLanguage == null || SelectedTargetLanguage == null)
            {
                return;
            }

            IsTranslating = true;

            foreach (var item in selectedItems)
            {
                if (string.IsNullOrEmpty(item.ValueOriginal))
                {
                    continue; // Skip items that are already translated or empty
                }

                var translation = await _autoTranslator.Translate(item.ValueOriginal, SelectedSourceLanguage.Code, SelectedTargetLanguage.Code, default);
                item.ValueTranslation = translation;
            }
            IsTranslating = false;
        }

        [RelayCommand]
        public async Task OpenBaseAndTranslation()
        {
            var storageProvider = Window!.StorageProvider;

            // Define file type filter for JSON files
            var jsonFileType = new FilePickerFileType("JSON Files")
            {
                Patterns = new[] { "*.json" },
                MimeTypes = new[] { "application/json" }
            };

            // Open first file (base)
            var baseFiles = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select base/source JSON file",
                AllowMultiple = false,
                FileTypeFilter = [jsonFileType]
            });

            if (baseFiles.Count > 0)
            {
                LoadJsonBase(baseFiles[0].Path.LocalPath);

                // Open second file (translation)
                var translationFiles = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Select translation JSON File",
                    AllowMultiple = false,
                    FileTypeFilter = [jsonFileType]
                });

                if (translationFiles.Count > 0)
                {
                    LoadJsonTranslation(translationFiles[0].Path.LocalPath);
                }
            }
        }

        [RelayCommand]
        public async Task OpenSeBaseAndTranslation()
        {
            var storageProvider = Window!.StorageProvider;

            var url = "https://github.com/niksedk/subtitleedit-avalonia/raw/refs/heads/main/src/UI/Assets/Languages/English.json";

            var httpClient = new HttpClient();
            try
            {
                string content = await httpClient.GetStringAsync(url);
                LoadBaseJson(content);
                _baseFileName = "English.json";
            }
            catch (Exception exception)
            {
                await MessageBox.Show(Window!, "Failed to load base JSON from URL", "Could not fetch English base file for SE\n\nError: " + exception.Message, MessageBoxButtons.OK);
                return;
            }

            var jsonFileType = new FilePickerFileType("JSON Files")
            {
                Patterns = new[] { "*.json" },
                MimeTypes = new[] { "application/json" }
            };
            var translationFiles = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select translation JSON File",
                AllowMultiple = false,
                FileTypeFilter = new[] { jsonFileType }
            });

            if (translationFiles.Count > 0)
            {
                LoadJsonTranslation(translationFiles[0].Path.LocalPath);
            }
        }

        [RelayCommand]
        public async Task SaveJson()
        {
            var storageProvider = Window!.StorageProvider;

            // Define file type filter for JSON files
            var jsonFileType = new FilePickerFileType("JSON Files")
            {
                Patterns = new[] { "*.json" },
                MimeTypes = new[] { "application/json" }
            };

            // Open save file dialog
            var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save JSON File",
                DefaultExtension = "json",
                SuggestedFileName = Path.GetFileName(_translationFileName),
                FileTypeChoices = new[] { jsonFileType }
            });

            if (file != null)
            {
                SaveJson(file.Path.LocalPath);
            }
        }

        partial void OnSelectedNodeChanged(JsonTreeNode? value)
        {
            NodeProperties = new ObservableCollection<JsonGridItem>(value?.Properties ?? new List<JsonGridItem>());
            SelectedNodeProperty = NodeProperties.FirstOrDefault();
        }

        public void LoadJsonBase(string filePath)
        {
            _baseFileName = filePath;
            var json = System.IO.File.ReadAllText(filePath);
            LoadBaseJson(json);
        }

        private void LoadBaseJson(string json)
        {
            var document = JsonDocument.Parse(json);
            _lookupBaseDictionary = new Dictionary<string, JsonGridItem>();
            var root = ParseJson(_lookupBaseDictionary, document.RootElement, "Root");
            JsonTree.Clear();
            JsonTree.Add(root);
            ExpandAll();
        }

        public void LoadJsonTranslation(string filePath)
        {
            _translationFileName = filePath;
            var json = System.IO.File.ReadAllText(filePath);
            var document = JsonDocument.Parse(json);
            _lookupTranslationDictionary = new Dictionary<string, JsonGridItem>();
            var root = ParseJson(_lookupTranslationDictionary, document.RootElement, "Root");
            var jsonTree = new List<JsonTreeNode>();
            SetTranslationValues(JsonTree, jsonTree);
            IsLoaded = true;
            IsNotLoaded = false;

            if (Window != null)
            {
                Window.Title = $"{Window.Title} - {Path.GetFileName(_baseFileName)} -> {Path.GetFileName(_translationFileName)}";
            }

            SelectedNode = JsonTree.FirstOrDefault();
            if (SelectedNode != null)
            {
                SelectedNodeProperty = SelectedNode.Properties.FirstOrDefault();
            }

            // Get language from "cultureName"
            var cultureName = root.GetValue("cultureName");
            if (!string.IsNullOrEmpty(cultureName))
            {
                var codes = cultureName.Split('-');
                if (codes.Length > 0)
                {
                    cultureName = codes[0]; // Use the first part of the culture name
                }

                var newLanguage = TargetLanguages.FirstOrDefault(x => x.Code.StartsWith(cultureName));
                if (newLanguage != null)
                {
                    SelectedTargetLanguage = newLanguage;
                    return; // Exit early if we found a matching language
                }
            }

            // try to auto-detect language from the text
            var text = root.GetText();
            var languageCode = LanguageAutoDetect.AutoDetectGoogleLanguageOrNull2(text);
            if (languageCode != null && languageCode != SelectedTargetLanguage.Code)
            {
                var newLanguage = TargetLanguages.FirstOrDefault(x => x.Code.StartsWith(languageCode));
                if (newLanguage != null)
                {
                    SelectedTargetLanguage = newLanguage;
                }
            }
        }

        private void SetTranslationValues(ObservableCollection<JsonTreeNode> baseLanguage, List<JsonTreeNode> translation)
        {
            foreach (var item in _lookupBaseDictionary.Values)
            {
                if (_lookupTranslationDictionary.TryGetValue(item.Path.ToLowerInvariant(), out var translationItem))
                {
                    item.ValueTranslation = translationItem.ValueOriginal;
                }
            }
        }

        public void SaveJson(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) && JsonTree.Count > 0)
            {
                return;
            }

            var json = JsonTree[0].ConvertTreeToJson();
            System.IO.File.WriteAllText(filePath, json, Encoding.UTF8);
        }

        private JsonTreeNode ParseJson(Dictionary<string, JsonGridItem> lookupDictionary, JsonElement element, string name)
        {
            var node = new JsonTreeNode
            {
                DisplayName = name.CapitalizeFirstLetter(),
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
                            var gridItem = new JsonGridItem(node, element, prop);
                            if (!lookupDictionary.ContainsKey(gridItem.Path.ToLowerInvariant()))
                            {
                                lookupDictionary.Add(gridItem.Path.ToLowerInvariant(), gridItem);
                            }

                            node.Properties.Add(gridItem);
                        }

                        if (prop.Value.ValueKind == JsonValueKind.Object)
                        {
                            var child = ParseJson(lookupDictionary, prop.Value, prop.Name);
                            node.Children.Add(child);
                        }
                    }
                    break;
                case JsonValueKind.Array:
                    int index = 0;
                    foreach (var item in element.EnumerateArray())
                    {
                        var child = ParseJson(lookupDictionary, item, $"[{index++}]");
                        node.Children.Add(child);
                    }
                    break;
                default:
                    break;
            }

            return node;
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
                    JsonTreeView.ExpandSubTree(item);
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

        internal void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.F6)
            {
                SelectNextBlankPropertyValueCommand.Execute(null);
                e.Handled = true; // Prevent further processing of this key event
            }
            else if (e.Key == Key.F8)
            {
                TranslateSelectedItemsCommand.Execute(null);
                e.Handled = true; // Prevent further processing of this key event
            }
        }
    }
}

// Extension method for JsonTreeNode to help find children
public static class JsonTreeNodeExtensions
{
    public static JsonTreeNode? FindChild(this JsonTreeNode node, string displayName)
    {
        if (node.DisplayName == displayName)
        {
            return node;
        }

        foreach (var child in node.Children)
        {
            var found = child.FindChild(displayName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}