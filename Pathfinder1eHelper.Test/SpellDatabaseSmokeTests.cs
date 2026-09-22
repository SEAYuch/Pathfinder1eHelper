using System.Text.RegularExpressions;
using Pathfinder1eHelper.Data;
using Pathfinder1eHelper.Models;
using Pathfinder1eHelper.Services;

namespace Pathfinder1eHelper.Test;

/// <summary>
/// Integration smoke test against the real, shipped <c>pathfinder1e.duckdb</c> (copied to the test
/// output by the csproj; skipped when absent). Validates that the DuckDB.NET engine bundled with the
/// FreeSql provider can open the CLI-produced file read-only and that the entity mapping round-trips.
/// </summary>
public class SpellDatabaseSmokeTests
{
    /// <summary>Distance-only values that must never remain in <c>area</c> when <c>range</c> is empty.</summary>
    private static readonly Regex DistanceOnly = new(
        "^(接触|近战武器触及范围|近距|中距|远距|长距|个人|自身|无限|无限制|特殊|见下文|见后文|见描述|见下)$",
        RegexOptions.Compiled);

    [Fact]
    public async Task Reference_database_opens_readonly_maps_entities_and_returns_rows()
    {
        TestDatabase.SkipIfUnavailable(); // AppContext.BaseDirectory\data\pathfinder1e.duckdb
        var fsql = FreeSqlFactory.CreateReadOnly(TestDatabase.Path);
        try
        {
            var repo = new SpellRepository(fsql);

            var total = await repo.CountAsync(new SpellQuery(null, null, null, 0, int.MaxValue));
            Assert.True(total > 3000, $"expected a fully populated DB (>3000 spells), got {total}");

            var sources = await repo.GetSourcesAsync();
            Assert.Equal(101, sources.Count);

            var fireballs = await repo.SearchAsync(new SpellQuery("Fireball", null, null, 0, 50));
            Assert.Contains(fireballs, s => s.NameEn == "Fireball");

            // Entity mapping: the canonical CRB Fireball has its key fields populated.
            var fireball = fireballs.First(s => s.NameEn == "Fireball" && s.Source == "CRB");
            Assert.False(string.IsNullOrWhiteSpace(fireball.NameZh));
            Assert.Equal("F", fireball.FirstLetter);

            var byId = await repo.GetByIdAsync(fireball.Id);
            Assert.NotNull(byId);
            Assert.Equal(fireball.Id, byId!.Id);
            Assert.Equal(fireball.NameEn, byId.NameEn);
        }
        finally
        {
            fsql.Dispose();
        }
    }

    [Fact]
    public async Task English_search_is_case_insensitive()
    {
        TestDatabase.SkipIfUnavailable();
        var fsql = FreeSqlFactory.CreateReadOnly(TestDatabase.Path);
        try
        {
            var repo = new SpellRepository(fsql);

            foreach (var term in new[] { "fireball", "FIREBALL", "fIrEbAlL" })
            {
                var hits = await repo.SearchAsync(new SpellQuery(term, null, null, 0, 50));
                Assert.Contains(hits, s => s.NameEn == "Fireball");
            }
        }
        finally
        {
            fsql.Dispose();
        }
    }

    [Fact]
    public async Task Spell_buff_table_exposes_structured_effects()
    {
        TestDatabase.SkipIfUnavailable();
        var fsql = FreeSqlFactory.CreateReadOnly(TestDatabase.Path);
        try
        {
            var repo = new SpellRepository(fsql);

            var bless = await repo.GetBuffsForSpellAsync("Bless", "祝福术");
            Assert.NotEmpty(bless);
            Assert.Contains(bless, b => b.BonusType == "Morale" && b.Target == "MeleeAttack");

            // 大小写不敏感 + 中文别名均可命中。
            var barkskin = await repo.GetBuffsForSpellAsync("barkskin", null);
            var naturalArmor = Assert.Single(barkskin);
            Assert.Equal("Enhancement", naturalArmor.BonusType);
            Assert.Equal("NaturalArmor", naturalArmor.EnhancementSubject);
            Assert.Equal(3, naturalArmor.ScaleStep);
        }
        finally
        {
            fsql.Dispose();
        }
    }

    [Fact]
    public async Task Source_and_first_letter_filters_narrow_results()
    {
        TestDatabase.SkipIfUnavailable();
        var fsql = FreeSqlFactory.CreateReadOnly(TestDatabase.Path);
        try
        {
            var repo = new SpellRepository(fsql);

            var crbA = await repo.SearchAsync(new SpellQuery(null, "CRB", "A", 0, 500));

            Assert.NotEmpty(crbA);
            Assert.All(crbA, (Spell s) =>
            {
                Assert.Equal("CRB", s.Source);
                Assert.Equal("A", s.FirstLetter);
            });
        }
        finally
        {
            fsql.Dispose();
        }
    }

    [Fact]
    public async Task Class_and_level_filters_query_spell_levels_index()
    {
        TestDatabase.SkipIfUnavailable();
        var fsql = FreeSqlFactory.CreateReadOnly(TestDatabase.Path);
        try
        {
            var repo = new SpellRepository(fsql);

            // 职业+环位：CRB 火球术在 spell_levels 中为“术士/法师 3”；下拉已摊平，按单个职业匹配。
            var wiz3 = await repo.SearchAsync(new SpellQuery(null, "CRB", null, 0, 200, ClassName: "法师", ClassLevel: 3));
            Assert.Contains(wiz3, s => s.NameEn == "Fireball");
            Assert.All(wiz3, s => Assert.Equal("CRB", s.Source));

            // 复合职业的另一分量“术士”同样命中火球术（分词匹配）。
            var sor3 = await repo.SearchAsync(new SpellQuery(null, "CRB", null, 0, 200, ClassName: "术士", ClassLevel: 3));
            Assert.Contains(sor3, s => s.NameEn == "Fireball");

            // 仅环位：9 环（只应命中 kind='class' 的主职业行）。
            var lvl9 = await repo.SearchAsync(new SpellQuery(null, "CRB", null, 0, 200, ClassLevel: 9));
            Assert.NotEmpty(lvl9);

            // 职业下拉数据源已摊平：含单个职业，且不再出现任何复合项。
            var classes = await repo.GetClassesAsync();
            Assert.Contains("法师", classes);
            Assert.Contains("术士", classes);
            Assert.DoesNotContain("术士/法师", classes);
            Assert.DoesNotContain(classes, c => c.Contains('/'));
        }
        finally
        {
            fsql.Dispose();
        }
    }

    /// <summary>
    /// Regression for the range/area misalignment: books like APG label the range line as
    /// “范围”, so distance values ended up in <c>area</c>. The migration moved distance content
    /// into <c>range</c> (never overwriting an existing range); nothing distance-only may remain
    /// in <c>area</c> while <c>range</c> is empty.
    /// </summary>
    [Fact]
    public async Task No_distance_only_values_remain_in_area_when_range_is_empty()
    {
        TestDatabase.SkipIfUnavailable();
        var fsql = FreeSqlFactory.CreateReadOnly(TestDatabase.Path);
        try
        {
            var repo = new SpellRepository(fsql);
            var all = await repo.SearchAsync(new SpellQuery(null, null, null, 0, 5000));
            Assert.True(all.Count > 3000);

            var misplaced = all
                .Where(s => string.IsNullOrWhiteSpace(s.Range) && !string.IsNullOrWhiteSpace(s.Area))
                .Where(s => DistanceOnly.IsMatch(s.Area!))
                .Select(s => $"{s.Source}/{s.NameEn}: {s.Area}")
                .ToList();

            Assert.True(misplaced.Count == 0, "distance-only values still in area:\n" + string.Join("\n", misplaced));
        }
        finally
        {
            fsql.Dispose();
        }
    }
}
