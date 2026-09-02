using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Pathfinder1eHelper.ViewModels;
using Pathfinder1eHelper.Views;
using Splat;

namespace Pathfinder1eHelper;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // By this point UseReactiveUIWithAutofac (Program.BuildAvaloniaApp) has built the container
        // and set it as the Splat/ReactiveUI locator, so the shell + its view model resolve from DI.
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = (MainWindow)Locator.Current.GetService(typeof(MainWindow))!;
            window.DataContext = Locator.Current.GetService(typeof(MainWindowViewModel));
            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
