using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Pathfinder1eHelper.Views.Pages;
using TKCode.Arc;

namespace Pathfinder1eHelper.Test;

/// <summary>Row clicks must reach the master/detail view model (Arc's Table ignores clicks when IsSelectable is off).</summary>
public sealed class TableClickSelectionTests
{
    [Fact]
    public void Clicking_a_templated_cell_selects_its_item()
    {
        var first = new object();
        var second = new object();
        var cell = new Border { DataContext = first };

        var (table, behavior, selected) = CreateTable(cell, first, second);
        using (behavior)
        {
            Press(cell, table);

            Assert.Same(first, selected());
            Assert.Same(first, Assert.Single(table.SelectedItems!.Cast<object>()));
        }
    }

    /// <summary>
    /// Text columns have no cell controls: the hit target is Arc's internal TableRow, which does not
    /// expose the item as DataContext. The behaviour must still find the row's item.
    /// </summary>
    [Fact]
    public void Clicking_a_text_row_selects_its_item()
    {
        var first = new object();
        var second = new object();
        var row = new TableRow { Item = first };

        var (table, behavior, selected) = CreateTable(row, first, second);
        using (behavior)
        {
            Assert.Null(row.DataContext);

            Press(row, table);

            Assert.Same(first, selected());
            Assert.Same(first, Assert.Single(table.SelectedItems!.Cast<object>()));
        }
    }

    [Fact]
    public void Clicking_the_page_background_keeps_the_selection()
    {
        var first = new object();
        var second = new object();
        var background = new Panel();

        var (table, behavior, selected) = CreateTable(background, first, second);
        using (behavior)
        {
            Press(background, table);

            Assert.Null(selected());
            Assert.True(table.SelectedItems is null or { Count: 0 });
        }
    }

    private static (Table Table, TableSingleSelectionBehavior Behavior, Func<object?> Selected) CreateTable(
        Control hitTarget,
        object first,
        object second)
    {
        var table = new Table
        {
            ItemsSource = new ObservableCollection<object> { first, second },
            IsSelectable = false,
        };

        table.Template = new FuncControlTemplate<Table>((_, _) => new Panel { Children = { hitTarget } });
        table.ApplyTemplate();

        object? selected = null;
        var behavior = new TableSingleSelectionBehavior(table, () => selected, value => selected = value);
        return (table, behavior, () => selected);
    }

    private static void Press(Control target, Control root)
    {
        target.RaiseEvent(new PointerPressedEventArgs(
            target,
            null!,
            root,
            new Point(4, 4),
            0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed),
            KeyModifiers.None));
    }

    /// <summary>Stand-in for Arc's internal TableRow: matched by type name plus a public Item property.</summary>
    private sealed class TableRow : Control
    {
        public object? Item { get; set; }
    }
}
