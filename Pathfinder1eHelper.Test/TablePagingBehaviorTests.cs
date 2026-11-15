using System.Collections.Generic;
using Avalonia.Input;
using Pathfinder1eHelper.Views.Pages;
using TKCode.Arc;

namespace Pathfinder1eHelper.Test;

/// <summary>Arc Table paging and single-selection behaviour that Arc itself does not ship.</summary>
public sealed class TablePagingBehaviorTests
{
    [Fact]
    public void Table_load_more_triggers_only_near_the_end()
    {
        var items = new List<object>();
        for (var i = 0; i < 200; i++)
        {
            items.Add(new object());
        }

        var table = new TKCode.Arc.Table { ItemsSource = items };
        var canLoadMore = true;
        var isLoadingMore = false;
        var calls = 0;
        var behavior = new TableLoadMoreBehavior(table, () => canLoadMore, () => isLoadingMore, () => calls++);

        Assert.False(behavior.TryLoadMore(1200, 400, 100));
        Assert.Equal(0, calls);

        // 1200 - 400 - 680 = 120 → exactly at the preload distance.
        Assert.True(behavior.TryLoadMore(1200, 400, 680));
        Assert.Equal(1, calls);

        // The same row count cannot request again until the scroll leaves the preload zone.
        Assert.False(behavior.TryLoadMore(1200, 400, 700));
        Assert.Equal(1, calls);
        Assert.False(behavior.TryLoadMore(1200, 400, 100));
        Assert.True(behavior.TryLoadMore(1200, 400, 700));
        Assert.Equal(2, calls);
    }

    [Fact]
    public void Table_load_more_ignores_underfilled_grids_busy_state_and_empty_sources()
    {
        var items = new List<object> { new(), new() };
        var table = new TKCode.Arc.Table { ItemsSource = items };
        var canLoadMore = true;
        var isLoadingMore = false;
        var calls = 0;
        var behavior = new TableLoadMoreBehavior(table, () => canLoadMore, () => isLoadingMore, () => calls++);

        Assert.False(behavior.TryLoadMore(300, 400, 0));
        Assert.Equal(0, calls);

        canLoadMore = false;
        Assert.False(behavior.TryLoadMore(2000, 400, 1500));
        Assert.Equal(0, calls);

        canLoadMore = true;
        isLoadingMore = true;
        Assert.False(behavior.TryLoadMore(2000, 400, 1500));
        Assert.Equal(0, calls);

        isLoadingMore = false;
        table.ItemsSource = new List<object>();
        Assert.False(behavior.TryLoadMore(2000, 400, 1500));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void Table_single_selection_paints_the_selected_row_and_restores_it()
    {
        var first = new object();
        var second = new object();
        var third = new object();
        var table = new TKCode.Arc.Table { ItemsSource = new List<object> { first, second, third } };
        object? selected = second;
        var behavior = new TableSingleSelectionBehavior(table, () => selected, value => selected = value);

        behavior.SyncSelection();
        Assert.Equal(new[] { second }, table.SelectedItems!.Cast<object>());

        // Arc clears SelectedItems on every source re-read when IsSelectable is off; the
        // behaviour must put the view-model selection back.
        table.SelectedItems = new List<object>();
        Assert.Equal(new[] { second }, table.SelectedItems!.Cast<object>());

        selected = first;
        behavior.SyncSelection();
        Assert.Equal(new[] { first }, table.SelectedItems!.Cast<object>());
    }

    [Fact]
    public void Table_navigation_moves_the_single_selection()
    {
        var first = new object();
        var second = new object();
        var third = new object();
        var table = new TKCode.Arc.Table { ItemsSource = new List<object> { first, second, third } };
        object? selected = first;
        var behavior = new TableSingleSelectionBehavior(table, () => selected, value => selected = value);

        Assert.True(behavior.HandleNavigationKey(Key.Down));
        Assert.Same(second, selected);
        Assert.Same(second, Assert.Single(table.SelectedItems!.Cast<object>()));

        Assert.True(behavior.HandleNavigationKey(Key.End));
        Assert.Same(third, selected);
        Assert.True(behavior.HandleNavigationKey(Key.Home));
        Assert.Same(first, selected);

        // Already at the first row: the key does not move and is not swallowed.
        Assert.False(behavior.HandleNavigationKey(Key.Up));
        Assert.Same(first, selected);
    }
}
