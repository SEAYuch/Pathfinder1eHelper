using Avalonia.Controls;
using Pathfinder1eHelper.ViewModels;
using ReactiveUI.Avalonia;

namespace Pathfinder1eHelper.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
        SizeChanged += OnSizeChanged;
        UpdateSidebarCollapse(Bounds.Width);
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
        => UpdateSidebarCollapse(e.NewSize.Width);

    private void UpdateSidebarCollapse(double windowWidth)
    {
        Navigation.IsCollapsed = SidebarResponsivePolicy.ShouldCollapse(windowWidth, Navigation.IsCollapsed);
    }
}
