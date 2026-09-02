using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pathfinder1eHelper.Models;

namespace Pathfinder1eHelper.Services;

/// <summary>Read-only data access for the <c>spells</c> reference table.</summary>
public interface ISpellRepository
{
    /// <summary>Returns a page of spells matching <paramref name="query"/>, ordered by English name.</summary>
    Task<IReadOnlyList<Spell>> SearchAsync(SpellQuery query, CancellationToken ct = default);

    /// <summary>Returns the total number of spells matching <paramref name="query"/> (ignoring paging).</summary>
    Task<int> CountAsync(SpellQuery query, CancellationToken ct = default);

    /// <summary>Returns the distinct set of source-book codes present in the data, sorted.</summary>
    Task<IReadOnlyList<string>> GetSourcesAsync(CancellationToken ct = default);

    /// <summary>Loads a single spell by primary key, or null if not found.</summary>
    Task<Spell?> GetByIdAsync(int id, CancellationToken ct = default);
}
