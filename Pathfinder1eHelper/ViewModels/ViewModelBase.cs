using ReactiveUI;

namespace Pathfinder1eHelper.ViewModels;

/// <summary>
/// Base class for all view models. Derives from <see cref="ReactiveObject"/> for change
/// notification and implements <see cref="IActivatableViewModel"/> so views deriving from
/// <c>ReactiveUserControl&lt;T&gt;</c>/<c>ReactiveWindow&lt;T&gt;</c> can drive
/// <c>this.WhenActivated(...)</c> when they are attached to the visual tree.
/// </summary>
public abstract class ViewModelBase : ReactiveObject, IActivatableViewModel
{
    public ViewModelActivator Activator { get; } = new();
}
