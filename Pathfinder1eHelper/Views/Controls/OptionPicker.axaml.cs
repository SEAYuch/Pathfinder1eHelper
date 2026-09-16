using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;

namespace Pathfinder1eHelper.Views.Controls;

/// <summary>
/// 紧凑的选项选择器：常态只显示当前值（悬浮按钮），点击后弹出浮层列出选项，
/// 不再在下拉框上挤占固定宽度。
/// </summary>
public partial class OptionPicker : UserControl
{
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<OptionPicker, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<OptionPicker, object?>(nameof(SelectedItem), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<OptionPicker, IDataTemplate?>(nameof(ItemTemplate));

    public static readonly StyledProperty<string?> PlaceholderProperty =
        AvaloniaProperty.Register<OptionPicker, string?>(nameof(Placeholder), "—");

    public OptionPicker()
    {
        InitializeComponent();
    }

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public IDataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public string? Placeholder
    {
        get => GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox list)
        {
            SelectedItem = list.SelectedItem;
            Trigger.Flyout?.Hide();
        }
    }
}
