using System;
using System.Collections.ObjectModel;
using FluentIcons.Common;
using Pathfinder1eHelper.ViewModels.Pages;
using ReactiveUI;
using ReactiveUI.Primitives;

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
            new NavItemViewModel("法术", Icon.Wand, spellsFactory),
            new NavItemViewModel("专长", Icon.Medal, featsFactory),
            new NavItemViewModel("战斗", Icon.Flash, combatFactory),
            new NavItemViewModel("怪物", Icon.Bug, monstersFactory)
        ];

        // The shell lives for the whole app, so a constructor subscription is fine (nothing to leak).
        // Top-level tab switching resets the stack (see NavigateAndReset) so it does not grow unbounded.
        this.WhenAnyValue(x => x.SelectedNavItem)
            .Subscribe(item =>
            {
                if (item?.Page is not { } page) return;
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

    /// <summary>ReactiveUI routing state; the current view model is shown by <c>RoutedViewHost</c>.</summary>
    public RoutingState Router { get; } = new();

    public NavItemViewModel? SelectedNavItem
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
}