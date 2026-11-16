using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pathfinder1eHelper.Models;

namespace Pathfinder1eHelper.Services;

/// <summary>Default <see cref="IMonsterService"/>. Normalizes queries before delegating.</summary>
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

    internal static MonsterQuery Normalize(MonsterQuery query) =>
        new(Term: QueryNormalization.Optional(query.Term), FirstLetter: QueryNormalization.Optional(query.FirstLetter),
            CreatureType: QueryNormalization.Optional(query.CreatureType), Skip: QueryNormalization.Skip(query.Skip),
            Take: QueryNormalization.Take(query.Take, DefaultPageSize));
}