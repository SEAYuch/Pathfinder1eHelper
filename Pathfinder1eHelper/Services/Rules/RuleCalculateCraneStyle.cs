namespace Pathfinder1eHelper.Services.Rules;

/// <summary>
/// 白鹤拳（UC 流浪者）的数值。CHM 原文：
/// 「你在进行防御式战斗时攻击骰只受 -2 减值。你使用白鹤拳流派架势时，
/// 从防御式战斗与全回合防御获得的 AC 闪避加值 +1。」
/// <para>
/// AC 闪避 +1 与基准无关，作为普通修饰条目收录；「−4 降为 −2」则必须知道当前是否处于
/// 防御式战斗，故做成规则条目：检测到启用的 <see cref="Models.Combat.CombatStance.DefensiveFighting"/>
/// 条目时，补偿 +2 使净减值变为 −2。
/// </para>
/// </summary>
public static class RuleCalculateCraneStyle
{
    /// <summary>防御式战斗的攻击减值（CHM「战斗机动」：−4）。</summary>
    public const int DefensiveFightingAttackPenalty = -4;

    /// <summary>白鹤拳把该减值放宽到的档位（CHM：「只受 -2 减值」）。</summary>
    public const int DefensiveFightingAttackPenaltyWithCraneStyle = -2;

    /// <summary>
    /// 白鹤拳提供的攻击减值补偿值：把 <see cref="DefensiveFightingAttackPenalty"/>
    /// 抬回 <see cref="DefensiveFightingAttackPenaltyWithCraneStyle"/>。
    /// 仅在启用防御式战斗时生效，否则会凭空多出 +2。
    /// </summary>
    public static int AttackRelief(bool hasCraneStyle, bool defensiveFightingActive) =>
        hasCraneStyle && defensiveFightingActive
            ? DefensiveFightingAttackPenaltyWithCraneStyle - DefensiveFightingAttackPenalty
            : 0;
}
