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
}
