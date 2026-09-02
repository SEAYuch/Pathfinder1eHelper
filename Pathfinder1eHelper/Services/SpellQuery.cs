namespace Pathfinder1eHelper.Services;

/// <summary>
/// Immutable search/filter descriptor for spell queries.
/// </summary>
/// <param name="Term">Free-text term matched against Chinese/English names (null/blank = no filter).</param>
/// <param name="Source">Exact source-book code filter, e.g. <c>CRB</c> (null/blank = all sources).</param>
/// <param name="FirstLetter">Exact English first-letter filter A–Z (null/blank = all letters).</param>
/// <param name="Skip">Rows to skip (paging offset).</param>
/// <param name="Take">Max rows to return (page size).</param>
public sealed record SpellQuery(
    string? Term,
    string? Source,
    string? FirstLetter,
    int Skip,
    int Take);
