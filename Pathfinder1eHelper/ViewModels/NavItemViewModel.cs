using System;
using AvaloniaFluentUI.Controls;

namespace Pathfinder1eHelper.ViewModels;

/// <summary>
/// One entry in the shell's navigation. Immutable; the page view model is created lazily
/// (and cached) via <paramref name="pageFactory"/> so pages are only built when first selected.
/// </summary>
public sealed class NavItemViewModel(
    string header,
    Symbol icon,
    Func<ViewModelBase>? pageFactory,
    bool isEnabled = true)
{
    private ViewModelBase? _page;

    public string Header { get; } = header;

    public Symbol Icon { get; } = icon;

    public bool IsEnabled { get; } = isEnabled;

    /// <summary>Lazily-created, cached page view model, or null for placeholder entries.</summary>
    public ViewModelBase? Page => _page ??= pageFactory?.Invoke();
}
