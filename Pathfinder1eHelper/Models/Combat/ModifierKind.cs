namespace Pathfinder1eHelper.Models.Combat;

/// <summary>修饰条目类型。<see cref="PowerAttack"/> 为特殊条目：不直接取值，而由计算层按 BAB 与握法展开。</summary>
public enum ModifierKind
{
    /// <summary>普通加值/减值，按描述符与通道参与叠加。</summary>
    Normal,

    /// <summary>「猛力攻击」：近战攻击减值换伤害，数值随 BAB 与握法自动计算。</summary>
    PowerAttack,
}
