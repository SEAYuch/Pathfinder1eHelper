namespace Pathfinder1eHelper.Models.Combat;

/// <summary>加值条目的来源，用于区分手动条目与临时效果（一键清除只清临时效果）。</summary>
public enum BonusOrigin
{
    /// <summary>玩家手动添加/编辑，视为长期生效。</summary>
    Manual,

    /// <summary>
    /// 历史来源：Buff 联动功能已随 <c>spell_buffs</c>/<c>feat_buffs</c> 表一并移除，此成员只用于旧档兼容。
    /// </summary>
    /// <remarks>
    /// 保留成员是存档兼容性要求：枚举按字符串序列化，旧档里可能已有 "SpellBuff"，
    /// 删掉会让 <see cref="System.Text.Json.Serialization.JsonStringEnumConverter"/> 抛错，
    /// 整份角色随后被 <c>LoadAll</c> 跳过（角色凭空消失）。
    /// </remarks>
    SpellBuff,

    /// <summary>
    /// 历史来源：Buff 联动功能已随 <c>spell_buffs</c>/<c>feat_buffs</c> 表一并移除，此成员只用于旧档兼容（旧档中确有此项）。
    /// </summary>
    /// <remarks>同上：为旧档兼容而保留，勿删。</remarks>
    FeatBuff,

    /// <summary>由状态预设按钮添加。</summary>
    Preset,
}
