namespace Pathfinder1eHelper.Models.Combat;

/// <summary>
/// 战斗机动（CRB/KPoP「战斗机动」章节）标记，挂在 <see cref="ModifierEntry"/> 上用于识别
/// 「防御式战斗」「全防御」这类<strong>姿态条目</strong>。姿态本身是普通修饰条目，
/// 由计算层按条目的描述符与通道直接叠加（不再有与专长互斥的特判）。
/// <para>姿态本身是普通修饰条目（可叠加、可停用、可删除），因此做成条目标签而不是档案状态。</para>
/// </summary>
public enum CombatStance
{
    /// <summary>防御式战斗：AC +2 闪避、攻击检定 −4。</summary>
    DefensiveFighting,

    /// <summary>全防御：AC +4 闪避、无攻击减值。</summary>
    TotalDefense,
}
