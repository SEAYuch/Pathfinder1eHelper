using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FreeSql;
using Pathfinder1eHelper.Models;

namespace Pathfinder1eHelper.Services;

/// <summary>FreeSql-backed <see cref="ISpellRepository"/> over the read-only DuckDB reference data.</summary>
public sealed class SpellRepository(IFreeSql fsql) : ISpellRepository
{
    private ISelect<Spell> Filtered(SpellQuery q)
    {
        // 中文名按原样匹配；英文名两侧统一 lower()，使英文搜索不区分大小写。
        var term = q.Term;
        var lowerTerm = term?.ToLowerInvariant();
        var sel = fsql.Select<Spell>()
            .WhereIf(
                !string.IsNullOrWhiteSpace(term),
                s => s.NameZh.Contains(term!) || s.NameEn.ToLower().Contains(lowerTerm!))
            .WhereIf(!string.IsNullOrEmpty(q.Source), s => s.Source == q.Source)
            .WhereIf(!string.IsNullOrEmpty(q.FirstLetter), s => s.FirstLetter == q.FirstLetter);

        // 环位/职业筛选走 spell_levels（EXISTS 子查询，命中 (class_name, level) 索引）。
        // 指定职业/领域时按其 class_name 精确匹配（含 domain 行）；仅指定环位时限定 kind='class' 的主职业行。
        var hasClass = !string.IsNullOrWhiteSpace(q.ClassName);
        var hasLevel = q.ClassLevel is >= 0 and <= 9;
        if (hasClass || hasLevel)
        {
            sel = sel.Where(s => fsql.Select<SpellLevel>().Any(sl =>
                sl.SpellId == s.Id
                && (!hasLevel || sl.Level == q.ClassLevel)
                && (hasClass ? sl.ClassName == q.ClassName : sl.Kind == "class")));
        }

        return sel;
    }

    public async Task<IReadOnlyList<Spell>> SearchAsync(SpellQuery query, CancellationToken ct = default) =>
        await Filtered(query)
            .OrderBy(s => s.NameEn)
            .Skip(query.Skip)
            .Take(query.Take)
            .ToListAsync(ct);

    public async Task<int> CountAsync(SpellQuery query, CancellationToken ct = default) =>
        (int)await Filtered(query).CountAsync(ct);

    // 去重与排序下推到 SQL（DISTINCT + ORDER BY），避免把整列拉进内存再处理。
    public async Task<IReadOnlyList<string>> GetSourcesAsync(CancellationToken ct = default) =>
        await fsql.Select<Spell>()
            .Where(s => s.Source != null && s.Source != "")
            .Distinct()
            .OrderBy(s => s.Source)
            .ToListAsync(s => s.Source, ct);

    public async Task<IReadOnlyList<string>> GetClassesAsync(CancellationToken ct = default) =>
        await fsql.Select<SpellLevel>()
            .Where(sl => sl.ClassName != null && sl.ClassName != "")
            .Distinct()
            .OrderBy(sl => sl.ClassName)
            .ToListAsync(sl => sl.ClassName, ct);

    public async Task<Spell?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await fsql.Select<Spell>().Where(s => s.Id == id).FirstAsync(ct);

    public async Task<IReadOnlyList<SpellBuff>> GetBuffsForSpellAsync(string? nameEn, string? nameZh, CancellationToken ct = default)
    {
        var lowerEnglish = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim().ToLowerInvariant();
        var chinese = string.IsNullOrWhiteSpace(nameZh) ? null : nameZh.Trim();
        if (lowerEnglish is null && chinese is null)
        {
            return [];
        }

        return await fsql.Select<SpellBuff>()
            .Where(b => (b.NameEn != null && b.NameEn.ToLower() == lowerEnglish)
                || (b.NameZh != null && b.NameZh == chinese))
            .OrderBy(b => b.SortOrder)
            .ToListAsync(ct);
    }
}
