using FreeSql.DataAnnotations;

namespace Pathfinder1eHelper.Models;

/// <summary>
/// FreeSql entity mapped to the read-only <c>spells</c> table in <c>spells.duckdb</c>
/// (~3373 rows, 101 source books; rebuilt from pf_searcher_v1.0 JSON, with legacy-only rows
/// back-filled so nothing is lost). Columns are snake_case in the DB; each property maps explicitly.
/// <para>
/// <see cref="TableAttribute.DisableSyncStructure"/> is set so FreeSql never attempts DDL against
/// the reference database. Many detail fields are frequently NULL in the source data, hence the
/// nullable strings; the same spell can appear under multiple <see cref="Source"/> books.
/// </para>
/// </summary>
[Table(Name = "spells", DisableSyncStructure = true)]
public sealed class Spell
{
    [Column(Name = "id", IsPrimary = true)]
    public int Id { get; set; }

    [Column(Name = "source")] public string Source { get; set; } = string.Empty;
    [Column(Name = "source_zh")] public string? SourceZh { get; set; }
    [Column(Name = "source_en")] public string? SourceEn { get; set; }

    [Column(Name = "name_zh")] public string NameZh { get; set; } = string.Empty;
    [Column(Name = "name_en")] public string NameEn { get; set; } = string.Empty;
    [Column(Name = "first_letter")] public string FirstLetter { get; set; } = string.Empty;

    [Column(Name = "school")] public string? School { get; set; }
    [Column(Name = "level")] public string? Level { get; set; }
    [Column(Name = "casting_time")] public string? CastingTime { get; set; }
    [Column(Name = "components")] public string? Components { get; set; }
    [Column(Name = "range")] public string? Range { get; set; }
    [Column(Name = "effect")] public string? Effect { get; set; }
    [Column(Name = "area")] public string? Area { get; set; }
    [Column(Name = "targets")] public string? Targets { get; set; }
    [Column(Name = "duration")] public string? Duration { get; set; }
    [Column(Name = "saving_throw")] public string? SavingThrow { get; set; }
    [Column(Name = "spell_resistance")] public string? SpellResistance { get; set; }
    [Column(Name = "description")] public string? Description { get; set; }
    [Column(Name = "extra")] public string? Extra { get; set; }
    [Column(Name = "spell_type")] public string? SpellType { get; set; }

    /// <summary>Convenience display name combining Chinese and English names when both are present.</summary>
    public string DisplayName =>
        string.IsNullOrWhiteSpace(NameEn) ? NameZh :
        string.IsNullOrWhiteSpace(NameZh) ? NameEn :
        $"{NameZh} · {NameEn}";

    /// <summary>出处显示文本：中文名存在时为“代码（中文名）”，否则仅代码。</summary>
    public string SourceDisplay =>
        string.IsNullOrWhiteSpace(SourceZh) ? Source : $"{Source}（{SourceZh}）";

    /// <summary>供 AutoCompleteBox 等做文本匹配/显示（同时包含中英文名，便于两种搜索）。</summary>
    public override string ToString() => DisplayName;
}