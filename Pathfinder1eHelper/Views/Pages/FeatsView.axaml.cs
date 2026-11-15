using Avalonia.Controls;
using Pathfinder1eHelper.Models;
using Pathfinder1eHelper.ViewModels.Pages;
using ReactiveUI.Avalonia;
using TKCode.Arc;

namespace Pathfinder1eHelper.Views.Pages;

public partial class FeatsView : ReactiveUserControl<FeatsViewModel>
{
    private readonly TableLoadMoreBehavior _loadMoreBehavior;
    private readonly TableSingleSelectionBehavior _selectionBehavior;

    public FeatsView()
    {
        InitializeComponent();
        BuildColumns();
        _loadMoreBehavior = new TableLoadMoreBehavior(
            ResultsTable,
            () => ViewModel?.LoadMoreCommand.CanExecute(null) == true,
            () => ViewModel?.IsLoadingMore == true,
            () => ViewModel?.LoadMoreCommand.Execute(null));
        _selectionBehavior = new TableSingleSelectionBehavior(
            ResultsTable,
            () => ViewModel?.SelectedFeat,
            item =>
            {
                if (ViewModel is { } viewModel)
                {
                    viewModel.SelectedFeat = item as Feat;
                }
            });
        DataContextChanged += (_, _) => _selectionBehavior.SyncSelection();
    }

    // 同 SpellsView：不用 Arc 的 Auto 列宽（只测一次并锁住，首屏数据未到时按表头量，最大化后也不重分），
    // 改用固定像素 + 星号分配，宽度变化时星号列重新分配。
    private void BuildColumns()
    {
        AddColumn(DataTableColumn.Create<Feat>("中文名", feat => feat.NameZh, "nameZh"), new GridLength(150));
        AddColumn(DataTableColumn.Create<Feat>("英文名", feat => feat.NameEn, "nameEn"), Star(2));
        AddColumn(DataTableColumn.Create<Feat>("出处", feat => feat.Source, "source"), Star());
        AddColumn(DataTableColumn.Create<Feat>("类型", feat => feat.FeatType, "featType"), Star());
    }

    private static GridLength Star(double weight = 1) => new(weight, GridUnitType.Star);

    private void AddColumn(DataTableColumn column, GridLength width, bool isNumeric = false)
    {
        column.IsSortable = false;
        column.IsNumeric = isNumeric;
        column.Width = width;
        ResultsTable.Columns.Add(column);
    }
}
