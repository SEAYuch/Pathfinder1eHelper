using Avalonia;
using Avalonia.Headless;
using Pathfinder1eHelper;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.Builder;
using ReactiveUI.Primitives.Concurrency;
using Splat;

[assembly: Xunit.AssemblyFixture(typeof(Pathfinder1eHelper.Test.TestAppFixture))]

namespace Pathfinder1eHelper.Test;

/// <summary>
/// One-time test-assembly setup, run as an xUnit.net v3 assembly fixture (execution phase, NOT during
/// test discovery — heavy initialization in a <c>[ModuleInitializer]</c> can hang MTP discovery).
/// <list type="bullet">
/// <item>ReactiveUI 24 builder must run before <c>WhenAnyValue</c>/activation work; the main-thread
/// scheduler is <see cref="ImmediateSequencer"/> so command output is delivered synchronously.</item>
/// <item>A headless Avalonia platform is started so tests may construct real XAML views, and the
/// Avalonia activation fetcher is registered for <c>ReactiveUserControl</c>-based views.</item>
/// </list>
/// </summary>
public sealed class TestAppFixture
{
    public TestAppFixture()
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
