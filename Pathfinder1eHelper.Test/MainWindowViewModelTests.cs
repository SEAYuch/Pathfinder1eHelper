using Pathfinder1eHelper.ViewModels;
using Pathfinder1eHelper.ViewModels.Pages;
using ReactiveUI;

namespace Pathfinder1eHelper.Test;

/// <summary>Shell navigation: the data-driven NavItems drive ReactiveUI routing (RoutedViewHost).</summary>
public class MainWindowViewModelTests
{
    [Fact]
    public void First_nav_item_is_navigated_on_construction()
    {
        var (vm, spells, _) = Create();

        Assert.Same(spells, vm.Router.GetCurrentViewModel());
        Assert.Single(vm.Router.NavigationStack);
    }

    [Fact]
    public void Selecting_another_nav_item_resets_to_that_page()
    {
        var (vm, _, combat) = Create();

        vm.SelectedNavItem = vm.NavItems[1];

        Assert.Same(combat, vm.Router.GetCurrentViewModel());
        Assert.Single(vm.Router.NavigationStack); // NavigateAndReset keeps a single entry
        Assert.Same(vm, combat.HostScreen);
    }

    [Fact]
    public void Disabled_nav_item_does_not_navigate()
    {
        var (vm, spells, _) = Create();

        vm.SelectedNavItem = vm.NavItems[2]; // 角色（占位项，无页面）

        Assert.Same(spells, vm.Router.GetCurrentViewModel());
    }

    private static (MainWindowViewModel Vm, SpellsViewModel Spells, CombatViewModel Combat) Create()
    {
        var spells = new SpellsViewModel(new FakeSpellService());
        var combat = new CombatViewModel(new FakeCharacterRepository(), new FakeSpellService());
        var vm = new MainWindowViewModel(() => spells, () => combat);
        return (vm, spells, combat);
    }
}
