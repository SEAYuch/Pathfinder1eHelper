using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.VisualTree;
using Pathfinder1eHelper.Models.Combat;
using Pathfinder1eHelper.ViewModels.Combat;
using Pathfinder1eHelper.Views.Pages;

namespace Pathfinder1eHelper.Test;

/// <summary>
/// 战斗页 XAML 的加载期校验。<c>StaticResource</c> 只在加载时解析、编译期不检查，
/// 所以「构建通过」不能证明模板引用有效——必须真正构造视图。
/// </summary>
[Collection(AvaloniaUiCollection.Name)]
public class CombatViewTests
{
    [Fact]
    public void View_loads_and_resolves_every_static_resource()
    {
        // 缺少 x:Key 的 StaticResource 会在加载时抛 KeyNotFoundException，
        // 所以构造成功本身就是「三处 ItemTemplate 引用都解析到了」的证据。
        var view = new CombatView();

        Assert.NotNull(view.Content);
        Assert.IsType<DataTemplate>(view.Resources["ContributionTemplate"]);
    }

    [Fact]
    public void Combat_options_expose_valid_generator_ranges()
    {
        Assert.Equal(2, CombatOptions.PenaltySources.Count);
        Assert.Contains(CombatOptions.PenaltySources, option => option.Value == PenaltySource.WeaponAttack);
        Assert.Contains(CombatOptions.PenaltySources, option => option.Value == PenaltySource.ArmorClass);
        Assert.Equal(2, CombatOptions.BonusDestinations.Count);
        Assert.Equal(20, CombatOptions.Multipliers.Count);
        Assert.Equal("0.5", CombatOptions.Multipliers[0]);
        Assert.Equal("10.0", CombatOptions.Multipliers[^1]);
    }

    [Fact]
    public void Contribution_template_keeps_the_compact_row_shape()
    {
        // 从三处内联模板抽成共享资源后，这份形状（标签伸展 + 数值定宽靠右）
        // 仍是战斗卡与武器卡共用的排版契约。
        var built = Build(new Contribution("基础", 10, Note: "BAB 5 + 力量 5"));

        var grid = Assert.IsType<Grid>(built);
        Assert.Equal(2, grid.ColumnDefinitions.Count);

        var textBlocks = built.GetVisualDescendants().OfType<TextBlock>().ToList();
        Assert.Equal(2, textBlocks.Count);
        Assert.Equal(1, Grid.GetColumn(textBlocks[1]));
        Assert.Equal("Consolas", textBlocks[1].FontFamily?.Name);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Contribution_template_builds_for_enabled_and_disabled_rows(bool included)
    {
        var built = Build(new Contribution("猛力攻击", -2, Included: included, Note: "说明"));

        Assert.IsType<Grid>(built);
    }

    /// <summary>
    /// <c>IDataTemplate.Build</c> 只负责实例化；编译后的绑定要挂到有根的可视树上才求值，
    /// 而 headless 宿主不做渲染合成，故这里只验证模板结构与可实例化性，不断言绑定后的文本。
    /// </summary>
    private static Control Build(Contribution contribution)
    {
        var view = new CombatView();
        var template = (IDataTemplate)view.Resources["ContributionTemplate"]!;

        var built = (Control)template.Build(contribution)!;
        built.Measure(new Size(400, 100));
        built.Arrange(new Rect(0, 0, 400, 100));
        return built;
    }
}
