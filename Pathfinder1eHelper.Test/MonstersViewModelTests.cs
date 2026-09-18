using System.Windows.Input;
using Pathfinder1eHelper.Models;
using Pathfinder1eHelper.Services;
using Pathfinder1eHelper.ViewModels.Pages;
using ReactiveUI.Primitives; // Subscribe

namespace Pathfinder1eHelper.Test;

/// <summary>怪物页视图模型：默认哨兵、搜索管道、概论链接与成员。</summary>
public class MonstersViewModelTests
{
    [Fact]
    public void Defaults_filters_to_all_sentinels()
    {
        var vm = new MonstersViewModel(new FakeMonsterService());

        Assert.Equal(MonstersViewModel.AllLetters, vm.SelectedLetter);
        Assert.Equal(MonstersViewModel.AllTypes, vm.SelectedCreatureType);
        Assert.Contains(MonstersViewModel.AllLetters, vm.Letters);
        Assert.Contains(MonstersViewModel.AllTypes, vm.CreatureTypes);
    }

    [Fact]
    public async Task Executing_search_populates_monsters_and_selects_first()
    {
        var service = new FakeMonsterService();
        service.Results.Add(new Monster { Id = 1, NameZh = "底栖魔鱼", NameEn = "Aboleth", FirstLetter = "A" });
        service.Results.Add(new Monster { Id = 2, NameZh = "掘地虫", NameEn = "Ankheg", FirstLetter = "A" });

        var vm = new MonstersViewModel(service);
        using var activation = vm.Activator.Activate();

        var completed = new TaskCompletionSource();
        using var subscription = vm.SearchCommand.Subscribe(_ => completed.TrySetResult());
        ((ICommand)vm.SearchCommand).Execute(new MonsterQuery(null, null, null, 0, 200));
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(2, vm.Monsters.Count);
        Assert.NotNull(vm.SelectedMonster);
        Assert.Equal(1, vm.SelectedMonster!.Id);
        Assert.Equal("共 2 条怪物", vm.ResultSummary);
    }

    [Fact]
    public async Task Activated_pipeline_maps_sentinels_to_null_filters()
    {
        var service = new FakeMonsterService();
        var vm = new MonstersViewModel(service)
        {
            SelectedLetter = MonstersViewModel.AllLetters,
            SelectedCreatureType = MonstersViewModel.AllTypes,
        };

        var completed = new TaskCompletionSource();
        using var subscription = vm.SearchCommand.Subscribe(_ => completed.TrySetResult());

        using var activation = vm.Activator.Activate();
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.NotNull(service.LastQuery);
        Assert.Null(service.LastQuery!.FirstLetter);
        Assert.Null(service.LastQuery.CreatureType);
    }

    [Fact]
    public async Task Opening_a_group_loads_its_members()
    {
        var service = new FakeMonsterService();
        service.GroupMembers.Add(new Monster { Id = 1, NameZh = "彩色龙，黑龙", NameEn = "Chromatic Dragon, Black" });
        var group = new MonsterGroup { Id = 1, NameZh = "龙类绪论", NameEn = "Dragon" };

        var vm = new MonstersViewModel(service);
        using var activation = vm.Activator.Activate();

        ((ICommand)vm.OpenGroupCommand).Execute(group);
        await Task.Delay(50);

        Assert.Same(group, vm.SelectedGroup);
        Assert.Single(vm.GroupMembers);

        ((ICommand)vm.CloseGroupCommand).Execute(null);
        Assert.Null(vm.SelectedGroup);
    }

    [Fact]
    public void Opening_a_group_member_selects_it_even_if_it_is_not_in_the_current_list()
    {
        var service = new FakeMonsterService();
        var member = new Monster { Id = 99, NameZh = "金属龙，金龙", NameEn = "Metallic Dragon, Gold" };
        service.GroupMembers.Add(member);
        var vm = new MonstersViewModel(service);
        using var activation = vm.Activator.Activate();

        ((ICommand)vm.OpenMonsterCommand).Execute(member);

        Assert.Equal(99, vm.SelectedMonster!.Id);
        Assert.Contains(vm.Monsters, m => m.Id == 99);
        Assert.Null(vm.SelectedGroup);
    }

    [Fact]
    public async Task Related_groups_load_when_a_monster_is_selected()
    {
        var service = new FakeMonsterService();
        service.Groups.Add(new MonsterGroup { Id = 1, NameZh = "龙类绪论", NameEn = "Dragon" });
        var vm = new MonstersViewModel(service);
        using var activation = vm.Activator.Activate();

        vm.SelectedMonster = new Monster { Id = 1, NameZh = "彩色龙，黑龙" };
        await Task.Delay(50);

        Assert.Single(vm.RelatedGroups);
        Assert.Equal("龙类绪论", vm.RelatedGroups[0].NameZh);
    }
}
