using System;
using System.Collections.Generic;
using System.Linq;
using Pathfinder1eHelper.Models.Combat;
using Pathfinder1eHelper.Services.Rules;

namespace Pathfinder1eHelper.Services;

/// <summary>
/// 战斗数值计算：把角色档案的基础值/修饰路由到 DLL 规则（<see cref="Rules"/>），
/// 合成 <see cref="CombatSheet"/>。数值语义与《开拓者：正义之怒》一致。
/// </summary>
public static class CombatCalculator
{
    public static CombatSheet Calculate(CharacterProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        profile.ApplyDefaults();

        var dexterity = Attribute(profile, Ability.Dexterity);
        var constitution = Attribute(profile, Ability.Constitution);
        var wisdom = Attribute(profile, Ability.Wisdom);
        var strength = Attribute(profile, Ability.Strength);

        var strMod = strength.Bonus;
        var dexMod = dexterity.Bonus;
        var conMod = constitution.Bonus;
        var wisMod = wisdom.Bonus;
        var sizeAttack = SizeModifiers.AttackAndAc(profile.Size);
        var sizeManeuver = SizeModifiers.Maneuver(profile.Size);

        // 全局攻击命中（近战/远程/接触共用，按描述符叠加），不区分武器。
        var globalAttack = Mods(profile, CombatStat.Attack, WeaponScope.AllWeapons);
        var globalDamage = Mods(profile, CombatStat.Damage, WeaponScope.AllWeapons);

        // AC 修饰：姿态条目（防御式战斗/全防御）本身就是普通 Dodge 条目，直接参与叠加。
        var acModifiers = Mods(profile, CombatStat.ArmorClass);

        var armorClass = RuleCalculateArmorClass.Compute(new ArmorClassRequest
        {
            BaseAttributeBonus = dexMod,
            MaxDexBonusFromArmor = profile.MaxDexBonus,
            Modifiers = acModifiers,
        });

        var fortitude = RuleCalculateSavingThrow.Compute(
            profile.BaseFortitude, conMod, Mods(profile, CombatStat.Fortitude), "体质");
        var reflex = RuleCalculateSavingThrow.Compute(
            profile.BaseReflex, dexMod, Mods(profile, CombatStat.Reflex), "敏捷");
        var will = RuleCalculateSavingThrow.Compute(
            profile.BaseWill, wisMod, Mods(profile, CombatStat.Will), "感知");

        var maneuverLabel = profile.UseDexForManeuvers ? "敏捷" : "力量";
        var maneuverBonus = profile.UseDexForManeuvers ? dexMod : strMod;

        var cmb = RuleCalculateCombatManeuver.Cmb(new CmbRequest
        {
            BaseAttackBonus = profile.BaseAttackBonus,
            ManeuverAbilityBonus = maneuverBonus,
            ManeuverAbilityLabel = maneuverLabel,
            SizeBonus = sizeManeuver,
            AdditionalCmb = Combine(Mods(profile, CombatStat.Cmb), Mods(profile, CombatStat.AdditionalCMB)),
            // 战技沿用通用攻击加值；WeaponAttack 是武器专属通道，不进入 CMB。
            AdditionalAttackBonus = Combine(
                globalAttack,
                [Size(sizeAttack)]),
        });

        var cmd = RuleCalculateCombatManeuver.Cmd(new CmdRequest
        {
            BaseAttackBonus = profile.BaseAttackBonus,
            ManeuverAbilityBonus = maneuverBonus,
            ManeuverAbilityLabel = maneuverLabel,
            DexterityBonus = dexMod,
            SizeBonus = sizeManeuver,
            AdditionalCmd = Combine(Mods(profile, CombatStat.Cmd), Mods(profile, CombatStat.AdditionalCMD)),
            ArmorClassModifiers = acModifiers,
        });

        var castingAbilityBonus = Attribute(profile, profile.CastingAbility).Bonus;
        var concentration = RuleCheckConcentration.Value(
            profile.CasterLevel, castingAbilityBonus, Mods(profile, CombatStat.Concentration));
        var spellDc = RuleCheckConcentration.SpellDc(profile.SpellLevel, castingAbilityBonus);

        var initiative = RuleCalculateInitiative.Compute(dexMod, Mods(profile, CombatStat.Initiative));

        var weapons = profile.Weapons
            .Select(weapon => CalculateWeapon(
                weapon, profile, globalAttack, globalDamage, sizeAttack, strMod, dexMod))
            .ToList();

        return new CombatSheet(
            armorClass.ArmorClass,
            armorClass.Touch,
            armorClass.FlatFooted,
            armorClass.FlatFootedTouch,
            fortitude,
            reflex,
            will,
            cmb,
            cmd,
            concentration,
            initiative,
            spellDc,
            weapons);
    }

    private static WeaponResult CalculateWeapon(
        WeaponProfile weapon,
        CharacterProfile profile,
        IReadOnlyList<Modifier> globalAttack,
        IReadOnlyList<Modifier> globalDamage,
        int sizeAttack,
        int strMod,
        int dexMod)
    {
        var isRanged = weapon.IsRangedAttack;
        var attackAbility = ResolveAbility(weapon.AttackBonusStat, isRanged, strMod, dexMod);

        var attackMods = Combine(
            globalAttack,
            Mods(profile, CombatStat.Attack, WeaponScope.SingleWeapon, weapon.Id),
            Mods(profile, CombatStat.WeaponAttack, WeaponScope.AllWeapons),
            Mods(profile, CombatStat.WeaponAttack, WeaponScope.SingleWeapon, weapon.Id),
            [Size(sizeAttack)]);
        var damageMods = Combine(
            globalDamage,
            Mods(profile, CombatStat.Damage, WeaponScope.SingleWeapon, weapon.Id));

        var damageStat = weapon.DamageBonusStat ?? (isRanged ? null : Ability.Strength);
        var (damageAbility, damageLabel) = damageStat switch
        {
            Ability.Strength => (strMod, "力量"),
            Ability.Dexterity => (dexMod, "敏捷"),
            Ability.Constitution => (Attribute(profile, Ability.Constitution).Bonus, "体质"),
            Ability.Intelligence => (Attribute(profile, Ability.Intelligence).Bonus, "智力"),
            Ability.Wisdom => (Attribute(profile, Ability.Wisdom).Bonus, "感知"),
            Ability.Charisma => (Attribute(profile, Ability.Charisma).Bonus, "魅力"),
            _ => (0, "力量"),
        };

        var stats = RuleCalculateWeaponStats.Compute(new WeaponStatsRequest
        {
            BaseAttackBonus = profile.BaseAttackBonus,
            AttackAbilityBonus = attackAbility,
            DamageAbilityBonus = damageAbility,
            DamageAbilityLabel = damageLabel,
            DamageAbilityMultiplier = weapon.DamageAbilityMultiplier,
            Enhancement = weapon.Enhancement,
            AttackIsMelee = !isRanged,
            CriticalThreatLow = weapon.CriticalThreatLow,
            CriticalMultiplier = weapon.CriticalMultiplier,
            BaseDamageDice = weapon.BaseDamage,
            WeaponSize = weapon.WeaponSize,
            DamageDiceSizeShift = weapon.DamageDiceSizeShift,
            AdditionalAttackBonus = attackMods,
            DamageModifiers = damageMods,
        });

        return new WeaponResult(
            weapon,
            stats.Attack,
            stats.Damage,
            stats.AttacksCount,
            stats.CriticalThreatLow,
            stats.CriticalMultiplier,
            stats.DamageDice);
    }

    private static int ResolveAbility(Ability? declared, bool isRanged, int strMod, int dexMod) => declared switch
    {
        Ability.Strength => strMod,
        Ability.Dexterity => dexMod,
        _ => isRanged ? dexMod : strMod,
    };

    private static AttributeValue Attribute(CharacterProfile profile, Ability ability)
    {
        var value = new AttributeValue(profile.Abilities.GetScore(ability));
        foreach (var entry in profile.Modifiers.Where(m =>
                     m is { IsEnabled: true, Stat: CombatStat.AbilityScore } && m.Ability == ability))
        {
            value.AddModifier(entry.ToModifier());
        }

        return value;
    }

    /// <summary>通道修饰（不区分武器；武器专属条目按 WeaponId 另行过滤）。</summary>
    /// <param name="scope">
    /// 武器范围。<see cref="WeaponScope.Any"/> 取全部（含武器专属）；
    /// <see cref="WeaponScope.AllWeapons"/> 只取 <c>WeaponId is null</c> 的全局条目；
    /// <see cref="WeaponScope.SingleWeapon"/> 只取绑定到 <paramref name="weaponId"/> 的条目。
    /// —— 三者语义互不相同（Any 与 AllWeapons 尤其容易写反），故用枚举而非可空 Guid 表达。
    /// </param>
    private static List<Modifier> Mods(
        CharacterProfile profile,
        CombatStat stat,
        WeaponScope scope = WeaponScope.Any,
        Guid weaponId = default)
    {
        return profile.Modifiers
            .Where(entry => entry is { IsEnabled: true, Kind: ModifierKind.Normal } && entry.Stat == stat)
            .Select(entry => new
            {
                entry,
                matches = scope switch
                {
                    WeaponScope.AllWeapons => entry.WeaponId is null,
                    WeaponScope.SingleWeapon => entry.WeaponId == weaponId,
                    _ => true,
                }
            })
            .Where(t => t.matches)
            .Select(t => t.entry.ToModifier()).ToList();
    }

    /// <summary>修饰的武器适用范围。</summary>
    private enum WeaponScope
    {
        /// <summary>不按武器过滤（AC、豁免、战技等非武器通道）。</summary>
        Any,

        /// <summary>作用于全部武器的条目（武器专攻/专精等带 WeaponId 的除外）。</summary>
        AllWeapons,

        /// <summary>仅作用于指定武器的条目。</summary>
        SingleWeapon,
    }

    private static Modifier Size(int sizeBonus) =>
        new(ModifierDescriptor.Size, sizeBonus, Source: "体型");

    /// <summary>把多组修饰拼成一个列表（保持顺序，便于明细稳定）。</summary>
    private static List<Modifier> Combine(params IReadOnlyList<Modifier>[] lists)
    {
        var result = new List<Modifier>();
        foreach (var list in lists)
        {
            result.AddRange(list);
        }

        return result;
    }
}