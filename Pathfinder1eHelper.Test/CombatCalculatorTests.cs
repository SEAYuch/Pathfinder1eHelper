using Pathfinder1eHelper.Models.Combat;
using Pathfinder1eHelper.Services;

namespace Pathfinder1eHelper.Test;

/// <summary>战斗数值与 DLL 叠加规则的单元测试（纯内存，不依赖数据库）。</summary>
public class CombatCalculatorTests
{
    private static ModifierEntry Bonus(
        ModifierDescriptor descriptor,
        CombatStat stat,
        int value,
        Ability? ability = null,
        bool enabled = true,
        string name = "加值") =>
        new()
        {
            Name = name,
            Descriptor = descriptor,
            Stat = stat,
            Ability = ability,
            Value = value,
            IsEnabled = enabled,
        };

    [Fact]
    public void Weapon_attack_uses_bab_strength_and_size()
    {
        var profile = Profile();
        profile.Abilities.Strength = 20; // +5
        profile.BaseAttackBonus = 5;
        profile.Size = SizeCategory.Small; // +1
        profile.Weapons.Add(new WeaponProfile { BaseDamage = "1d8" });

        var sheet = CombatCalculator.Calculate(profile);
        var weapon = Assert.Single(sheet.Weapons);

        Assert.Equal(11, weapon.Attack.Total);
    }

    [Fact]
    public void Attack_channel_modifier_applies_to_melee_and_ranged_weapons()
    {
        var profile = Profile();
        profile.Abilities.Dexterity = 16; // +3
        profile.BaseAttackBonus = 4;
        profile.Modifiers.Add(Bonus(ModifierDescriptor.Luck, CombatStat.Attack, 1, name: "幸运"));
        profile.Weapons.Add(new WeaponProfile { Name = "长剑", BaseDamage = "1d8" });
        profile.Weapons.Add(new WeaponProfile
        {
            Name = "长弓",
            AttackType = WeaponAttackType.Ranged,
            BaseDamage = "1d8",
        });

        var sheet = CombatCalculator.Calculate(profile);

        Assert.Equal(5, sheet.Weapons[0].Attack.Total); // BAB4 + 力量0 + 1
        Assert.Equal(8, sheet.Weapons[1].Attack.Total); // BAB4 + 敏捷3 + 1
    }

    [Fact]
    public void Same_descriptor_takes_highest_positive_and_suppresses_the_rest()
    {
        var profile = Profile();
        profile.Modifiers.Add(Bonus(ModifierDescriptor.Morale, CombatStat.ArmorClass, 2, name: "勇气"));
        profile.Modifiers.Add(Bonus(ModifierDescriptor.Morale, CombatStat.ArmorClass, 3, name: "英雄气概"));

        var sheet = CombatCalculator.Calculate(profile);

        Assert.Equal(13, sheet.ArmorClass.Total);
        Assert.Contains(sheet.ArmorClass.Contributions, c => c.Included && c.Value == 3);
        Assert.Contains(sheet.ArmorClass.Contributions, c => !c.Included && c.Value == 2 && c.Note!.Contains("压制"));
    }

    [Fact]
    public void Dodge_and_none_bonuses_stack()
    {
        var profile = Profile();
        profile.Modifiers.Add(Bonus(ModifierDescriptor.Dodge, CombatStat.ArmorClass, 1, name: "闪避1"));
        profile.Modifiers.Add(Bonus(ModifierDescriptor.Dodge, CombatStat.ArmorClass, 1, name: "闪避2"));
        profile.Modifiers.Add(Bonus(ModifierDescriptor.None, CombatStat.ArmorClass, 2, name: "无名"));

        var sheet = CombatCalculator.Calculate(profile);

        Assert.Equal(14, sheet.ArmorClass.Total);
    }

    [Fact]
    public void Same_descriptor_keeps_best_positive_and_worst_negative()
    {
        var profile = Profile();
        profile.Modifiers.Add(Bonus(ModifierDescriptor.Morale, CombatStat.ArmorClass, 3, name: "士气"));
        profile.Modifiers.Add(Bonus(ModifierDescriptor.Morale, CombatStat.ArmorClass, -2, name: "士气减值"));

        var sheet = CombatCalculator.Calculate(profile);

        Assert.Equal(11, sheet.ArmorClass.Total); // 10 + 3 - 2
    }

    [Fact]
    public void Penalty_descriptor_sums_signed_values()
    {
        var profile = Profile();
        profile.Modifiers.Add(Bonus(ModifierDescriptor.Penalty, CombatStat.ArmorClass, -2, name: "目眩"));
        profile.Modifiers.Add(Bonus(ModifierDescriptor.Penalty, CombatStat.ArmorClass, -3, name: "俯卧"));

        var sheet = CombatCalculator.Calculate(profile);

        Assert.Equal(5, sheet.ArmorClass.Total);
    }

    [Fact]
    public void Enhancement_to_natural_armor_layers_on_top_of_natural_armor()
    {
        var profile = Profile();
        profile.Modifiers.Add(Bonus(ModifierDescriptor.NaturalArmor, CombatStat.ArmorClass, 1, name: "天生护甲"));
        profile.Modifiers.Add(Bonus(ModifierDescriptor.NaturalArmorEnhancement, CombatStat.ArmorClass, 3, name: "树皮术"));

        var sheet = CombatCalculator.Calculate(profile);

        Assert.Equal(14, sheet.ArmorClass.Total);
        Assert.Equal(10, sheet.TouchArmorClass.Total);
    }

    [Fact]
    public void Touch_and_flat_footed_remove_the_right_components()
    {
        var profile = Profile();
        profile.Abilities.Dexterity = 14; // +2
        profile.Modifiers.Add(Bonus(ModifierDescriptor.Armor, CombatStat.ArmorClass, 5, name: "链甲"));
        profile.Modifiers.Add(Bonus(ModifierDescriptor.Shield, CombatStat.ArmorClass, 2, name: "重盾"));
        profile.Modifiers.Add(Bonus(ModifierDescriptor.NaturalArmor, CombatStat.ArmorClass, 1, name: "天生护甲"));
        profile.Modifiers.Add(Bonus(ModifierDescriptor.NaturalArmorEnhancement, CombatStat.ArmorClass, 3, name: "树皮术"));
        profile.Modifiers.Add(Bonus(ModifierDescriptor.Deflection, CombatStat.ArmorClass, 1, name: "防护戒指"));
        profile.Modifiers.Add(Bonus(ModifierDescriptor.Dodge, CombatStat.ArmorClass, 1, name: "闪避"));

        var sheet = CombatCalculator.Calculate(profile);

        Assert.Equal(25, sheet.ArmorClass.Total);
        Assert.Equal(14, sheet.TouchArmorClass.Total);
        Assert.Equal(22, sheet.FlatFootedArmorClass.Total);
        Assert.Equal(11, sheet.FlatFootedTouchArmorClass.Total);
    }

    [Fact]
    public void Cmd_takes_only_the_allowed_ac_bonus_types()
    {
        var profile = Profile();
        profile.Abilities.Strength = 12; // +1
        profile.Abilities.Dexterity = 12; // +1
        profile.BaseAttackBonus = 3;
        profile.Modifiers.Add(Bonus(ModifierDescriptor.Armor, CombatStat.ArmorClass, 5, name: "全身甲"));
        profile.Modifiers.Add(Bonus(ModifierDescriptor.NaturalArmor, CombatStat.ArmorClass, 1, name: "天生护甲"));
        profile.Modifiers.Add(Bonus(ModifierDescriptor.Deflection, CombatStat.ArmorClass, 1, name: "偏斜"));
        profile.Modifiers.Add(Bonus(ModifierDescriptor.Morale, CombatStat.ArmorClass, 2, name: "士气"));

        var sheet = CombatCalculator.Calculate(profile);

        Assert.Equal(18, sheet.Cmd.Total);
    }

    [Fact]
    public void Cmb_uses_special_maneuver_size_modifier()
    {
        var profile = Profile();
        profile.Abilities.Strength = 14; // +2
        profile.BaseAttackBonus = 2;
        profile.Size = SizeCategory.Tiny;

        var sheet = CombatCalculator.Calculate(profile);

        Assert.Equal(2, sheet.Cmb.Total);
        Assert.Equal(12, sheet.Cmd.Total);
    }

    [Fact]
    public void Concentration_uses_caster_level_and_casting_ability()
    {
        var profile = Profile();
        profile.CasterLevel = 7;
        profile.CastingAbility = Ability.Intelligence;
        profile.Abilities.Intelligence = 16; // +3
        profile.Modifiers.Add(Bonus(ModifierDescriptor.Feat, CombatStat.Concentration, 2, name: "战斗施法"));

        var sheet = CombatCalculator.Calculate(profile);

        Assert.Equal(12, sheet.Concentration.Total);
    }

    [Fact]
    public void Ability_score_bonus_is_applied_before_deriving_modifiers()
    {
        var profile = Profile();
        profile.Abilities.Strength = 14;
        profile.Modifiers.Add(Bonus(
            ModifierDescriptor.Enhancement, CombatStat.AbilityScore, 4, Ability.Strength, name: "公牛之力"));

        var sheet = CombatCalculator.Calculate(profile);

        Assert.Equal(4, sheet.Cmb.Total); // (14+4) → +4
    }

    [Fact]
    public void Weapon_applies_two_handed_multiplier_and_enhancement()
    {
        var profile = Profile();
        profile.Abilities.Strength = 18; // +4
        profile.BaseAttackBonus = 5;
        profile.Modifiers.Add(Bonus(ModifierDescriptor.Competence, CombatStat.Damage, 2, name: "勇气"));
        profile.Weapons.Add(new WeaponProfile
        {
            Name = "巨剑",
            BaseDamage = "2d6",
            Hand = WeaponHand.TwoHanded,
            Enhancement = 1,
        });

        var sheet = CombatCalculator.Calculate(profile);
        var weapon = Assert.Single(sheet.Weapons);

        Assert.Equal("+10", weapon.AttackDisplay);
        Assert.Equal("2d6+9", weapon.DamageDisplay); // floor(4*1.5)=6 + 1 + 2
    }

    [Fact]
    public void Off_hand_weapon_keeps_full_negative_strength_penalty()
    {
        var profile = Profile();
        profile.Abilities.Strength = 8; // -1
        profile.Weapons.Add(new WeaponProfile
        {
            Name = "短剑",
            BaseDamage = "1d6",
            Hand = WeaponHand.OffHand,
        });

        var sheet = CombatCalculator.Calculate(profile);
        var weapon = Assert.Single(sheet.Weapons);

        Assert.Equal("1d6-1", weapon.DamageDisplay);
    }

    [Fact]
    public void Weapon_can_deal_dexterity_based_damage()
    {
        var profile = Profile();
        profile.Abilities.Dexterity = 16; // +3
        profile.BaseAttackBonus = 5;
        profile.Weapons.Add(new WeaponProfile
        {
            Name = "细剑",
            BaseDamage = "1d6",
            DamageBonusStat = Ability.Dexterity,
            Enhancement = 1,
        });

        var sheet = CombatCalculator.Calculate(profile);
        var weapon = Assert.Single(sheet.Weapons);

        Assert.Equal("1d6+4", weapon.DamageDisplay); // 3（敏捷）+ 1（增强）
        Assert.Contains(weapon.Damage.Contributions, c => c.Included && c.Label.StartsWith("敏捷"));
    }

    [Fact]
    public void Ranged_weapon_uses_dexterity_attack_and_attack_channel()
    {
        var profile = Profile();
        profile.Abilities.Dexterity = 16; // +3
        profile.BaseAttackBonus = 4;
        profile.Modifiers.Add(Bonus(ModifierDescriptor.Luck, CombatStat.Attack, 1, name: "幸运"));
        profile.Weapons.Add(new WeaponProfile
        {
            Name = "长弓",
            AttackType = WeaponAttackType.Ranged,
            BaseDamage = "1d8",
        });

        var sheet = CombatCalculator.Calculate(profile);
        var weapon = Assert.Single(sheet.Weapons);

        Assert.Equal(8, weapon.Attack.Total); // BAB4 + 敏捷3 + 幸运1
        Assert.Equal("1d8", weapon.DamageDisplay);
    }

    [Fact]
    public void Weapon_reports_iterative_attack_count_from_bab()
    {
        var profile = Profile();
        profile.BaseAttackBonus = 16;
        profile.Weapons.Add(new WeaponProfile { BaseDamage = "1d8" });

        var sheet = CombatCalculator.Calculate(profile);
        var weapon = Assert.Single(sheet.Weapons);

        Assert.Equal(4, weapon.AttacksCount);
        Assert.Equal("20/×2", weapon.CriticalDisplay);
    }

    [Fact]
    public void Large_weapon_scales_damage_dice()
    {
        var profile = Profile();
        profile.Weapons.Add(new WeaponProfile { BaseDamage = "1d8", WeaponSize = SizeCategory.Large });

        var sheet = CombatCalculator.Calculate(profile);
        var weapon = Assert.Single(sheet.Weapons);

        Assert.StartsWith("2d6", weapon.DamageDisplay);
    }

    [Fact]
    public void Disabled_entries_are_ignored()
    {
        var profile = Profile();
        profile.Modifiers.Add(Bonus(ModifierDescriptor.None, CombatStat.ArmorClass, 5, enabled: false, name: "未启用"));

        var sheet = CombatCalculator.Calculate(profile);

        Assert.Equal(10, sheet.ArmorClass.Total);
    }

    [Fact]
    public void Scoped_modifier_only_affects_its_weapon()
    {
        var profile = Profile();
        var first = new WeaponProfile { Name = "甲", BaseDamage = "1d8" };
        var second = new WeaponProfile { Name = "乙", BaseDamage = "1d8" };
        profile.Weapons.Add(first);
        profile.Weapons.Add(second);
        profile.Modifiers.Add(new ModifierEntry
        {
            Name = "武器专攻（甲）",
            Descriptor = ModifierDescriptor.UntypedStackable,
            Stat = CombatStat.Attack,
            Value = 1,
            WeaponId = first.Id,
        });

        var sheet = CombatCalculator.Calculate(profile);

        Assert.Equal(1, sheet.Weapons[0].Attack.Total);
        Assert.Equal(0, sheet.Weapons[1].Attack.Total);
    }

    [Fact]
    public void Power_attack_applies_to_melee_weapon_only()
    {
        var profile = Profile();
        profile.Abilities.Strength = 16; // +3
        profile.BaseAttackBonus = 4;
        profile.Modifiers.Add(PowerAttackBuff());
        profile.Weapons.Add(new WeaponProfile { Name = "长剑", BaseDamage = "1d8" });
        profile.Weapons.Add(new WeaponProfile
        {
            Name = "长弓",
            AttackType = WeaponAttackType.Ranged,
            BaseDamage = "1d8",
        });

        var sheet = CombatCalculator.Calculate(profile);

        Assert.Equal(5, sheet.Weapons[0].Attack.Total); // 4 + 3 - 2
        Assert.Equal("1d8+7", sheet.Weapons[0].DamageDisplay); // 3 + 4
        Assert.Equal(4, sheet.Weapons[1].Attack.Total); // 不受猛力攻击
        Assert.Equal("1d8", sheet.Weapons[1].DamageDisplay);
    }

    [Fact]
    public void Power_attack_penalty_does_not_apply_to_cmb()
    {
        var profile = Profile();
        profile.Abilities.Strength = 16; // +3
        profile.BaseAttackBonus = 4;
        profile.Modifiers.Add(PowerAttackBuff());

        var sheet = CombatCalculator.Calculate(profile);

        Assert.Equal(7, sheet.Cmb.Total); // 4 + 3（无猛力攻击减值）
    }

    [Fact]
    public void Disabled_power_attack_entry_is_ignored()
    {
        var profile = Profile();
        profile.Abilities.Strength = 16; // +3
        profile.BaseAttackBonus = 4;
        var entry = PowerAttackBuff();
        entry.IsEnabled = false;
        profile.Modifiers.Add(entry);
        profile.Weapons.Add(new WeaponProfile { Name = "长剑", BaseDamage = "1d8" });

        var sheet = CombatCalculator.Calculate(profile);

        Assert.Equal(7, sheet.Weapons[0].Attack.Total); // 4 + 3
        Assert.Equal("1d8+3", sheet.Weapons[0].DamageDisplay);
    }

    private static ModifierEntry PowerAttackBuff() => new()
    {
        Name = "猛力攻击",
        Kind = ModifierKind.PowerAttack,
        Descriptor = ModifierDescriptor.UntypedStackable,
        Stat = CombatStat.Attack,
    };

    private static CharacterProfile Profile() => new();
}
