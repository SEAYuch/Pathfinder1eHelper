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

    public Task<IReadOnlyList<FeatBuff>> GetBuffsForFeatAsync(string? nameEn, string? nameZh, CancellationToken ct = default) =>
        repository.GetBuffsForFeatAsync(nameEn, nameZh, ct);

    internal static FeatQuery Normalize(FeatQuery query)
    {
        var term = string.IsNullOrWhiteSpace(query.Term) ? null : query.Term.Trim();
        var source = string.IsNullOrWhiteSpace(query.Source) ? null : query.Source.Trim();
        var letter = string.IsNullOrWhiteSpace(query.FirstLetter) ? null : query.FirstLetter.Trim();
        var type = string.IsNullOrWhiteSpace(query.Type) ? null : query.Type.Trim();
        var skip = query.Skip < 0 ? 0 : query.Skip;
        var take = query.Take <= 0 ? DefaultPageSize : query.Take;
        return query with { Term = term, Source = source, FirstLetter = letter, Type = type, Skip = skip, Take = take };
    }
}
