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

    public Task<IReadOnlyList<string>> GetClassesAsync(CancellationToken ct = default) =>
        repository.GetClassesAsync(ct);

    public Task<Spell?> GetByIdAsync(int id, CancellationToken ct = default) =>
        repository.GetByIdAsync(id, ct);

    internal static SpellQuery Normalize(SpellQuery query) =>
        query with
        {
            Term = QueryNormalization.Optional(query.Term),
            Source = QueryNormalization.Optional(query.Source),
            FirstLetter = QueryNormalization.Optional(query.FirstLetter),
            Skip = QueryNormalization.Skip(query.Skip),
            Take = QueryNormalization.Take(query.Take, DefaultPageSize),
            ClassName = QueryNormalization.Optional(query.ClassName),
            // 环位只接受 0–9（PF 1e 戏法~九环），越界视为不筛选。
            ClassLevel = query.ClassLevel is >= 0 and <= 9 ? query.ClassLevel : null,
        };
}
