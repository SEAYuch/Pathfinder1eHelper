using System.Runtime.CompilerServices;
using ReactiveUI;
using ReactiveUI.Builder;
using ReactiveUI.Primitives.Concurrency;

namespace Pathfinder1eHelper.Test;

/// <summary>
/// One-time ReactiveUI initialization for the test assembly. ReactiveUI 24 requires the builder
/// to run before <c>WhenAnyValue</c>/activation work; the app does this via
/// <c>UseReactiveUIWithAutofac</c>, but the test host must do it explicitly. The main-thread
/// scheduler is set to <see cref="ImmediateSequencer"/> so command output is delivered
/// synchronously without an Avalonia dispatcher.
/// </summary>
internal static class TestInit
{
    [ModuleInitializer]
    public static void Initialize() =>
        RxAppBuilder.CreateReactiveUIBuilder()
            .WithPlatformServices()
            .WithMainThreadScheduler(ImmediateSequencer.Instance)
            .BuildApp();
}
