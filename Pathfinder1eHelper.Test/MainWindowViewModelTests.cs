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
        var (vm, spells, _, _, _) = Create();

        Assert.Same(spells, vm.Router.GetCurrentViewModel());
        Assert.Single(vm.Router.NavigationStack);
    }

    [Fact]
    public void Selecting_feats_navigates_to_the_feat_page()
    {
        var (vm, _, feats, _, _) = Create();

        vm.SelectedNavItem = vm.NavItems[1]; // 专长

        Assert.Same(feats, vm.Router.GetCurrentViewModel());
        Assert.Same(vm, feats.HostScreen);
    }

    [Fact]
    public void Selecting_another_nav_item_resets_to_that_page()
    {
        var (vm, _, _, combat, _) = Create();

        vm.SelectedNavItem = vm.NavItems[2]; // 战斗

        Assert.Same(combat, vm.Router.GetCurrentViewModel());
        Assert.Single(vm.Router.NavigationStack); // NavigateAndReset keeps a single entry
        Assert.Same(vm, combat.HostScreen);
    }

    [Fact]
    public void Selecting_monsters_navigates_to_the_monster_page()
    {
        var (vm, _, _, _, monsters) = Create();

        vm.SelectedNavItem = vm.NavItems[3]; // 怪物

        Assert.Same(monsters, vm.Router.GetCurrentViewModel());
        Assert.Same(vm, monsters.HostScreen);
    }

    private static (MainWindowViewModel Vm, SpellsViewModel Spells, FeatsViewModel Feats, CombatViewModel Combat, MonstersViewModel Monsters) Create()
    {
        var spells = new SpellsViewModel(new FakeSpellService());
        var feats = new FeatsViewModel(new FakeFeatService());
        var combat = new CombatViewModel(new FakeCharacterRepository(), new FakeSpellService(), new FakeFeatService());
        var monsters = new MonstersViewModel(new FakeMonsterService());
        var vm = new MainWindowViewModel(() => spells, () => feats, () => combat, () => monsters);
        return (vm, spells, feats, combat, monsters);
    }
}
