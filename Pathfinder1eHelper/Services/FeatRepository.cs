using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FreeSql;
using Pathfinder1eHelper.Models;

namespace Pathfinder1eHelper.Services;

/// <summary>FreeSql-backed <see cref="IFeatRepository"/> over the read-only DuckDB reference data.</summary>
public sealed class FeatRepository(IFreeSql fsql) : IFeatRepository
{
    private ISelect<Feat> Filtered(FeatQuery q)
    {
        var term = q.Term;
        var lowerTerm = term?.ToLowerInvariant();
        return fsql.Select<Feat>()
            .WhereIf(
                !string.IsNullOrWhiteSpace(term),
                f => f.NameZh.Contains(term!)
                    || (f.NameEn != null && f.NameEn.ToLower().Contains(lowerTerm!))
                    || (f.Prerequisites != null && f.Prerequisites.ToLower().Contains(lowerTerm!)))
            .WhereIf(!string.IsNullOrEmpty(q.Source), f => f.Source == q.Source)
            .WhereIf(!string.IsNullOrEmpty(q.FirstLetter), f => f.FirstLetter == q.FirstLetter)
            .WhereIf(!string.IsNullOrEmpty(q.Type), f => f.FeatType == q.Type);
    }

    public async Task<IReadOnlyList<Feat>> SearchAsync(FeatQuery query, CancellationToken ct = default) =>
        await Filtered(query)
            .OrderBy(f => f.NameEn)
            .Skip(query.Skip)
            .Take(query.Take)
            .ToListAsync(ct);

    public async Task<int> CountAsync(FeatQuery query, CancellationToken ct = default) =>
        (int)await Filtered(query).CountAsync(ct);

    // 去重与排序下推到 SQL（DISTINCT + ORDER BY），避免把整列拉进内存再处理。
    public async Task<IReadOnlyList<string>> GetSourcesAsync(CancellationToken ct = default) =>
        await fsql.Select<Feat>()
            .Where(f => f.Source != null && f.Source != "")
            .Distinct()
            .OrderBy(f => f.Source)
            .ToListAsync(f => f.Source, ct);

    public async Task<IReadOnlyList<string>> GetTypesAsync(CancellationToken ct = default) =>
        await fsql.Select<Feat>()
            .Where(f => f.FeatType != null && f.FeatType != "")
            .Distinct()
            .OrderBy(f => f.FeatType)
            .ToListAsync(f => f.FeatType, ct);

    public async Task<IReadOnlyList<FeatBuff>> GetBuffsForFeatAsync(string? nameEn, string? nameZh, CancellationToken ct = default)
    {
        var lowerEnglish = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim().ToLowerInvariant();
        var chinese = string.IsNullOrWhiteSpace(nameZh) ? null : nameZh.Trim();
        if (lowerEnglish is null && chinese is null)
        {
            return [];
        }

        return await fsql.Select<FeatBuff>()
            .Where(b => (b.NameEn != null && b.NameEn.ToLower() == lowerEnglish)
                || (b.NameZh != null && b.NameZh == chinese))
            .OrderBy(b => b.SortOrder)
            .ToListAsync(ct);
    }
}
