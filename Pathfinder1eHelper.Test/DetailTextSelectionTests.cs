using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Pathfinder1eHelper.Models;
using Pathfinder1eHelper.Views.Controls;
using Pathfinder1eHelper.Views.Pages;

namespace Pathfinder1eHelper.Test;

/// <summary>
/// 详情面板的文本必须可选中复制(车卡时要能粘到角色表里)。
/// 断言按结构做:<c>IDataTemplate.Build</c> 只实例化不求值,headless 宿主也不做渲染合成,
/// 所以这里数「详情里有几个 <see cref="SelectableTextBlock"/>」,而不是断言绑定后的文本。
/// </summary>
[Collection(AvaloniaUiCollection.Name)]
public sealed class DetailTextSelectionTests
{
    [Fact]
    public void Spell_detail_text_is_selectable()
    {
        var detail = BuildDetailTemplate(new SpellsView(), new Spell());

        // 数据区:标题 2 + 字段值 11 + 描述 1 + 附加/法术类型/出处 3 = 17。
        Assert.Equal(17, detail.Descendants<SelectableTextBlock>().Count());
    }

    [Fact]
    public void Feat_detail_text_is_selectable()
    {
        var detail = BuildDetailTemplate(new FeatsView(), new Feat());

        // 数据区:标题 2 + 字段值 4 + 效果正文 1 + 风味 1 + 出处 1 = 9。
        Assert.Equal(9, detail.Descendants<SelectableTextBlock>().Count());
    }

    /// <summary>怪物详情直接绑在页面 XAML 上(没有 ContentTemplate),所以直接数已加载的视图。</summary>
    [Fact]
    public void Monster_detail_text_is_selectable()
    {
        var view = new MonstersView();

        // 怪物详情:标题 2 + CR/体型/类型/亚种/阵营/属性 6 + 环境/组织/宝物 3 + 描述 1 = 12;
        // 绪论详情:标题 2。共 14。
        Assert.Equal(14, view.Descendants<SelectableTextBlock>().Count());
    }

    /// <summary>详情里的普通标签(如「学派」「类型」)保持不可选,避免 Tab 停靠在一堆标签上。</summary>
    [Fact]
    public void Detail_labels_stay_plain_text_blocks()
    {
        var detail = BuildDetailTemplate(new SpellsView(), new Spell());
        var labels = detail.Descendants<TextBlock>().Where(block => block is not SelectableTextBlock).ToList();

        // 11 个字段标签(学派…法术抗力) + 3 个附属标签(附加/法术类型/出处)。
        Assert.Equal(14, labels.Count);
        Assert.All(labels, label => Assert.True(label.IsVisible));
    }

    /// <summary>怪物正文由代码构建(不走 XAML 模板),段落与表格单元格都要可选中。</summary>
    [Fact]
    public void Monster_text_view_renders_selectable_paragraphs_and_cells()
    {
        var view = new MonsterTextView
        {
            Text = "第一段原文。\n\n| 属性 | 数值 |\n| --- | --- |\n| 力量 | 18 |",
        };

        var textBlocks = view.Descendants<TextBlock>().ToList();

        // 段落 1 个 + 表头 2 格 + 数据行 2 格 = 5。
        Assert.Equal(5, textBlocks.Count);
        Assert.All(textBlocks, block => Assert.IsType<SelectableTextBlock>(block));
    }

    private static Control BuildDetailTemplate(Control page, object model)
    {
        var contentControl = Assert.Single(page.GetLogicalDescendants().OfType<ContentControl>(),
            control => control.ContentTemplate is not null);
        var template = Assert.IsAssignableFrom<IDataTemplate>(contentControl.ContentTemplate);
        return Assert.IsAssignableFrom<Control>(template.Build(model));
    }
}

internal static class LogicalTreeQuery
{
    internal static IEnumerable<T> Descendants<T>(this Control control) where T : Control =>
        control.GetLogicalDescendants().OfType<T>();
}
