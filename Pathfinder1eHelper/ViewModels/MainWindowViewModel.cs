using System;
using System.Collections.ObjectModel;
using System.Linq;
using Pathfinder1eHelper.ViewModels.Pages;
using ReactiveUI;
using ReactiveUI.Primitives;
using TKCode.Arc;

namespace Pathfinder1eHelper.ViewModels;

/// <summary>
/// Shell view model: a data-driven navigation list (<see cref="NavItems"/>) drives ReactiveUI routing
/// (<see cref="Router"/>), whose current view model is rendered by <c>RoutedViewHost</c> through the
/// name-convention <see cref="Views.ViewLocator"/>.
/// </summary>
public sealed class MainWindowViewModel : ViewModelBase, IScreen
{
    public MainWindowViewModel(Func<SpellsViewModel> spellsFactory, Func<FeatsViewModel> featsFactory, Func<CombatViewModel> combatFactory, Func<MonstersViewModel> monstersFactory)
    {
        NavItems =
        [
            new NavItemViewModel("spells", "法术", ArcIcons.BookOpen, spellsFactory),
            new NavItemViewModel("feats", "专长", ArcIcons.Bookmark, featsFactory),
            new NavItemViewModel("combat", "战斗", ArcIcons.Zap, combatFactory),
            new NavItemViewModel("monsters", "怪物", ArcIcons.Bug, monstersFactory)
        ];
        SidebarItems = new ObservableCollection<SidebarItem>();
        foreach (var item in NavItems)
        {
            SidebarItems.Add(new SidebarItem
            {
                Key = item.Key,
                Label = item.Header,
                Icon = item.Icon,
                IsEnabled = item.IsEnabled,
                Tag = item
            });
        }

        // The shell lives for the whole app, so constructor subscriptions are fine (nothing to leak).
        // Sidebar keys only adapt selection; page construction and routing remain owned by this VM.
        this.WhenAnyValue(x => x.SelectedNavKey)
            .Subscribe(key =>
            {
                if (key is null)
                {
                    if (SelectedNavItem is { } current) SelectedNavKey = current.Key;
                    return;
                }

                var match = NavItems.FirstOrDefault(item => item.Key == key);

                if (match is not null && !ReferenceEquals(match, SelectedNavItem))
                {
                    SelectedNavItem = match;
                }
                else if (match is null && SelectedNavItem is { } current)
                {
                    SelectedNavKey = current.Key;
                }
            });
        this.WhenAnyValue(x => x.SelectedNavItem)
            .Subscribe(item =>
            {
                if (item is null) return;
                if (SelectedNavKey != item.Key) SelectedNavKey = item.Key;
                if (item.Page is not { } page) return;
                page.HostScreen = this;
                Router.NavigateAndReset.Execute(page).Subscribe(_ => { });
            });

        SelectedNavItem = NavItems[0];
    }

    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public MainWindowViewModel() : this(() => new SpellsViewModel(), () => new FeatsViewModel(), () => new CombatViewModel(), () => new MonstersViewModel())
    {
    }

    public static string Title => "Pathfinder 1e中文助手";

    public ObservableCollection<NavItemViewModel> NavItems { get; }

    /// <summary>Arc Sidebar's stable route key; kept in sync with the existing selected navigation item.</summary>
    public string? SelectedNavKey
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>Arc Sidebar page models, adapted from <see cref="NavItems"/> without owning route creation.</summary>
    public ObservableCollection<SidebarItem> SidebarItems { get; }

    /// <summary>ReactiveUI routing state; the current view model is shown by <c>RoutedViewHost</c>.</summary>
    public RoutingState Router { get; } = new();

    public NavItemViewModel? SelectedNavItem
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
}