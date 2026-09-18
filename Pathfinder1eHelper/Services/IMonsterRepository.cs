using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pathfinder1eHelper.Models;

namespace Pathfinder1eHelper.Services;

/// <summary>Read-only data access for the <c>monsters</c> reference tables.</summary>
public interface IMonsterRepository
{
    /// <summary>Returns a page of monsters matching <paramref name="query"/>, ordered by English name.</summary>
    Task<IReadOnlyList<Monster>> SearchAsync(MonsterQuery query, CancellationToken ct = default);

    /// <summary>Returns the total number of monsters matching <paramref name="query"/> (ignoring paging).</summary>
    Task<int> CountAsync(MonsterQuery query, CancellationToken ct = default);

    /// <summary>Returns the distinct creature types (for the filter dropdown), sorted.</summary>
    Task<IReadOnlyList<string>> GetCreatureTypesAsync(CancellationToken ct = default);

    /// <summary>Returns the overview/绪论 pages a monster belongs to (related links).</summary>
    Task<IReadOnlyList<MonsterGroup>> GetGroupsForMonsterAsync(int monsterId, CancellationToken ct = default);

    /// <summary>Returns the members of an overview/绪论 page.</summary>
    Task<IReadOnlyList<Monster>> GetGroupMembersAsync(int groupId, CancellationToken ct = default);
}
