using FreeSql.DataAnnotations;

namespace Pathfinder1eHelper.Models;

/// <summary>
/// FreeSql entity mapped to the read-only <c>spell_buffs</c> table: structured bonus effects of
/// common buff spells, keyed by spell English/Chinese name. Populated on the DB side (see
/// <c>spell_buffs.sql</c>), so the conversion data lives in the database rather than in code.
/// </summary>
[Table(Name = "spell_buffs", DisableSyncStructure = true)]
public sealed class SpellBuff
{
    [Column(Name = "id", IsPrimary = true)] public int Id { get; set; }

    [Column(Name = "spell_id")] public int? SpellId { get; set; }

    [Column(Name = "name_en")] public string? NameEn { get; set; }

    [Column(Name = "name_zh")] public string? NameZh { get; set; }

    [Column(Name = "effect_name")] public string EffectName { get; set; } = string.Empty;

    /// <summary>加值类型（<c>BonusType</c> 枚举名）。</summary>
    [Column(Name = "bonus_type")] public string BonusType { get; set; } = string.Empty;

    /// <summary>作用目标（<c>BonusTarget</c> 枚举名）。</summary>
    [Column(Name = "target")] public string Target { get; set; } = string.Empty;

    [Column(Name = "value")] public int? Value { get; set; }

    /// <summary>增强对象（<c>EnhancementSubject</c> 枚举名，可空）。</summary>
    [Column(Name = "enhancement_subject")] public string? EnhancementSubject { get; set; }

    /// <summary>属性（<c>Ability</c> 枚举名，可空）。</summary>
    [Column(Name = "ability")] public string? Ability { get; set; }

    [Column(Name = "scale_base")] public int? ScaleBase { get; set; }

    [Column(Name = "scale_offset")] public int? ScaleOffset { get; set; }

    [Column(Name = "scale_step")] public int? ScaleStep { get; set; }

    [Column(Name = "scale_min")] public int? ScaleMin { get; set; }

    [Column(Name = "scale_max")] public int? ScaleMax { get; set; }

    [Column(Name = "notes")] public string? Notes { get; set; }

    [Column(Name = "sort_order")] public int? SortOrder { get; set; }
}
