using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pathfinder1eHelper.Models;

namespace Pathfinder1eHelper.Services;

/// <summary>
/// Default <see cref="ISpellService"/>. Normalises queries (trims the term, clamps the page size)
/// before delegating to the repository.
/// </summary>
public sealed class SpellService(ISpellRepository repository) : ISpellService
{
    /// <summary>Default page size used when a query supplies a non-positive <see cref="SpellQuery.Take"/>.</summary>
    public const int DefaultPageSize = 200;

    public Task<IReadOnlyList<Spell>> SearchAsync(SpellQuery query, CancellationToken ct = default) =>
        repository.SearchAsync(Normalize(query), ct);

    public Task<int> CountAsync(SpellQuery query, CancellationToken ct = default) =>
        repository.CountAsync(Normalize(query), ct);

    public Task<IReadOnlyList<string>> GetSourcesAsync(CancellationToken ct = default) =>
        repository.GetSourcesAsync(ct);

    public Task<Spell?> GetByIdAsync(int id, CancellationToken ct = default) =>
        repository.GetByIdAsync(id, ct);

    internal static SpellQuery Normalize(SpellQuery query)
    {
        var term = string.IsNullOrWhiteSpace(query.Term) ? null : query.Term.Trim();
        var skip = query.Skip < 0 ? 0 : query.Skip;
        var take = query.Take <= 0 ? DefaultPageSize : query.Take;
        return query with { Term = term, Skip = skip, Take = take };
    }
}
