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
    public async Task Load_more_expands_the_page_and_reports_the_true_total()
    {
        var service = new FakeFeatService();
        service.Results.Add(new Feat { Id = 1, NameEn = "Acrobatic", NameZh = "特技专家" });
        service.Results.Add(new Feat { Id = 2, NameEn = "Dodge", NameZh = "闪避" });
        service.Results.Add(new Feat { Id = 3, NameEn = "Iron Will", NameZh = "钢铁意志" });
        var vm = new FeatsViewModel(service);
        using var activation = vm.Activator.Activate();

        var firstPage = new TaskCompletionSource();
        using var subscription = vm.SearchCommand.Subscribe(_ => firstPage.TrySetResult());
        ((ICommand)vm.SearchCommand).Execute(new FeatQuery(null, null, null, null, 0, 2));
        await firstPage.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(2, vm.Feats.Count);
        Assert.True(vm.HasMoreResults);
        Assert.Contains("共 3 条", vm.ResultSummary);

        ((ICommand)vm.LoadMoreCommand).Execute(null);
        await Task.Delay(100);

        Assert.Equal(3, vm.Feats.Count);
        Assert.False(vm.HasMoreResults);
    }
}
