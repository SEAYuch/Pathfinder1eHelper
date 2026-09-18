namespace Pathfinder1eHelper.Services;

/// <summary>怪物查询条件（不可变）。</summary>
public sealed record MonsterQuery(
    string? Term,
    string? FirstLetter,
    string? CreatureType,
    int Skip,
    int Take);
