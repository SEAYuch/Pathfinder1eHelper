using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using TKCode.Arc;

namespace Pathfinder1eHelper.Views.Pages;

/// <summary>
/// Restores the single-selection master/detail contract Arc's <see cref="Table"/> does not offer
/// (Arc only ships checkbox multi-selection): clicking a row or using the arrow keys selects exactly
/// one item, and the selected row is highlighted through <see cref="Table.SelectedItems"/>.
/// </summary>
internal sealed class TableSingleSelectionBehavior : IDisposable
{
    private const int PageStep = 10;
    private const double FallbackRowHeight = 32;

    private readonly Table _table;
    private readonly Func<object?> _getSelected;
    private readonly Action<object?> _setSelected;
    private INotifyCollectionChanged? _observedItems;
    private ScrollViewer? _scrollViewer;
    private bool _restoringSelection;
    private Type? _rowType;
    private System.Reflection.PropertyInfo? _rowItemProperty;

    public TableSingleSelectionBehavior(Table table, Func<object?> getSelected, Action<object?> setSelected)
    {
        _table = table ?? throw new ArgumentNullException(nameof(table));
        _getSelected = getSelected ?? throw new ArgumentNullException(nameof(getSelected));
        _setSelected = setSelected ?? throw new ArgumentNullException(nameof(setSelected));
        _table.TemplateApplied += OnTemplateApplied;
        _table.PropertyChanged += OnTablePropertyChanged;
        _table.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        _table.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        AttachItems(_table.ItemsSource);
    }

    /// <summary>Re-reads the view-model selection and repaints the highlighted row.</summary>
    public void SyncSelection()
    {
        var selected = _getSelected();
        var current = _table.SelectedItems;
        if (selected is null)
        {
            if (current is { Count: > 0 })
            {
                _table.SelectedItems = new List<object>();
            }

            return;
        }

        if (current is { Count: 1 } && ReferenceEquals(current[0], selected))
        {
            return;
        }

        SetSelectedItems(selected);
    }

    private void OnTemplateApplied(object? sender, TemplateAppliedEventArgs e)
    {
        _scrollViewer = e.NameScope.Find<ScrollViewer>(Table.ScrollViewerPart);
    }

    private void OnTablePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Table.ItemsSourceProperty)
        {
            AttachItems(e.NewValue as IEnumerable);
            SyncSelection();
            return;
        }

        // Arc only hands SelectedItems out when IsSelectable is on: on every source re-read it
        // writes an empty list back. Re-apply the view-model selection so the highlight survives
        // new searches and appended pages.
        if (e.Property == Table.SelectedItemsProperty && !_restoringSelection)
        {
            var selected = _getSelected();
            if (selected is not null && e.NewValue is not System.Collections.IList { Count: > 0 })
            {
                SetSelectedItems(selected);
            }
        }
    }

    private void AttachItems(IEnumerable? items)
    {
        if (_observedItems is not null)
        {
            _observedItems.CollectionChanged -= OnItemsChanged;
            _observedItems = null;
        }

        if (items is INotifyCollectionChanged observable)
        {
            _observedItems = observable;
            observable.CollectionChanged += OnItemsChanged;
        }
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action is NotifyCollectionChangedAction.Add or NotifyCollectionChangedAction.Replace)
        {
            return;
        }

        SyncSelection();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(_table).Properties.IsLeftButtonPressed)
        {
            return;
        }

        var item = FindRowItem(e.Source as Visual);
        if (item is null)
        {
            return;
        }

        Select(item);
        FocusRow(e.Source as Visual);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (HandleNavigationKey(e.Key))
        {
            e.Handled = true;
        }
    }

    /// <summary>Moves the selection by a navigation key; the return value says whether it moved.</summary>
    internal bool HandleNavigationKey(Key key)
    {
        var items = Snapshot();
        if (items.Count == 0)
        {
            return false;
        }

        var current = _getSelected();
        var index = current is null ? -1 : items.IndexOf(current);
        var next = key switch
        {
            Key.Down => Math.Min(index + 1, items.Count - 1),
            Key.Up => Math.Max(index - 1, 0),
            Key.PageDown => Math.Min(index + PageStep, items.Count - 1),
            Key.PageUp => Math.Max(index - PageStep, 0),
            Key.Home => 0,
            Key.End => items.Count - 1,
            _ => index,
        };

        if (next < 0 || next == index)
        {
            return false;
        }

        Select(items[next]);
        RevealRow(next);
        return true;
    }

    internal void Select(object item)
    {
        _setSelected(item);
        SetSelectedItems(item);
    }

    private void SetSelectedItems(object item)
    {
        _restoringSelection = true;
        try
        {
            _table.SelectedItems = new List<object> { item };
        }
        finally
        {
            _restoringSelection = false;
        }
    }

    private List<object> Snapshot()
    {
        if (_table.ItemsSource is not IEnumerable items)
        {
            return [];
        }

        var list = new List<object>();
        foreach (var item in items)
        {
            if (item is not null)
            {
                list.Add(item);
            }
        }

        return list;
    }

    private double EstimateRowHeight()
    {
        var count = (_table.ItemsSource as ICollection)?.Count ?? 0;
        var extent = _scrollViewer?.Extent.Height ?? 0;
        if (count > 0 && extent > 0)
        {
            var estimate = extent / count;
            if (estimate > 0)
            {
                return estimate;
            }
        }

        return FallbackRowHeight;
    }

    private void RevealRow(int index)
    {
        if (_scrollViewer is null)
        {
            return;
        }

        var rowHeight = EstimateRowHeight();

        var top = index * rowHeight;
        var bottom = top + rowHeight;
        var offset = _scrollViewer.Offset.Y;
        var viewport = _scrollViewer.Viewport.Height;

        if (top < offset)
        {
            _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X, Math.Max(0, top));
        }
        else if (viewport > 0 && bottom > offset + viewport)
        {
            _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X, Math.Max(0, bottom - viewport));
        }
    }

    // Arc only focuses the row when IsSelectable is on; without that the arrow keys would never
    // reach this table, so move focus to the nearest focusable row part after a click.
    private void FocusRow(Visual? source)
    {
        for (var current = source; current is not null && current != _table; current = current.GetVisualParent())
        {
            if (current is InputElement { Focusable: true } focusable)
            {
                focusable.Focus();
                return;
            }
        }
    }

    private object? FindRowItem(Visual? source)
    {
        if (_table.ItemsSource is not IEnumerable items)
        {
            return null;
        }

        var known = new HashSet<object>();
        foreach (var item in items)
        {
            if (item is not null)
            {
                known.Add(item);
            }
        }

        if (known.Count == 0)
        {
            return null;
        }

        // Cells built from a CellTemplate carry their item as DataContext; plain text cells are
        // drawn by the row itself, so there the hit target is Arc's internal TableRow (no
        // DataContext) and its item has to be read from the public Item property.
        for (var current = source; current is not null && current != _table; current = current.GetVisualParent())
        {
            if (current is Control { DataContext: { } dataContext } && known.Contains(dataContext))
            {
                return dataContext;
            }

            if (current is Visual row && row.GetType().Name == "TableRow")
            {
                var item = ReadRowItem(row);
                if (item is not null && known.Contains(item))
                {
                    return item;
                }
            }
        }

        return null;
    }

    private object? ReadRowItem(Visual row)
    {
        var type = row.GetType();
        if (type != _rowType)
        {
            _rowType = type;
            _rowItemProperty = type
                .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Where(property => property.Name == "Item" && property.CanRead && property.GetIndexParameters().Length == 0)
                .OrderByDescending(property => property.DeclaringType == type)
                .FirstOrDefault();
        }

        return _rowItemProperty?.GetValue(row);
    }

    public void Dispose()
    {
        AttachItems(null);
        _table.TemplateApplied -= OnTemplateApplied;
        _table.PropertyChanged -= OnTablePropertyChanged;
        _table.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
        _table.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
    }
}
