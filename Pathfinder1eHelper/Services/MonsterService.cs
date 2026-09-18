using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pathfinder1eHelper.Models;

namespace Pathfinder1eHelper.Services;

/// <summary>Default <see cref="IMonsterService"/>. Normalises queries before delegating.</summary>
public sealed class MonsterService(IMonsterRepository repository) : IMonsterService
{
    /// <summary>Default page size used when a query supplies a non-positive <see cref="MonsterQuery.Take"/>.</summary>
    public const int DefaultPageSize = 200;

    public Task<IReadOnlyList<Monster>> SearchAsync(MonsterQuery query, CancellationToken ct = default) =>
        repository.SearchAsync(Normalize(query), ct);

    public Task<int> CountAsync(MonsterQuery query, CancellationToken ct = default) =>
        repository.CountAsync(Normalize(query), ct);

    public Task<IReadOnlyList<string>> GetCreatureTypesAsync(CancellationToken ct = default) =>
        repository.GetCreatureTypesAsync(ct);

    public Task<IReadOnlyList<MonsterGroup>> GetGroupsForMonsterAsync(int monsterId, CancellationToken ct = default) =>
        repository.GetGroupsForMonsterAsync(monsterId, ct);

    public Task<IReadOnlyList<Monster>> GetGroupMembersAsync(int groupId, CancellationToken ct = default) =>
        repository.GetGroupMembersAsync(groupId, ct);

    internal static MonsterQuery Normalize(MonsterQuery query)
    {
        var term = string.IsNullOrWhiteSpace(query.Term) ? null : query.Term.Trim();
        var letter = string.IsNullOrWhiteSpace(query.FirstLetter) ? null : query.FirstLetter.Trim();
        var type = string.IsNullOrWhiteSpace(query.CreatureType) ? null : query.CreatureType.Trim();
        var skip = query.Skip < 0 ? 0 : query.Skip;
        var take = query.Take <= 0 ? DefaultPageSize : query.Take;
        return query with { Term = term, FirstLetter = letter, CreatureType = type, Skip = skip, Take = take };
    }
}
