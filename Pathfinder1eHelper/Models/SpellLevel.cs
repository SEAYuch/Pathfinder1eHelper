using FreeSql.DataAnnotations;

namespace Pathfinder1eHelper.Models;

/// <summary>
/// FreeSql entity mapped to the read-only <c>spell_levels</c> table: a 1:N “per spell, per
/// class/domain + level” index built by splitting <c>spells.level</c> text (see the DB-side
/// build script). One spell can have many rows here.
/// </summary>
/// <remarks>
/// 复合主键 (spell_id, class_name, kind) 与库中 PK 一致；level 非主键。
/// </remarks>
[Table(Name = "spell_levels", DisableSyncStructure = true)]
public sealed class SpellLevel
{
    [Column(Name = "spell_id", IsPrimary = true)] public int SpellId { get; set; }

    /// <summary>职业组名（如 <c>术士/法师</c>）或领域/子域名（kind = domain 时）。</summary>
    [Column(Name = "class_name", IsPrimary = true)] public string ClassName { get; set; } = "";

    [Column(Name = "level")] public int Level { get; set; }

    /// <summary><c>class</c>（主职业）或 <c>domain</c>（领域/子域）。</summary>
    [Column(Name = "kind", IsPrimary = true)] public string Kind { get; set; } = "";
}
