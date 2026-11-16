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

    private readonly Func<bool> _canLoadMore;
    private readonly Func<bool> _isLoadingMore;
    private readonly Action _loadMore;
    private int _lastRequestedItemCount = -1;

    internal Table ObservedTable { get; }

    internal ScrollViewer? ObservedScrollViewer { get; private set; }

    public TableLoadMoreBehavior(Table table, Func<bool> canLoadMore, Func<bool> isLoadingMore, Action loadMore)
    {
        ObservedTable = table ?? throw new ArgumentNullException(nameof(table));
        _canLoadMore = canLoadMore ?? throw new ArgumentNullException(nameof(canLoadMore));
        _isLoadingMore = isLoadingMore ?? throw new ArgumentNullException(nameof(isLoadingMore));
        _loadMore = loadMore ?? throw new ArgumentNullException(nameof(loadMore));
        ObservedTable.TemplateApplied += OnTemplateApplied;
        ObservedTable.AttachedToVisualTree += OnAttachedToVisualTree;
        if (ObservedTable.IsAttachedToVisualTree())
        {
            ObservedTable.ApplyTemplate();
        }
    }

    internal void AttachTemplateParts(INameScope nameScope)
    {
        DetachScrollViewer();
        ObservedScrollViewer = nameScope.Find<ScrollViewer>(Table.ScrollViewerPart);
        if (ObservedScrollViewer is null) return;
        ObservedScrollViewer.ScrollChanged += OnScrollChanged;
        TryLoadMore(ObservedScrollViewer.Extent.Height, ObservedScrollViewer.Viewport.Height, ObservedScrollViewer.Offset.Y);
    }

    internal bool TryLoadMore(double extent, double viewport, double offset)
    {
        if (!double.IsFinite(extent) || !double.IsFinite(viewport) || !double.IsFinite(offset) ||
            extent <= 0 || viewport < 0 || offset < 0 || extent <= viewport)
        {
            return false;
        }

        var remaining = extent - viewport - offset;
        switch (remaining)
        {
            case > PreloadDistance:
                _lastRequestedItemCount = -1;
                return false;
            case < 0:
                return false;
        }

        var count = (ObservedTable.ItemsSource as ICollection)?.Count ?? 0;
        if (count == 0 || count == _lastRequestedItemCount || !_canLoadMore() || _isLoadingMore())
        {
            return false;
        }

        _lastRequestedItemCount = count;
        _loadMore();
        return true;
    }

    private void OnTemplateApplied(object? sender, TemplateAppliedEventArgs e) => AttachTemplateParts(e.NameScope);

    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e) => ObservedTable.ApplyTemplate();

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (ObservedScrollViewer is not null)
        {
            TryLoadMore(ObservedScrollViewer.Extent.Height, ObservedScrollViewer.Viewport.Height, ObservedScrollViewer.Offset.Y);
        }
    }

    private void DetachScrollViewer()
    {
        if (ObservedScrollViewer is null) return;
        ObservedScrollViewer.ScrollChanged -= OnScrollChanged;
        ObservedScrollViewer = null;
    }

    public void Dispose()
    {
        DetachScrollViewer();
        ObservedTable.TemplateApplied -= OnTemplateApplied;
        ObservedTable.AttachedToVisualTree -= OnAttachedToVisualTree;
    }
}
