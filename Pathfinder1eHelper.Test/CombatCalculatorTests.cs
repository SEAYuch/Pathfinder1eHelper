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

    [Fact]
    public void Combat_expertise_trades_melee_attack_for_ac()
    {
        var profile = Profile();
        profile.Abilities.Dexterity = 14; // +2
        profile.BaseAttackBonus = 4;      // 档位 1 + 4/4 = 2
        profile.Modifiers.Add(CombatExpertiseBuff());
        profile.Weapons.Add(new WeaponProfile { Name = "长剑", BaseDamage = "1d8" });
        profile.Weapons.Add(new WeaponProfile
        {
            Name = "长弓",
            AttackType = WeaponAttackType.Ranged,
            BaseDamage = "1d8",
        });

        var sheet = CombatCalculator.Calculate(profile);

        // AC = 10 + 2(敏捷) + 2(寓守于攻闪避)
        Assert.Equal(14, sheet.ArmorClass.Total);
        // 近战 = 4(BAB) + 0(力量10) − 2(寓守于攻)
        Assert.Equal(2, sheet.Weapons[0].Attack.Total);
        // 远程不吃近战减值：4 + 2(敏捷)
        Assert.Equal(6, sheet.Weapons[1].Attack.Total);
    }

    [Fact]
    public void Combat_expertise_penalty_does_apply_to_cmb()
    {
        var profile = Profile();
        profile.BaseAttackBonus = 4; // 档位 2 → 战技 −2
        profile.Modifiers.Add(CombatExpertiseBuff());

        var sheet = CombatCalculator.Calculate(profile);

        Assert.Equal(2, sheet.Cmb.Total); // 4 + 0 − 2
    }

    [Fact]
    public void Disabled_combat_expertise_entry_is_ignored()
    {
        var profile = Profile();
        profile.BaseAttackBonus = 4;
        var entry = CombatExpertiseBuff();
        entry.IsEnabled = false;
        profile.Modifiers.Add(entry);
        profile.Weapons.Add(new WeaponProfile { Name = "长剑", BaseDamage = "1d8" });

        var sheet = CombatCalculator.Calculate(profile);

        Assert.Equal(4, sheet.Weapons[0].Attack.Total);
        Assert.Equal(10, sheet.ArmorClass.Total);
    }

    [Fact]
    public void Combat_expertise_ac_bonus_stacks_with_the_dodge_feat()
    {
        var profile = Profile();
        profile.BaseAttackBonus = 4; // 档位 2
        profile.Modifiers.Add(CombatExpertiseBuff());
        profile.Modifiers.Add(new ModifierEntry
        {
            Name = "闪避",
            Descriptor = ModifierDescriptor.Dodge,
            Stat = CombatStat.ArmorClass,
            Value = 1,
        });

        var sheet = CombatCalculator.Calculate(profile);

        // 10 + 2(寓守于攻) + 1(闪避专长)，同为 Dodge 描述符故求和
        Assert.Equal(13, sheet.ArmorClass.Total);
    }

    [Theory]
    [InlineData("防御式战斗", 12, -4)]
    [InlineData("全防御", 14, 0)]
    public void Defensive_stance_preset_trades_attack_for_dodge_ac(string presetName, int expectedAc, int expectedAttack)
    {
        var profile = Profile();
        var preset = CombatPresets.All.Single(p => p.Name == presetName);
        foreach (var entry in preset.CreateEntries())
        {
            profile.Modifiers.Add(entry);
        }
        profile.Weapons.Add(new WeaponProfile { Name = "长剑", BaseDamage = "1d8" });

        var sheet = CombatCalculator.Calculate(profile);

        // CHM 战斗机动：防御式战斗 = AC +2 闪避 / 攻击 −4；全防御 = AC +4 闪避、无攻击减值
        Assert.Equal(expectedAc, sheet.ArmorClass.Total);
        Assert.Equal(expectedAttack, sheet.Weapons[0].Attack.Total);
    }

    [Fact]
    public void Crane_style_and_wing_stack_as_dodge_ac()
    {
        var profile = Profile();
        profile.Modifiers.Add(new ModifierEntry
        {
            Name = "白鹤拳", Descriptor = ModifierDescriptor.Dodge, Stat = CombatStat.ArmorClass, Value = 1,
        });
        profile.Modifiers.Add(new ModifierEntry
        {
            Name = "白鹤亮翅", Descriptor = ModifierDescriptor.Dodge, Stat = CombatStat.ArmorClass, Value = 4,
        });

        var sheet = CombatCalculator.Calculate(profile);

        Assert.Equal(15, sheet.ArmorClass.Total); // 10 + 1 + 4
    }

    [Fact]
    public void Total_defense_suppresses_combat_expertise()
    {
        var profile = Profile();
        profile.BaseAttackBonus = 4; // 档位 2
        profile.Modifiers.Add(CombatExpertiseBuff());
        foreach (var entry in CombatPresets.All.Single(p => p.Name == "全防御").CreateEntries())
        {
            profile.Modifiers.Add(entry);
        }
        profile.Weapons.Add(new WeaponProfile { Name = "长剑", BaseDamage = "1d8" });

        var sheet = CombatCalculator.Calculate(profile);

        // CHM：全防御期间无法从「寓守于攻」获益 → 只剩全防御的 AC +4 闪避，攻击无减值
        Assert.Equal(14, sheet.ArmorClass.Total);
        Assert.Equal(4, sheet.Weapons[0].Attack.Total);
        Assert.Equal(4, sheet.Cmb.Total);
    }

    [Fact]
    public void Defensive_fighting_does_not_suppress_combat_expertise()
    {
        var profile = Profile();
        profile.BaseAttackBonus = 4; // 档位 2
        profile.Modifiers.Add(CombatExpertiseBuff());
        foreach (var entry in CombatPresets.All.Single(p => p.Name == "防御式战斗").CreateEntries())
        {
            profile.Modifiers.Add(entry);
        }
        profile.Weapons.Add(new WeaponProfile { Name = "长剑", BaseDamage = "1d8" });

        var sheet = CombatCalculator.Calculate(profile);

        // CHM 只禁止「全防御」，防御式战斗下寓守于攻照常生效：AC 10+2(姿态)+2(专长)，攻击 4−4−2
        Assert.Equal(14, sheet.ArmorClass.Total);
        Assert.Equal(-2, sheet.Weapons[0].Attack.Total);
    }

    [Fact]
    public void Disabled_total_defense_entry_does_not_suppress_combat_expertise()
    {
        var profile = Profile();
        profile.BaseAttackBonus = 4;
        profile.Modifiers.Add(CombatExpertiseBuff());
        // 预设条目是共享实例，改动前必须复制，否则会污染其它用例
        var template = CombatPresets.All.Single(p => p.Name == "全防御").Entries[0];
        var totalDefense = new ModifierEntry
        {
            Name = template.Name,
            Descriptor = template.Descriptor,
            Stat = template.Stat,
            Value = template.Value,
            Stance = template.Stance,
            IsEnabled = false,
        };
        profile.Modifiers.Add(totalDefense);
        profile.Weapons.Add(new WeaponProfile { Name = "长剑", BaseDamage = "1d8" });

        var sheet = CombatCalculator.Calculate(profile);

        // 停用的全防御既不提供 AC，也不压制寓守于攻
        Assert.Equal(12, sheet.ArmorClass.Total);
        Assert.Equal(2, sheet.Weapons[0].Attack.Total);
    }

    [Fact]
    public void Total_defense_entry_does_not_suppress_combat_expertise()
    {
        var profile = Profile();
        profile.BaseAttackBonus = 4; // 档位 2
        profile.Modifiers.Add(CombatExpertiseBuff());
        // 清掉 Stance 标记：这只应退化为普通 +4 闪避加值
        foreach (var entry in CombatPresets.All.Single(p => p.Name == "全防御").CreateEntries())
        {
            entry.Stance = null;
            profile.Modifiers.Add(entry);
        }
        profile.Weapons.Add(new WeaponProfile { Name = "长剑", BaseDamage = "1d8" });

        var sheet = CombatCalculator.Calculate(profile);

        // 未打标记 ⇒ 不压制寓守于攻：AC 10+4+2，攻击 4−2
        Assert.Equal(16, sheet.ArmorClass.Total);
        Assert.Equal(2, sheet.Weapons[0].Attack.Total);
    }

    [Fact]
    public void Crane_style_relaxes_defensive_fighting_penalty_to_minus_two()
    {
        var profile = Profile();
        profile.BaseAttackBonus = 6;
        foreach (var entry in CombatPresets.All.Single(p => p.Name == "防御式战斗").CreateEntries())
        {
            profile.Modifiers.Add(entry);
        }
        profile.Modifiers.Add(CraneStyleBuff());
        profile.Weapons.Add(new WeaponProfile { Name = "长剑", BaseDamage = "1d8" });

        var sheet = CombatCalculator.Calculate(profile);

        // 防御式战斗 −4，被白鹤拳放宽到 −2：6 − 2 = 4；AC 仍是 10 + 2（防御式战斗）
        Assert.Equal(4, sheet.Weapons[0].Attack.Total);
        Assert.Equal(12, sheet.ArmorClass.Total);
        // 走同一条 Attack 通道，战技一致抵消：6 − 2 = 4
        Assert.Equal(4, sheet.Cmb.Total);
    }

    [Fact]
    public void Crane_style_without_defensive_fighting_grants_nothing()
    {
        var profile = Profile();
        profile.BaseAttackBonus = 6;
        profile.Modifiers.Add(CraneStyleBuff());
        profile.Weapons.Add(new WeaponProfile { Name = "长剑", BaseDamage = "1d8" });

        var sheet = CombatCalculator.Calculate(profile);

        Assert.Equal(6, sheet.Weapons[0].Attack.Total);
        Assert.Equal(10, sheet.ArmorClass.Total);
    }

    [Fact]
    public void Defensive_fighting_alone_keeps_the_full_minus_four()
    {
        var profile = Profile();
        profile.BaseAttackBonus = 6;
        foreach (var entry in CombatPresets.All.Single(p => p.Name == "防御式战斗").CreateEntries())
        {
            profile.Modifiers.Add(entry);
        }
        profile.Weapons.Add(new WeaponProfile { Name = "长剑", BaseDamage = "1d8" });

        var sheet = CombatCalculator.Calculate(profile);

        Assert.Equal(2, sheet.Weapons[0].Attack.Total); // 6 − 4
    }

    [Fact]
    public void Two_weapon_fighting_halves_the_off_hand_damage_bonus_only()
    {
        var profile = Profile();
        profile.Abilities.Strength = 16; // +3
        profile.BaseAttackBonus = 6;
        profile.Modifiers.Add(TwoWeaponFightingBuff());
        profile.Weapons.Add(new WeaponProfile { Name = "长剑", BaseDamage = "1d8" });
        profile.Weapons.Add(new WeaponProfile
        {
            Name = "短剑",
            BaseDamage = "1d6",
            IsSecondary = true,
            Category = WeaponCategory.Light,
        });

        var sheet = CombatCalculator.Calculate(profile);

        // 主手伤害 +3（力量全取）；副手力量×0.5 → 1，再减半 → 0
        Assert.Equal(3, sheet.Weapons[0].Damage.Total);
        Assert.Equal(0, sheet.Weapons[1].Damage.Total);
    }

    [Fact]
    public void A_double_weapon_in_the_main_hand_waives_the_extra_off_hand_penalty()
    {
        var profile = Profile();
        profile.BaseAttackBonus = 6;
        profile.Abilities.Strength = 16; // +3
        profile.Modifiers.Add(TwoWeaponFightingBuff());
        profile.Weapons.Add(new WeaponProfile
        {
            Name = "双头剑",
            BaseDamage = "1d8",
            IsDouble = true,
        });
        profile.Weapons.Add(new WeaponProfile
        {
            Name = "巨剑",
            BaseDamage = "1d6",
            IsSecondary = true,
            Category = WeaponCategory.Medium,
        });

        var sheet = CombatCalculator.Calculate(profile);

        // 主手是双头武器 → 副手不再追加 −2，仍为 −8
        Assert.Equal(1, sheet.Weapons[1].Attack.Total);
    }

    private static ModifierEntry TwoWeaponFightingBuff() => new()
    {
        Name = "双武器格斗",
        Kind = ModifierKind.TwoWeaponFighting,
        Descriptor = ModifierDescriptor.UntypedStackable,
        Stat = CombatStat.Attack,
    };

    [Fact]
    public void Two_weapon_fighting_applies_the_per_hand_penalties()
    {
        var profile = Profile();
        profile.BaseAttackBonus = 6;
        profile.Abilities.Strength = 16; // +3
        profile.Modifiers.Add(TwoWeaponFightingBuff());
        profile.Weapons.Add(new WeaponProfile { Name = "长剑", BaseDamage = "1d8" });
        profile.Weapons.Add(new WeaponProfile
        {
            Name = "短剑",
            BaseDamage = "1d6",
            IsSecondary = true,
            Category = WeaponCategory.Light,
        });

        var sheet = CombatCalculator.Calculate(profile);

        // 主手 6 + 3 − 4；副手 6 + 3 − 8（轻型，豁免追加 −2）
        Assert.Equal(5, sheet.Weapons[0].Attack.Total);
        Assert.Equal(1, sheet.Weapons[1].Attack.Total);
    }

    [Fact]
    public void Two_weapon_fighting_penalises_a_non_light_off_hand()
    {
        var profile = Profile();
        profile.BaseAttackBonus = 6;
        profile.Abilities.Strength = 16; // +3
        profile.Modifiers.Add(TwoWeaponFightingBuff());
        profile.Weapons.Add(new WeaponProfile { Name = "长剑", BaseDamage = "1d8" });
        profile.Weapons.Add(new WeaponProfile
        {
            Name = "巨剑",
            BaseDamage = "1d6",
            IsSecondary = true,
            Category = WeaponCategory.Medium,
        });

        var sheet = CombatCalculator.Calculate(profile);

        // 副手非轻型 → −8 − 2 = −10
        Assert.Equal(-1, sheet.Weapons[1].Attack.Total);
    }

    [Fact]
    public void Two_weapon_fighting_does_nothing_without_a_secondary_weapon()
    {
        var profile = Profile();
        profile.BaseAttackBonus = 6;
        profile.Abilities.Strength = 16; // +3
        profile.Modifiers.Add(TwoWeaponFightingBuff());
        profile.Weapons.Add(new WeaponProfile { Name = "长剑", BaseDamage = "1d8" });

        var sheet = CombatCalculator.Calculate(profile);

        Assert.Equal(9, sheet.Weapons[0].Attack.Total);
    }

    private static ModifierEntry CraneStyleBuff() => new()
    {
        Name = "白鹤拳",
        Kind = ModifierKind.CraneStyle,
        Descriptor = ModifierDescriptor.None,
        Stat = CombatStat.Attack,
    };

    private static ModifierEntry CombatExpertiseBuff() => new()
    {
        Name = "寓守于攻",
        Kind = ModifierKind.CombatExpertise,
        Descriptor = ModifierDescriptor.Dodge,
        Stat = CombatStat.ArmorClass,
    };

    private static CharacterProfile Profile() => new();
}
