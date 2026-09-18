using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pathfinder1eHelper.Models;

namespace Pathfinder1eHelper.Services;

/// <summary>Search/filter orchestration over <see cref="IMonsterRepository"/>.</summary>
public interface IMonsterService
{
    Task<IReadOnlyList<Monster>> SearchAsync(MonsterQuery query, CancellationToken ct = default);
    Task<int> CountAsync(MonsterQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetCreatureTypesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MonsterGroup>> GetGroupsForMonsterAsync(int monsterId, CancellationToken ct = default);
    Task<IReadOnlyList<Monster>> GetGroupMembersAsync(int groupId, CancellationToken ct = default);
}
