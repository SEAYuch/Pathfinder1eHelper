using FreeSql.DataAnnotations;

namespace Pathfinder1eHelper.Models;

/// <summary>
/// FreeSql entity mapped to the read-only <c>spells</c> table in <c>spells.duckdb</c>
/// (~1902 rows, 17 sources). Columns are snake_case in the DB; each property maps explicitly.
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

    [Column(Name = "source")] public string Source { get; set; } = "";
    [Column(Name = "source_zh")] public string? SourceZh { get; set; }
    [Column(Name = "source_en")] public string? SourceEn { get; set; }

    [Column(Name = "name_zh")] public string NameZh { get; set; } = "";
    [Column(Name = "name_en")] public string NameEn { get; set; } = "";
    [Column(Name = "first_letter")] public string FirstLetter { get; set; } = "";

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

    /// <summary>Convenience display name combining Chinese and English names when both are present.</summary>
    public string DisplayName =>
        string.IsNullOrWhiteSpace(NameEn) ? NameZh :
        string.IsNullOrWhiteSpace(NameZh) ? NameEn :
        $"{NameZh} · {NameEn}";

    /// <summary>出处显示文本：中文名存在时为“代码（中文名）”，否则仅代码。</summary>
    public string SourceDisplay =>
        string.IsNullOrWhiteSpace(SourceZh) ? Source : $"{Source}（{SourceZh}）";
}