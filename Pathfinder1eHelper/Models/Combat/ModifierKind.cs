namespace Pathfinder1eHelper.Models.Combat;

/// <summary>
/// 修饰条目类型。普通条目按描述符与通道参与叠加；其余成员是**历史遗留**：
/// 「猛力攻击」「寓守于攻」「白鹤拳」「双武器格斗」四个规则已移除，保留枚举值只为让含这些
/// <c>kind</c> 字符串的旧存档仍能反序列化（<see cref="Services.CharacterProfileCodec"/> 用
/// <c>JsonStringEnumConverter</c> 按名字存取，删除成员会让整份旧档读取失败被跳过）。
/// 这些条目现在与普通条目同样按 <see cref="ModifierEntry.Value"/> 参与叠加。
/// </summary>
public enum ModifierKind
{
    /// <summary>普通加值/减值，按描述符与通道参与叠加。</summary>
    Normal,

    /// <summary>仅用于旧档兼容（「猛力攻击」规则已移除）。</summary>
    PowerAttack,

    /// <summary>仅用于旧档兼容（「寓守于攻」规则已移除）。</summary>
    CombatExpertise,

    /// <summary>仅用于旧档兼容（「白鹤拳」规则已移除）。</summary>
    CraneStyle,

    /// <summary>仅用于旧档兼容（「双武器格斗」规则已移除）。</summary>
    TwoWeaponFighting,
}
