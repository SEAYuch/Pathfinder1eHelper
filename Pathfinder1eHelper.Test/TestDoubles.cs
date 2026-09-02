using Pathfinder1eHelper.Models;
using Pathfinder1eHelper.Services;

namespace Pathfinder1eHelper.Test;

/// <summary>In-memory <see cref="ISpellRepository"/> that records the last query it received.</summary>
public sealed class FakeSpellRepository : ISpellRepository
{
    public SpellQuery? LastQuery { get; private set; }
    public List<Spell> Data { get; } = new();
    public IReadOnlyList<string> SourceList { get; set; } = new List<string>();

    public Task<IReadOnlyList<Spell>> SearchAsync(SpellQuery query, CancellationToken ct = default)
    {
        LastQuery = query;
        return Task.FromResult<IReadOnlyList<Spell>>(Data.ToList());
    }

    public Task<int> CountAsync(SpellQuery query, CancellationToken ct = default)
    {
        LastQuery = query;
        return Task.FromResult(Data.Count);
    }

    public Task<IReadOnlyList<string>> GetSourcesAsync(CancellationToken ct = default) =>
        Task.FromResult(SourceList);

    public Task<Spell?> GetByIdAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(Data.FirstOrDefault(s => s.Id == id));
}

/// <summary>In-memory <see cref="ISpellService"/> for view-model tests.</summary>
public sealed class FakeSpellService : ISpellService
{
    public List<Spell> Results { get; } = new();
    public IReadOnlyList<string> SourceList { get; set; } = new List<string> { "CRB", "APG" };
    public SpellQuery? LastQuery { get; private set; }

    public Task<IReadOnlyList<Spell>> SearchAsync(SpellQuery query, CancellationToken ct = default)
    {
        LastQuery = query;
        return Task.FromResult<IReadOnlyList<Spell>>(Results.ToList());
    }

    public Task<int> CountAsync(SpellQuery query, CancellationToken ct = default) =>
        Task.FromResult(Results.Count);

    public Task<IReadOnlyList<string>> GetSourcesAsync(CancellationToken ct = default) =>
        Task.FromResult(SourceList);

    public Task<Spell?> GetByIdAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(Results.FirstOrDefault(s => s.Id == id));
}
