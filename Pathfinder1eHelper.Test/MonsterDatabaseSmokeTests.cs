using Pathfinder1eHelper.Data;
using Pathfinder1eHelper.Models;
using Pathfinder1eHelper.Services;

namespace Pathfinder1eHelper.Test;

/// <summary>Integration smoke test for the shipped <c>pathfinder1e.duckdb</c> monster tables.</summary>
public class MonsterDatabaseSmokeTests
{
    [Fact]
    public async Task Monster_tables_are_populated()
    {
        TestDatabase.SkipIfUnavailable();
        var fsql = FreeSqlFactory.CreateReadOnly(TestDatabase.Path);
        try
        {
            var repo = new MonsterRepository(fsql);

            var total = await repo.CountAsync(new MonsterQuery(null, null, null, 0, int.MaxValue));
            Assert.True(total > 800, $"expected >800 monsters, got {total}");

            var types = await repo.GetCreatureTypesAsync();
            Assert.Contains("龙类", types);
        }
        finally
        {
            fsql.Dispose();
        }
    }

    [Fact]
    public async Task Monster_fields_and_first_letter_filter_round_trip()
    {
        TestDatabase.SkipIfUnavailable();
        var fsql = FreeSqlFactory.CreateReadOnly(TestDatabase.Path);
        try
        {
            var repo = new MonsterRepository(fsql);

            var hits = await repo.SearchAsync(new MonsterQuery("Chromatic Dragon, Black", null, null, 0, 50));
            var black = Assert.Single(hits, m => m.NameEn == "Chromatic Dragon, Black");
            Assert.Equal("彩色龙，黑龙", black.NameZh);
            Assert.Equal("C", black.FirstLetter);
            Assert.Equal("3", black.Cr);
            Assert.Equal("龙类", black.CreatureType);
            Assert.Equal("混乱邪恶", black.Alignment);
            Assert.Equal(11, black.Strength);

            var a = await repo.SearchAsync(new MonsterQuery(null, "A", null, 0, 500));
            Assert.NotEmpty(a);
            Assert.All(a, (Monster m) => Assert.Equal("A", m.FirstLetter));

            // 源页面中的 <table>（黑龙年龄层表）应被抽取为 GFM 管道表。
            var combined = (black.StatBlock ?? "") + "\n" + (black.SpecialAbilities ?? "");
            Assert.Contains("| 年龄层 |", combined);
            Assert.Contains("| --- |", combined);

            // HTML 实体应被解码（不再残留 &#...;）。
            Assert.DoesNotContain("&#", combined);
        }
        finally
        {
            fsql.Dispose();
        }
    }

    [Fact]
    public async Task True_dragons_link_to_their_overview()
    {
        TestDatabase.SkipIfUnavailable();
        var fsql = FreeSqlFactory.CreateReadOnly(TestDatabase.Path);
        try
        {
            var repo = new MonsterRepository(fsql);

            var hits = await repo.SearchAsync(new MonsterQuery("Chromatic Dragon, Black", null, null, 0, 50));
            var black = hits.First(m => m.NameEn == "Chromatic Dragon, Black");

            var groups = await repo.GetGroupsForMonsterAsync(black.Id);
            var dragon = Assert.Single(groups, g => g.NameEn == "Dragon");
            Assert.Equal("龙类绪论", dragon.NameZh);

            // 绪论里的跑团资料（规则正文 + 表格）应被抽取为可渲染内容。
            Assert.NotNull(dragon.Content);
            Assert.Contains("| 年龄层 |", dragon.Content);
            Assert.Contains("【龙的攻击和速度", dragon.Content);

            var members = await repo.GetGroupMembersAsync(dragon.Id);
            Assert.Equal(10, members.Count); // 彩色龙 5 + 金属龙 5（不含非真龙的仙灵龙）
            Assert.Contains(members, m => m.NameEn == "Chromatic Dragon, Black");
            Assert.Contains(members, m => m.NameEn == "Metallic Dragon, Gold");
        }
        finally
        {
            fsql.Dispose();
        }
    }
}
