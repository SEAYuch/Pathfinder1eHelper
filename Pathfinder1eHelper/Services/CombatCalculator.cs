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

        // 姿态识别：防御式战斗的 −4 攻击减值会被「白鹤拳」放宽到 −2。
        var defensiveFighting = profile.Modifiers.Any(
            m => m.IsEnabled && m.Stance == CombatStance.DefensiveFighting);
        var craneStyle = profile.Modifiers.Any(m => m.IsEnabled && m.Kind == ModifierKind.CraneStyle);

        // 全局攻击命中（近战/远程/接触共用，按描述符叠加），不区分武器。
        // 白鹤拳的补偿走同一条 Attack 通道，故武器攻击与战技（CMB）一致抵消（CHM 说的是「攻击骰」）。
        var craneRelief = RuleCalculateCraneStyle.AttackRelief(craneStyle, defensiveFighting);
        var globalAttack = Combine(
            GlobalMods(profile, CombatStat.Attack),
            Rule(craneRelief, "白鹤拳", ModifierDescriptor.UntypedStackable));
        var globalDamage = GlobalMods(profile, CombatStat.Damage);

        // 「猛力攻击」以 buff 条目存在，启用时按 BAB 与握法逐武器展开。
        var powerAttack = profile.Modifiers.Any(m => m.IsEnabled && m.Kind == ModifierKind.PowerAttack);

        // 「双武器格斗」：需同时具备专长条目与双持状态（存在副手武器）。
        var twoWeaponFighting = profile.Modifiers.Any(m => m.IsEnabled && m.Kind == ModifierKind.TwoWeaponFighting)
            && RuleCalculateTwoWeaponFighting.IsTwoWeaponing(profile);
        var mainHandIsDouble = (profile.Weapons ?? []).Any(w => !w.IsSecondary && w.IsDouble);

        // 「寓守于攻」：AC 闪避加值对整轮生效，攻击/战技减值按近战武器展开。
        // CHM：全防御期间无法从「寓守于攻」获益，故先看是否存在启用的全防御条目。
        var totalDefense = profile.Modifiers.Any(
            m => m.IsEnabled && m.Stance == CombatStance.TotalDefense);
        var combatExpertise = !totalDefense
            && profile.Modifiers.Any(m => m.IsEnabled && m.Kind == ModifierKind.CombatExpertise);
        var expertise = combatExpertise
            ? RuleCalculateCombatExpertise.Compute(profile.BaseAttackBonus, isMeleeWeaponAttack: false)
            : (AttackPenalty: 0, ArmorClassDodgeBonus: 0);
        var expertiseMelee = combatExpertise
            ? RuleCalculateCombatExpertise.Compute(profile.BaseAttackBonus, isMeleeWeaponAttack: true)
            : (AttackPenalty: 0, ArmorClassDodgeBonus: 0);

        // 战斗姿态的 AC 闪避加值走 Dodge（与「闪避」专长同源，敏捷加值被限制时一并失效）。
        var acModifiers = Combine(
            Mods(profile, CombatStat.ArmorClass),
            Rule(expertise.ArmorClassDodgeBonus, "寓守于攻", ModifierDescriptor.Dodge));

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
            // 战技沿用全局攻击加值（猛力攻击按 DLL 只作用于攻击检定，不入 CMB）；
            // 寓守于攻则按 CHM 同时作用于战技检定。
            AdditionalAttackBonus = Combine(
                globalAttack,
                [Size(sizeAttack)],
                Rule(expertiseMelee.AttackPenalty, "寓守于攻", ModifierDescriptor.UntypedStackable)),
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

        var weapons = (profile.Weapons ?? [])
            .Select(weapon => CalculateWeapon(
                weapon, profile, globalAttack, globalDamage, sizeAttack, strMod, dexMod,
                powerAttack, combatExpertise, twoWeaponFighting, mainHandIsDouble))
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
        int dexMod,
        bool powerAttack,
        bool combatExpertise,
        bool twoWeaponFighting,
        bool mainHandIsDouble)
    {
        var isRanged = weapon.IsRangedAttack;
        var attackAbility = ResolveAbility(weapon.AttackBonusStat, isRanged, strMod, dexMod);

        var attackMods = Combine(
            globalAttack,
            ScopedMods(profile, CombatStat.Attack, weapon.Id),
            [Size(sizeAttack)]);
        var damageMods = Combine(
            globalDamage,
            ScopedMods(profile, CombatStat.Damage, weapon.Id));

        if (powerAttack)
        {
            var (penalty, damage) = RuleCalculatePowerAttack.Compute(
                profile.BaseAttackBonus,
                isMelee: !isRanged,
                isTouch: weapon.AttackType == WeaponAttackType.Touch,
                isSecondary: weapon.IsSecondary,
                holdInTwoHands: weapon.Hand == WeaponHand.TwoHanded);

            if (penalty != 0)
            {
                attackMods.Add(new Modifier(ModifierDescriptor.UntypedStackable, penalty, Source: "猛力攻击"));
            }

            if (damage != 0)
            {
                damageMods.Add(new Modifier(ModifierDescriptor.UntypedStackable, damage, Source: "猛力攻击"));
            }
        }

        // 寓守于攻只作用于近战武器攻击（CHM：「只能在以近战武器进行攻击或全力攻击时」）。
        if (combatExpertise && !isRanged)
        {
            var (penalty, _) = RuleCalculateCombatExpertise.Compute(profile.BaseAttackBonus, isMeleeWeaponAttack: true);
            attackMods.Add(new Modifier(ModifierDescriptor.UntypedStackable, penalty, Source: "寓守于攻"));
        }

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
            // 「双武器格斗」：仅在双持（有副手武器）时按主/副手档位施加攻击减值；
            // 副手还额外触发伤害加值减半（DLL TwoWeaponFightingDamagePenalty）。
            AttackPenalty = twoWeaponFighting
                ? RuleCalculateTwoWeaponFighting.AttackPenalty(
                    weapon.IsSecondary, weapon.Category, mainHandIsDouble)
                : 0,
            HalfDamageBonus = twoWeaponFighting && weapon.IsSecondary,
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
                     m.IsEnabled && m.Stat == CombatStat.AbilityScore && m.Ability == ability))
        {
            value.AddModifier(entry.ToModifier());
        }

        return value;
    }

    /// <summary>通道修饰（不区分武器；特殊条目如猛力攻击由计算层单独展开）。</summary>
    private static IReadOnlyList<Modifier> Mods(CharacterProfile profile, CombatStat stat) =>
        profile.Modifiers
            .Where(m => m.IsEnabled && m.Kind == ModifierKind.Normal && m.Stat == stat)
            .Select(m => m.ToModifier())
            .ToList();

    /// <summary>作用于全部武器的通道修饰（武器专攻/专精等带 WeaponId 的除外）。</summary>
    private static IReadOnlyList<Modifier> GlobalMods(CharacterProfile profile, CombatStat stat) =>
        profile.Modifiers
            .Where(m => m.IsEnabled && m.Kind == ModifierKind.Normal && m.Stat == stat && m.WeaponId is null)
            .Select(m => m.ToModifier())
            .ToList();

    /// <summary>仅作用于指定武器的通道修饰。</summary>
    private static IReadOnlyList<Modifier> ScopedMods(CharacterProfile profile, CombatStat stat, Guid weaponId) =>
        profile.Modifiers
            .Where(m => m.IsEnabled && m.Kind == ModifierKind.Normal && m.Stat == stat && m.WeaponId == weaponId)
            .Select(m => m.ToModifier())
            .ToList();

    private static Modifier Size(int sizeBonus) =>
        new(ModifierDescriptor.Size, sizeBonus, Source: "体型");

    /// <summary>单个由规则展开的修饰（数值为 0 时不产生条目，避免污染叠加明细）。</summary>
    private static List<Modifier> Rule(int value, string source, ModifierDescriptor descriptor) =>
        value == 0 ? [] : [new Modifier(descriptor, value, Source: source)];

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
