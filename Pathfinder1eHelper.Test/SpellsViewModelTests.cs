using System.Windows.Input;
using Pathfinder1eHelper.Models;
using Pathfinder1eHelper.Services;
using Pathfinder1eHelper.ViewModels.Pages;
using ReactiveUI.Primitives;   // Subscribe

namespace Pathfinder1eHelper.Test;

/// <summary>
/// View-model tests. ReactiveUI is initialized once for the assembly in <see cref="TestInit"/>
/// with an immediate main-thread scheduler, so command output is delivered synchronously.
/// </summary>
public class SpellsViewModelTests
{
    [Fact]
    public void Defaults_source_filter_to_all_sentinel()
    {
        var vm = new SpellsViewModel(new FakeSpellService());

        Assert.Equal(SpellsViewModel.AllSources, vm.SelectedSource);
        Assert.Contains(SpellsViewModel.AllSources, vm.Sources);
    }

    [Fact]
    public async Task Executing_search_populates_spells_and_selects_first()
    {
        var service = new FakeSpellService();
        service.Results.Add(new Spell { Id = 1, NameEn = "Acid Splash", NameZh = "酸液飞溅" });
        service.Results.Add(new Spell { Id = 2, NameEn = "Bless", NameZh = "祝福术" });

        var vm = new SpellsViewModel(service);
        using var activation = vm.Activator.Activate();

        var completed = new TaskCompletionSource();
        using var subscription = vm.SearchCommand.Subscribe(_ => completed.TrySetResult());
        ((ICommand)vm.SearchCommand).Execute(new SpellQuery(null, null, null, 0, 200));
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(2, vm.Spells.Count);
        Assert.NotNull(vm.SelectedSpell);
        Assert.Equal(1, vm.SelectedSpell!.Id);
        Assert.Equal("共 2 条法术", vm.ResultSummary);
    }

    [Fact]
    public async Task Activated_pipeline_maps_all_sources_sentinel_to_null_source_filter()
    {
        var service = new FakeSpellService();
        var vm = new SpellsViewModel(service) { SelectedSource = SpellsViewModel.AllSources };

        var completed = new TaskCompletionSource();
        using var subscription = vm.SearchCommand.Subscribe(_ => completed.TrySetResult());

        // Activating drives the debounced pipeline, which maps the "(全部)" sentinel to a null
        // source filter before invoking the search command.
        using var activation = vm.Activator.Activate();
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.NotNull(service.LastQuery);
        Assert.Null(service.LastQuery!.Source);
    }

    [Fact]
    public async Task Clear_filters_resets_every_spell_filter_and_runs_one_default_search()
    {
        var service = new FakeSpellService();
        var vm = new SpellsViewModel(service)
        {
            SearchText = "火球",
            SelectedSource = "CRB",
            SelectedLetter = "F",
            SelectedClass = "法师",
            SelectedLevel = "3",
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
        Assert.Equal(SpellsViewModel.AllSources, vm.SelectedSource);
        Assert.Equal(SpellsViewModel.AllLetters, vm.SelectedLetter);
        Assert.Equal(SpellsViewModel.AllClasses, vm.SelectedClass);
        Assert.Equal(SpellsViewModel.AllLevels, vm.SelectedLevel);
        Assert.Equal(2, service.SearchCallCount);
        Assert.NotNull(service.LastQuery);
        Assert.Null(service.LastQuery!.Term);
        Assert.Null(service.LastQuery.Source);
        Assert.Null(service.LastQuery.FirstLetter);
        Assert.Null(service.LastQuery.ClassName);
        Assert.Null(service.LastQuery.ClassLevel);
        Assert.Equal(0, service.LastQuery.Skip);
        Assert.Equal(SpellsViewModel.DefaultPageSize, service.LastQuery.Take);
        Assert.False(vm.ClearFiltersCommand.CanExecute(null));
    }

    [Fact]
    public async Task Load_more_expands_the_page_and_reports_the_true_total()
    {
        var service = new FakeSpellService();
        for (var id = 1; id <= Pagination.DefaultPageSize + 1; id++)
        {
            service.Results.Add(new Spell { Id = id, NameEn = $"Spell {id}" });
        }
        var vm = new SpellsViewModel(service);
        using var activation = vm.Activator.Activate();

        // 首批 200 条后仍有结果，模拟列表到底部时加载下一段。
        var firstPage = new TaskCompletionSource();
        using var subscription = vm.SearchCommand.Subscribe(_ => firstPage.TrySetResult());
        ((ICommand)vm.SearchCommand).Execute(new SpellQuery(null, null, null, 0, Pagination.DefaultPageSize));
        await firstPage.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var selectedBeforeLoad = vm.SelectedSpell;
        Assert.Equal(Pagination.DefaultPageSize, vm.Spells.Count);
        Assert.True(vm.HasMoreResults);
        Assert.Contains($"共 {Pagination.DefaultPageSize + 1} 条", vm.ResultSummary);

        // 加载更多时追加结果，不替换已有项或重置选择。
        var loaded = TestSignals.WaitForPropertyAsync(
            vm,
            nameof(SpellsViewModel.HasMoreResults),
            () => !vm.HasMoreResults && vm.Spells.Count == Pagination.DefaultPageSize + 1);
        ((ICommand)vm.LoadMoreCommand).Execute(null);
        await loaded;

        Assert.Equal(Pagination.DefaultPageSize + 1, vm.Spells.Count);
        Assert.Same(selectedBeforeLoad, vm.SelectedSpell);
        Assert.Equal(Enumerable.Range(1, Pagination.DefaultPageSize + 1), vm.Spells.Select(spell => spell.Id));
        Assert.Equal(Pagination.DefaultPageSize, service.LastQuery!.Skip);
        Assert.Equal(Pagination.PageSizeStep, service.LastQuery.Take);
        Assert.False(vm.HasMoreResults);
    }
}
