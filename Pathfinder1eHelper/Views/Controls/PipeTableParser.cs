using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Pathfinder1eHelper.Views.Controls;

/// <summary>块类型：普通段落 / 管道表。</summary>
public enum MonsterBlockKind
{
    Paragraph,
    Table,
}

/// <summary>解析结果块：段落用 <see cref="Lines"/>，表格用 <see cref="Rows"/>（首行为表头）。</summary>
public sealed record MonsterBlock(
    MonsterBlockKind Kind,
    IReadOnlyList<string> Lines,
    IReadOnlyList<IReadOnlyList<string>> Rows);

/// <summary>
/// 把抽取阶段生成的“文本 + GFM 管道表”拆成可渲染的块；不含 UI 依赖，便于单测。
/// 管道表行：以 <c>|</c> 开头结尾；<c>| --- |</c> 分隔行被忽略；<c>\|</c> 转义。
/// </summary>
public static class PipeTableParser
{
    private static readonly Regex SeparatorCell = new("^:?-{1,}:?$", RegexOptions.Compiled);

    public static IReadOnlyList<MonsterBlock> Parse(string? text)
    {
        var blocks = new List<MonsterBlock>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return blocks;
        }

        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var paragraph = new List<string>();
        var table = new List<IReadOnlyList<string>>();

        void FlushTable()
        {
            if (table.Count > 0)
            {
                blocks.Add(new MonsterBlock(MonsterBlockKind.Table, [], table.ToArray()));
                table.Clear();
            }
        }

        void FlushParagraph()
        {
            while (paragraph.Count > 0 && paragraph[^1].Length == 0)
            {
                paragraph.RemoveAt(paragraph.Count - 1);
            }

            if (paragraph.Count > 0)
            {
                blocks.Add(new MonsterBlock(MonsterBlockKind.Paragraph, paragraph.ToArray(), []));
            }

            paragraph.Clear();
        }

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (IsTableRow(line))
            {
                FlushParagraph();
                var cells = SplitRow(line);
                if (!IsSeparatorRow(cells))
                {
                    table.Add(cells);
                }

                continue;
            }

            FlushTable();
            paragraph.Add(raw.TrimEnd());
        }

        FlushTable();
        FlushParagraph();
        return blocks;
    }

    /// <summary>是否为一行 GFM 管道表（以 <c>|</c> 开头结尾）。</summary>
    public static bool IsTableRow(string line) =>
        line.Length >= 2 && line[0] == '|' && line[^1] == '|';

    private static IReadOnlyList<string> SplitRow(string line)
    {
        var inner = line[1..^1];
        var cells = new List<string>();
        var sb = new StringBuilder();
        for (var i = 0; i < inner.Length; i++)
        {
            var c = inner[i];
            if (c == '\\' && i + 1 < inner.Length && inner[i + 1] == '|')
            {
                sb.Append('|');
                i++;
            }
            else if (c == '|')
            {
                cells.Add(sb.ToString().Trim());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }

        cells.Add(sb.ToString().Trim());
        return cells;
    }

    private static bool IsSeparatorRow(IReadOnlyList<string> cells)
    {
        if (cells.Count == 0)
        {
            return false;
        }

        foreach (var cell in cells)
        {
            if (!SeparatorCell.IsMatch(cell))
            {
                return false;
            }
        }

        return true;
    }
}
