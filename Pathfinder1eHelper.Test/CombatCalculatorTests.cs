using Pathfinder1eHelper.Models.Combat;
using Pathfinder1eHelper.Services;

namespace Pathfinder1eHelper.Test;

/// <summary>战斗数值与加值叠加规则的单元测试（纯内存，不依赖数据库）。</summary>
public class CombatCalculatorTests
{
    [Fact]
    public void Attack_uses_bab_strength_and_size()
    {
        var profile = Profile();
        profile.Abilities.Strength = 20; // +5
        profile.BaseAttackBonus = 5;
        profile.Size = SizeCategory.Small; // +1

        var sheet = CombatCalculator.Calculate(profile);

        Assert.Equal(11, sheet.MeleeAttack.Total);
    }

    [Fact]
    public void Ranged_touch_shares_ranged_bonuses_but_excludes_weapon_enhancement()
    {
        var profile = Profile();
        profile.Abilities.Dexterity = 16; // +3
        profile.BaseAttackBonus = 4;
        profile.Bonuses.Add(new BonusEntry { Name = "远程幸运", Type = BonusType.Luck, Target = BonusTarget.RangedAttack, Value = 1 });
        profile.Bonuses.Add(new BonusEntry { Name = "魔化武器", Type = BonusType.Enhancement, Target = BonusTarget.RangedAttack, Value = 2 });

        var sheet = CombatCalculator.Calculate(profile);

        Assert.Equal(10, sheet.RangedAttack.Total); // 4 + 3 + 1 + 2
        Assert.Equal(8, sheet.RangedTouchAttack.Total); // 4 + 3 + 1（不含增强）
        Assert.Contains(sheet.RangedTouchAttack.Contributions, c => c.Included && c.Label == "远程幸运");
        Assert.DoesNotContain(sheet.RangedTouchAttack.Contributions, c => c.Included && c.Label == "魔化武器");
    }

    [Fact]
    public void Same_type_bonuses_take_the_highest_and_suppress_the_rest()
    {
        var profile = Profile();
        profile.Bonuses.Add(new BonusEntry { Name = "勇气", Type = BonusType.Morale, Target = BonusTarget.ArmorClass, Value = 2 });
        profile.Bonuses.Add(new BonusEntry { Name = "英雄气概", Type = BonusType.Morale, Target = BonusTarget.ArmorClass, Value = 3 });

        var sheet = CombatCalculator.Calculate(profile);

        Assert.Equal(13, sheet.ArmorClass.Total);
        Assert.Contains(sheet.ArmorClass.Contributions, c => c.Included && c.Value == 3);
        Assert.Contains(sheet.ArmorClass.Contributions, c => !c.Included && c.Value == 2 && c.Note!.Contains("压制"));
    }

    [Fact]
    public void Dodge_and_untyped_bonuses_stack()
    {
        var profile = Profile();
        profile.Bonuses.Add(new BonusEntry { Name = "闪避1", Type = BonusType.Dodge, Target = BonusTarget.ArmorClass, Value = 1 });
        profile.Bonuses.Add(new BonusEntry { Name = "闪避2", Type = BonusType.Dodge, Target = BonusTarget.ArmorClass, Value = 1 });
        profile.Bonuses.Add(new BonusEntry { Name = "无名", Type = BonusType.Untyped, Target = BonusTarget.ArmorClass, Value = 2 });

        var sheet = CombatCalculator.Calculate(profile);

        Assert.Equal(14, sheet.ArmorClass.Total);
    }

    [Fact]
    public void Penalties_always_stack_and_subtract()
    {
        var profile = Profile();
        profile.Bonuses.Add(new BonusEntry { Name = "目眩", Type = BonusType.Penalty, Target = BonusTarget.ArmorClass, Value = 2 });
        profile.Bonuses.Add(new BonusEntry { Name = "俯卧", Type = BonusType.Penalty, Target = BonusTarget.ArmorClass, Value = 3 });

        var sheet = CombatCalculator.Calculate(profile);

        Assert.Equal(5, sheet.ArmorClass.Total);
    }

    [Fact]
    public void Enhancement_to_natural_armor_layers_on_top_of_natural_armor()
    {
        var profile = Profile();
        profile.Bonuses.Add(new BonusEntry { Name = "天生护甲", Type = BonusType.NaturalArmor, Target = BonusTarget.ArmorClass, Value = 1 });
        profile.Bonuses.Add(new BonusEntry
        {
            Name = "树皮术",
            Type = BonusType.Enhancement,
            Enhancement = EnhancementSubject.NaturalArmor,
            Target = BonusTarget.ArmorClass,
            Value = 3,
        });

        var sheet = CombatCalculator.Calculate(profile);

        Assert.Equal(14, sheet.ArmorClass.Total);
        Assert.Equal(10, sheet.TouchArmorClass.Total);
    }

    [Fact]
    public void Touch_and_flat_footed_remove_the_right_components()
    {
        var profile = Profile();
        profile.Abilities.Dexterity = 14; // +2
        profile.Bonuses.Add(new BonusEntry { Name = "链甲", Type = BonusType.Armor, Target = BonusTarget.ArmorClass, Value = 5 });
        profile.Bonuses.Add(new BonusEntry { Name = "重盾", Type = BonusType.Shield, Target = BonusTarget.ArmorClass, Value = 2 });
        profile.Bonuses.Add(new BonusEntry { Name = "天生护甲", Type = BonusType.NaturalArmor, Target = BonusTarget.ArmorClass, Value = 1 });
        profile.Bonuses.Add(new BonusEntry
        {
            Name = "树皮术",
            Type = BonusType.Enhancement,
            Enhancement = EnhancementSubject.NaturalArmor,
            Target = BonusTarget.ArmorClass,
            Value = 3,
        });
        profile.Bonuses.Add(new BonusEntry { Name = "防护戒指", Type = BonusType.Deflection, Target = BonusTarget.ArmorClass, Value = 1 });
        profile.Bonuses.Add(new BonusEntry { Name = "闪避", Type = BonusType.Dodge, Target = BonusTarget.ArmorClass, Value = 1 });

        var sheet = CombatCalculator.Calculate(profile);

        Assert.Equal(25, sheet.ArmorClass.Total);
        Assert.Equal(14, sheet.TouchArmorClass.Total);
        Assert.Equal(22, sheet.FlatFootedArmorClass.Total);
    }

    [Fact]
    public void Cmd_takes_only_the_allowed_ac_bonus_types()
    {
        var profile = Profile();
        profile.Abilities.Strength = 12; // +1
        profile.Abilities.Dexterity = 12; // +1
        profile.BaseAttackBonus = 3;
        profile.Bonuses.Add(new BonusEntry { Name = "全身甲", Type = BonusType.Armor, Target = BonusTarget.ArmorClass, Value = 5 });
        profile.Bonuses.Add(new BonusEntry { Name = "天生护甲", Type = BonusType.NaturalArmor, Target = BonusTarget.ArmorClass, Value = 1 });
        profile.Bonuses.Add(new BonusEntry { Name = "偏斜", Type = BonusType.Deflection, Target = BonusTarget.ArmorClass, Value = 1 });
        profile.Bonuses.Add(new BonusEntry { Name = "士气", Type = BonusType.Morale, Target = BonusTarget.ArmorClass, Value = 2 });

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
        profile.Bonuses.Add(new BonusEntry { Name = "专攻", Type = BonusType.Untyped, Target = BonusTarget.Concentration, Value = 2 });

        var sheet = CombatCalculator.Calculate(profile);

        Assert.Equal(12, sheet.Concentration.Total);
    }

    [Fact]
    public void Weapon_applies_strength_multiplier_enhancement_and_damage_bonus()
    {
        var profile = Profile();
        profile.Abilities.Strength = 18; // +4
        profile.BaseAttackBonus = 5;
        profile.Bonuses.Add(new BonusEntry { Name = "勇气", Type = BonusType.Competence, Target = BonusTarget.Damage, Value = 2 });
        profile.Weapons.Add(new WeaponProfile
        {
            Name = "巨剑",
            DamageDice = "2d6",
            StrengthMultiplier = 1.5,
            Enhancement = 1,
        });

        var sheet = CombatCalculator.Calculate(profile);
        var weapon = Assert.Single(sheet.Weapons);

        Assert.Equal("+10", weapon.AttackDisplay);
        Assert.Equal("2d6+9", weapon.DamageDisplay);
    }

    [Fact]
    public void Off_hand_weapon_keeps_full_negative_strength_penalty()
    {
        var profile = Profile();
        profile.Abilities.Strength = 8; // -1
        profile.Weapons.Add(new WeaponProfile
        {
            Name = "短剑",
            DamageDice = "1d6",
            StrengthMultiplier = 0.5,
        });

        var sheet = CombatCalculator.Calculate(profile);
        var weapon = Assert.Single(sheet.Weapons);

        Assert.Equal("1d6-1", weapon.DamageDisplay);
    }

    [Fact]
    public void Disabled_entries_are_ignored()
    {
        var profile = Profile();
        profile.Bonuses.Add(new BonusEntry
        {
            Name = "未启用",
            Type = BonusType.Untyped,
            Target = BonusTarget.ArmorClass,
            Value = 5,
            IsEnabled = false,
        });

        var sheet = CombatCalculator.Calculate(profile);

        Assert.Equal(10, sheet.ArmorClass.Total);
    }

    private static CharacterProfile Profile() => new();
}
