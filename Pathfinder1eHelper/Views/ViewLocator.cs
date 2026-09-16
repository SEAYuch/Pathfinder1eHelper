using System;
using System.Diagnostics.CodeAnalysis;
using ReactiveUI;
using Splat;

namespace Pathfinder1eHelper.Views;

/// <summary>
/// Name-convention <see cref="IViewLocator"/> used by ReactiveUI routing
/// (<c>RoutedViewHost</c>/<c>ViewModelViewHost</c>): <c>*.ViewModels.*ViewModel</c> → <c>*.Views.*View</c>.
/// Views are resolved from the DI container (Splat/Autofac), falling back to
/// <see cref="Activator"/> when unregistered.
/// </summary>
public sealed class ViewLocator : IViewLocator
{
    public IViewFor<TViewModel>? ResolveView<TViewModel>()
        where TViewModel : class =>
        ResolveView<TViewModel>(null);

    public IViewFor<TViewModel>? ResolveView<TViewModel>(string? contract)
        where TViewModel : class =>
        ResolveViewByType(typeof(TViewModel)) as IViewFor<TViewModel>;

    [RequiresUnreferencedCode("Uses reflection to map the view model type name to a view type at runtime.")]
    [RequiresDynamicCode("Uses reflection to map the view model type name to a view type at runtime.")]
    public IViewFor? ResolveView(object? instance) => ResolveView(instance, null);

    [RequiresUnreferencedCode("Uses reflection to map the view model type name to a view type at runtime.")]
    [RequiresDynamicCode("Uses reflection to map the view model type name to a view type at runtime.")]
    public IViewFor? ResolveView(object? instance, string? contract) =>
        instance is null ? null : ResolveViewByType(instance.GetType());

    private static IViewFor? ResolveViewByType(Type viewModelType)
    {
        var viewTypeName = viewModelType.FullName!
            .Replace("ViewModels", "Views", StringComparison.Ordinal);
        viewTypeName = viewTypeName.EndsWith("ViewModel", StringComparison.Ordinal)
            ? string.Concat(viewTypeName.AsSpan(0, viewTypeName.Length - "ViewModel".Length), "View")
            : viewTypeName + "View";

        var viewType = Type.GetType(viewTypeName);
        if (viewType is null)
        {
            return null;
        }

        var view = Locator.Current.GetService(viewType) ?? Activator.CreateInstance(viewType);
        return view as IViewFor;
    }
}
