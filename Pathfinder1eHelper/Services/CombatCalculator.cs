using System;
using System.Collections.Generic;
using System.Linq;
using Pathfinder1eHelper.Models.Combat;

namespace Pathfinder1eHelper.Services;

/// <summary>
/// 战斗数值计算：基础公式（BAB/属性/体型）加上玩家手动录入的加值，按 PF1 叠加规则合并，
/// 并输出每项明细（叠加规则见 <see cref="BonusEngine"/>）。
/// </summary>
public static class CombatCalculator
{
    private static readonly HashSet<BonusType> CmdAcBonusTypes =
    [
        BonusType.Circumstance,
        BonusType.Deflection,
        BonusType.Dodge,
        BonusType.Insight,
        BonusType.Luck,
        BonusType.Morale,
        BonusType.Profane,
        BonusType.Sacred,
    ];

    public static CombatSheet Calculate(CharacterProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var abilities = AbilityResolver.Effective(profile);
        var strMod = abilities.GetModifier(Ability.Strength);
        var dexMod = abilities.GetModifier(Ability.Dexterity);
        var conMod = abilities.GetModifier(Ability.Constitution);
        var wisMod = abilities.GetModifier(Ability.Wisdom);
        var bab = profile.BaseAttackBonus;
        var sizeAttack = SizeModifiers.AttackAndAc(profile.Size);
        var sizeManeuver = SizeModifiers.Maneuver(profile.Size);
        var bonuses = profile.Bonuses ?? [];

        var maneuverLabel = profile.UseDexForManeuvers ? "敏捷" : "力量";
        var maneuverMod = profile.UseDexForManeuvers ? dexMod : strMod;

        var meleeAttack = BonusEngine.Stat(
        [
            BonusEngine.Base("BAB", bab),
            BonusEngine.Base("力量", strMod),
            BonusEngine.Base("体型", sizeAttack),
            .. BonusEngine.ToContributions(BonusEngine.Resolve(Target(bonuses, BonusTarget.MeleeAttack))),
        ]);

        var rangedAttack = BonusEngine.Stat(
        [
            BonusEngine.Base("BAB", bab),
            BonusEngine.Base("敏捷", dexMod),
            BonusEngine.Base("体型", sizeAttack),
            .. BonusEngine.ToContributions(BonusEngine.Resolve(Target(bonuses, BonusTarget.RangedAttack))),
        ]);

        // 远程接触攻击（射线）沿用远程攻击的通用加值，但不计武器增强（Enhancement）类加值。
        // 接触攻击“被视为使用武器攻击”的差异只在防御端：目标使用接触 AC。
        var rangedTouchAttack = BonusEngine.Stat(
        [
            BonusEngine.Base("BAB", bab),
            BonusEngine.Base("敏捷", dexMod),
            BonusEngine.Base("体型", sizeAttack),
            .. BonusEngine.ToContributions(BonusEngine.Resolve(
                bonuses.Where(e => e.Target == BonusTarget.RangedAttack && e.Type != BonusType.Enhancement))),
        ]);

        var dexContribution = profile.MaxDexBonus is { } maxDex && dexMod > maxDex
            ? new Contribution("敏捷（护甲上限）", maxDex, true, $"护甲限制敏捷加值为 {maxDex}", ExcludedFromFlatFooted: true)
            : new Contribution("敏捷", dexMod, true, null, ExcludedFromFlatFooted: true);

        var armorClass = BonusEngine.Stat(
        [
            BonusEngine.Base("基础", 10),
            dexContribution,
            BonusEngine.Base("体型", sizeAttack),
            .. BonusEngine.ToContributions(BonusEngine.Resolve(Target(bonuses, BonusTarget.ArmorClass))),
        ]);
        var touchArmorClass = armorClass.Excluding(touch: true);
        var flatFootedArmorClass = armorClass.Excluding(touch: false);

        var fortitude = BonusEngine.Stat(
        [
            BonusEngine.Base("基础", profile.BaseFortitude),
            BonusEngine.Base("体质", conMod),
            .. BonusEngine.ToContributions(BonusEngine.Resolve(Target(bonuses, BonusTarget.Fortitude))),
        ]);

        var reflex = BonusEngine.Stat(
        [
            BonusEngine.Base("基础", profile.BaseReflex),
            BonusEngine.Base("敏捷", dexMod),
            .. BonusEngine.ToContributions(BonusEngine.Resolve(Target(bonuses, BonusTarget.Reflex))),
        ]);

        var will = BonusEngine.Stat(
        [
            BonusEngine.Base("基础", profile.BaseWill),
            BonusEngine.Base("感知", wisMod),
            .. BonusEngine.ToContributions(BonusEngine.Resolve(Target(bonuses, BonusTarget.Will))),
        ]);

        var cmb = BonusEngine.Stat(
        [
            BonusEngine.Base("BAB", bab),
            BonusEngine.Base(maneuverLabel, maneuverMod),
            BonusEngine.Base("体型", sizeManeuver),
            .. BonusEngine.ToContributions(BonusEngine.Resolve(Target(bonuses, BonusTarget.Cmb))),
        ]);

        var cmdAcEntries = bonuses.Where(e =>
            e.Target == BonusTarget.ArmorClass
            && (CmdAcBonusTypes.Contains(e.Type) || e.Type == BonusType.Penalty));
        var cmd = BonusEngine.Stat(
        [
            BonusEngine.Base("基础", 10),
            BonusEngine.Base("BAB", bab),
            BonusEngine.Base(maneuverLabel, maneuverMod),
            BonusEngine.Base("敏捷", dexMod),
            BonusEngine.Base("体型", sizeManeuver),
            .. BonusEngine.ToContributions(BonusEngine.Resolve(cmdAcEntries)),
            .. BonusEngine.ToContributions(BonusEngine.Resolve(Target(bonuses, BonusTarget.Cmd))),
        ]);

        var concentration = BonusEngine.Stat(
        [
            BonusEngine.Base("施法者等级", profile.CasterLevel),
            BonusEngine.Base(CombatText.Ability(profile.CastingAbility), abilities.GetModifier(profile.CastingAbility)),
            .. BonusEngine.ToContributions(BonusEngine.Resolve(Target(bonuses, BonusTarget.Concentration))),
        ]);

        var initiative = BonusEngine.Stat(
        [
            BonusEngine.Base("敏捷", dexMod),
            .. BonusEngine.ToContributions(BonusEngine.Resolve(Target(bonuses, BonusTarget.Initiative))),
        ]);

        var weapons = (profile.Weapons ?? [])
            .Select(weapon => CalculateWeapon(weapon, bonuses, bab, strMod, dexMod, sizeAttack))
            .ToList();

        return new CombatSheet(
            meleeAttack,
            rangedAttack,
            rangedTouchAttack,
            armorClass,
            touchArmorClass,
            flatFootedArmorClass,
            fortitude,
            reflex,
            will,
            cmb,
            cmd,
            concentration,
            initiative,
            weapons);
    }

    private static WeaponResult CalculateWeapon(
        WeaponProfile weapon,
        IReadOnlyList<BonusEntry> bonuses,
        int bab,
        int strMod,
        int dexMod,
        int sizeAttack)
    {
        var abilityLabel = weapon.IsRanged ? "敏捷" : "力量";
        var abilityMod = weapon.IsRanged ? dexMod : strMod;
        var attackTarget = weapon.IsRanged ? BonusTarget.RangedAttack : BonusTarget.MeleeAttack;

        var attack = BonusEngine.Stat(
        [
            BonusEngine.Base("BAB", bab),
            BonusEngine.Base(abilityLabel, abilityMod),
            BonusEngine.Base("体型", sizeAttack),
            BonusEngine.Base("武器增强", weapon.Enhancement),
            .. BonusEngine.ToContributions(BonusEngine.Resolve(Target(bonuses, attackTarget))),
        ]);

        var strengthDamage = strMod < 0 ? strMod : (int)Math.Floor(strMod * weapon.StrengthMultiplier);
        var damage = BonusEngine.Stat(
        [
            BonusEngine.Base($"力量×{weapon.StrengthMultiplier:0.##}", strengthDamage),
            BonusEngine.Base("武器增强", weapon.Enhancement),
            .. BonusEngine.ToContributions(BonusEngine.Resolve(Target(bonuses, BonusTarget.Damage))),
        ]);

        return new WeaponResult(weapon, attack, damage);
    }

    private static IReadOnlyList<BonusEntry> Target(IReadOnlyList<BonusEntry> entries, BonusTarget target) =>
        entries.Where(e => e.Target == target).ToList();
}
