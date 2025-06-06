using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Fluent;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;
using System;

namespace JsonContentTranslator
{
    internal sealed class Program
    {
        [STAThread]
        public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        public static AppBuilder BuildAvaloniaApp()
        {
            IconProvider.Current
           .Register<FontAwesomeIconProvider>();

            return AppBuilder.Configure<App>()
                        .UsePlatformDetect()
                        .WithInterFont()
                        .AfterSetup(b =>
                        {
                            b.Instance?.Styles.Add(new FluentTheme());
                            b.Instance?.Styles.Add(new StyleInclude(new Uri("avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml", UriKind.Absolute))
                            {
                                Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml")
                            });
                        })
                        .LogToTrace();
        }
    }
}
