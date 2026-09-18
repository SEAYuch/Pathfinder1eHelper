using Pathfinder1eHelper.Views.Controls;

namespace Pathfinder1eHelper.Test;

/// <summary>怪物详情文本解析：段落 + GFM 管道表 → 渲染块。</summary>
public class PipeTableParserTests
{
    [Fact]
    public void Parses_paragraph_then_table_with_header()
    {
        var text = "第一段\n| 年龄层 | 特殊能力 | 施法者等级 |\n| --- | --- | --- |\n| 雏龙 | 强酸免疫 | – |\n| 幼龙 | 沼泽漫步 | – |";

        var blocks = PipeTableParser.Parse(text);

        Assert.Equal(2, blocks.Count);
        Assert.Equal(MonsterBlockKind.Paragraph, blocks[0].Kind);
        Assert.Equal("第一段", blocks[0].Lines[0]);

        Assert.Equal(MonsterBlockKind.Table, blocks[1].Kind);
        Assert.Equal(3, blocks[1].Rows.Count); // 表头 + 2 行（分隔行被忽略）
        Assert.Equal("年龄层", blocks[1].Rows[0][0]);
        Assert.Equal("沼泽漫步", blocks[1].Rows[2][1]);
    }

    [Fact]
    public void Separator_row_is_ignored_and_escaped_pipe_is_unescaped()
    {
        var blocks = PipeTableParser.Parse("| 名称 | 值 |\n| --- | --- |\n| a\\|b | 1 |");

        var table = Assert.Single(blocks);
        Assert.Equal(MonsterBlockKind.Table, table.Kind);
        Assert.Equal("a|b", table.Rows[1][0]);
    }

    [Fact]
    public void Null_or_whitespace_text_yields_no_blocks()
    {
        Assert.Empty(PipeTableParser.Parse(null));
        Assert.Empty(PipeTableParser.Parse("   "));
    }

    [Fact]
    public void Table_without_body_still_parses()
    {
        var blocks = PipeTableParser.Parse("| A | B |\n| --- | --- |");

        var table = Assert.Single(blocks);
        Assert.Single(table.Rows);
        Assert.Equal("A", table.Rows[0][0]);
    }

    [Fact]
    public void Text_without_tables_is_a_single_paragraph()
    {
        var blocks = PipeTableParser.Parse("第一行\n第二行");

        var paragraph = Assert.Single(blocks);
        Assert.Equal(MonsterBlockKind.Paragraph, paragraph.Kind);
        Assert.Equal(2, paragraph.Lines.Count);
    }
}
