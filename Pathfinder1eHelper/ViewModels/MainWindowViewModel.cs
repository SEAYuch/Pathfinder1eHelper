using System;
using System.Collections.ObjectModel;
using Material.Icons;
using Pathfinder1eHelper.ViewModels.Pages;
using ReactiveUI;
using ReactiveUI.Primitives;

namespace Pathfinder1eHelper.ViewModels;

/// <summary>
/// Shell view model: a data-driven <c>NavMenu</c> (<see cref="NavItems"/>) drives a ViewModel-first
/// content region (<see cref="CurrentPage"/>) rendered by the name-convention <c>ViewLocator</c>.
/// </summary>
public sealed class MainWindowViewModel : ViewModelBase
{
    private NavItemViewModel? _selectedNavItem;
    private ViewModelBase? _currentPage;
    private bool _isNavExpanded = true;

    public MainWindowViewModel(Func<SpellsViewModel> spellsFactory)
    {
        NavItems =
        [
            new NavItemViewModel("法术", MaterialIconKind.AutoFix, spellsFactory),
            new NavItemViewModel("战斗", MaterialIconKind.Sword, pageFactory: null, isEnabled: false),
            new NavItemViewModel("角色", MaterialIconKind.AccountGroup, pageFactory: null, isEnabled: false)
        ];

        // The shell lives for the whole app, so a constructor subscription is fine (nothing to leak).
        this.WhenAnyValue(x => x.SelectedNavItem)
            .Subscribe(item =>
            {
                if (item?.Page is { } page)
                {
                    CurrentPage = page;
                }
            });

        SelectedNavItem = NavItems[0];
    }

    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public MainWindowViewModel() : this(() => new SpellsViewModel())
    {
    }

    public static string Title => "Pathfinder 1e中文助手";

    public ObservableCollection<NavItemViewModel> NavItems { get; }

    public NavItemViewModel? SelectedNavItem
    {
        get => _selectedNavItem;
        set => this.RaiseAndSetIfChanged(ref _selectedNavItem, value);
    }

    public ViewModelBase? CurrentPage
    {
        get => _currentPage;
        private set => this.RaiseAndSetIfChanged(ref _currentPage, value);
    }

    /// <summary>侧边导航是否展开；收缩时仅显示图标（由视图收窄列宽并隐藏文字）。</summary>
    public bool IsNavExpanded
    {
        get => _isNavExpanded;
        set => this.RaiseAndSetIfChanged(ref _isNavExpanded, value);
    }
}