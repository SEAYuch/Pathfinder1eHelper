using Pathfinder1eHelper.Models;
using Pathfinder1eHelper.Models.Combat;
using Pathfinder1eHelper.Services;

namespace Pathfinder1eHelper.Test;

/// <summary>In-memory <see cref="ICharacterRepository"/> that records saves and deletes.</summary>
public sealed class FakeCharacterRepository : ICharacterRepository
{
    public List<CharacterProfile> Data { get; } = new();

    public CharacterProfile? LastSaved { get; private set; }

    public List<Guid> Deleted { get; } = new();

    public string DirectoryPath => "(fake)";

    public IReadOnlyList<CharacterProfile> LoadAll() => Data;

    public void Save(CharacterProfile profile) => LastSaved = profile;

    public void Delete(Guid id) => Deleted.Add(id);
}

/// <summary>In-memory <see cref="ISpellRepository"/> that records the last query it received.</summary>
public sealed class FakeSpellRepository : ISpellRepository
{
    public SpellQuery? LastQuery { get; private set; }
    public List<Spell> Data { get; } = new();
    public IReadOnlyList<string> SourceList { get; set; } = new List<string>();
    public IReadOnlyList<string> ClassList { get; set; } = new List<string>();

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

    public Task<IReadOnlyList<string>> GetClassesAsync(CancellationToken ct = default) =>
        Task.FromResult(ClassList);

    public Task<Spell?> GetByIdAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(Data.FirstOrDefault(s => s.Id == id));

    public Task<IReadOnlyList<SpellBuff>> GetBuffsForSpellAsync(string? nameEn, string? nameZh, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<SpellBuff>>([]);
}

/// <summary>In-memory <see cref="ISpellService"/> for view-model tests.</summary>
public sealed class FakeSpellService : ISpellService
{
    public List<Spell> Results { get; } = new();
    public List<SpellBuff> Buffs { get; } = new();
    public IReadOnlyList<string> SourceList { get; set; } = new List<string> { "CRB", "APG" };
    public IReadOnlyList<string> ClassList { get; set; } = new List<string>();
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

    public Task<IReadOnlyList<string>> GetClassesAsync(CancellationToken ct = default) =>
        Task.FromResult(ClassList);

    public Task<Spell?> GetByIdAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(Results.FirstOrDefault(s => s.Id == id));

    public Task<IReadOnlyList<SpellBuff>> GetBuffsForSpellAsync(string? nameEn, string? nameZh, CancellationToken ct = default)
    {
        var matched = Buffs
            .Where(b => (!string.IsNullOrWhiteSpace(nameEn) && string.Equals(b.NameEn, nameEn, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(nameZh) && string.Equals(b.NameZh, nameZh, StringComparison.Ordinal)))
            .ToList();
        return Task.FromResult<IReadOnlyList<SpellBuff>>(matched);
    }
}
