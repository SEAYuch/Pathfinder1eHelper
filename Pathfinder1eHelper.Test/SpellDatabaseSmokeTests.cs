using System.Text.RegularExpressions;
using Pathfinder1eHelper.Data;
using Pathfinder1eHelper.Infrastructure;
using Pathfinder1eHelper.Models;
using Pathfinder1eHelper.Services;

namespace Pathfinder1eHelper.Test;

/// <summary>
/// Integration smoke test against the real, shipped <c>spells.duckdb</c> (copied to the test
/// output by the csproj). Validates that the DuckDB.NET engine bundled with the FreeSql provider
/// can open the CLI-produced file read-only and that the entity mapping round-trips.
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
        var provider = new DbPathProvider(); // AppContext.BaseDirectory\data\spells.duckdb
        var fsql = FreeSqlFactory.CreateReadOnly(provider.SpellsDbPath);
        try
        {
            var repo = new SpellRepository(fsql);

            var total = await repo.CountAsync(new SpellQuery(null, null, null, 0, int.MaxValue));
            Assert.True(total > 1900, $"expected a fully populated DB (>1900 spells), got {total}");

            var sources = await repo.GetSourcesAsync();
            Assert.Equal(17, sources.Count);

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
    public async Task Source_and_first_letter_filters_narrow_results()
    {
        var provider = new DbPathProvider();
        var fsql = FreeSqlFactory.CreateReadOnly(provider.SpellsDbPath);
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

    /// <summary>
    /// Regression for the range/area misalignment: books like APG label the range line as
    /// “范围”, so distance values ended up in <c>area</c>. The migration moved distance content
    /// into <c>range</c> (never overwriting an existing range); nothing distance-only may remain
    /// in <c>area</c> while <c>range</c> is empty.
    /// </summary>
    [Fact]
    public async Task No_distance_only_values_remain_in_area_when_range_is_empty()
    {
        var provider = new DbPathProvider();
        var fsql = FreeSqlFactory.CreateReadOnly(provider.SpellsDbPath);
        try
        {
            var repo = new SpellRepository(fsql);
            var all = await repo.SearchAsync(new SpellQuery(null, null, null, 0, 5000));
            Assert.True(all.Count > 1900);

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

    [Fact]
    public async Task Range_area_migration_spot_checks()
    {
        var provider = new DbPathProvider();
        var fsql = FreeSqlFactory.CreateReadOnly(provider.SpellsDbPath);
        try
        {
            var repo = new SpellRepository(fsql);

            // Pure distance that used to sit in area (APG labels the range line “范围”).
            var absorbingTouch = (await repo.SearchAsync(new SpellQuery("Absorbing Touch", "APG", null, 0, 10)))
                .Single(s => s.NameEn == "Absorbing Touch");
            Assert.Equal("接触", absorbingTouch.Range);
            Assert.Null(absorbingTouch.Area);

            // Mixed “distance + area” values are split, not merged.
            var towerOfIronWill = (await repo.SearchAsync(new SpellQuery("Tower of Iron Will", "OA", null, 0, 10)))
                .Single(s => s.NameEn == "Tower of Iron Will I");
            Assert.Equal("10尺", towerOfIronWill.Range);
            Assert.Equal("以你为中心，半径10尺的发散区域", towerOfIronWill.Area);

            // Rows that already had a real range keep both fields untouched.
            var alarm = (await repo.SearchAsync(new SpellQuery("Alarm", "CRB", null, 0, 10)))
                .Single(s => s.NameEn == "Alarm");
            Assert.Equal("近距", alarm.Range);
            Assert.Equal("以空间中的一点为中心, 20尺半径发散区域", alarm.Area);
        }
        finally
        {
            fsql.Dispose();
        }
    }
}
