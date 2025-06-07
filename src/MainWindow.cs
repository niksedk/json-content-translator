using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using JsonTreeViewEditor;

namespace JsonContentTranslator
{
    public class MainWindow : Window
    {
        public MainWindow(MainWindowViewModel viewModel)
        {
            DataContext = viewModel;
            Title = "JSON content translator";
            Width = 800;
            Height = 600;
            MinWidth = 600;
            MinHeight = 400;

            var toolBar = MakeToolBar(viewModel);
            var treeView = MakeTreeView(viewModel);
            var gridView = MakeGridView(viewModel);
            var editView = MakeEditView(viewModel);

            var mainGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,3*"),
                RowDefinitions = new RowDefinitions("Auto,*,Auto"),
                Width = double.NaN,
                Height = double.NaN,
                VerticalAlignment = VerticalAlignment.Stretch,
                ColumnSpacing = 10,
                RowSpacing = 10,
                Margin = new Thickness(0, 0, 10, 0),
            };

            mainGrid.Children.Add(toolBar);
            toolBar.SetValue(Grid.RowProperty, 0);
            toolBar.SetValue(Grid.ColumnProperty, 0);
            toolBar.SetValue(Grid.ColumnSpanProperty, 2);

            mainGrid.Children.Add(treeView);
            treeView.SetValue(Grid.RowProperty, 1);
            treeView.SetValue(Grid.ColumnProperty, 0);
            treeView.SetValue(Grid.RowSpanProperty, 2);

            mainGrid.Children.Add(gridView);
            gridView.SetValue(Grid.RowProperty, 1);
            gridView.SetValue(Grid.ColumnProperty, 1);

            mainGrid.Children.Add(editView);
            editView.SetValue(Grid.RowProperty, 2);
            editView.SetValue(Grid.ColumnProperty, 1);

            Content = mainGrid;
        }

        private StackPanel MakeToolBar(MainWindowViewModel viewModel)
        {
            var openButton = new Button { Content = string.Empty }.WithIconLeft("fa-folder-open");
            openButton.Click += async (_, _) =>
            {
                var ofd = new OpenFileDialog { Filters = { new FileDialogFilter { Name = "English JSON", Extensions = { "json" } } } };
                var files = await ofd.ShowAsync(this);
                if (files != null && files.Length > 0)
                {
                    viewModel.LoadJsonBase(files[0]);


                    ofd = new OpenFileDialog { Filters = { new FileDialogFilter { Name = "English JSON", Extensions = { "json" } } } };
                    files = await ofd.ShowAsync(this);
                    if (files != null && files.Length > 0)
                    {
                        viewModel.LoadJsonTranslation(files[0]);
                    }
                }
            };

            var saveButton = new Button { Content = string.Empty }.WithIconLeft("fa-floppy-disk");
            saveButton.Click += async (_, _) =>
            {
                var sfd = new SaveFileDialog { DefaultExtension = "json", InitialFileName = "output.json" };
                var path = await sfd.ShowAsync(this);
                if (!string.IsNullOrWhiteSpace(path))
                {
                    viewModel.SaveJson(path);
                }
            };

            var buttonGoToNextEmpty = new Button { Content = "Next blank" }.WithIconLeft("fa-magnifying-glass");

            var labelFrom = new Label
            {
                Content = "From",
                Margin = new Thickness(0, 10, 0, 0),
                VerticalContentAlignment = VerticalAlignment.Center,
            };
            var comboBoxSourceLanguage = new ComboBox
            {
                ItemsSource = viewModel.SourceLanguages,
                DataContext = viewModel,
                [!ComboBox.SelectedItemProperty] = new Binding(nameof(viewModel.SelectedSourceLanguage)),
                Margin = new Thickness(0,10,0,0),
            };

            var labelTo = new Label
            {
                Content = "To",
                Margin = new Thickness(0, 10, 0, 0),
                VerticalContentAlignment = VerticalAlignment.Center,
            };
            var comboBoxTargetLanguage = new ComboBox
            {
                ItemsSource = viewModel.TargetLanguages,
                DataContext = viewModel,
                [!ComboBox.SelectedItemProperty] = new Binding(nameof(viewModel.SelectedTargetLanguage)),
                Margin = new Thickness(0, 10, 0, 0),
            };

            var buttonTranslate = new Button { Content = "Translate selected" }.WithIconLeft("fa-language");

            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children = 
                { 
                    openButton, 
                    saveButton, 
                    buttonGoToNextEmpty, 
                    labelFrom,
                    comboBoxSourceLanguage,
                    labelTo,
                    comboBoxTargetLanguage,
                    buttonTranslate },
            };

            return stackPanel;
        }

        private static Border MakeGridView(MainWindowViewModel viewModel)
        {
            var dataGrid = new DataGrid
            {
                AutoGenerateColumns = false,
                [!DataGrid.ItemsSourceProperty] = new Binding(nameof(viewModel.NodeProperties)),
                [!DataGrid.SelectedItemProperty] = new Binding(nameof(viewModel.SelectedNodeProperty)),
                Height = double.NaN,
                VerticalAlignment = VerticalAlignment.Stretch,
                Columns =
                {
                    new DataGridTextColumn
                    {
                        Header = "Name",
                        Binding = new Binding(nameof(JsonGridItem.DisplayName)),
                        Width = new DataGridLength(2, DataGridLengthUnitType.Auto)
                    },
                    new DataGridTextColumn
                    {
                        Header = "Base value",
                        Binding = new Binding(nameof(JsonGridItem.ValueOriginal)),
                        Width = new DataGridLength(2, DataGridLengthUnitType.Auto)
                    },
                    new DataGridTextColumn
                    {
                        Header = "Translation value",
                        Binding = new Binding(nameof(JsonGridItem.ValueTranslation)),
                        Width = new DataGridLength(2, DataGridLengthUnitType.Auto)
                    },
                    new DataGridTextColumn
                    {
                        Header = "Type",
                        Binding = new Binding(nameof(JsonGridItem.ValueKind)),
                        Width = new DataGridLength(1, DataGridLengthUnitType.Auto)
                    },
                },
            };
            dataGrid.SelectionChanged += viewModel.OnDataGridSelectionChanged;

            var border = new Border
            {
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Colors.Gray),
                Padding = new Thickness(5),
                Child = dataGrid,
                Height = double.NaN,
                VerticalAlignment = VerticalAlignment.Stretch,
                CornerRadius = new CornerRadius(5),
            };

            return border;
        }

        private static StackPanel MakeEditView(MainWindowViewModel viewModel)
        {
            var textBox = new TextBox
            {
                AcceptsReturn = true,
                Height = 100,
                [!TextBox.TextProperty] = new Binding($"{nameof(viewModel.SelectedNodeProperty)}.{nameof(JsonGridItem.ValueTranslation)}") { Mode = BindingMode.TwoWay },
                [!TextBox.IsEnabledProperty] = new Binding(nameof(viewModel.IsTextBoxEnabled)),
            };

            var stackPanel = new StackPanel()
            {
                Orientation = Orientation.Vertical,
                Children = { textBox },
                Margin = new Thickness(0, 0, 0, 10),
            };

            return stackPanel;
        }

        private static Border MakeTreeView(MainWindowViewModel viewModel)
        {
            var treeView = new TreeView
            {
                [!TreeView.ItemsSourceProperty] = new Binding(nameof(viewModel.JsonTree)),
                [!TreeView.SelectedItemProperty] = new Binding(nameof(viewModel.SelectedNode)),
                DataContext = viewModel,
                Height = double.NaN,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            viewModel.JsonTreeView = treeView;
            var factory = new FuncTreeDataTemplate<JsonTreeNode>(
                        node => true,
                        (node, _) =>
                        {
                            var textBlock = new TextBlock();
                            textBlock.DataContext = node;
                            textBlock.Bind(TextBlock.TextProperty, new Binding(nameof(JsonTreeNode.DisplayName))
                            {
                                Mode = BindingMode.TwoWay,
                                Source = node,
                            });

                            return textBlock;
                        },
                        node => node.Children
                    );

            treeView.ItemTemplate = factory;

            var border = new Border
            {
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Colors.Gray),
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(5),
                Child = treeView,
                Height = double.NaN,
                VerticalAlignment = VerticalAlignment.Stretch,
                CornerRadius = new CornerRadius(5),
            };

            return border;
        }
    }
}