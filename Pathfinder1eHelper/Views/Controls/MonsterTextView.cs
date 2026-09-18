using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Pathfinder1eHelper.Views.Controls;

/// <summary>
/// 渲染“原文文本 + GFM 管道表”:普通行按段落显示(等宽换行),管道表块渲染为带边框的表格。
/// 仅用代码构建控件(无模板),AOT/裁剪友好。
/// </summary>
public sealed class MonsterTextView : UserControl
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<MonsterTextView, string?>(nameof(Text));

    private readonly StackPanel _panel = new() { Spacing = 6 };

    public MonsterTextView()
    {
        Content = _panel;
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TextProperty)
        {
            Rebuild();
        }
    }

    private void Rebuild()
    {
        _panel.Children.Clear();
        foreach (var block in PipeTableParser.Parse(Text))
        {
            _panel.Children.Add(block.Kind == MonsterBlockKind.Table
                ? BuildTable(block.Rows)
                : BuildParagraph(block.Lines));
        }
    }

    private static Control BuildParagraph(IReadOnlyList<string> lines) =>
        new TextBlock
        {
            Text = string.Join("\n", lines),
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 21,
        };

    private Control BuildTable(IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var columns = rows.Max(r => r.Count);
        var grid = new Grid();
        for (var c = 0; c < columns; c++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }

        for (var r = 0; r < rows.Count; r++)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        }

        var brush = this.TryGetResource("AppDividerBrush", null, out var resource) && resource is IBrush found
            ? found
            : Brushes.Gray;

        for (var r = 0; r < rows.Count; r++)
        {
            for (var c = 0; c < columns; c++)
            {
                var cell = new Border
                {
                    BorderBrush = brush,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(6, 3),
                    Child = new TextBlock
                    {
                        Text = c < rows[r].Count ? rows[r][c] : "",
                        TextWrapping = TextWrapping.Wrap,
                        FontWeight = r == 0 ? FontWeight.SemiBold : FontWeight.Normal,
                    },
                };
                Grid.SetRow(cell, r);
                Grid.SetColumn(cell, c);
                grid.Children.Add(cell);
            }
        }

        return grid;
    }
}
