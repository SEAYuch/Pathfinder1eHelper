using Avalonia.Controls;
using Avalonia.Interactivity;
using Pathfinder1eHelper.Models;
using Pathfinder1eHelper.ViewModels.Pages;
using ReactiveUI.Avalonia;

namespace Pathfinder1eHelper.Views.Pages;

public partial class MonstersView : ReactiveUserControl<MonstersViewModel>
{
    public MonstersView()
    {
        InitializeComponent();
    }

    /// <summary>点击“相关绪论”链接 → 打开概论（描述 + 成员）。</summary>
    private void OnGroupClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: MonsterGroup group } && DataContext is MonstersViewModel vm)
        {
            vm.OpenGroupCommand.Execute(group);
        }
    }

    /// <summary>点击概论成员 → 回到该怪物详情。</summary>
    private void OnMemberClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: Monster monster } && DataContext is MonstersViewModel vm)
        {
            vm.OpenMonsterCommand.Execute(monster);
        }
    }
}
