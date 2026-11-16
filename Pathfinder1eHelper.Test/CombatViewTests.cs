using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.VisualTree;
using Pathfinder1eHelper.Models.Combat;
using Pathfinder1eHelper.ViewModels.Combat;
using Pathfinder1eHelper.Views.Controls;
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
        Assert.Contains(view.GetLogicalDescendants().OfType<TKCode.Arc.Input>(), input => input.Label == "名称");
        var arcButtons = view.GetLogicalDescendants().OfType<TKCode.Arc.Button>().ToList();
        Assert.Contains(arcButtons, button => button.Content?.ToString() == "添加修饰");
        Assert.Contains(arcButtons, button => button.Content?.ToString() == "添加武器");
        Assert.Contains(arcButtons, button => button.Content?.ToString() == "添加两条修饰");
        Assert.Contains(arcButtons, button => button.Content?.ToString() == "新建" && button.Variant == TKCode.Arc.ButtonVariant.Primary);
        Assert.Contains(arcButtons, button => button.Content?.ToString() == "添加修饰" && button.Variant == TKCode.Arc.ButtonVariant.Primary);
        // 数值字段全部迁到 Arc NumberField（原生 NumericUpDown 已清空）。
        Assert.Empty(view.GetLogicalDescendants().OfType<NumericUpDown>());
        var numberField = Assert.Single(view.GetLogicalDescendants().OfType<TKCode.Arc.NumberField>(),
            field => field.Label is "减值幅度（生成 −N）");
        Assert.True(numberField.IsLabelVisible);
        Assert.Equal("减值幅度", Avalonia.Automation.AutomationProperties.GetName(numberField));
        Assert.Equal(0m, numberField.Minimum);
        Assert.Equal(11m, numberField.Maximum);
        Assert.Equal(1m, numberField.Step);
        Assert.False(numberField.IsScrubEnabled);
        var fixedChoiceSelects = view.GetLogicalDescendants().OfType<TKCode.Arc.Select>().ToList();
        Assert.Contains(fixedChoiceSelects, select => select.Label == "施法属性");
        Assert.Contains(fixedChoiceSelects, select => select.Label == "体型");
        Assert.Contains(fixedChoiceSelects, select => select.Label == "减值来源");
        Assert.Contains(fixedChoiceSelects, select => select.Label == "加值去处");
        Assert.Contains(fixedChoiceSelects, select => select.Label == "情境");
        var combatSelectors = view.GetLogicalDescendants().OfType<TKCode.Arc.Combobox>().ToList();
        Assert.Contains(combatSelectors, selector =>
            selector.Label is null &&
            Avalonia.Automation.AutomationProperties.GetName(selector) == "档案");
    }

    [Fact]
    public void Stat_cards_pack_side_by_side_in_their_fixed_width_slots()
    {
        var view = new CombatView();
        var cards = Assert.IsType<ItemsControl>(view.FindControl<ItemsControl>("StatCards"));
        var panel = Assert.IsType<WrapPanel>(cards.ItemsPanel!.Build());
        var itemTemplate = Assert.IsAssignableFrom<IDataTemplate>(cards.ItemTemplate);

        var first = Assert.IsAssignableFrom<Control>(itemTemplate.Build(
            new StatCard("防御等级 AC", new StatResult(10, []))));
        var second = Assert.IsAssignableFrom<Control>(itemTemplate.Build(
            new StatCard("强韧", new StatResult(5, []))));
        panel.Children.Add(first);
        panel.Children.Add(second);

        panel.Measure(new Size(700, 600));
        panel.Arrange(new Rect(0, 0, 700, 600));

        // 一张卡一个 220 槽位：并排而不是各占一行。
        Assert.Equal(220, first.Bounds.Width);
        Assert.Equal(220, second.Bounds.Width);
        Assert.Equal(0, first.Bounds.X);
        Assert.Equal(220, second.Bounds.X);
    }

    [Fact]
    public void Combat_stat_cards_use_arc_expandable_cards_in_fixed_width_slots()
    {
        var view = new CombatView();
        var cards = Assert.IsType<ItemsControl>(view.FindControl<ItemsControl>("StatCards"));

        // 紧凑来自面板：每张卡一个固定的 220 槽位，因此多张并排、展开只长高。
        var panel = Assert.IsType<WrapPanel>(cards.ItemsPanel!.Build());
        Assert.Equal(220, panel.ItemWidth);

        var itemTemplate = Assert.IsAssignableFrom<IDataTemplate>(cards.ItemTemplate);
        var card = Assert.IsAssignableFrom<TKCode.Arc.ExpandableCard>(itemTemplate.Build(
            new StatCard("攻击", new StatResult(7, [])))
            as Control);
        // 用的就是 Arc 原版卡片，宽度与槽位一致。
        Assert.Equal(typeof(TKCode.Arc.ExpandableCard), card.GetType());
        Assert.Equal(220, card.CollapsedWidth);

        // 这一段不再用原生 Expander。
        Assert.Empty(view.GetLogicalDescendants().OfType<Expander>());
    }

    [Fact]
    public void Combat_sections_have_visible_but_subtler_outlines_than_item_cards()
    {
        var view = new CombatView();
        var scrollViewer = Assert.IsType<ScrollViewer>(view.Content);
        var sections = Assert.IsType<StackPanel>(scrollViewer.Content).Children.OfType<Border>().ToList();

        Assert.Equal(8, sections.Count);
        Assert.All(sections, section => Assert.Equal(new Thickness(1.25), section.BorderThickness));

        var modifierItems = Assert.IsType<ItemsControl>(view.FindControl<ItemsControl>("BonusEntries"));
        var modifier = new BonusEntryViewModel(new ModifierEntry { Name = "测试修饰" }, () => { }, _ => { });
        var modifierCard = Assert.IsType<Border>(modifierItems.ItemTemplate!.Build(modifier));
        Assert.True(sections[4].BorderThickness.Left < modifierCard.BorderThickness.Left);
    }

    [Fact]
    public void Destructive_combat_actions_use_arc_hold_to_confirm()
    {
        var view = new CombatView();
        var confirmations = view.GetLogicalDescendants().OfType<TKCode.Arc.HoldToConfirm>().ToList();

        Assert.Collection(confirmations,
            delete =>
            {
                Assert.Equal("按住以删除角色档案", delete.Label);
                Assert.Equal("已删除", delete.ConfirmedLabel);
                Assert.Equal(TKCode.Arc.HoldToConfirmTone.Danger, delete.Tone);
                Assert.Equal("按住以删除当前角色档案", Avalonia.Automation.AutomationProperties.GetName(delete));
            },
            clear =>
            {
                Assert.Equal("按住以清空全部修饰条目", clear.Label);
                Assert.Equal("已清空", clear.ConfirmedLabel);
                Assert.Equal(TKCode.Arc.HoldToConfirmTone.Danger, clear.Tone);
                Assert.Equal("按住以清空全部修饰条目", Avalonia.Automation.AutomationProperties.GetName(clear));
            });
    }

    [Fact]
    public void Weapon_detail_breakdown_uses_arc_expandable_card()
    {
        var view = new CombatView();
        var weaponItems = Assert.IsType<ItemsControl>(view.FindControl<ItemsControl>("Weapons"));
        var weapon = new WeaponViewModel(new WeaponProfile { Name = "测试武器" }, () => { }, _ => { });
        var weaponCard = Assert.IsAssignableFrom<Control>(weaponItems.ItemTemplate!.Build(weapon));

        var details = Assert.Single(weaponCard.GetLogicalDescendants().OfType<TKCode.Arc.ExpandableCard>());
        Assert.Equal("攻击与伤害组成明细", Avalonia.Automation.AutomationProperties.GetName(details));
        Assert.False(details.IsExpanded);
        Assert.Empty(weaponCard.GetLogicalDescendants().OfType<Expander>());
        Assert.Equal(2, details.Content is Control content
            ? content.GetLogicalDescendants().OfType<ItemsControl>().Count()
            : 0);
    }

    [Fact]
    public void Item_templates_keep_modifier_delete_direct_and_confirm_weapon_delete()
    {
        var view = new CombatView();
        var bonusItems = Assert.IsType<ItemsControl>(view.FindControl<ItemsControl>("BonusEntries"));
        var weaponItems = Assert.IsType<ItemsControl>(view.FindControl<ItemsControl>("Weapons"));
        var bonus = new BonusEntryViewModel(new ModifierEntry { Name = "测试修饰" }, () => { }, _ => { });
        var weapon = new WeaponViewModel(new WeaponProfile { Name = "测试武器" }, () => { }, _ => { });

        var bonusCard = Assert.IsAssignableFrom<Control>(bonusItems.ItemTemplate!.Build(bonus));
        var weaponCard = Assert.IsAssignableFrom<Control>(weaponItems.ItemTemplate!.Build(weapon));

        var modifierDelete = Assert.Single(
            bonusCard.GetLogicalDescendants().OfType<TKCode.Arc.Button>(),
            button => button.Content?.ToString() == "✕");
        Assert.Equal(TKCode.Arc.ButtonVariant.Danger, modifierDelete.Variant);
        Assert.Equal("删除修饰条目", Avalonia.Automation.AutomationProperties.GetName(modifierDelete));
        Assert.Empty(bonusCard.GetLogicalDescendants().OfType<TKCode.Arc.HoldToConfirm>());
        AssertHoldToConfirmRemoval(weaponCard, "按住以删除此武器");

    }

    [Fact]
    public void Modifier_and_weapon_cards_use_stronger_arc_outlines()
    {
        var view = new CombatView();
        var bonusItems = Assert.IsType<ItemsControl>(view.FindControl<ItemsControl>("BonusEntries"));
        var weaponItems = Assert.IsType<ItemsControl>(view.FindControl<ItemsControl>("Weapons"));
        var bonus = new BonusEntryViewModel(new ModifierEntry { Name = "测试修饰" }, () => { }, _ => { });
        var weapon = new WeaponViewModel(new WeaponProfile { Name = "测试武器" }, () => { }, _ => { });
        var bonusCard = Assert.IsAssignableFrom<Control>(bonusItems.ItemTemplate!.Build(bonus));
        var weaponCard = Assert.IsAssignableFrom<Control>(weaponItems.ItemTemplate!.Build(weapon));

        AssertCardOutline(bonusCard);
        AssertCardOutline(weaponCard);
    }

    [Fact]
    public void Hold_to_confirm_can_be_returned_to_resting_state()
    {
        var view = new CombatView();
        var confirmation = Assert.Single(
            view.GetLogicalDescendants().OfType<TKCode.Arc.HoldToConfirm>(),
            control => control.Label == "按住以清空全部修饰条目");
        confirmation.IsConfirmed = true;

        CombatView.ResetConfirmation(confirmation);

        Assert.False(confirmation.IsConfirmed);
    }

    [Fact]
    public void Arc_combobox_clear_keeps_nullable_selection_available()
    {
        var selector = new TKCode.Arc.Combobox
        {
            ItemsSource = CombatOptions.Abilities,
            SelectedItem = CombatOptions.Abilities[0],
        };

        selector.Clear();

        Assert.Null(selector.SelectedItem);
    }

    [Fact]
    public void Combat_view_selectors_are_all_arc_controls()
    {
        var view = new CombatView();

        // 迁移完成后战斗页不再有原生 ComboBox（档案/属性/命中属性/伤害属性都是 Arc Combobox）。
        Assert.Empty(view.GetLogicalDescendants().OfType<ComboBox>());

        var arcSelectors = view.GetLogicalDescendants().OfType<TKCode.Arc.Combobox>().ToList();
        Assert.Contains(arcSelectors, selector =>
            selector.Label is null &&
            Avalonia.Automation.AutomationProperties.GetName(selector) == "档案");
        Assert.All(arcSelectors, selector =>
            Assert.False(string.IsNullOrWhiteSpace(Avalonia.Automation.AutomationProperties.GetName(selector))));
    }

    [Fact]
    public void Ability_scores_use_arc_number_fields()
    {
        var view = new CombatView();
        var fields = view.GetLogicalDescendants().OfType<TKCode.Arc.NumberField>().ToList();

        Assert.Empty(view.GetLogicalDescendants().OfType<TKCode.Arc.ScrubInput>());

        // 六项属性各一个 NumberField（另有「减值幅度」也是 NumberField，故不按总数断言）。
        Assert.All(new[] { "力量", "敏捷", "体质", "智力", "感知", "魅力" }, name =>
            Assert.Contains(fields, field => Avalonia.Automation.AutomationProperties.GetName(field) == name));
    }

    [Fact]
    public void Combat_view_checkboxes_use_arc_and_have_accessible_names()
    {
        var view = new CombatView();
        var checkBoxes = view.GetLogicalDescendants().OfType<TKCode.Arc.Checkbox>().ToList();

        Assert.NotEmpty(checkBoxes);
        Assert.All(checkBoxes, checkBox =>
            Assert.False(string.IsNullOrWhiteSpace(Avalonia.Automation.AutomationProperties.GetName(checkBox))));
    }

    [Fact]
    public void Concentration_result_is_plain_text_aligned_to_the_field_row()
    {
        // headless 宿主不求值编译绑定（`AvaloniaUseCompiledBindingsByDefault`），
        // 所以这里只锁定结构：结果仍是 TextBlock，外面套 44px（Arc 字段框 Medium 高度）
        // 的底对齐容器——绑定写错会在编译期失败，不需要运行时断言。
        var view = new CombatView();
        var result = Assert.Single(
            view.GetLogicalDescendants().OfType<TextBlock>(),
            text => Avalonia.Automation.AutomationProperties.GetName(text) == "专注检定结果");
        Assert.Equal(VerticalAlignment.Center, result.VerticalAlignment);

        var frame = Assert.IsType<Border>(result.Parent);
        Assert.Equal(44, frame.Height);
        Assert.Equal(VerticalAlignment.Bottom, frame.VerticalAlignment);

        // 不再用输入控件承载派生值。
        Assert.DoesNotContain(
            view.GetLogicalDescendants().OfType<TKCode.Arc.Input>(),
            input => Avalonia.Automation.AutomationProperties.GetName(input) == "专注检定结果");
    }

    [Fact]
    public void Combat_view_numeric_inputs_are_arc_number_fields_with_accessible_names()
    {
        var view = new CombatView();
        var numberFields = view.GetLogicalDescendants().OfType<TKCode.Arc.NumberField>().ToList();

        Assert.Empty(view.GetLogicalDescendants().OfType<NumericUpDown>());
        Assert.NotEmpty(numberFields);
        Assert.All(numberFields, field =>
            Assert.False(string.IsNullOrWhiteSpace(Avalonia.Automation.AutomationProperties.GetName(field))));

        // 基础数值里的派生数值统一 0–40（属性不在此列，仍是 1–99）。
        string[] baseStatFields =
        [
            "基本攻击加值（BAB）", "强韧基础", "反射基础", "意志基础", "施法者等级", "最大敏捷加值",
        ];
        Assert.All(baseStatFields, name =>
            Assert.Contains(numberFields, field =>
                Avalonia.Automation.AutomationProperties.GetName(field) == name &&
                field.Minimum == 0m && field.Maximum == 40m));

        // 基础数值的标签统一用 Arc 控件自带的 Label（与 Select/Combobox 同一套排版），不再叠自绘 TextBlock。
        Assert.All(baseStatFields, name =>
            Assert.Contains(numberFields, field =>
                Avalonia.Automation.AutomationProperties.GetName(field) == name &&
                field.IsLabelVisible && field.Label is not null));

        Assert.All(new[] { "力量", "敏捷", "体质", "智力", "感知", "魅力" }, name =>
            Assert.Contains(numberFields, field =>
                Avalonia.Automation.AutomationProperties.GetName(field) == name &&
                field.Minimum == 1m && field.Maximum == 99m));
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
    private static void AssertHoldToConfirmRemoval(Control card, string label)
    {
        var confirmation = Assert.Single(card.GetLogicalDescendants().OfType<TKCode.Arc.HoldToConfirm>());
        Assert.Equal(label, confirmation.Label);
        Assert.Equal("已删除", confirmation.ConfirmedLabel);
        Assert.Equal(TKCode.Arc.HoldToConfirmTone.Danger, confirmation.Tone);
        Assert.Equal(label, Avalonia.Automation.AutomationProperties.GetName(confirmation));
    }

    private static void AssertCardOutline(Control card)
    {
        var border = Assert.IsType<Border>(card);
        Assert.Equal(new Thickness(1.5), border.BorderThickness);
    }

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
