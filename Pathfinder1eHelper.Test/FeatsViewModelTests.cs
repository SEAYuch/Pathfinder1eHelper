using System.Windows.Input;
using Pathfinder1eHelper.Models;
using Pathfinder1eHelper.Services;
using Pathfinder1eHelper.ViewModels.Pages;
using ReactiveUI.Primitives;   // Subscribe

namespace Pathfinder1eHelper.Test;

/// <summary>专长页视图模型：默认哨兵、搜索管道与分页。</summary>
public class FeatsViewModelTests
{
    [Fact]
    public void Defaults_source_and_type_filters_to_sentinels()
    {
        var vm = new FeatsViewModel(new FakeFeatService());

        Assert.Equal(FeatsViewModel.AllSources, vm.SelectedSource);
        Assert.Equal(FeatsViewModel.AllTypes, vm.SelectedType);
        Assert.Contains(FeatsViewModel.AllSources, vm.Sources);
        Assert.Contains(FeatsViewModel.AllTypes, vm.Types);
    }

    [Fact]
    public async Task Executing_search_populates_feats_and_selects_first()
    {
        var service = new FakeFeatService();
        service.Results.Add(new Feat { Id = 1, NameEn = "Acrobatic", NameZh = "特技专家" });
        service.Results.Add(new Feat { Id = 2, NameEn = "Power Attack", NameZh = "猛力攻击" });

        var vm = new FeatsViewModel(service);
        using var activation = vm.Activator.Activate();

        var completed = new TaskCompletionSource();
        using var subscription = vm.SearchCommand.Subscribe(_ => completed.TrySetResult());
        ((ICommand)vm.SearchCommand).Execute(new FeatQuery(null, null, null, null, 0, 200));
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(2, vm.Feats.Count);
        Assert.NotNull(vm.SelectedFeat);
        Assert.Equal(1, vm.SelectedFeat!.Id);
        Assert.Equal("共 2 条专长", vm.ResultSummary);
    }

    [Fact]
    public async Task Activated_pipeline_maps_sentinels_to_null_filters()
    {
        var service = new FakeFeatService();
        var vm = new FeatsViewModel(service)
        {
            SelectedSource = FeatsViewModel.AllSources,
            SelectedType = FeatsViewModel.AllTypes,
        };

        var completed = new TaskCompletionSource();
        using var subscription = vm.SearchCommand.Subscribe(_ => completed.TrySetResult());

        using var activation = vm.Activator.Activate();
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.NotNull(service.LastQuery);
        Assert.Null(service.LastQuery!.Source);
        Assert.Null(service.LastQuery.Type);
    }

    [Fact]
    public async Task Clear_filters_resets_every_feat_filter_and_runs_one_default_search()
    {
        var service = new FakeFeatService();
        var vm = new FeatsViewModel(service)
        {
            SearchText = "猛力",
            SelectedSource = "CRB",
            SelectedLetter = "P",
            SelectedType = "战斗",
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
        Assert.Equal(FeatsViewModel.AllSources, vm.SelectedSource);
        Assert.Equal(FeatsViewModel.AllLetters, vm.SelectedLetter);
        Assert.Equal(FeatsViewModel.AllTypes, vm.SelectedType);
        Assert.Equal(2, service.SearchCallCount);
        Assert.NotNull(service.LastQuery);
        Assert.Null(service.LastQuery!.Term);
        Assert.Null(service.LastQuery.Source);
        Assert.Null(service.LastQuery.FirstLetter);
        Assert.Null(service.LastQuery.Type);
        Assert.Equal(0, service.LastQuery.Skip);
        Assert.Equal(FeatsViewModel.DefaultPageSize, service.LastQuery.Take);
        Assert.False(vm.ClearFiltersCommand.CanExecute(null));
    }

    [Fact]
    public async Task Load_more_expands_the_page_and_reports_the_true_total()
    {
        var service = new FakeFeatService();
        for (var id = 1; id <= Pagination.DefaultPageSize + 1; id++)
        {
            service.Results.Add(new Feat { Id = id, NameEn = $"Feat {id}" });
        }
        var vm = new FeatsViewModel(service);
        using var activation = vm.Activator.Activate();

        var firstPage = new TaskCompletionSource();
        using var subscription = vm.SearchCommand.Subscribe(_ => firstPage.TrySetResult());
        ((ICommand)vm.SearchCommand).Execute(new FeatQuery(null, null, null, null, 0, Pagination.DefaultPageSize));
        await firstPage.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var selectedBeforeLoad = vm.SelectedFeat;
        Assert.Equal(Pagination.DefaultPageSize, vm.Feats.Count);
        Assert.True(vm.HasMoreResults);
        Assert.Contains($"共 {Pagination.DefaultPageSize + 1} 条", vm.ResultSummary);

        var loaded = TestSignals.WaitForPropertyAsync(
            vm,
            nameof(FeatsViewModel.HasMoreResults),
            () => !vm.HasMoreResults && vm.Feats.Count == Pagination.DefaultPageSize + 1);
        ((ICommand)vm.LoadMoreCommand).Execute(null);
        await loaded;

        Assert.Equal(Pagination.DefaultPageSize + 1, vm.Feats.Count);
        Assert.Same(selectedBeforeLoad, vm.SelectedFeat);
        Assert.Equal(Enumerable.Range(1, Pagination.DefaultPageSize + 1), vm.Feats.Select(feat => feat.Id));
        Assert.Equal(Pagination.DefaultPageSize, service.LastQuery!.Skip);
        Assert.Equal(Pagination.PageSizeStep, service.LastQuery.Take);
        Assert.False(vm.HasMoreResults);
    }
}
