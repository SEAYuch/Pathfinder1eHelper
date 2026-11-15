using Avalonia.Controls;
using Pathfinder1eHelper.Models;
using Pathfinder1eHelper.ViewModels.Pages;
using ReactiveUI.Avalonia;
using TKCode.Arc;

namespace Pathfinder1eHelper.Views.Pages;

public partial class SpellsView : ReactiveUserControl<SpellsViewModel>
{
    private readonly TableLoadMoreBehavior _loadMoreBehavior;
    private readonly TableSingleSelectionBehavior _selectionBehavior;

    public SpellsView()
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
            () => ViewModel?.SelectedSpell,
            item =>
            {
                if (ViewModel is { } viewModel)
                {
                    viewModel.SelectedSpell = item as Spell;
                }
            });
        DataContextChanged += (_, _) => _selectionBehavior.SyncSelection();
    }

    // Arc binding properties on DataTableColumn cannot use the page's compiled-binding data type,
    // so the columns use typed selectors instead (the documented AOT-safe form).
    // Arc 的 Auto 列宽只测一次并锁住（首屏数据还没到时会按表头量出来），最大化后也不参与分配，
    // 长文本因此被截成省略号。这里全部用「固定像素 + 星号分配」：星号列总是按剩余宽度重新分配，
    // 与 Auto 的一次性测量无关，宽度变化（最大化/还原）时列宽跟着走。
    private void BuildColumns()
    {
        AddColumn(DataTableColumn.Create<Spell>("中文名", spell => spell.NameZh, "nameZh"), new GridLength(150));
        AddColumn(DataTableColumn.Create<Spell>("英文名", spell => spell.NameEn, "nameEn"), Star(2));
        AddColumn(DataTableColumn.Create<Spell>("出处", spell => spell.Source, "source"), Star());
        AddColumn(DataTableColumn.Create<Spell>("学派", spell => spell.School, "school"), Star());
        AddColumn(DataTableColumn.Create<Spell>("环位", spell => spell.Level, "level"), Star(), isNumeric: true);
    }

    private static GridLength Star(double weight = 1) => new(weight, GridUnitType.Star);

    private void AddColumn(DataTableColumn column, GridLength width, bool isNumeric = false)
    {
        // Sorting is server driven (paging by Skip/Take); a client-side sort would break the loaded order.
        column.IsSortable = false;
        column.IsNumeric = isNumeric;
        column.Width = width;
        ResultsTable.Columns.Add(column);
    }
}
