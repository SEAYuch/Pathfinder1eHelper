using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Pathfinder1eHelper.ViewModels;
using Splat;

namespace Pathfinder1eHelper;

/// <summary>
/// Resolves a View for a ViewModel by name convention:
/// <c>*.ViewModels.*ViewModel</c> → <c>*.Views.*View</c>.
/// Handles nested namespaces too (e.g. <c>ViewModels.Pages.SpellsViewModel</c> →
/// <c>Views.Pages.SpellsView</c>). Views are resolved from the DI container (so they may take
/// injected dependencies), falling back to <see cref="Activator"/> when unregistered.
/// </summary>
public sealed class ViewLocator : IDataTemplate
{
    public Control Build(object? data)
    {
        if (data is null)
        {
            return new TextBlock { Text = "ViewLocator: null data" };
        }

        var name = data.GetType().FullName!
            .Replace("ViewModels", "Views", StringComparison.Ordinal);
        name = name.EndsWith("ViewModel", StringComparison.Ordinal)
            ? string.Concat(name.AsSpan(0, name.Length - "ViewModel".Length), "View")
            : name + "View";

        var type = Type.GetType(name);
        if (type is null)
        {
            return new TextBlock { Text = "View not found: " + name };
        }

        // Prefer DI so views may take injected deps; fall back to Activator.
        var view = Locator.Current.GetService(type) ?? Activator.CreateInstance(type);
        return view as Control ?? new TextBlock { Text = "Not a Control: " + name };
    }

    public bool Match(object? data) => data is ViewModelBase;
}