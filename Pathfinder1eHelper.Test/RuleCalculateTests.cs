using System.Linq;
using Pathfinder1eHelper.Models.Combat;
using Pathfinder1eHelper.Services;
using Pathfinder1eHelper.Services.Rules;

namespace Pathfinder1eHelper.Test;

/// <summary>规则层测试：对照 WotR DLL 的攻击/AC/豁免/先攻/战技/武器/专注公式。</summary>
public class RuleCalculateTests
{
    private static Modifier M(ModifierDescriptor descriptor, int value, string? source = null) =>
        new(descriptor, value, Source: source);

    [Theory]
    [InlineData(20, 5)]
    [InlineData(10, 0)]
    [InlineData(8, -1)]
    [InlineData(1, -5)]
    public void Attribute_bonus_is_half_score_minus_five(int score, int expected)
    {
        var attribute = new AttributeValue(score);

        Assert.Equal(expected, attribute.Bonus);
    }

    [Fact]
    public void Attribute_bonus_includes_modifiers_and_floors_at_one()
    {
        var attribute = new AttributeValue(18);
        attribute.AddModifier(new Modifier(ModifierDescriptor.Enhancement, 4));

        Assert.Equal(6, attribute.Bonus); // 22/2-5

        var drained = new AttributeValue(10);
        drained.AddModifier(new Modifier(ModifierDescriptor.Penalty, -50));

        Assert.Equal(-5, drained.Bonus); // 值夹到 1 → 1/2-5
    }

    [Fact]
    public void Attack_adds_bab_ability_size_and_flanking()
    {
        var attack = RuleCalculateAttackBonus.Compute(new AttackBonusRequest
        {
            BaseAttackBonus = 5,
            AbilityBonus = 5,
            AdditionalAttackBonus = [M(ModifierDescriptor.Size, 1, "体型")],
            IsMelee = true,
            TargetIsFlanked = true,
        });

        Assert.Equal(13, attack.Total); // 5 + 5 + 1 + 2
    }

    [Fact]
    public void Attack_applies_shoot_into_combat_penalty()
    {
        var attack = RuleCalculateAttackBonus.Compute(new AttackBonusRequest
        {
            BaseAttackBonus = 6,
            AbilityBonus = 3,
            ShootIntoCombatPenalty = true,
        });

        Assert.Equal(5, attack.Total); // 6 + 3 - 4
    }

    [Fact]
    public void Attack_does_not_stack_weapon_enhancement_when_stronger_enchantment_present()
    {
        var attack = RuleCalculateAttackBonus.Compute(new AttackBonusRequest
        {
            BaseAttackBonus = 0,
            AbilityBonus = 0,
            WeaponEnhancement = 1,
            AdditionalAttackBonus = [M(ModifierDescriptor.Enhancement, 3, "高等魔化武器")],
        });

        Assert.Equal(3, attack.Total);
    }

    [Fact]
    public void Attack_uses_weapon_enhancement_when_it_beats_existing_enchantment()
    {
        var attack = RuleCalculateAttackBonus.Compute(new AttackBonusRequest
        {
            BaseAttackBonus = 0,
            AbilityBonus = 0,
            WeaponEnhancement = 2,
            AdditionalAttackBonus = [M(ModifierDescriptor.Enhancement, 1, "魔化武器")],
        });

        Assert.Equal(2, attack.Total);
    }

    [Fact]
    public void Armor_class_derives_touch_and_flat_footed()
    {
        var result = RuleCalculateArmorClass.Compute(new ArmorClassRequest
        {
            BaseAttributeBonus = 2,
            Modifiers =
            [
                M(ModifierDescriptor.Armor, 5, "链甲"),
                M(ModifierDescriptor.Shield, 2, "重盾"),
                M(ModifierDescriptor.NaturalArmor, 1, "天生护甲"),
                M(ModifierDescriptor.NaturalArmorEnhancement, 3, "树皮术"),
                M(ModifierDescriptor.Deflection, 1, "防护戒指"),
                M(ModifierDescriptor.Dodge, 1, "闪避"),
            ],
        });

        Assert.Equal(25, result.ArmorClass.Total);
        Assert.Equal(14, result.Touch.Total);
        Assert.Equal(22, result.FlatFooted.Total);
        Assert.Equal(11, result.FlatFootedTouch.Total);
    }

    [Fact]
    public void Touch_armor_class_keeps_negative_armor_penalties()
    {
        var result = RuleCalculateArmorClass.Compute(new ArmorClassRequest
        {
            BaseAttributeBonus = 0,
            Modifiers = [M(ModifierDescriptor.Armor, -2, "破损护甲")],
        });

        Assert.Equal(8, result.ArmorClass.Total);
        Assert.Equal(8, result.Touch.Total); // 负值不被接触过滤
    }

    [Fact]
    public void Armor_class_applies_max_dex_limit()
    {
        var result = RuleCalculateArmorClass.Compute(new ArmorClassRequest
        {
            BaseAttributeBonus = 5,
            MaxDexBonusFromArmor = 2,
        });

        Assert.Equal(12, result.ArmorClass.Total); // 10 + min(5,2)
    }

    [Fact]
    public void Saving_throw_adds_base_ability_and_resistance()
    {
        var save = RuleCalculateSavingThrow.Compute(
            3,
            2,
            [M(ModifierDescriptor.Resistance, 1, "抗力斗篷")],
            "体质");

        Assert.Equal(6, save.Total);
    }

    [Fact]
    public void Initiative_adds_dexterity_and_feat_bonus()
    {
        var initiative = RuleCalculateInitiative.Compute(3, [M(ModifierDescriptor.Feat, 4, "精通先攻")]);

        Assert.Equal(7, initiative.Total);
    }

    [Fact]
    public void Cmb_uses_maneuver_size_modifier()
    {
        var cmb = RuleCalculateCombatManeuver.Cmb(new CmbRequest
        {
            BaseAttackBonus = 2,
            ManeuverAbilityBonus = 2,
            SizeBonus = -2, // Tiny
        });

        Assert.Equal(2, cmb.Total);
    }

    [Fact]
    public void Cmd_denies_positive_dexterity_when_flat_footed()
    {
        var cmd = RuleCalculateCombatManeuver.Cmd(new CmdRequest
        {
            BaseAttackBonus = 2,
            ManeuverAbilityBonus = 2,
            DexterityBonus = 3,
            SizeBonus = -2,
            DenyDexterityBonus = true,
        });

        Assert.Equal(12, cmd.Total); // 10 + 2 + 2 + 0(denied) - 2
    }

    [Fact]
    public void Weapon_stats_apply_strength_multiplier_and_enhancement()
    {
        var weapon = RuleCalculateWeaponStats.Compute(new WeaponStatsRequest
        {
            BaseAttackBonus = 5,
            AttackAbilityBonus = 4,
            DamageAbilityBonus = 4,
            DamageAbilityMultiplier = 1.5,
            Enhancement = 1,
            AttackIsMelee = true,
        });

        Assert.Equal(10, weapon.Attack.Total); // 5 + 4 + 1
        Assert.Equal(7, weapon.Damage.Total); // floor(4*1.5) + 1
    }

    [Fact]
    public void Weapon_stats_do_not_multiply_negative_damage_ability()
    {
        var weapon = RuleCalculateWeaponStats.Compute(new WeaponStatsRequest
        {
            DamageAbilityBonus = -1,
            DamageAbilityMultiplier = 0.5,
        });

        Assert.Equal(-1, weapon.Damage.Total);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(5, 1)]
    [InlineData(6, 2)]
    [InlineData(10, 2)]
    [InlineData(11, 3)]
    [InlineData(15, 3)]
    [InlineData(16, 4)]
    [InlineData(20, 4)]
    [InlineData(21, 4)]
    public void Weapon_attacks_count_follows_bab(int bab, int expected)
    {
        Assert.Equal(expected, RuleCalculateWeaponStats.AttacksCount(bab));
    }

    [Fact]
    public void Weapon_stats_scale_damage_dice_by_weapon_size()
    {
        var weapon = RuleCalculateWeaponStats.Compute(new WeaponStatsRequest
        {
            BaseDamageDice = "1d8",
            WeaponSize = SizeCategory.Large,
        });

        Assert.Equal("2d6", weapon.DamageDice);
    }

    [Fact]
    public void Weapon_stats_scale_damage_dice_by_size_shift()
    {
        var weapon = RuleCalculateWeaponStats.Compute(new WeaponStatsRequest
        {
            BaseDamageDice = "1d8",
            DamageDiceSizeShift = 1,
        });

        Assert.Equal("2d6", weapon.DamageDice);
    }

    [Fact]
    public void Concentration_value_uses_caster_level_and_focus_bonus()
    {
        var value = RuleCheckConcentration.Value(7, 3, [M(ModifierDescriptor.Feat, 2, "战斗施法")]);

        Assert.Equal(12, value.Total);
    }

    [Fact]
    public void Concentration_dcs_match_dll_formulas()
    {
        Assert.Equal(21, RuleCheckConcentration.DefensiveCastingDc(3)); // 15 + 2*3
        Assert.Equal(18, RuleCheckConcentration.DamageWhileCastingDc(3, 10)); // 10 + 3 + 10/2
        Assert.Equal(18, RuleCheckConcentration.GeneralDc(3)); // 15 + 3
        Assert.Equal(17, RuleCheckConcentration.SpellDc(3, 4)); // 10 + 3 + 4
    }
}
