using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Projektanker.Icons.Avalonia;

namespace JsonContentTranslator;

public static class UiUtil
{
    public static Button WithIconLeft(this Button control, string iconName)
    {
        var label = new TextBlock() { Text = control.Content?.ToString(), Padding = new Thickness(5, 5, 0, 0) };
        var image = new ContentControl() { FontSize = 22 };
        Attached.SetIcon(image, iconName);
        var stackPanelApplyFixes = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children = { image, label },
        };

        control.Content = stackPanelApplyFixes;
        control.Margin = new Thickness(0, 10, 2, 0);

        return control;
    }
}
