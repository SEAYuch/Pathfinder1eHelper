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
    private ISelect<Spell> Filtered(SpellQuery q) =>
        fsql.Select<Spell>()
            .WhereIf(
                !string.IsNullOrWhiteSpace(q.Term),
                s => s.NameZh.Contains(q.Term!) || s.NameEn.Contains(q.Term!))
            .WhereIf(!string.IsNullOrEmpty(q.Source), s => s.Source == q.Source)
            .WhereIf(!string.IsNullOrEmpty(q.FirstLetter), s => s.FirstLetter == q.FirstLetter);

    public async Task<IReadOnlyList<Spell>> SearchAsync(SpellQuery query, CancellationToken ct = default) =>
        await Filtered(query)
            .OrderBy(s => s.NameEn)
            .Skip(query.Skip)
            .Take(query.Take)
            .ToListAsync(ct);

    public async Task<int> CountAsync(SpellQuery query, CancellationToken ct = default) =>
        (int)await Filtered(query).CountAsync(ct);

    public async Task<IReadOnlyList<string>> GetSourcesAsync(CancellationToken ct = default)
    {
        var sources = await fsql.Select<Spell>().ToListAsync(s => s.Source, ct);
        return sources
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<Spell?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await fsql.Select<Spell>().Where(s => s.Id == id).FirstAsync(ct);
}
