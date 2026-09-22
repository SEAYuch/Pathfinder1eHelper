namespace Pathfinder1eHelper.Models.Combat;

/// <summary>加值条目的来源，用于区分手动条目与临时效果（一键清除只清临时效果）。</summary>
public enum BonusOrigin
{
    /// <summary>玩家手动添加/编辑，视为长期生效。</summary>
    Manual,

    /// <summary>由“Buff 联动”从 spell_buffs 表添加。</summary>
    SpellBuff,

    /// <summary>由“Buff 联动”从 feat_buffs 表添加。</summary>
    FeatBuff,

    /// <summary>由状态预设按钮添加。</summary>
    Preset,
}
