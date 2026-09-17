using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Headless;
using Pathfinder1eHelper;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.Builder;
using ReactiveUI.Primitives.Concurrency;
using Splat;

namespace Pathfinder1eHelper.Test;

/// <summary>
/// One-time initialization for the test assembly:
/// <list type="bullet">
/// <item>ReactiveUI 24 builder must run before <c>WhenAnyValue</c>/activation work; the app does
/// this via <c>UseReactiveUIWithAutofac</c>, the test host does it explicitly. The main-thread
/// scheduler is <see cref="ImmediateSequencer"/> so command output is delivered synchronously.</item>
/// <item>The Avalonia activation fetcher is registered so tests may construct
/// <c>ReactiveUserControl</c>-based views.</item>
/// <item>A headless Avalonia platform is started so tests can instantiate real XAML views
/// (the locator tests do) without a windowing backend.</item>
/// </list>
/// </summary>
internal static class TestInit
{
    [ModuleInitializer]
    public static void Initialize()
    {
        AppBuilder.Configure<App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions())
            .SetupWithoutStarting();

        RxAppBuilder.CreateReactiveUIBuilder()
            .WithPlatformServices()
            .WithMainThreadScheduler(ImmediateSequencer.Instance)
            .BuildApp();

        Locator.CurrentMutable.Register(
            () => new AvaloniaActivationForViewFetcher(),
            typeof(IActivationForViewFetcher));
    }
}
