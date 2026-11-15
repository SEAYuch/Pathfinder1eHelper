using System.Reflection;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Pathfinder1eHelper.Views.Pages;

namespace Pathfinder1eHelper.Test;

/// <summary>Arc query-page XAML loads and retains its master/detail regions.</summary>
[Collection(AvaloniaUiCollection.Name)]
public sealed class QueryPageViewTests
{
    [Fact]
    public void Spell_page_loads_arc_filters_and_query_status()
    {
        var view = new SpellsView();

        AssertHasArcSearchAndFilters(view, "搜索法术", 3, 1, "清除法术搜索与筛选条件");
        AssertHasStatusRegions(view, "没有找到法术", "搜索法术失败", "法术搜索结果", "正在搜索法术");
        AssertDetailEmptyState(view, "法术详情区域", "未选择法术");
        var table = Assert.Single(view.GetLogicalDescendants().OfType<TKCode.Arc.Table>());
        Assert.Equal("法术搜索结果", AutomationProperties.GetName(table));
        Assert.Equal("正在搜索法术", table.LoadingMessage);
        Assert.DoesNotContain(table.Columns, column => column.IsSortable);

        // 不用 Arc 的 Auto 列宽（一次性测量、不随宽度重分配），否则首屏会错位、最大化后长文本被截成省略号。

        Assert.DoesNotContain(table.Columns, column => column.Width.IsAuto);

        Assert.Contains(table.Columns, column => column.Width.IsStar);
        AssertTableScrollBehaviorInstalled(view, table);
        Assert.DoesNotContain(view.GetLogicalDescendants().OfType<TKCode.Arc.Button>(), button =>
            AutomationProperties.GetName(button)?.Contains("加载更多", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void Feat_page_loads_arc_filters_and_query_status()
    {
        var view = new FeatsView();

        AssertHasArcSearchAndFilters(view, "搜索专长", 3, 0, "清除专长搜索与筛选条件");
        AssertHasStatusRegions(view, "没有找到专长", "搜索专长失败", "专长搜索结果", "正在搜索专长");
        AssertDetailEmptyState(view, "专长详情区域", "未选择专长");
        var table = Assert.Single(view.GetLogicalDescendants().OfType<TKCode.Arc.Table>());
        Assert.Equal("专长搜索结果", AutomationProperties.GetName(table));
        Assert.Equal("正在搜索专长", table.LoadingMessage);
        Assert.DoesNotContain(table.Columns, column => column.IsSortable);

        // 不用 Arc 的 Auto 列宽（一次性测量、不随宽度重分配），否则首屏会错位、最大化后长文本被截成省略号。

        Assert.DoesNotContain(table.Columns, column => column.Width.IsAuto);

        Assert.Contains(table.Columns, column => column.Width.IsStar);
        AssertTableScrollBehaviorInstalled(view, table);
        Assert.DoesNotContain(view.GetLogicalDescendants().OfType<TKCode.Arc.Button>(), button =>
            AutomationProperties.GetName(button)?.Contains("加载更多", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void Monster_page_loads_arc_filters_and_master_detail()
    {
        var view = new MonstersView();

        AssertHasArcSearchAndFilters(view, "搜索怪物", 2, 0, "清除怪物搜索与筛选条件");
        AssertHasStatusRegions(view, "没有找到怪物", "搜索怪物失败", "怪物搜索结果", "正在搜索怪物");
        AssertDetailEmptyState(view, "怪物详情区域", "未选择怪物");
        var table = Assert.Single(view.GetLogicalDescendants().OfType<TKCode.Arc.Table>());
        Assert.Equal("怪物搜索结果", AutomationProperties.GetName(table));
        Assert.Equal("正在搜索怪物", table.LoadingMessage);
        Assert.DoesNotContain(table.Columns, column => column.IsSortable);

        // 不用 Arc 的 Auto 列宽（一次性测量、不随宽度重分配），否则首屏会错位、最大化后长文本被截成省略号。

        Assert.DoesNotContain(table.Columns, column => column.Width.IsAuto);

        Assert.Contains(table.Columns, column => column.Width.IsStar);
        AssertTableScrollBehaviorInstalled(view, table);
        Assert.DoesNotContain(view.GetLogicalDescendants().OfType<TKCode.Arc.Button>(), button =>
            AutomationProperties.GetName(button)?.Contains("加载更多", StringComparison.Ordinal) == true);
        Assert.Equal(2, view.GetLogicalDescendants().OfType<ItemsControl>().Count(control => control.ItemTemplate is not null));
        Assert.Contains(view.GetLogicalDescendants().OfType<TKCode.Arc.Button>(), button => button.Content?.ToString() == "← 返回怪物");
    }

    private static void AssertTableScrollBehaviorInstalled(Control view, TKCode.Arc.Table table)
    {
        var behaviorField = view.GetType().GetField("_loadMoreBehavior", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(behaviorField);
        var behavior = Assert.IsType<TableLoadMoreBehavior>(behaviorField!.GetValue(view));
        Assert.Same(table, behavior.ObservedTable);
        var scrollViewer = new ScrollViewer();
        table.Template = new FuncControlTemplate<TKCode.Arc.Table>((_, names) =>
        {
            names.Register(TKCode.Arc.Table.ScrollViewerPart, scrollViewer);
            return new Panel { Children = { scrollViewer } };
        });
        table.ApplyTemplate();
        Assert.Same(scrollViewer, behavior.ObservedScrollViewer);
    }

    private static void AssertHasArcSearchAndFilters(
        Control view,
        string searchLabel,
        int comboboxCount,
        int selectCount,
        string resetAutomationName)
    {
        var search = Assert.Single(view.GetLogicalDescendants().OfType<TKCode.Arc.SearchField>());
        Assert.Equal(searchLabel, search.Label);
        Assert.Equal(comboboxCount, view.GetLogicalDescendants().OfType<TKCode.Arc.Combobox>().Count());
        Assert.Equal(selectCount, view.GetLogicalDescendants().OfType<TKCode.Arc.Select>().Count());
        var reset = Assert.Single(view.GetLogicalDescendants().OfType<TKCode.Arc.Button>(),
            button => button.Content?.ToString() == "重置筛选");
        Assert.Equal("Secondary", reset.Variant.ToString());
        Assert.Equal(resetAutomationName, AutomationProperties.GetName(reset));
    }

    private static void AssertDetailEmptyState(Control view, string label, string title)
    {
        var detailState = Assert.Single(view.GetLogicalDescendants().OfType<TKCode.Arc.EmptyState>(),
            state => state.Label == label);
        Assert.Equal(title, detailState.Title);
        Assert.Equal(TKCode.Arc.ControlSize.Small, detailState.Size);
    }

    private static void AssertHasStatusRegions(
        Control view,
        string emptyTitle,
        string alertTitle,
        string label,
        string loadingLabel)
    {
        var emptyState = Assert.Single(view.GetLogicalDescendants().OfType<TKCode.Arc.EmptyState>(),
            state => state.Title == emptyTitle);
        Assert.Equal(label, emptyState.Label);

        var alert = Assert.Single(view.GetLogicalDescendants().OfType<TKCode.Arc.Alert>());
        Assert.Equal(alertTitle, alert.Title);
        var progressBars = view.GetLogicalDescendants().OfType<ProgressBar>().ToArray();
        Assert.Single(progressBars);
        Assert.Contains(progressBars, progress => AutomationProperties.GetName(progress)?.Contains("下一批", StringComparison.Ordinal) == true);
        {
            // The Arc Table hosts the search state in its loading veil (LoadingMessage),
            // so the only bare progress bar is the next-page one.
            Assert.DoesNotContain(progressBars, progress => AutomationProperties.GetName(progress) == loadingLabel);
        }

        Assert.Empty(view.GetLogicalDescendants().OfType<TKCode.Arc.Skeleton>());
    }
}
