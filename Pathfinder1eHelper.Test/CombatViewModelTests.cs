using System.Windows.Input;
using Pathfinder1eHelper.Models;
using Pathfinder1eHelper.Models.Combat;
using Pathfinder1eHelper.Services;
using Pathfinder1eHelper.ViewModels.Combat;
using Pathfinder1eHelper.ViewModels.Pages;

namespace Pathfinder1eHelper.Test;

/// <summary>战斗页视图模型：初始化、重算、多角色管理、预设与法术 Buff 联动。</summary>
public class CombatViewModelTests
{
    [Fact]
    public void Loaded_profile_is_calculated_on_construction()
    {
        var repository = new FakeCharacterRepository();
        repository.Data.Add(new CharacterProfile
        {
            Name = "测试",
            Abilities = new AbilityScores { Strength = 20 },
            BaseAttackBonus = 5,
        });

        var vm = Create(repository);

        Assert.Equal(10, vm.Sheet.Cmb.Total);
        Assert.Equal(12, vm.Cards.Count);
    }

    [Fact]
    public async Task Changing_strength_recalculates_and_saves_in_background()
    {
        var repository = new FakeCharacterRepository();
        repository.Data.Add(new CharacterProfile { Name = "测试" });
        var vm = Create(repository);

        vm.Strength = 20;
        Assert.Equal(5, vm.Sheet.Cmb.Total); // 重算仍同步生效

        await vm.FlushPendingSavesAsync(); // 落盘在后台合并进行

        Assert.NotNull(repository.LastSaved);
        Assert.Equal(20, repository.LastSaved!.Abilities.Strength);
    }

    [Fact]
    public void Ability_score_bonus_is_applied_before_deriving_modifiers()
    {
        var repository = new FakeCharacterRepository();
        repository.Data.Add(new CharacterProfile { Name = "测试" });
        var vm = Create(repository);

        vm.Bonuses.Add(new BonusEntryViewModel(
            new ModifierEntry
            {
                Name = "公牛之力",
                Descriptor = ModifierDescriptor.Enhancement,
                Stat = CombatStat.AbilityScore,
                Ability = Ability.Strength,
                Value = 4,
            },
            () => { },
            _ => { }));
        vm.Strength = 14; // 14 + 4 = 18 → +4

        Assert.Equal(4, vm.Sheet.Cmb.Total);
    }

    [Fact]
    public void Add_and_remove_bonus_updates_collection_and_total()
    {
        var repository = new FakeCharacterRepository();
        var vm = Create(repository);

        ((ICommand)vm.AddBonusCommand).Execute(null);
        var bonus = Assert.Single(vm.Bonuses);
        bonus.Value = 4;

        Assert.Equal(14, vm.Sheet.ArmorClass.Total);

        ((ICommand)bonus.RemoveCommand).Execute(null);
        Assert.Empty(vm.Bonuses);
        Assert.Equal(10, vm.Sheet.ArmorClass.Total);
    }

    [Fact]
    public void Concentration_dc_follows_the_selected_situation()
    {
        var vm = Create(new FakeCharacterRepository());
        vm.SpellLevel = 3;

        Assert.Equal(21, vm.ConcentrationDc); // 15 + 2 × 3

        vm.SelectedConcentrationSituation = CombatOptions.Concentration(ConcentrationSituation.Custom);
        vm.CustomDc = 18;

        Assert.Equal(18, vm.ConcentrationDc);
    }

    [Fact]
    public void Applying_a_preset_adds_its_modifier_entries()
    {
        var vm = Create(new FakeCharacterRepository());
        var charge = vm.Presets.First(p => p.Name == "冲锋");

        ((ICommand)charge.ApplyCommand).Execute(null);

        Assert.Equal(2, vm.Bonuses.Count);
        Assert.Contains(vm.Bonuses, b => b.Name == "冲锋");
    }

    [Fact]
    public void Weapon_damage_can_switch_between_strength_and_dexterity()
    {
        var vm = Create(new FakeCharacterRepository());
        vm.Dexterity = 16; // +3
        ((ICommand)vm.AddWeaponCommand).Execute(null);

        var weapon = Assert.Single(vm.Weapons);
        Assert.Null(weapon.DamageBonusStat); // 自动（近战默认力量）

        weapon.DamageBonusStat = CombatOptions.Ability(Ability.Dexterity);

        Assert.Equal(Ability.Dexterity, weapon.DamageBonusStat!.Value);
        Assert.Equal(3, vm.Sheet.Weapons[0].Damage.Total);
    }

    [Fact]
    public void Applying_a_weapon_preset_adds_a_weapon()
    {
        var vm = Create(new FakeCharacterRepository());
        var greatsword = vm.WeaponPresets.First(p => p.Name == "巨剑");

        ((ICommand)greatsword.ApplyCommand).Execute(null);

        var weapon = Assert.Single(vm.Weapons);
        Assert.Equal("巨剑", weapon.Name);
        Assert.Equal("2d6", weapon.BaseDamage);
        Assert.Equal(WeaponHand.TwoHanded, weapon.Hand.Value);
        Assert.Equal(19, weapon.CriticalThreatLow);
        Assert.Equal(2, weapon.CriticalMultiplier);
    }

    [Fact]
    public void New_character_command_adds_and_selects_a_profile()
    {
        var repository = new FakeCharacterRepository();
        repository.Data.Add(new CharacterProfile { Name = "原有" });
        var vm = Create(repository);

        ((ICommand)vm.NewCharacterCommand).Execute(null);

        Assert.Equal(2, vm.Characters.Count);
        Assert.Equal("新角色 2", vm.SelectedCharacter!.Name);
        Assert.Equal(vm.SelectedCharacter.Profile.Id, repository.LastSaved!.Id);
    }

    [Fact]
    public void Duplicate_character_command_copies_the_current_values()
    {
        var repository = new FakeCharacterRepository();
        var source = new CharacterProfile
        {
            Name = "瓦伦",
            Abilities = new AbilityScores { Strength = 20 },
            BaseAttackBonus = 5,
        };
        repository.Data.Add(source);
        var vm = Create(repository);

        ((ICommand)vm.DuplicateCharacterCommand).Execute(null);

        Assert.Equal(2, vm.Characters.Count);
        Assert.Equal("瓦伦 副本", vm.SelectedCharacter!.Name);
        Assert.NotEqual(source.Id, vm.SelectedCharacter.Profile.Id);
        Assert.Equal(10, vm.Sheet.Cmb.Total);
    }

    [Fact]
    public void Delete_character_command_removes_and_deletes_the_profile()
    {
        var repository = new FakeCharacterRepository();
        var first = new CharacterProfile { Name = "甲" };
        var second = new CharacterProfile { Name = "乙" };
        repository.Data.Add(first);
        repository.Data.Add(second);
        var vm = Create(repository);
        var selectedId = vm.SelectedCharacter!.Profile.Id;

        ((ICommand)vm.DeleteCharacterCommand).Execute(null);

        Assert.Single(vm.Characters);
        Assert.Contains(selectedId, repository.Deleted);
    }

    [Fact]
    public void Switching_character_loads_its_values()
    {
        var repository = new FakeCharacterRepository();
        repository.Data.Add(new CharacterProfile { Name = "弱", Abilities = new AbilityScores { Strength = 10 } });
        repository.Data.Add(new CharacterProfile { Name = "强", Abilities = new AbilityScores { Strength = 20 } });
        var vm = Create(repository);

        vm.SelectedCharacter = vm.Characters.First(c => c.Name == "强");

        Assert.Equal("强", vm.Name);
        Assert.Equal(5, vm.Sheet.Cmb.Total);
    }

    [Fact]
    public void Clear_all_bonuses_removes_every_entry()
    {
        var vm = Create(new FakeCharacterRepository());
        ((ICommand)vm.AddBonusCommand).Execute(null);
        var charge = vm.Presets.First(p => p.Name == "冲锋");
        ((ICommand)charge.ApplyCommand).Execute(null);

        Assert.NotEmpty(vm.Bonuses);

        ((ICommand)vm.ClearAllBonusesCommand).Execute(null);

        Assert.Empty(vm.Bonuses);
        Assert.Equal(10, vm.Sheet.ArmorClass.Total);
    }

    [Fact]
    public void Weapon_focus_button_adds_a_scoped_attack_modifier()
    {
        var repository = new FakeCharacterRepository();
        var vm = Create(repository);
        ((ICommand)vm.AddWeaponCommand).Execute(null);
        var weapon = Assert.Single(vm.Weapons);

        ((ICommand)weapon.AddWeaponFocusCommand).Execute(null);

        var bonus = Assert.Single(vm.Bonuses);
        Assert.Equal(CombatStat.Attack, bonus.Stat.Value);
        Assert.Equal(ModifierDescriptor.UntypedStackable, bonus.Descriptor.Value);
        Assert.Equal(weapon.Id, bonus.WeaponId);
        Assert.Equal(1, vm.Sheet.Weapons[0].Attack.Total);
    }

    [Fact]
    public void Preset_apply_preserves_the_stance_tag()
    {
        var vm = Create(new FakeCharacterRepository());

        vm.Presets.Single(p => p.Name == "全防御").ApplyCommand.Execute(null);
        vm.Presets.Single(p => p.Name == "防御式战斗").ApplyCommand.Execute(null);

        Assert.All(vm.Bonuses, b => Assert.NotNull(b.Stance));
        Assert.Single(vm.Bonuses, b => b.Stance == CombatStance.TotalDefense);
        Assert.Equal(2, vm.Bonuses.Count(b => b.Stance == CombatStance.DefensiveFighting));
    }

    [Fact]
    public void Applying_a_preset_does_not_mutate_the_shared_preset_entries()
    {
        var before = CombatPresets.All
            .SelectMany(p => p.Entries.Select(e => (e.Name, e.IsEnabled, e.Stance)))
            .ToList();

        var vm = Create(new FakeCharacterRepository());
        vm.Presets.Single(p => p.Name == "全防御").ApplyCommand.Execute(null);
        vm.Bonuses[0].IsEnabled = false;

        var after = CombatPresets.All
            .SelectMany(p => p.Entries.Select(e => (e.Name, e.IsEnabled, e.Stance)))
            .ToList();
        Assert.Equal(before, after);
    }

    [Theory]
    [InlineData("防御式战斗", 2)]
    [InlineData("全防御", 1)]
    public void Defensive_stance_presets_add_ac_and_attack_entries(string name, int expectedEntries)
    {
        var vm = Create(new FakeCharacterRepository());

        vm.Presets.Single(p => p.Name == name).ApplyCommand.Execute(null);

        Assert.Equal(expectedEntries, vm.Bonuses.Count);
        Assert.Contains(vm.Bonuses, b => b.Stat.Value == CombatStat.ArmorClass && b.Descriptor.Value == ModifierDescriptor.Dodge);
    }

    [Fact]
    public void Presets_do_not_include_removed_rule_buttons()
    {
        var vm = Create(new FakeCharacterRepository());

        Assert.DoesNotContain(vm.Presets, preset => preset.Name is "猛力攻击" or "寓守于攻" or "双武器格斗");
        Assert.Contains(vm.Presets, preset => preset.Name == "狂暴");
    }

    [Fact]
    public void Weapon_focus_buttons_work_on_weapons_loaded_from_disk()
    {
        // 复现：重启后武器来自磁盘 LoadProfile 路径，按钮必须仍然可用
        // （此前 LoadProfile 用了两参构造，addFocus/addSpecialization 为 null，按钮静默失效）。
        var repository = new FakeCharacterRepository();
        repository.Data.Add(new CharacterProfile
        {
            Name = "旧档",
            Abilities = new AbilityScores { Strength = 16 },
            BaseAttackBonus = 6,
            Weapons = [new WeaponProfile { Name = "徒手击打", BaseDamage = "1d3" }],
        });
        var vm = Create(repository);

        var weapon = Assert.Single(vm.Weapons);
        ((ICommand)weapon.AddWeaponFocusCommand).Execute(null);
        ((ICommand)weapon.AddWeaponSpecializationCommand).Execute(null);

        Assert.Equal(2, vm.Bonuses.Count);
        Assert.Equal(weapon.Id, vm.Bonuses[0].WeaponId);
        Assert.Equal(10, vm.Sheet.Weapons[0].Attack.Total); // 6 + 3 + 1
        Assert.Equal(5, vm.Sheet.Weapons[0].Damage.Total); // 3 + 2
    }

    [Fact]
    public async Task Weapon_focus_survives_a_restart()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"pf1e-restart-{Guid.NewGuid():N}");
        try
        {
            var repository = new JsonCharacterRepository(directory);
            var vm = new CombatViewModel(repository);
            ((ICommand)vm.AddWeaponCommand).Execute(null);
            var weapon = Assert.Single(vm.Weapons);
            ((ICommand)weapon.AddWeaponFocusCommand).Execute(null);
            ((ICommand)weapon.AddWeaponSpecializationCommand).Execute(null);
            await vm.FlushPendingSavesAsync();

            var restarted = new CombatViewModel(repository);

            Assert.Single(restarted.Weapons);
            Assert.Equal(1, restarted.Sheet.Weapons[0].Attack.Total);
            Assert.Equal(2, restarted.Sheet.Weapons[0].Damage.Total);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Theory]
    [InlineData(1, "0.5", 0)]
    [InlineData(3, "1.5", 4)]
    [InlineData(11, "10.0", 110)]
    public void Exchange_generator_adds_two_normal_entries(int penalty, string multiplier, int expectedBonus)
    {
        var vm = Create(new FakeCharacterRepository());
        vm.ExchangePenalty = penalty;
        vm.BonusMultiplierText = multiplier;
        var recalculations = 0;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CombatViewModel.Cards))
            {
                recalculations++;
            }
        };

        vm.AddPenaltyBonusCommand.Execute(null);

        Assert.Equal(2, vm.Bonuses.Count);
        var penaltyEntry = vm.Bonuses[0].ToModel();
        var bonusEntry = vm.Bonuses[1].ToModel();
        Assert.Equal(-penalty, penaltyEntry.Value);
        Assert.Equal(ModifierDescriptor.Penalty, penaltyEntry.Descriptor);
        Assert.Equal($"减值换加值：武器攻击命中（AB，不影响 CMB） −{penalty}", penaltyEntry.Name);
        Assert.Equal(CombatStat.WeaponAttack, penaltyEntry.Stat);
        Assert.Equal(expectedBonus, bonusEntry.Value);
        Assert.Equal(ModifierDescriptor.UntypedStackable, bonusEntry.Descriptor);
        Assert.Equal(CombatStat.Damage, bonusEntry.Stat);
        Assert.All(vm.Bonuses, entry =>
        {
            var model = entry.ToModel();
            Assert.Equal(ModifierKind.Normal, model.Kind);
            Assert.Equal(BonusOrigin.Manual, model.Origin);
            Assert.True(model.IsEnabled);
        });
        Assert.Equal(1, recalculations);
    }

    [Theory]
    [InlineData("0.0")]
    [InlineData("0.6")]
    [InlineData("10.5")]
    [InlineData("abc")]
    [InlineData("")]
    public void Exchange_generator_rejects_invalid_multiplier_without_partial_entries(string multiplier)
    {
        var vm = Create(new FakeCharacterRepository());
        vm.BonusMultiplierText = multiplier;

        vm.AddPenaltyBonusCommand.Execute(null);

        Assert.Empty(vm.Bonuses);
        Assert.NotNull(vm.ExchangeValidationError);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(12)]
    public void Exchange_generator_rejects_out_of_range_penalties(int penalty)
    {
        var vm = Create(new FakeCharacterRepository());
        vm.ExchangePenalty = penalty;

        vm.AddPenaltyBonusCommand.Execute(null);

        Assert.Empty(vm.Bonuses);
        Assert.False(vm.CanAddExchange);
        Assert.NotNull(vm.ExchangeValidationError);
    }

    [Fact]
    public void Exchange_generator_uses_custom_entry_name_for_both_modifiers()
    {
        var vm = Create(new FakeCharacterRepository());
        vm.ExchangeEntryName = "猛攻";
        vm.ExchangePenalty = 2;
        vm.BonusMultiplierText = "1.5";

        vm.AddPenaltyBonusCommand.Execute(null);

        Assert.Equal("猛攻：武器攻击命中（AB，不影响 CMB） −2", vm.Bonuses[0].ToModel().Name);
        Assert.Equal("猛攻：伤害（所有武器） +3（倍率 ×1.5）", vm.Bonuses[1].ToModel().Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Exchange_generator_rejects_empty_entry_name(string name)
    {
        var vm = Create(new FakeCharacterRepository());
        vm.ExchangeEntryName = name;

        vm.AddPenaltyBonusCommand.Execute(null);

        Assert.Empty(vm.Bonuses);
        Assert.Equal("请输入词条名称。", vm.ExchangeValidationError);
    }

    [Fact]
    public void Exchange_generator_can_target_ac_and_use_ac_penalty()
    {
        var vm = Create(new FakeCharacterRepository());
        vm.Strength = 14; // 基线力量 +2，对 CMB 有影响但不影响 AC
        vm.SelectedPenaltySource = CombatOptions.PenaltySources[0];
        vm.SelectedBonusDestination = CombatOptions.BonusDestinations[1];
        vm.ExchangePenalty = 2;
        vm.BonusMultiplierText = "2.0";

        vm.AddPenaltyBonusCommand.Execute(null);

        Assert.Equal(12, vm.Sheet.ArmorClass.Total); // AC 10 −2 + AC +4
        Assert.Equal(2, vm.Sheet.Cmb.Total); // 力量 +2；AC 换加值不影响 CMB
        Assert.Equal(-2, vm.Bonuses[0].ToModel().Value);
        Assert.Equal(CombatStat.ArmorClass, vm.Bonuses[0].Stat.Value);
        Assert.Equal(4, vm.Bonuses[1].ToModel().Value);
        Assert.Equal(CombatStat.ArmorClass, vm.Bonuses[1].Stat.Value);
    }

    [Fact]
    public async Task Exchange_generator_entries_are_persisted_but_settings_are_not()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"pf1e-exchange-{Guid.NewGuid():N}");
        try
        {
            var repository = new JsonCharacterRepository(directory);
            var vm = new CombatViewModel(repository);
            vm.ExchangePenalty = 3;
            vm.BonusMultiplierText = "1.5";
            vm.AddPenaltyBonusCommand.Execute(null);
            await vm.FlushPendingSavesAsync();

            var restarted = new CombatViewModel(repository);

            Assert.Equal(2, restarted.Bonuses.Count);
            Assert.Equal(-3, restarted.Bonuses[0].ToModel().Value);
            Assert.Equal(4, restarted.Bonuses[1].ToModel().Value);
            Assert.Equal(1, restarted.ExchangePenalty);
            Assert.Equal("1.0", restarted.BonusMultiplierText);
            Assert.Equal("减值换加值", restarted.ExchangeEntryName);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void Applying_a_multi_entry_preset_recalculates_only_once()
    {
        var vm = Create(new FakeCharacterRepository());
        var recalculations = 0;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CombatViewModel.Cards))
            {
                recalculations++;
            }
        };

        // 战栗：攻击/强韧/反射/意志 4 条，批量加入后只应重算一次。
        vm.Presets.Single(p => p.Name == "战栗").ApplyCommand.Execute(null);

        Assert.Equal(4, vm.Bonuses.Count);
        Assert.Equal(1, recalculations);
    }

    [Fact]
    public void Single_entry_edit_still_recalculates_immediately()
    {
        var vm = Create(new FakeCharacterRepository());
        var recalculations = 0;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CombatViewModel.Cards))
            {
                recalculations++;
            }
        };

        ((ICommand)vm.AddBonusCommand).Execute(null);
        Assert.Equal(1, recalculations);

        vm.Bonuses[0].Value = 5;
        Assert.Equal(2, recalculations);
    }

    [Fact]
    public void Legacy_buff_origin_names_still_load_from_disk()
    {
        // Buff 联动已删除，但真实旧档里仍有 "SpellBuff"/"FeatBuff" 的 origin。
        // 枚举按字符串序列化；删掉这两个成员会让反序列化抛错、整份角色被 LoadAll 静默跳过。
        var directory = Path.Combine(Path.GetTempPath(), $"pf1e-origin-{Guid.NewGuid():N}");
        var id = Guid.NewGuid();
        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, $"{id:N}.json"), $$"""
            {
              "schemaVersion": 2,
              "id": "{{id}}",
              "name": "旧档",
              "modifiers": [
                { "origin": "FeatBuff", "name": "闪避", "stat": "ArmorClass", "descriptor": "Dodge", "value": 1 },
                { "origin": "SpellBuff", "name": "祝福术", "stat": "Attack", "descriptor": "Morale", "value": 1 }
              ]
            }
            """);

            var profiles = new JsonCharacterRepository(directory).LoadAll();

            var profile = Assert.Single(profiles);
            Assert.Equal(2, profile.Modifiers.Count);
            Assert.Equal(BonusOrigin.FeatBuff, profile.Modifiers[0].Origin);
            Assert.Equal(BonusOrigin.SpellBuff, profile.Modifiers[1].Origin);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    private static CombatViewModel Create(FakeCharacterRepository repository) => new(repository);
}
