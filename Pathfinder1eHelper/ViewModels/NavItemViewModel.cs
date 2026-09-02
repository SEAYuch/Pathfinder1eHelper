using System;
using Material.Icons;

namespace Pathfinder1eHelper.ViewModels;

/// <summary>
/// One entry in the shell's <c>NavMenu</c>. Immutable; the page view model is created lazily
/// (and cached) via <paramref name="pageFactory"/> so pages are only built when first selected.
/// </summary>
public sealed class NavItemViewModel(
    string header,
    MaterialIconKind icon,
    Func<ViewModelBase>? pageFactory,
    bool isEnabled = true)
{
    private ViewModelBase? _page;

    public string Header { get; } = header;

    public MaterialIconKind Icon { get; } = icon;

    public bool IsEnabled { get; } = isEnabled;

    /// <summary>Lazily-created, cached page view model, or null for placeholder entries.</summary>
    public ViewModelBase? Page => _page ??= pageFactory?.Invoke();
}
