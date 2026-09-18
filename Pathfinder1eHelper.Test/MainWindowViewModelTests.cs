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
        var (vm, spells, _, _) = Create();

        Assert.Same(spells, vm.Router.GetCurrentViewModel());
        Assert.Single(vm.Router.NavigationStack);
    }

    [Fact]
    public void Selecting_another_nav_item_resets_to_that_page()
    {
        var (vm, _, combat, _) = Create();

        vm.SelectedNavItem = vm.NavItems[1];

        Assert.Same(combat, vm.Router.GetCurrentViewModel());
        Assert.Single(vm.Router.NavigationStack); // NavigateAndReset keeps a single entry
        Assert.Same(vm, combat.HostScreen);
    }

    [Fact]
    public void Selecting_monsters_navigates_to_the_monster_page()
    {
        var (vm, _, _, monsters) = Create();

        vm.SelectedNavItem = vm.NavItems[2]; // 怪物

        Assert.Same(monsters, vm.Router.GetCurrentViewModel());
        Assert.Same(vm, monsters.HostScreen);
    }

    private static (MainWindowViewModel Vm, SpellsViewModel Spells, CombatViewModel Combat, MonstersViewModel Monsters) Create()
    {
        var spells = new SpellsViewModel(new FakeSpellService());
        var combat = new CombatViewModel(new FakeCharacterRepository(), new FakeSpellService());
        var monsters = new MonstersViewModel(new FakeMonsterService());
        var vm = new MainWindowViewModel(() => spells, () => combat, () => monsters);
        return (vm, spells, combat, monsters);
    }
}
