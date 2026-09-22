using Pathfinder1eHelper.Data;
using Pathfinder1eHelper.Models;
using Pathfinder1eHelper.Services;

namespace Pathfinder1eHelper.Test;

/// <summary>Integration smoke test for the shipped <c>pathfinder1e.duckdb</c> feat tables.</summary>
public class FeatDatabaseSmokeTests
{
    [Fact]
    public async Task Feat_tables_are_populated_and_mapped()
    {
        TestDatabase.SkipIfUnavailable();
        var fsql = FreeSqlFactory.CreateReadOnly(TestDatabase.Path);
        try
        {
            var repo = new FeatRepository(fsql);

            var total = await repo.CountAsync(new FeatQuery(null, null, null, null, 0, int.MaxValue));
            Assert.True(total > 1000, $"expected a populated feat table (>1000), got {total}");

            var sources = await repo.GetSourcesAsync();
            Assert.Equal(9, sources.Count);
            Assert.Contains("CRB", sources);

            var powerAttacks = await repo.SearchAsync(new FeatQuery("Power Attack", null, null, null, 0, 50));
            var power = Assert.Single(powerAttacks, f => f.NameEn == "Power Attack" && f.Source == "CRB");
            Assert.Equal("猛力攻击", power.NameZh);
            Assert.Equal("P", power.FirstLetter);
            Assert.Equal("战斗", power.FeatType);
            Assert.True(power.IsFighterBonus);
            Assert.False(string.IsNullOrWhiteSpace(power.Benefit));
        }
        finally
        {
            fsql.Dispose();
        }
    }

    [Fact]
    public async Task Source_first_letter_and_type_filters_narrow_results()
    {
        TestDatabase.SkipIfUnavailable();
        var fsql = FreeSqlFactory.CreateReadOnly(TestDatabase.Path);
        try
        {
            var repo = new FeatRepository(fsql);

            var crbA = await repo.SearchAsync(new FeatQuery(null, "CRB", "A", null, 0, 500));
            Assert.NotEmpty(crbA);
            Assert.All(crbA, (Feat f) =>
            {
                Assert.Equal("CRB", f.Source);
                Assert.Equal("A", f.FirstLetter);
            });

            var combat = await repo.SearchAsync(new FeatQuery(null, null, null, "战斗", 0, 500));
            Assert.NotEmpty(combat);
            Assert.All(combat, (Feat f) => Assert.Equal("战斗", f.FeatType));

            var types = await repo.GetTypesAsync();
            Assert.Contains("战斗", types);
            Assert.Contains("通用", types);
        }
        finally
        {
            fsql.Dispose();
        }
    }

    [Fact]
    public async Task Feat_buff_table_exposes_structured_effects()
    {
        TestDatabase.SkipIfUnavailable();
        var fsql = FreeSqlFactory.CreateReadOnly(TestDatabase.Path);
        try
        {
            var repo = new FeatRepository(fsql);

            var dodge = await repo.GetBuffsForFeatAsync("Dodge", "闪避");
            var row = Assert.Single(dodge);
            Assert.Equal("Dodge", row.BonusType);
            Assert.Equal("ArmorClass", row.Target);
            Assert.Equal(1, row.Value);
            Assert.NotNull(row.FeatId);

            var dodgeByZh = await repo.GetBuffsForFeatAsync(null, "闪避");
            Assert.Single(dodgeByZh);
        }
        finally
        {
            fsql.Dispose();
        }
    }
}
