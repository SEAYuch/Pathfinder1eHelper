using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pathfinder1eHelper.Models;

namespace Pathfinder1eHelper.Services;

/// <summary>Read-only data access for the <c>feats</c> reference table.</summary>
public interface IFeatRepository
{
    /// <summary>Returns a page of feats matching <paramref name="query"/>, ordered by English name.</summary>
    Task<IReadOnlyList<Feat>> SearchAsync(FeatQuery query, CancellationToken ct = default);

    /// <summary>Returns the total number of feats matching <paramref name="query"/> (ignoring paging).</summary>
    Task<int> CountAsync(FeatQuery query, CancellationToken ct = default);

    /// <summary>Returns the distinct set of source-book codes present in the data, sorted.</summary>
    Task<IReadOnlyList<string>> GetSourcesAsync(CancellationToken ct = default);

    /// <summary>Returns the distinct set of feat types (feat_type), sorted.</summary>
    Task<IReadOnlyList<string>> GetTypesAsync(CancellationToken ct = default);

    /// <summary>Returns structured buff effects (feat_buffs) matching a feat name, ordered by sort_order.</summary>
    Task<IReadOnlyList<FeatBuff>> GetBuffsForFeatAsync(string? nameEn, string? nameZh, CancellationToken ct = default);
}
