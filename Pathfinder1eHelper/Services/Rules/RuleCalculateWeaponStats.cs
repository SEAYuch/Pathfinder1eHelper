using System;
using System.Collections.Generic;
using System.Linq;
using Pathfinder1eHelper.Models.Combat;

namespace Pathfinder1eHelper.Services.Rules;

/// <summary>武器攻击/伤害计算入参，对应 WotR <c>RuleCalculateWeaponStats</c> / <c>RuleCalculateAttacksCount</c>。</summary>
public sealed record WeaponStatsRequest
{
    public int BaseAttackBonus { get; init; }

    public int AttackAbilityBonus { get; init; }

    public int DamageAbilityBonus { get; init; }

    public string DamageAbilityLabel { get; init; } = "力量";

    /// <summary>伤害属性倍率：主手 1、双手 1.5、副手 0.5（负属性不乘）。</summary>
    public double DamageAbilityMultiplier { get; init; } = 1.0;

    public int Enhancement { get; init; }

    public int AttackPenalty { get; init; }

    public bool AttackIsMelee { get; init; }

    /// <summary>
    /// 伤害加值减半（复刻 WotR <c>RuleCalculateWeaponStats.HalfDamageBonus</c>，由
    /// <c>TwoWeaponFightingDamagePenalty</c> 在用副手武器攻击时置位）。
    /// 只减半「加值」部分（属性×倍率 + 武器增强 + 各修饰），基础伤害骰不变。
    /// </summary>
    public bool HalfDamageBonus { get; init; }

    /// <summary>基础伤害骰文本（未缩放），如 <c>2d6</c>。</summary>
    public string BaseDamageDice { get; init; } = "1d4";

    /// <summary>武器体型（伤害骰缩放的基准体型）。</summary>
    public SizeCategory WeaponSize { get; init; } = SizeCategory.Medium;

    /// <summary>武器体型偏移（+1 增大一档）。</summary>
    public int DamageDiceSizeShift { get; init; }

    /// <summary>为 true 时不缩放伤害骰。</summary>
    public bool DoNotScaleDamage { get; init; }

    public int CriticalThreatLow { get; init; } = 20;

    public int CriticalMultiplier { get; init; } = 2;

    public bool TargetIsFlanked { get; init; }

    public bool AttackerHasConcealmentAdvantage { get; init; }

    public bool ShootIntoCombatPenalty { get; init; }

    public IReadOnlyList<Modifier> AdditionalAttackBonus { get; init; } = [];

    public IReadOnlyList<Modifier> DamageModifiers { get; init; } = [];
}

/// <summary>武器攻击/伤害结果。</summary>
public sealed record WeaponStatsResult(
    StatResult Attack,
    StatResult Damage,
    int AttacksCount,
    int CriticalThreatLow,
    int CriticalMultiplier,
    double DamageAbilityMultiplier,
    string DamageDice);

/// <summary>
/// 武器数值，对应 WotR <c>RuleCalculateWeaponStats</c>：
/// 攻击沿用 <see cref="RuleCalculateAttackBonus"/>；伤害 = 属性×倍率（负属性不乘）+ 增强 + 修饰；
/// 多重攻击数由 BAB 推导。
/// </summary>
public static class RuleCalculateWeaponStats
{
    public const int IterativeStep = 5;
    public const int MaxExtraAttacks = 3;

    public static WeaponStatsResult Compute(WeaponStatsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var attack = RuleCalculateAttackBonus.Compute(new AttackBonusRequest
        {
            BaseAttackBonus = request.BaseAttackBonus,
            AbilityBonus = request.AttackAbilityBonus,
            AttackPenalty = request.AttackPenalty,
            WeaponEnhancement = request.Enhancement,
            AdditionalAttackBonus = request.AdditionalAttackBonus,
            IsMelee = request.AttackIsMelee,
            TargetIsFlanked = request.TargetIsFlanked,
            AttackerHasConcealmentAdvantage = request.AttackerHasConcealmentAdvantage,
            ShootIntoCombatPenalty = request.ShootIntoCombatPenalty,
        });

        var contributions = new List<Contribution>
        {
            new($"{request.DamageAbilityLabel}×{request.DamageAbilityMultiplier:0.##}", AbilityDamage(request.DamageAbilityBonus, request.DamageAbilityMultiplier)),
        };

        if (request.Enhancement > 0)
        {
            contributions.Add(new Contribution("武器增强", request.Enhancement, Descriptor: ModifierDescriptor.Enhancement));
        }

        contributions.AddRange(ModifierEngine.Evaluate(0, request.DamageModifiers)
            .Contributions.Select(RuleMath.FromModifier));

        var effectiveSize = request.WeaponSize.Shift(request.DamageDiceSizeShift);
        var damageDice = WeaponDamageScaleTable.ScaleToText(
            request.BaseDamageDice, effectiveSize, SizeCategory.Medium, request.DoNotScaleDamage);

        var damage = RuleMath.Stat(contributions);
        if (request.HalfDamageBonus)
        {
            damage = HalveDamageBonus(damage);
        }

        return new WeaponStatsResult(
            attack,
            damage,
            AttacksCount(request.BaseAttackBonus),
            request.CriticalThreatLow,
            request.CriticalMultiplier,
            request.DamageAbilityMultiplier,
            damageDice);
    }

    /// <summary>
    /// 双武器战斗：副手伤害加值减半（游戏 <c>TwoWeaponFightingDamagePenalty</c>）。
    /// 加值合计向下取整减半，并补一行差额使明细仍能对上总数；基础伤害骰不减半。
    /// </summary>
    private static StatResult HalveDamageBonus(StatResult damage)
    {
        if (damage.Total == 0)
        {
            return damage;
        }

        var halved = (int)Math.Floor(damage.Total / 2.0);
        var delta = halved - damage.Total;
        if (delta == 0)
        {
            return damage;
        }

        var list = new List<Contribution>(damage.Contributions)
        {
            new("副手伤害加值减半", delta, Note: $"{damage.Total} → {halved}"),
        };
        return new StatResult(halved, list);
    }

    /// <summary>BAB 带来的额外减益攻击数：<c>max(0, BAB/5 − (BAB%5==0 ? 1 : 0))</c>，上限 3。</summary>
    public static int ExtraAttacks(int baseAttackBonus)
    {
        var extra = baseAttackBonus / 5 - (baseAttackBonus % 5 == 0 ? 1 : 0);
        return Math.Min(MaxExtraAttacks, Math.Max(0, extra));
    }

    /// <summary>总攻击次数（含基础一击）。</summary>
    public static int AttacksCount(int baseAttackBonus) => 1 + ExtraAttacks(baseAttackBonus);

    private static int AbilityDamage(int abilityBonus, double multiplier) =>
        abilityBonus < 0 ? abilityBonus : (int)(abilityBonus * multiplier);
}
