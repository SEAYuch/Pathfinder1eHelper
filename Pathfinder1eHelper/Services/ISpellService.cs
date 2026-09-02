using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pathfinder1eHelper.Models;

namespace Pathfinder1eHelper.Services;

/// <summary>
/// Search/filter orchestration over <see cref="ISpellRepository"/>. Keeps view models thin by
/// centralising query normalisation (trimming, sensible paging defaults).
/// </summary>
public interface ISpellService
{
    Task<IReadOnlyList<Spell>> SearchAsync(SpellQuery query, CancellationToken ct = default);
    Task<int> CountAsync(SpellQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetSourcesAsync(CancellationToken ct = default);
    Task<Spell?> GetByIdAsync(int id, CancellationToken ct = default);
}
