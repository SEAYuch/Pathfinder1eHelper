using FreeSql.DataAnnotations;

namespace Pathfinder1eHelper.Models;

/// <summary>
/// FreeSql entity mapped to the read-only <c>monster_groups</c> table: the “绪论/概述” pages
/// (e.g. 龙类绪论) that individual monster entries relate to.
/// </summary>
[Table(Name = "monster_groups", DisableSyncStructure = true)]
public sealed class MonsterGroup
{
    [Column(Name = "id", IsPrimary = true)] public int Id { get; set; }

    [Column(Name = "source")] public string Source { get; set; } = "";

    [Column(Name = "page")] public string Page { get; set; } = "";

    [Column(Name = "name_zh")] public string NameZh { get; set; } = "";

    [Column(Name = "name_en")] public string? NameEn { get; set; }

    [Column(Name = "description")] public string? Description { get; set; }

    /// <summary>可渲染正文（段落 + GFM 管道表），供详情页展示。</summary>
    [Column(Name = "content")] public string? Content { get; set; }

    [Column(Name = "raw_text")] public string? RawText { get; set; }
}
