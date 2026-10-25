using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pathfinder1eHelper.Models;

namespace Pathfinder1eHelper.Services;

/// <summary>
/// Default <see cref="IFeatService"/>. Normalises queries (trims the term, clamps the page size)
/// before delegating to the repository.
/// </summary>
public sealed class FeatService(IFeatRepository repository) : IFeatService
{
    /// <summary>Default page size used when a query supplies a non-positive <see cref="FeatQuery.Take"/>.</summary>
    public const int DefaultPageSize = 200;

    public Task<IReadOnlyList<Feat>> SearchAsync(FeatQuery query, CancellationToken ct = default) =>
        repository.SearchAsync(Normalize(query), ct);

    public Task<int> CountAsync(FeatQuery query, CancellationToken ct = default) =>
        repository.CountAsync(Normalize(query), ct);

    public Task<IReadOnlyList<string>> GetSourcesAsync(CancellationToken ct = default) =>
        repository.GetSourcesAsync(ct);

    public Task<IReadOnlyList<string>> GetTypesAsync(CancellationToken ct = default) =>
        repository.GetTypesAsync(ct);

    internal static FeatQuery Normalize(FeatQuery query) =>
        query with
        {
            Term = QueryNormalization.Optional(query.Term),
            Source = QueryNormalization.Optional(query.Source),
            FirstLetter = QueryNormalization.Optional(query.FirstLetter),
            Type = QueryNormalization.Optional(query.Type),
            Skip = QueryNormalization.Skip(query.Skip),
            Take = QueryNormalization.Take(query.Take, DefaultPageSize),
        };
}
