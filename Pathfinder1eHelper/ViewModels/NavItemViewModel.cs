using System;
using Avalonia.Media;
using ReactiveUI;

namespace Pathfinder1eHelper.ViewModels;

/// <summary>
/// One entry in the shell's navigation. Immutable; the routable page view model is created lazily
/// (and cached) via <paramref name="pageFactory"/> so pages are only built when first selected.
/// </summary>
public sealed class NavItemViewModel(
    string key,
    string header,
    Geometry icon,
    Func<IPageViewModel>? pageFactory,
    bool isEnabled = true)
{
    public string Key { get; } = key;

    public string Header { get; } = header;

    public Geometry Icon { get; } = icon;

    public bool IsEnabled { get; } = isEnabled;

    /// <summary>Lazily-created, cached routable page view model, or null for placeholder entries.</summary>
    public IPageViewModel? Page => field ??= pageFactory?.Invoke();
}
