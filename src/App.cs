using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using JsonTreeViewEditor;

namespace JsonContentTranslator
{
    public partial class App : Application
    {
        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var vm = new MainWindowViewModel(); 
                desktop.MainWindow = new MainWindow(vm)
                {
                    DataContext = vm,
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}