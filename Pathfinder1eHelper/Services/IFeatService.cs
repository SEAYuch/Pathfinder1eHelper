using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pathfinder1eHelper.Models;

namespace Pathfinder1eHelper.Services;

/// <summary>
/// Search/filter orchestration over <see cref="IFeatRepository"/>. Keeps view models thin by
/// centralising query normalisation (trimming, sensible paging defaults).
/// </summary>
public interface IFeatService
{
    Task<IReadOnlyList<Feat>> SearchAsync(FeatQuery query, CancellationToken ct = default);
    Task<int> CountAsync(FeatQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetSourcesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetTypesAsync(CancellationToken ct = default);

    /// <summary>Returns structured buff effects (feat_buffs) matching a feat name.</summary>
    Task<IReadOnlyList<FeatBuff>> GetBuffsForFeatAsync(string? nameEn, string? nameZh, CancellationToken ct = default);
}
