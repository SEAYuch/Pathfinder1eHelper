namespace Pathfinder1eHelper.Models.Combat;

/// <summary>修饰条目类型。<see cref="PowerAttack"/> 为特殊条目：不直接取值，而由计算层按 BAB 与握法展开。</summary>
public enum ModifierKind
{
    /// <summary>普通加值/减值，按描述符与通道参与叠加。</summary>
    Normal,

    /// <summary>「猛力攻击」：近战攻击减值换伤害，数值随 BAB 与握法自动计算。</summary>
    PowerAttack,

    /// <summary>「寓守于攻」：近战攻击/战技减值换 AC 闪避加值，两者随 BAB 同步放大。</summary>
    CombatExpertise,

    /// <summary>「白鹤拳」：防御式战斗的 −4 攻击减值降为 −2（AC 闪避 +1 走普通条目）。</summary>
    CraneStyle,

    /// <summary>「双武器格斗」：双持时主手/副手各按固定档位承受攻击减值。</summary>
    TwoWeaponFighting,
}
