using System;
using Avalonia.Threading;
using Pathfinder1eHelper.ViewModels.Pages;
using ReactiveUI.Avalonia;

namespace Pathfinder1eHelper.Views.Pages;

public partial class CombatView : ReactiveUserControl<CombatViewModel>
{
    private static readonly TimeSpan ConfirmationFeedbackDuration = TimeSpan.FromSeconds(2);

    public CombatView()
    {
        InitializeComponent();
    }

    private void OnDestructiveActionConfirmed(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not TKCode.Arc.HoldToConfirm confirmation)
        {
            return;
        }

        _ = DispatcherTimer.RunOnce(
            () => ResetConfirmation(confirmation),
            ConfirmationFeedbackDuration,
            DispatcherPriority.Normal);
    }

    internal static void ResetConfirmation(TKCode.Arc.HoldToConfirm confirmation)
    {
        confirmation.IsConfirmed = false;
    }
}
