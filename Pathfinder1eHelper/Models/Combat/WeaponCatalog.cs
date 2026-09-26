using System.Collections.Generic;

namespace Pathfinder1eHelper.Models.Combat;

/// <summary>常用武器的快速添加模板（伤害骰/持握/重击，可再编辑）。</summary>
public sealed record WeaponPreset(string Name, WeaponProfile Weapon);

public static class WeaponCatalog
{
    private static WeaponProfile Weapon(
        string name,
        string damage,
        WeaponHand hand = WeaponHand.Primary,
        int threatLow = 20,
        int multiplier = 2,
        WeaponAttackType attackType = WeaponAttackType.Melee,
        Ability? damageStat = null) =>
        new()
        {
            Name = name,
            BaseDamage = damage,
            Hand = hand,
            CriticalThreatLow = threatLow,
            CriticalMultiplier = multiplier,
            AttackType = attackType,
            DamageBonusStat = damageStat,
        };

    public static IReadOnlyList<WeaponPreset> All { get; } =
    [
        new("巨剑", Weapon("巨剑", "2d6", WeaponHand.TwoHanded, 19, 2)),
        new("长剑", Weapon("长剑", "1d8", threatLow: 19)),
        new("弯刀", Weapon("弯刀", "1d6", threatLow: 18)),
        new("细剑", Weapon("细剑", "1d6", threatLow: 18)),
        new("短剑", Weapon("短剑", "1d6", threatLow: 19)),
        new("匕首", Weapon("匕首", "1d4", threatLow: 19)),
        new("战斗斧", Weapon("战斗斧", "1d8", multiplier: 3)),
        new("巨斧", Weapon("巨斧", "1d12", WeaponHand.TwoHanded, multiplier: 3)),
        new("战锤", Weapon("战锤", "1d8", multiplier: 3)),
        new("巨木棒", Weapon("巨木棒", "1d10", WeaponHand.TwoHanded)),
        new("长枪", Weapon("长枪", "1d8", multiplier: 3)),
        new("矛", Weapon("矛", "1d8", multiplier: 3)),
        new("巨镰", Weapon("巨镰", "2d4", WeaponHand.TwoHanded, multiplier: 4)),
        new("戟", Weapon("戟", "1d10", WeaponHand.TwoHanded, multiplier: 3)),
        new("徒手击打", Weapon("徒手击打", "1d3")),
        new("长弓", Weapon("长弓", "1d8", multiplier: 3, attackType: WeaponAttackType.Ranged)),
        new("复合长弓", Weapon("复合长弓", "1d8", multiplier: 3, attackType: WeaponAttackType.Ranged, damageStat: Ability.Strength)),
        new("短弓", Weapon("短弓", "1d6", multiplier: 3, attackType: WeaponAttackType.Ranged)),
        new("轻弩", Weapon("轻弩", "1d8", threatLow: 19, attackType: WeaponAttackType.Ranged)),
        new("重弩", Weapon("重弩", "1d10", threatLow: 19, attackType: WeaponAttackType.Ranged)),
        new("投石索", Weapon("投石索", "1d4", attackType: WeaponAttackType.Ranged)),
    ];
}
