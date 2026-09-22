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
        return Task.FromResult<IReadOnlyList<Spell>>(
            Results.Skip(query.Skip).Take(query.Take).ToList());
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

/// <summary>In-memory <see cref="IFeatRepository"/> that records the last query it received.</summary>
public sealed class FakeFeatRepository : IFeatRepository
{
    public FeatQuery? LastQuery { get; private set; }
    public List<Feat> Data { get; } = new();
    public IReadOnlyList<string> SourceList { get; set; } = new List<string>();
    public IReadOnlyList<string> TypeList { get; set; } = new List<string>();

    public Task<IReadOnlyList<Feat>> SearchAsync(FeatQuery query, CancellationToken ct = default)
    {
        LastQuery = query;
        return Task.FromResult<IReadOnlyList<Feat>>(Data.Skip(query.Skip).Take(query.Take).ToList());
    }

    public Task<int> CountAsync(FeatQuery query, CancellationToken ct = default)
    {
        LastQuery = query;
        return Task.FromResult(Data.Count);
    }

    public Task<IReadOnlyList<string>> GetSourcesAsync(CancellationToken ct = default) =>
        Task.FromResult(SourceList);

    public Task<IReadOnlyList<string>> GetTypesAsync(CancellationToken ct = default) =>
        Task.FromResult(TypeList);

    public Task<IReadOnlyList<FeatBuff>> GetBuffsForFeatAsync(string? nameEn, string? nameZh, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<FeatBuff>>([]);
}

/// <summary>In-memory <see cref="IFeatService"/> for view-model tests.</summary>
public sealed class FakeFeatService : IFeatService
{
    public List<Feat> Results { get; } = new();
    public List<FeatBuff> Buffs { get; } = new();
    public IReadOnlyList<string> SourceList { get; set; } = new List<string> { "CRB", "APG" };
    public IReadOnlyList<string> TypeList { get; set; } = new List<string> { "战斗", "通用" };
    public FeatQuery? LastQuery { get; private set; }

    public Task<IReadOnlyList<Feat>> SearchAsync(FeatQuery query, CancellationToken ct = default)
    {
        LastQuery = query;
        return Task.FromResult<IReadOnlyList<Feat>>(Results.Skip(query.Skip).Take(query.Take).ToList());
    }

    public Task<int> CountAsync(FeatQuery query, CancellationToken ct = default) =>
        Task.FromResult(Results.Count);

    public Task<IReadOnlyList<string>> GetSourcesAsync(CancellationToken ct = default) =>
        Task.FromResult(SourceList);

    public Task<IReadOnlyList<string>> GetTypesAsync(CancellationToken ct = default) =>
        Task.FromResult(TypeList);

    public Task<IReadOnlyList<FeatBuff>> GetBuffsForFeatAsync(string? nameEn, string? nameZh, CancellationToken ct = default)
    {
        var matched = Buffs
            .Where(b => (!string.IsNullOrWhiteSpace(nameEn) && string.Equals(b.NameEn, nameEn, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(nameZh) && string.Equals(b.NameZh, nameZh, StringComparison.Ordinal)))
            .ToList();
        return Task.FromResult<IReadOnlyList<FeatBuff>>(matched);
    }
}

/// <summary>In-memory <see cref="IMonsterRepository"/> for smoke-level tests.</summary>
public sealed class FakeMonsterRepository : IMonsterRepository
{
    public List<Monster> Data { get; } = new();
    public List<MonsterGroup> Groups { get; } = new();
    public IReadOnlyList<string> TypeList { get; set; } = new List<string>();
    public MonsterQuery? LastQuery { get; private set; }

    public Task<IReadOnlyList<Monster>> SearchAsync(MonsterQuery query, CancellationToken ct = default)
    {
        LastQuery = query;
        return Task.FromResult<IReadOnlyList<Monster>>(Data.ToList());
    }

    public Task<int> CountAsync(MonsterQuery query, CancellationToken ct = default)
    {
        LastQuery = query;
        return Task.FromResult(Data.Count);
    }

    public Task<IReadOnlyList<string>> GetCreatureTypesAsync(CancellationToken ct = default) =>
        Task.FromResult(TypeList);

    public Task<IReadOnlyList<MonsterGroup>> GetGroupsForMonsterAsync(int monsterId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<MonsterGroup>>(Groups);

    public Task<IReadOnlyList<Monster>> GetGroupMembersAsync(int groupId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Monster>>(Data);
}

/// <summary>In-memory <see cref="IMonsterService"/> for view-model tests.</summary>
public sealed class FakeMonsterService : IMonsterService
{
    public List<Monster> Results { get; } = new();
    public List<MonsterGroup> Groups { get; } = new();
    public List<Monster> GroupMembers { get; } = new();
    public IReadOnlyList<string> TypeList { get; set; } = new List<string> { "龙类", "异怪" };
    public MonsterQuery? LastQuery { get; private set; }

    public Task<IReadOnlyList<Monster>> SearchAsync(MonsterQuery query, CancellationToken ct = default)
    {
        LastQuery = query;
        return Task.FromResult<IReadOnlyList<Monster>>(
            Results.Skip(query.Skip).Take(query.Take).ToList());
    }

    public Task<int> CountAsync(MonsterQuery query, CancellationToken ct = default) =>
        Task.FromResult(Results.Count);

    public Task<IReadOnlyList<string>> GetCreatureTypesAsync(CancellationToken ct = default) =>
        Task.FromResult(TypeList);

    public Task<IReadOnlyList<MonsterGroup>> GetGroupsForMonsterAsync(int monsterId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<MonsterGroup>>(Groups);

    public Task<IReadOnlyList<Monster>> GetGroupMembersAsync(int groupId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Monster>>(GroupMembers);
}
