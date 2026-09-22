using Pathfinder1eHelper.Services;
using Pathfinder1eHelper.ViewModels;
using Pathfinder1eHelper.ViewModels.Pages;
using Pathfinder1eHelper.Views;
using Pathfinder1eHelper.Views.Pages;

namespace Pathfinder1eHelper.Test;

/// <summary>显式映射 ViewLocator：命中、未命中，以及泛型/实例两种入口的一致性。</summary>
public class ViewLocatorTests
{
    private readonly ViewLocator _locator = new();

    [Fact]
    public void Resolves_views_for_registered_page_view_models()
    {
        var spells = new SpellsViewModel(new FakeSpellService());
        var feats = new FeatsViewModel(new FakeFeatService());
        var combat = new CombatViewModel(new FakeCharacterRepository(), new FakeSpellService(), new FakeFeatService());

        Assert.IsType<SpellsView>(_locator.ResolveView(spells));
        Assert.IsType<FeatsView>(_locator.ResolveView(feats));
        Assert.IsType<CombatView>(_locator.ResolveView(combat));
        Assert.IsType<SpellsView>(_locator.ResolveView<SpellsViewModel>());
        Assert.IsType<FeatsView>(_locator.ResolveView<FeatsViewModel>());
        Assert.IsType<CombatView>(_locator.ResolveView<CombatViewModel>());
    }

    [Fact]
    public void Unknown_view_model_resolves_to_null()
    {
        Assert.Null(_locator.ResolveView(new object()));
        Assert.Null(_locator.ResolveView((object?)null));
        Assert.Null(_locator.ResolveView<MainWindowViewModel>()); // shell 不进映射
    }
}
