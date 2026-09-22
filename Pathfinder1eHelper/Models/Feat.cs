using FreeSql.DataAnnotations;

namespace Pathfinder1eHelper.Models;

/// <summary>
/// FreeSql entity mapped to the read-only <c>feats</c> table（Pathfinder 专长，约 1178 条 /
/// 9 个出处）。中/英文名、首字母、类型、先决条件、简表效果与详述效果。
/// </summary>
[Table(Name = "feats", DisableSyncStructure = true)]
public sealed class Feat
{
    [Column(Name = "id", IsPrimary = true)] public int Id { get; set; }

    /// <summary>出处：CRB / APG / ARG / UM / UC / UCa / ACG / UI / B1。</summary>
    [Column(Name = "source")] public string Source { get; set; } = string.Empty;

    [Column(Name = "name_zh")] public string NameZh { get; set; } = string.Empty;

    [Column(Name = "name_en")] public string? NameEn { get; set; }

    /// <summary>英文名首字母 A–Z（取不到为 <c>#</c>）。</summary>
    [Column(Name = "first_letter")] public string FirstLetter { get; set; } = string.Empty;

    /// <summary>专长类型标签（如 战斗/团队/超魔/造物），无标签为 通用；可含多个。</summary>
    [Column(Name = "feat_type")] public string FeatType { get; set; } = "通用";

    /// <summary>简表中英文名尾部 <c>*</c> = 战士奖励专长。</summary>
    [Column(Name = "is_fighter_bonus")] public bool IsFighterBonus { get; set; }

    [Column(Name = "prerequisites")] public string? Prerequisites { get; set; }

    /// <summary>简表一句话效果。</summary>
    [Column(Name = "summary")] public string? Summary { get; set; }

    /// <summary>详述“专长效果”全文（含 通常状况/特殊 段）。</summary>
    [Column(Name = "benefit")] public string? Benefit { get; set; }

    /// <summary>风味句。</summary>
    [Column(Name = "flavor")] public string? Flavor { get; set; }

    /// <summary>Convenience display name combining Chinese and English names when both are present.</summary>
    public string DisplayName =>
        string.IsNullOrWhiteSpace(NameEn) ? NameZh :
        string.IsNullOrWhiteSpace(NameZh) ? NameEn :
        $"{NameZh} · {NameEn}";

    /// <summary>是否战士奖励专长的显示文本。</summary>
    public string FighterBonusDisplay => IsFighterBonus ? "是" : "否";

    /// <summary>供 AutoCompleteBox 等做文本匹配/显示（同时包含中英文名，便于两种搜索）。</summary>
    public override string ToString() => DisplayName;
}
