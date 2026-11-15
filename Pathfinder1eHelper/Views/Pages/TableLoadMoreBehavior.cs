using System;
using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using TKCode.Arc;

namespace Pathfinder1eHelper.Views.Pages;

/// <summary>
/// Loads another result page when Arc's <see cref="Table"/> scroll viewer approaches its end.
/// Arc documents the template part name as <see cref="Table.ScrollViewerPart"/>.
/// </summary>
internal sealed class TableLoadMoreBehavior : IDisposable
{
    private const double PreloadDistance = 120;

    private readonly Table _table;
    private readonly Func<bool> _canLoadMore;
    private readonly Func<bool> _isLoadingMore;
    private readonly Action _loadMore;
    private ScrollViewer? _scrollViewer;
    private int _lastRequestedItemCount = -1;

    internal Table ObservedTable => _table;

    internal ScrollViewer? ObservedScrollViewer => _scrollViewer;

    public TableLoadMoreBehavior(Table table, Func<bool> canLoadMore, Func<bool> isLoadingMore, Action loadMore)
    {
        _table = table ?? throw new ArgumentNullException(nameof(table));
        _canLoadMore = canLoadMore ?? throw new ArgumentNullException(nameof(canLoadMore));
        _isLoadingMore = isLoadingMore ?? throw new ArgumentNullException(nameof(isLoadingMore));
        _loadMore = loadMore ?? throw new ArgumentNullException(nameof(loadMore));
        _table.TemplateApplied += OnTemplateApplied;
        _table.AttachedToVisualTree += OnAttachedToVisualTree;
        if (_table.IsAttachedToVisualTree())
        {
            _table.ApplyTemplate();
        }
    }

    internal void AttachTemplateParts(INameScope nameScope)
    {
        DetachScrollViewer();
        _scrollViewer = nameScope.Find<ScrollViewer>(Table.ScrollViewerPart);
        if (_scrollViewer is not null)
        {
            _scrollViewer.ScrollChanged += OnScrollChanged;
            TryLoadMore(_scrollViewer.Extent.Height, _scrollViewer.Viewport.Height, _scrollViewer.Offset.Y);
        }
    }

    internal bool TryLoadMore(double extent, double viewport, double offset)
    {
        if (!double.IsFinite(extent) || !double.IsFinite(viewport) || !double.IsFinite(offset) ||
            extent <= 0 || viewport < 0 || offset < 0 || extent <= viewport)
        {
            return false;
        }

        var remaining = extent - viewport - offset;
        if (remaining > PreloadDistance)
        {
            _lastRequestedItemCount = -1;
            return false;
        }

        if (remaining < 0)
        {
            return false;
        }

        var count = (_table.ItemsSource as ICollection)?.Count ?? 0;
        if (count == 0 || count == _lastRequestedItemCount || !_canLoadMore() || _isLoadingMore())
        {
            return false;
        }

        _lastRequestedItemCount = count;
        _loadMore();
        return true;
    }

    private void OnTemplateApplied(object? sender, TemplateAppliedEventArgs e) => AttachTemplateParts(e.NameScope);

    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e) => _table.ApplyTemplate();

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_scrollViewer is not null)
        {
            TryLoadMore(_scrollViewer.Extent.Height, _scrollViewer.Viewport.Height, _scrollViewer.Offset.Y);
        }
    }

    private void DetachScrollViewer()
    {
        if (_scrollViewer is not null)
        {
            _scrollViewer.ScrollChanged -= OnScrollChanged;
            _scrollViewer = null;
        }
    }

    public void Dispose()
    {
        DetachScrollViewer();
        _table.TemplateApplied -= OnTemplateApplied;
        _table.AttachedToVisualTree -= OnAttachedToVisualTree;
    }
}
