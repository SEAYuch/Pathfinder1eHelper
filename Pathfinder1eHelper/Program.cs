using Autofac;
using Avalonia;
using Pathfinder1eHelper.Infrastructure;
using ReactiveUI.Avalonia.Splat;
using System;

namespace Pathfinder1eHelper;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        AppLog.InstallGlobalHandlers();

        // 维护模式：--stamp-db / --version 在启动 UI 前短路返回。
        // 这是全应用唯一会对参考库写入的入口（详见 DbInfoCatalog 的说明）。
        if (MaintenanceMode.TryHandle(args, out var exitCode))
        {
            Environment.ExitCode = exitCode;
            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace()
            // ReactiveUI 24 + Autofac: this single call registers Autofac as the Splat/ReactiveUI
            // locator AND runs WithAvalonia() internally (activation-for-view-fetcher, binding hook,
            // command binding, and RxSchedulers.MainThreadScheduler = AvaloniaScheduler.Instance) in
            // the correct order. Do NOT also call .UseReactiveUI() — that would register the hooks
            // into the default locator which the Autofac locator then shadows.
            .UseReactiveUIWithAutofac(builder => builder.RegisterModule<AppModule>());
}
