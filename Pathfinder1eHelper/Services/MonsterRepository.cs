using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FreeSql;
using Pathfinder1eHelper.Models;

namespace Pathfinder1eHelper.Services;

/// <summary>FreeSql-backed <see cref="IMonsterRepository"/> over the read-only DuckDB reference data.</summary>
public sealed class MonsterRepository(IFreeSql fsql) : IMonsterRepository
{
    private ISelect<Monster> Filtered(MonsterQuery q)
    {
        var term = q.Term;
        var lowerTerm = term?.ToLowerInvariant();
        return fsql.Select<Monster>()
            .WhereIf(
                !string.IsNullOrWhiteSpace(term),
                m => m.NameZh.Contains(term!) || (m.NameEn != null && m.NameEn.ToLower().Contains(lowerTerm!)))
            .WhereIf(!string.IsNullOrEmpty(q.FirstLetter), m => m.FirstLetter == q.FirstLetter)
            .WhereIf(!string.IsNullOrEmpty(q.CreatureType), m => m.CreatureType == q.CreatureType);
    }

    public async Task<IReadOnlyList<Monster>> SearchAsync(MonsterQuery query, CancellationToken ct = default) =>
        await Filtered(query)
            .OrderBy(m => m.NameEn)
            .Skip(query.Skip)
            .Take(query.Take)
            .ToListAsync(ct);

    public async Task<int> CountAsync(MonsterQuery query, CancellationToken ct = default) =>
        (int)await Filtered(query).CountAsync(ct);

    // 去重与排序下推到 SQL（DISTINCT + ORDER BY），避免把整列拉进内存再处理。
    public async Task<IReadOnlyList<string>> GetCreatureTypesAsync(CancellationToken ct = default)
    {
        var types = await fsql.Select<Monster>()
            .Where(m => m.CreatureType != null && m.CreatureType != "")
            .Distinct()
            .OrderBy(m => m.CreatureType)
            .ToListAsync(m => m.CreatureType, ct);
        return types.Select(t => t!).ToList();
    }

    public async Task<IReadOnlyList<MonsterGroup>> GetGroupsForMonsterAsync(int monsterId, CancellationToken ct = default)
    {
        var groupIds = await fsql.Select<MonsterGroupMember>()
            .Where(x => x.MonsterId == monsterId)
            .ToListAsync(x => x.GroupId, ct);
        if (groupIds.Count == 0)
        {
            return [];
        }

        return await fsql.Select<MonsterGroup>()
            .Where(g => groupIds.Contains(g.Id))
            .OrderBy(g => g.Id)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Monster>> GetGroupMembersAsync(int groupId, CancellationToken ct = default)
    {
        var monsterIds = await fsql.Select<MonsterGroupMember>()
            .Where(x => x.GroupId == groupId)
            .ToListAsync(x => x.MonsterId, ct);
        if (monsterIds.Count == 0)
        {
            return [];
        }

        return await fsql.Select<Monster>()
            .Where(m => monsterIds.Contains(m.Id))
            .OrderBy(m => m.NameEn)
            .ToListAsync(ct);
    }
}
