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
    public async Task Clear_filters_resets_every_monster_filter_and_runs_one_default_search()
    {
        var service = new FakeMonsterService();
        var vm = new MonstersViewModel(service)
        {
            SearchText = "龙",
            SelectedLetter = "C",
            SelectedCreatureType = "龙类",
        };
        using var activation = vm.Activator.Activate();

        var initialSearch = new TaskCompletionSource();
        using var initialSubscription = vm.SearchCommand.Subscribe(_ => initialSearch.TrySetResult());
        await initialSearch.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(1, service.SearchCallCount);
        Assert.True(vm.ClearFiltersCommand.CanExecute(null));

        var resetSearch = new TaskCompletionSource();
        using var resetSubscription = vm.SearchCommand.Subscribe(_ => resetSearch.TrySetResult());
        ((ICommand)vm.ClearFiltersCommand).Execute(null);
        await resetSearch.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Null(vm.SearchText);
        Assert.Equal(MonstersViewModel.AllLetters, vm.SelectedLetter);
        Assert.Equal(MonstersViewModel.AllTypes, vm.SelectedCreatureType);
        Assert.Equal(2, service.SearchCallCount);
        Assert.NotNull(service.LastQuery);
        Assert.Null(service.LastQuery!.Term);
        Assert.Null(service.LastQuery.FirstLetter);
        Assert.Null(service.LastQuery.CreatureType);
        Assert.Equal(0, service.LastQuery.Skip);
        Assert.Equal(MonstersViewModel.DefaultPageSize, service.LastQuery.Take);
        Assert.False(vm.ClearFiltersCommand.CanExecute(null));
    }

    [Fact]
    public async Task Opening_a_group_loads_its_members()
    {
        var service = new FakeMonsterService();
        service.GroupMembers.Add(new Monster { Id = 1, NameZh = "彩色龙，黑龙", NameEn = "Chromatic Dragon, Black" });
        var group = new MonsterGroup { Id = 1, NameZh = "龙类绪论", NameEn = "Dragon" };

        var vm = new MonstersViewModel(service);
        using var activation = vm.Activator.Activate();

        var opened = TestSignals.WaitForCollectionAsync(vm.GroupMembers, () => vm.GroupMembers.Count == 1);
        ((ICommand)vm.OpenGroupCommand).Execute(group);
        await opened;

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

        var loaded = TestSignals.WaitForCollectionAsync(vm.RelatedGroups, () => vm.RelatedGroups.Count == 1);
        vm.SelectedMonster = new Monster { Id = 1, NameZh = "彩色龙，黑龙" };
        await loaded;

        Assert.Single(vm.RelatedGroups);
        Assert.Equal("龙类绪论", vm.RelatedGroups[0].NameZh);
    }

    [Fact]
    public async Task Load_more_expands_the_page_and_reports_the_true_total()
    {
        var service = new FakeMonsterService();
        for (var id = 1; id <= Pagination.DefaultPageSize + 1; id++)
        {
            service.Results.Add(new Monster { Id = id, NameZh = $"怪物{id}", NameEn = $"Monster {id}" });
        }
        var vm = new MonstersViewModel(service);
        using var activation = vm.Activator.Activate();

        var firstPage = new TaskCompletionSource();
        using var subscription = vm.SearchCommand.Subscribe(_ => firstPage.TrySetResult());
        ((ICommand)vm.SearchCommand).Execute(new MonsterQuery(null, null, null, 0, Pagination.DefaultPageSize));
        await firstPage.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var selectedBeforeLoad = vm.SelectedMonster;
        Assert.Equal(Pagination.DefaultPageSize, vm.Monsters.Count);
        Assert.True(vm.HasMoreResults);
        Assert.Contains($"共 {Pagination.DefaultPageSize + 1} 条", vm.ResultSummary);

        var loaded = TestSignals.WaitForPropertyAsync(
            vm,
            nameof(MonstersViewModel.HasMoreResults),
            () => !vm.HasMoreResults && vm.Monsters.Count == Pagination.DefaultPageSize + 1);
        ((ICommand)vm.LoadMoreCommand).Execute(null);
        await loaded;

        Assert.Equal(Pagination.DefaultPageSize + 1, vm.Monsters.Count);
        Assert.Same(selectedBeforeLoad, vm.SelectedMonster);
        Assert.Equal(Enumerable.Range(1, Pagination.DefaultPageSize + 1), vm.Monsters.Select(monster => monster.Id));
        Assert.Equal(Pagination.DefaultPageSize, service.LastQuery!.Skip);
        Assert.Equal(Pagination.PageSizeStep, service.LastQuery.Take);
        Assert.False(vm.HasMoreResults);
    }
}
