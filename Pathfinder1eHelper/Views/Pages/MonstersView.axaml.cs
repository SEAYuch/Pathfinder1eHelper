using Avalonia.Controls;
using Avalonia.Interactivity;
using Pathfinder1eHelper.Models;
using Pathfinder1eHelper.ViewModels.Pages;
using ReactiveUI.Avalonia;
using TKCode.Arc;

namespace Pathfinder1eHelper.Views.Pages;

public partial class MonstersView : ReactiveUserControl<MonstersViewModel>
{
    private readonly TableLoadMoreBehavior _loadMoreBehavior;
    private readonly TableSingleSelectionBehavior _selectionBehavior;

    public MonstersView()
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
            () => ViewModel?.SelectedMonster,
            item =>
            {
                if (ViewModel is { } viewModel)
                {
                    viewModel.SelectedMonster = item as Monster;
                }
            });
        DataContextChanged += (_, _) => _selectionBehavior.SyncSelection();
    }

    // 同 SpellsView：不用 Arc 的 Auto 列宽（只测一次并锁住，首屏数据未到时按表头量，最大化后也不重分），
    // 改用固定像素 + 星号分配，宽度变化时星号列重新分配。
    private void BuildColumns()
    {
        AddColumn(DataTableColumn.Create<Monster>("中文名", monster => monster.NameZh, "nameZh"), new GridLength(160));
        AddColumn(DataTableColumn.Create<Monster>("英文名", monster => monster.NameEn, "nameEn"), Star(2));
        AddColumn(DataTableColumn.Create<Monster>("CR", monster => monster.Cr, "cr"), new GridLength(70), isNumeric: true);
        AddColumn(DataTableColumn.Create<Monster>("类型", monster => monster.CreatureType, "creatureType"), Star());
        AddColumn(DataTableColumn.Create<Monster>("体型", monster => monster.Size, "size"), Star());
    }

    private static GridLength Star(double weight = 1) => new(weight, GridUnitType.Star);

    private void AddColumn(DataTableColumn column, GridLength width, bool isNumeric = false)
    {
        column.IsSortable = false;
        column.IsNumeric = isNumeric;
        column.Width = width;
        ResultsTable.Columns.Add(column);
    }

    /// <summary>点击“相关绪论”链接 → 打开概论（描述 + 成员）。</summary>
    private void OnGroupClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Avalonia.Controls.Button { DataContext: MonsterGroup group } && DataContext is MonstersViewModel vm)
        {
            vm.OpenGroupCommand.Execute(group);
        }
    }

    /// <summary>点击概论成员 → 回到该怪物详情。</summary>
    private void OnMemberClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Avalonia.Controls.Button { DataContext: Monster monster } && DataContext is MonstersViewModel vm)
        {
            vm.OpenMonsterCommand.Execute(monster);
        }
    }
}
