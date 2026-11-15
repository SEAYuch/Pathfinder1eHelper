using System.Windows.Input;
using Pathfinder1eHelper.Models;
using Pathfinder1eHelper.Services;
using Pathfinder1eHelper.ViewModels.Pages;
using ReactiveUI.Primitives;

namespace Pathfinder1eHelper.Test;

/// <summary>Successful empty searches expose the Arc empty-state flag on each query page.</summary>
public sealed class QueryPageEmptyStateTests
{
    [Fact]
    public async Task Empty_spell_search_sets_no_results()
    {
        var vm = new SpellsViewModel(new FakeSpellService());
        var resultsUpdated = TestSignals.WaitForPropertyAsync(vm, nameof(vm.HasNoResults), () => vm.HasNoResults);
        await ExecuteAsync(vm, new SpellQuery(null, null, null, 0, 200));
        await resultsUpdated;
        Assert.Null(vm.SelectedSpell);
    }

    [Fact]
    public async Task Empty_feat_search_sets_no_results()
    {
        var vm = new FeatsViewModel(new FakeFeatService());
        var resultsUpdated = TestSignals.WaitForPropertyAsync(vm, nameof(vm.HasNoResults), () => vm.HasNoResults);
        await ExecuteAsync(vm, new FeatQuery(null, null, null, null, 0, 200));
        await resultsUpdated;
        Assert.Null(vm.SelectedFeat);
    }

    [Fact]
    public async Task Empty_monster_search_sets_no_results()
    {
        var vm = new MonstersViewModel(new FakeMonsterService());
        var resultsUpdated = TestSignals.WaitForPropertyAsync(vm, nameof(vm.HasNoResults), () => vm.HasNoResults);
        await ExecuteAsync(vm, new MonsterQuery(null, null, null, 0, 200));
        await resultsUpdated;
        Assert.Null(vm.SelectedMonster);
    }

    private static async Task ExecuteAsync(SpellsViewModel vm, SpellQuery query)
    {
        using var activation = vm.Activator.Activate();
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = vm.SearchCommand.Subscribe(_ => completed.TrySetResult());
        ((ICommand)vm.SearchCommand).Execute(query);
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    private static async Task ExecuteAsync(FeatsViewModel vm, FeatQuery query)
    {
        using var activation = vm.Activator.Activate();
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = vm.SearchCommand.Subscribe(_ => completed.TrySetResult());
        ((ICommand)vm.SearchCommand).Execute(query);
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    private static async Task ExecuteAsync(MonstersViewModel vm, MonsterQuery query)
    {
        using var activation = vm.Activator.Activate();
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = vm.SearchCommand.Subscribe(_ => completed.TrySetResult());
        ((ICommand)vm.SearchCommand).Execute(query);
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }
}
