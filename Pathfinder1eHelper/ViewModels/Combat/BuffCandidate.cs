namespace Pathfinder1eHelper.ViewModels.Combat;

/// <summary>Buff 候选来源：法术（spell_buffs）/ 专长（feat_buffs）。</summary>
public enum BuffCandidateKind
{
    Spell,
    Feat,
}

/// <summary>
/// 战斗页“Buff 联动”搜索候选：统一法术与专长，标注来源类型，便于合并到一个自动完成框。
/// </summary>
public sealed record BuffCandidate(string? NameEn, string? NameZh, string Source, BuffCandidateKind Kind)
{
    /// <summary>来源显示文本（法术 / 专长）。</summary>
    public string KindDisplay => Kind == BuffCandidateKind.Spell ? "法术" : "专长";

    /// <summary>供 AutoCompleteBox 做文本匹配/显示（同时包含中英文名）。</summary>
    public string DisplayName =>
        string.IsNullOrWhiteSpace(NameEn) ? NameZh ?? string.Empty :
        string.IsNullOrWhiteSpace(NameZh) ? NameEn :
        $"{NameZh} · {NameEn}";

    public override string ToString() => DisplayName;
}
