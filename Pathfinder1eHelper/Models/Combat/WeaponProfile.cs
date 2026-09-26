using System;

namespace Pathfinder1eHelper.Models.Combat;

/// <summary>
/// 一把武器/一种攻击方式，字段按 WotR <c>ItemEntityWeapon</c> / <c>RuleCalculateWeaponStats</c>
/// 的语义设计：攻击方式、持握方式、属性来源、伤害骰与重击参数。
/// </summary>
public sealed class WeaponProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "近战武器";

    /// <summary>攻击方式（近战/接触/远程/远程接触）。</summary>
    public WeaponAttackType AttackType { get; set; } = WeaponAttackType.Melee;

    /// <summary>持握方式；决定伤害属性倍率（主手 1 / 双手 1.5 / 副手 0.5）。</summary>
    public WeaponHand Hand { get; set; } = WeaponHand.Primary;

    /// <summary>是否视为副手（伤害倍率 ×0.5；命中减值见 <see cref="Services.Rules.RuleCalculateTwoWeaponFighting"/>）。</summary>
    public bool IsSecondary { get; set; }

    /// <summary>武器分类（轻型/中型/重型）；「双武器战斗」据此判断副手是否为轻型。</summary>
    public WeaponCategory Category { get; set; } = WeaponCategory.Medium;

    /// <summary>
    /// 是否为双头武器（对应 WotR <c>ItemEntityWeaponBlueprint.Double</c>）。
    /// 游戏中主手持双头武器时，副手不再因非轻型而追加减值。
    /// </summary>
    public bool IsDouble { get; set; }

    /// <summary>命中属性；null 时按 DLL：近战用力量、远程用敏捷。</summary>
    public Ability? AttackBonusStat { get; set; }

    /// <summary>伤害属性；null 时近战默认力量，远程默认不计属性伤害（复合弓需显式指定力量）。</summary>
    public Ability? DamageBonusStat { get; set; }

    /// <summary>武器体型（用于伤害骰缩放）。</summary>
    public SizeCategory WeaponSize { get; set; } = SizeCategory.Medium;

    /// <summary>武器体型相对生物的增减步数（+1 增大一档）。</summary>
    public int DamageDiceSizeShift { get; set; }

    /// <summary>基础伤害骰，如 <c>1d8</c>、<c>2d6</c>。</summary>
    public string BaseDamage { get; set; } = "1d8";

    /// <summary>重击威胁下限（20 = 仅 20，19 = 19–20）。</summary>
    public int CriticalThreatLow { get; set; } = 20;

    /// <summary>重击倍率（默认 ×2）。</summary>
    public int CriticalMultiplier { get; set; } = 2;

    /// <summary>武器增强加值（同时计入攻击与伤害）。</summary>
    public int Enhancement { get; set; }

    /// <summary>有效伤害属性倍率（按持握方式推导；副手优先）。</summary>
    public double DamageAbilityMultiplier => IsSecondary || Hand == WeaponHand.OffHand
        ? 0.5
        : Hand == WeaponHand.TwoHanded
            ? 1.5
            : 1.0;

    /// <summary>是否为远程类攻击。</summary>
    public bool IsRangedAttack => AttackType is WeaponAttackType.Ranged or WeaponAttackType.RangedTouch;
}
