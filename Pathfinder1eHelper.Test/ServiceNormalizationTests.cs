using Pathfinder1eHelper.Services;

namespace Pathfinder1eHelper.Test;

/// <summary>怪物查询归一化：空白筛选值不参与精确匹配。</summary>
public class MonsterServiceTests
{
    [Fact]
    public async Task SearchAsync_trims_filters_and_clamps_paging()
    {
        var repo = new FakeMonsterRepository();
        var service = new MonsterService(repo);

        await service.SearchAsync(new MonsterQuery("  dragon  ", " A ", " 龙类 ", Skip: -3, Take: 0));

        Assert.Equal("dragon", repo.LastQuery!.Term);
        Assert.Equal("A", repo.LastQuery.FirstLetter);
        Assert.Equal("龙类", repo.LastQuery.CreatureType);
        Assert.Equal(0, repo.LastQuery.Skip);
        Assert.Equal(MonsterService.DefaultPageSize, repo.LastQuery.Take);
    }

    [Fact]
    public async Task SearchAsync_whitespace_filters_become_null()
    {
        var repo = new FakeMonsterRepository();
        var service = new MonsterService(repo);

        await service.SearchAsync(new MonsterQuery("   ", "  ", "\t", 0, 20));

        Assert.Null(repo.LastQuery!.Term);
        Assert.Null(repo.LastQuery.FirstLetter);
        Assert.Null(repo.LastQuery.CreatureType);
    }
}

/// <summary>专长查询归一化：空白筛选值不参与精确匹配。</summary>
public class FeatServiceTests
{
    [Fact]
    public async Task SearchAsync_trims_filters_and_clamps_paging()
    {
        var repo = new FakeFeatRepository();
        var service = new FeatService(repo);

        await service.SearchAsync(new FeatQuery(" Power ", " CRB ", " P ", " 战斗 ", Skip: -1, Take: -5));

        Assert.Equal("Power", repo.LastQuery!.Term);
        Assert.Equal("CRB", repo.LastQuery.Source);
        Assert.Equal("P", repo.LastQuery.FirstLetter);
        Assert.Equal("战斗", repo.LastQuery.Type);
        Assert.Equal(0, repo.LastQuery.Skip);
        Assert.Equal(FeatService.DefaultPageSize, repo.LastQuery.Take);
    }

    [Fact]
    public async Task SearchAsync_whitespace_filters_become_null()
    {
        var repo = new FakeFeatRepository();
        var service = new FeatService(repo);

        await service.SearchAsync(new FeatQuery(null, "  ", " ", "   ", 0, 20));

        Assert.Null(repo.LastQuery!.Source);
        Assert.Null(repo.LastQuery.FirstLetter);
        Assert.Null(repo.LastQuery.Type);
    }
}

/// <summary>法术查询归一化：Source/FirstLetter 与另两类目录保持一致。</summary>
public class SpellQueryNormalizationTests
{
    [Fact]
    public async Task Source_and_first_letter_are_trimmed()
    {
        var repo = new FakeSpellRepository();
        var service = new SpellService(repo);

        await service.SearchAsync(new SpellQuery(null, " CRB ", " F ", 0, 20));

        Assert.Equal("CRB", repo.LastQuery!.Source);
        Assert.Equal("F", repo.LastQuery.FirstLetter);
    }

    [Fact]
    public async Task Whitespace_source_and_first_letter_become_null()
    {
        var repo = new FakeSpellRepository();
        var service = new SpellService(repo);

        await service.SearchAsync(new SpellQuery(null, "   ", "\t", 0, 20));

        Assert.Null(repo.LastQuery!.Source);
        Assert.Null(repo.LastQuery.FirstLetter);
    }

    [Fact]
    public async Task Out_of_range_class_level_becomes_null()
    {
        var repo = new FakeSpellRepository();
        var service = new SpellService(repo);

        await service.SearchAsync(new SpellQuery(null, null, null, 0, 20, ClassName: " 法师 ", ClassLevel: 12));

        Assert.Equal("法师", repo.LastQuery!.ClassName);
        Assert.Null(repo.LastQuery.ClassLevel);
    }

    [Fact]
    public async Task Count_query_is_normalized_too()
    {
        var repo = new FakeSpellRepository();
        var service = new SpellService(repo);

        await service.CountAsync(new SpellQuery("  fire ", " CRB ", " f ", 0, 20));

        Assert.Equal("fire", repo.LastQuery!.Term);
        Assert.Equal("CRB", repo.LastQuery.Source);
        Assert.Equal("f", repo.LastQuery.FirstLetter);
    }
}
