using FreeSql.DataAnnotations;

namespace Pathfinder1eHelper.Models;

/// <summary>
/// FreeSql entity mapped to the read-only <c>monsters</c> table (Pathfinder Bestiary 1-3 data).
/// Names are kept verbatim from the source; <see cref="FirstLetter"/> is derived from the English name.
/// </summary>
[Table(Name = "monsters", DisableSyncStructure = true)]
public sealed class Monster
{
    [Column(Name = "id", IsPrimary = true)] public int Id { get; set; }

    /// <summary>出处：B1 / B2 / B3（怪物图鉴 I/II/III）。</summary>
    [Column(Name = "source")] public string Source { get; set; } = "";

    /// <summary>CHM 页面文件名，如 <c>100.html</c>。</summary>
    [Column(Name = "page")] public string Page { get; set; } = "";

    [Column(Name = "name_zh")] public string NameZh { get; set; } = "";

    [Column(Name = "name_en")] public string? NameEn { get; set; }

    /// <summary>英文名首字母 A–Z（取不到为 <c>#</c>）。</summary>
    [Column(Name = "first_letter")] public string FirstLetter { get; set; } = "";

    [Column(Name = "cr")] public string? Cr { get; set; }

    [Column(Name = "size")] public string? Size { get; set; }

    [Column(Name = "creature_type")] public string? CreatureType { get; set; }

    [Column(Name = "subtypes")] public string? Subtypes { get; set; }

    [Column(Name = "alignment")] public string? Alignment { get; set; }

    [Column(Name = "strength")] public int? Strength { get; set; }

    [Column(Name = "dexterity")] public int? Dexterity { get; set; }

    [Column(Name = "constitution")] public int? Constitution { get; set; }

    [Column(Name = "intelligence")] public int? Intelligence { get; set; }

    [Column(Name = "wisdom")] public int? Wisdom { get; set; }

    [Column(Name = "charisma")] public int? Charisma { get; set; }

    [Column(Name = "environment")] public string? Environment { get; set; }

    [Column(Name = "organization")] public string? Organization { get; set; }

    [Column(Name = "treasure")] public string? Treasure { get; set; }

    [Column(Name = "description")] public string? Description { get; set; }

    /// <summary>基值/防御/进攻/属性/生态等分段原文。</summary>
    [Column(Name = "stat_block")] public string? StatBlock { get; set; }

    [Column(Name = "special_abilities")] public string? SpecialAbilities { get; set; }

    /// <summary>整页纯文本（兜底）。</summary>
    [Column(Name = "raw_text")] public string? RawText { get; set; }
}
