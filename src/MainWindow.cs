using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using JsonTreeViewEditor;
using Projektanker.Icons.Avalonia;
using System;

namespace JsonContentTranslator
{
    public class MainWindow : Window
    {
        public MainWindow(MainWindowViewModel viewModel)
        {
            viewModel.Window = this;
            DataContext = viewModel;
            Title = viewModel.Title;
            Width = 1024;
            Height = 750;
            MinWidth = 600;
            MinHeight = 400;

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

            var toolBar1 = MakeToolBar1(viewModel);
            var toolBar2 = MakeToolBar2(viewModel);
            var treeView = MakeTreeView(viewModel);
            var gridView = MakeGridView(viewModel);
            var editView = MakeEditView(viewModel);

            mainGrid.Children.Add(toolBar1);
            toolBar1.SetValue(Grid.RowProperty, 0);
            toolBar1.SetValue(Grid.ColumnProperty, 0);
            toolBar1.SetValue(Grid.ColumnSpanProperty, 2);

            mainGrid.Children.Add(toolBar2);
            toolBar2.SetValue(Grid.RowProperty, 0);
            toolBar2.SetValue(Grid.ColumnProperty, 1);

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

        private StackPanel MakeToolBar1(MainWindowViewModel viewModel)
        {
            var buttonOpen = new Button
            {
                Content = "Open json base file...",
                Command = viewModel.OpenBaseAndTranslationCommand,
            }.WithIconLeft("fa-folder-open");
            buttonOpen.Bind(Button.IsVisibleProperty, new Binding(nameof(viewModel.IsNotLoaded)));

            var buttonOpenSeEnglish = new Button
            {
                Content = "Open SE English as base...",
                Command = viewModel.OpenSeBaseAndTranslationCommand,
            }.WithIconLeft("fa-folder-open");
            buttonOpenSeEnglish.Bind(Button.IsVisibleProperty, new Binding(nameof(viewModel.IsNotLoaded)));

            var buttonSave = new Button
            {
                Content = string.Empty,
                Command = viewModel.SaveJsonCommand,
            }.WithIconLeft("fa-floppy-disk");
            buttonSave.Bind(Button.IsVisibleProperty, new Binding(nameof(viewModel.IsLoaded)));

            var buttonGoToNextEmpty = new Button
            {
                Content = "Next blank",
                Command = viewModel.SelectNextBlankPropertyValueCommand,
            }.WithIconLeft("fa-magnifying-glass");
            buttonGoToNextEmpty.Bind(Button.IsVisibleProperty, new Binding(nameof(viewModel.IsLoaded)));

            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(10, 0, 0, 0),
                Children =
                {
                    buttonOpen,
                    buttonOpenSeEnglish,
                    buttonSave,
                    buttonGoToNextEmpty

                },
            };

            return stackPanel;
        }

        private StackPanel MakeToolBar2(MainWindowViewModel viewModel)
        {
            var labelFrom = new Label
            {
                Content = "From",
                Margin = new Thickness(10, 10, 0, 0),
                VerticalContentAlignment = VerticalAlignment.Center,
            };
            labelFrom.Bind(Label.IsVisibleProperty, new Binding(nameof(viewModel.IsLoaded)));

            var comboBoxSourceLanguage = new ComboBox
            {
                ItemsSource = viewModel.SourceLanguages,
                DataContext = viewModel,
                [!ComboBox.SelectedItemProperty] = new Binding(nameof(viewModel.SelectedSourceLanguage)),
                Margin = new Thickness(0, 12, 0, 0),
            };
            comboBoxSourceLanguage.Bind(ComboBox.IsVisibleProperty, new Binding(nameof(viewModel.IsLoaded)));

            var labelTo = new Label
            {
                Content = "To",
                Margin = new Thickness(0, 10, 0, 0),
                VerticalContentAlignment = VerticalAlignment.Center,
            };
            labelTo.Bind(Label.IsVisibleProperty, new Binding(nameof(viewModel.IsLoaded)));

            var comboBoxTargetLanguage = new ComboBox
            {
                ItemsSource = viewModel.TargetLanguages,
                DataContext = viewModel,
                [!ComboBox.SelectedItemProperty] = new Binding(nameof(viewModel.SelectedTargetLanguage)),
                Margin = new Thickness(0, 12, 5, 0),
            };
            comboBoxTargetLanguage.Bind(ComboBox.IsVisibleProperty, new Binding(nameof(viewModel.IsLoaded)));


            var buttonTranslate = new Button
            {
                Content = "Translate selected",
                Command = viewModel.TranslateSelectedItemsCommand,
            }.WithIconLeft("fa-globe");
            buttonTranslate.Bind(Button.IsVisibleProperty, new Binding(nameof(viewModel.IsLoaded)));


            var spinner = new ContentControl() { FontSize = 22, Margin = new Thickness(0, 10, 0, 0) };
            spinner.Bind(ContentControl.IsVisibleProperty, new Binding(nameof(viewModel.IsTranslating)));
            Attached.SetIcon(spinner, "fa-spinner");

            // Set the transform origin to center
            spinner.RenderTransformOrigin = RelativePoint.Center;
            spinner.RenderTransform = new RotateTransform();

            // Create a continuous rotation animation
            var animation = new Animation
            {
                Duration = TimeSpan.FromSeconds(1),
                IterationCount = IterationCount.Infinite,
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0.0),
                        Setters =
                        {
                            new Setter(RotateTransform.AngleProperty, 0.0)
                        }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1.0),
                        Setters =
                        {
                            new Setter(RotateTransform.AngleProperty, 360.0)
                        }
                    }
                }
            };

            animation.RunAsync(spinner);


            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(10, 0, 0, 0),
                Children =
                {
                    labelFrom,
                    comboBoxSourceLanguage,
                    labelTo,
                    comboBoxTargetLanguage,
                    buttonTranslate,
                    spinner,
                },
            };

            return stackPanel;
        }

        public static readonly ControlTheme DataGridNoBorderCellTheme = new ControlTheme(typeof(DataGridCell))
        {
            Setters =
            {
                new Setter(DataGridCell.BorderThicknessProperty, new Thickness(0)),
                new Setter(DataGridCell.BorderBrushProperty, Brushes.Transparent),
                new Setter(DataGridCell.BackgroundProperty, Brushes.Transparent),
                new Setter(DataGridCell.FocusAdornerProperty, null),
                new Setter(DataGridCell.PaddingProperty, new Thickness(4)),
            }
        };

        private static Border MakeGridView(MainWindowViewModel viewModel)
        {
            var dataGrid = new DataGrid
            {
                AutoGenerateColumns = false,
                [!DataGrid.ItemsSourceProperty] = new Binding(nameof(viewModel.NodeProperties)),
                [!DataGrid.SelectedItemProperty] = new Binding(nameof(viewModel.SelectedNodeProperty)),
                Height = double.NaN,
                VerticalAlignment = VerticalAlignment.Stretch,
                IsReadOnly = true,
                CanUserResizeColumns = true,
                Columns =
                {
                    new DataGridTextColumn
                    {
                        Header = "Name",
                        Binding = new Binding(nameof(JsonGridItem.DisplayName)),
                        Width = new DataGridLength(2, DataGridLengthUnitType.Auto),
                        CellTheme = DataGridNoBorderCellTheme,
                    },
                    new DataGridTextColumn
                    {
                        Header = "Base value",
                        Binding = new Binding(nameof(JsonGridItem.ValueOriginal)),
                        Width = new DataGridLength(2, DataGridLengthUnitType.Auto),
                        CellTheme = DataGridNoBorderCellTheme,
                    },
                    new DataGridTextColumn
                    {
                        Header = "Translation value",
                        Binding = new Binding(nameof(JsonGridItem.ValueTranslation)),
                        Width = new DataGridLength(2, DataGridLengthUnitType.Auto),
                        CellTheme = DataGridNoBorderCellTheme,
                    },
                },
                ContextMenu = new ContextMenu
                {
                    Items =
                {
                    new Avalonia.Controls.MenuItem
                    {
                        Header = "Translate selected",
                        Command = viewModel.TranslateSelectedItemsCommand,
                        [!Avalonia.Controls.MenuItem.IsVisibleProperty] = new Binding(nameof(viewModel.IsLoaded)),
                    },
                    new Avalonia.Controls.MenuItem
                    {
                        Header = "Transfer base value for selected",
                        Command = viewModel.TransferBaseForSelectedCommand,
                        [!Avalonia.Controls.MenuItem.IsVisibleProperty] = new Binding(nameof(viewModel.IsLoaded)),
                    }
                }
                }
            };
            dataGrid.SelectionChanged += viewModel.OnDataGridSelectionChanged;
            viewModel.JsonDataGrid = dataGrid;

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
                Margin = new Thickness(10, 0, 0, 10),
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