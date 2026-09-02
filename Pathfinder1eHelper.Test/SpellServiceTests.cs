using Pathfinder1eHelper.Models;
using Pathfinder1eHelper.Services;

namespace Pathfinder1eHelper.Test;

public class SpellServiceTests
{
    [Fact]
    public async Task SearchAsync_trims_term_and_forwards_filters()
    {
        var repo = new FakeSpellRepository();
        var service = new SpellService(repo);

        await service.SearchAsync(new SpellQuery("  fire  ", "CRB", "F", Skip: 10, Take: 50));

        Assert.Equal("fire", repo.LastQuery!.Term);
        Assert.Equal("CRB", repo.LastQuery.Source);
        Assert.Equal("F", repo.LastQuery.FirstLetter);
        Assert.Equal(10, repo.LastQuery.Skip);
        Assert.Equal(50, repo.LastQuery.Take);
    }

    [Fact]
    public async Task SearchAsync_blank_term_becomes_null()
    {
        var repo = new FakeSpellRepository();
        var service = new SpellService(repo);

        await service.SearchAsync(new SpellQuery("   ", null, null, 0, 20));

        Assert.Null(repo.LastQuery!.Term);
    }

    [Fact]
    public async Task SearchAsync_nonpositive_take_defaults_to_page_size()
    {
        var repo = new FakeSpellRepository();
        var service = new SpellService(repo);

        await service.SearchAsync(new SpellQuery(null, null, null, Skip: 0, Take: 0));

        Assert.Equal(SpellService.DefaultPageSize, repo.LastQuery!.Take);
    }

    [Fact]
    public async Task SearchAsync_negative_skip_is_clamped_to_zero()
    {
        var repo = new FakeSpellRepository();
        var service = new SpellService(repo);

        await service.SearchAsync(new SpellQuery(null, null, null, Skip: -5, Take: 10));

        Assert.Equal(0, repo.LastQuery!.Skip);
    }

    [Fact]
    public async Task SearchAsync_returns_repository_rows()
    {
        var repo = new FakeSpellRepository();
        repo.Data.Add(new Spell { Id = 1, NameEn = "Fireball", NameZh = "火球术" });
        repo.Data.Add(new Spell { Id = 2, NameEn = "Bless", NameZh = "祝福术" });
        var service = new SpellService(repo);

        var result = await service.SearchAsync(new SpellQuery(null, null, null, 0, 200));

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByIdAsync_forwards_to_repository()
    {
        var repo = new FakeSpellRepository();
        repo.Data.Add(new Spell { Id = 42, NameEn = "Wish" });
        var service = new SpellService(repo);

        var spell = await service.GetByIdAsync(42);

        Assert.NotNull(spell);
        Assert.Equal("Wish", spell!.NameEn);
    }
}
