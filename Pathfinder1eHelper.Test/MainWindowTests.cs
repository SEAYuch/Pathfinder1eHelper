using Pathfinder1eHelper.ViewModels;
using Pathfinder1eHelper.ViewModels.Pages;

namespace Pathfinder1eHelper.Test;

public class MainWindowTests
{
    [Fact]
    public void Sidebar_items_preserve_nav_labels_keys_enabled_state_and_route_tags()
    {
        var vm = new MainWindowViewModel(
            () => new SpellsViewModel(new FakeSpellService()),
            () => new FeatsViewModel(new FakeFeatService()),
            () => new CombatViewModel(new FakeCharacterRepository()),
            () => new MonstersViewModel(new FakeMonsterService()));

        Assert.Equal(vm.NavItems.Select(item => item.Key), vm.SidebarItems.Select(item => item.Key));
        Assert.Equal(vm.NavItems.Select(item => item.Header), vm.SidebarItems.Select(item => item.Label));
        Assert.Equal(vm.NavItems.Select(item => item.IsEnabled), vm.SidebarItems.Select(item => item.IsEnabled));
        Assert.Same(vm.NavItems[0], vm.SidebarItems[0].Tag);
    }
}
