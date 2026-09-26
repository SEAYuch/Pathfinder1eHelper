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
        var vm = Create(new FakeCharacterRepository(), out _);
        vm.SpellLevel = 3;

        Assert.Equal(21, vm.ConcentrationDc); // 15 + 2 × 3

        vm.SelectedConcentrationSituation = CombatOptions.Concentration(ConcentrationSituation.Custom);
        vm.CustomDc = 18;

        Assert.Equal(18, vm.ConcentrationDc);
    }

    [Fact]
    public void Applying_a_preset_adds_its_modifier_entries()
    {
        var vm = Create(new FakeCharacterRepository(), out _);
        var charge = vm.Presets.First(p => p.Name == "冲锋");

        ((ICommand)charge.ApplyCommand).Execute(null);

        Assert.Equal(2, vm.Bonuses.Count);
        Assert.Contains(vm.Bonuses, b => b.Name == "冲锋");
    }

    [Fact]
    public void Weapon_damage_can_switch_between_strength_and_dexterity()
    {
        var vm = Create(new FakeCharacterRepository(), out _);
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
        var vm = Create(new FakeCharacterRepository(), out _);
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
    public async Task Adding_a_catalog_spell_creates_entries_from_the_buff_table()
    {
        var spells = new FakeSpellService();
        spells.Results.Add(new Spell { Id = 1, NameZh = "祝福术", NameEn = "Bless", Source = "CRB" });
        spells.Buffs.Add(new SpellBuff
        {
            Id = 1,
            NameEn = "Bless",
            NameZh = "祝福术",
            EffectName = "祝福术·攻击",
            BonusType = "Morale",
            Target = "Attack",
            Value = 1,
            SortOrder = 1,
        });
        var vm = Create(new FakeCharacterRepository(), spells);

        await vm.LoadBuffCandidatesAsync();
        vm.SelectedSpellBuff = Assert.Single(vm.SpellBuffCandidates);
        await vm.AddSpellBuffAsync();

        var bonus = Assert.Single(vm.Bonuses);
        Assert.Equal("祝福术·攻击", bonus.Name);
        Assert.Equal(ModifierDescriptor.Morale, bonus.Descriptor.Value);
        Assert.Equal(CombatStat.Attack, bonus.Stat.Value);
        Assert.Equal(1, bonus.Value);
        Assert.Equal(BonusOrigin.SpellBuff, bonus.Origin);
        Assert.Contains("CRB", bonus.Notes);
    }

    [Fact]
    public async Task Buff_with_negative_value_is_preserved()
    {
        var spells = new FakeSpellService();
        spells.Results.Add(new Spell { Id = 9, NameZh = "虚弱诅咒", NameEn = "Bane", Source = "CRB" });
        spells.Buffs.Add(new SpellBuff
        {
            Id = 9,
            NameEn = "Bane",
            NameZh = "虚弱诅咒",
            EffectName = "虚弱诅咒·攻击",
            BonusType = "None",
            Target = "Attack",
            Value = -1,
        });
        var vm = Create(new FakeCharacterRepository(), spells);

        await vm.LoadBuffCandidatesAsync();
        vm.SelectedSpellBuff = Assert.Single(vm.SpellBuffCandidates);
        await vm.AddSpellBuffAsync();

        var bonus = Assert.Single(vm.Bonuses);
        Assert.Equal(ModifierDescriptor.None, bonus.Descriptor.Value);
        Assert.Equal(CombatStat.Attack, bonus.Stat.Value);
        Assert.Equal(-1, bonus.Value);
    }

    [Fact]
    public async Task Buff_with_inverted_scale_bounds_does_not_throw()
    {
        var spells = new FakeSpellService();
        spells.Results.Add(new Spell { Id = 10, NameZh = "异常", NameEn = "Weird", Source = "CRB" });
        spells.Buffs.Add(new SpellBuff
        {
            Id = 10,
            NameEn = "Weird",
            NameZh = "异常",
            EffectName = "异常",
            BonusType = "None",
            Target = "Attack",
            ScaleBase = 1,
            ScaleOffset = 0,
            ScaleStep = 1,
            ScaleMin = 5,
            ScaleMax = 2,
        });
        var vm = Create(new FakeCharacterRepository(), spells);

        await vm.LoadBuffCandidatesAsync();
        vm.SelectedSpellBuff = Assert.Single(vm.SpellBuffCandidates);
        await vm.AddSpellBuffAsync();

        Assert.Single(vm.Bonuses);
    }

    [Fact]
    public async Task Adding_a_feat_creates_entries_from_the_feat_buff_table()
    {
        var spells = new FakeSpellService();
        var feats = new FakeFeatService();
        feats.Results.Add(new Feat { Id = 1, NameZh = "闪避", NameEn = "Dodge", Source = "CRB" });
        feats.Buffs.Add(new FeatBuff
        {
            Id = 1,
            NameEn = "Dodge",
            NameZh = "闪避",
            EffectName = "闪避",
            BonusType = "Dodge",
            Target = "ArmorClass",
            Value = 1,
        });
        var vm = Create(new FakeCharacterRepository(), spells, feats);

        await vm.LoadBuffCandidatesAsync();
        vm.SelectedFeatBuff = Assert.Single(vm.FeatBuffCandidates);
        await vm.AddFeatBuffAsync();

        var bonus = Assert.Single(vm.Bonuses);
        Assert.Equal("闪避", bonus.Name);
        Assert.Equal(ModifierDescriptor.Dodge, bonus.Descriptor.Value);
        Assert.Equal(CombatStat.ArmorClass, bonus.Stat.Value);
        Assert.Equal(1, bonus.Value);
        Assert.Equal(BonusOrigin.FeatBuff, bonus.Origin);
        Assert.Contains("CRB", bonus.Notes);
    }

    [Fact]
    public void Clear_all_bonuses_removes_every_entry()
    {
        var vm = Create(new FakeCharacterRepository(), out _);
        ((ICommand)vm.AddBonusCommand).Execute(null);
        var charge = vm.Presets.First(p => p.Name == "冲锋");
        ((ICommand)charge.ApplyCommand).Execute(null);

        Assert.NotEmpty(vm.Bonuses);

        ((ICommand)vm.ClearAllBonusesCommand).Execute(null);

        Assert.Empty(vm.Bonuses);
        Assert.Equal(10, vm.Sheet.ArmorClass.Total);
    }

    [Fact]
    public async Task Buff_value_scales_with_caster_level()
    {
        var spells = new FakeSpellService();
        spells.Results.Add(new Spell { Id = 2, NameZh = "树皮术", NameEn = "Barkskin", Source = "CRB" });
        spells.Buffs.Add(new SpellBuff
        {
            Id = 2,
            NameEn = "Barkskin",
            NameZh = "树皮术",
            EffectName = "树皮术",
            BonusType = "NaturalArmorEnhancement",
            Target = "ArmorClass",
            Value = 2,
            ScaleBase = 2,
            ScaleOffset = 3,
            ScaleStep = 3,
            ScaleMin = 2,
            ScaleMax = 5,
        });
        var vm = Create(new FakeCharacterRepository(), spells);
        vm.CasterLevel = 9;

        await vm.LoadBuffCandidatesAsync();
        vm.SelectedSpellBuff = Assert.Single(vm.SpellBuffCandidates);
        await vm.AddSpellBuffAsync();

        var bonus = Assert.Single(vm.Bonuses);
        Assert.Equal(4, bonus.Value); // CL9 → 2 + (9-3)/3 = 4
        Assert.Equal(ModifierDescriptor.NaturalArmorEnhancement, bonus.Descriptor.Value);
    }

    [Fact]
    public async Task Unknown_spell_adds_a_blank_editable_entry()
    {
        var spells = new FakeSpellService();
        spells.Results.Add(new Spell { Id = 3, NameZh = "火球术", NameEn = "Fireball", Source = "CRB" });
        var vm = Create(new FakeCharacterRepository(), spells);

        await vm.LoadBuffCandidatesAsync();
        vm.SelectedSpellBuff = Assert.Single(vm.SpellBuffCandidates);
        await vm.AddSpellBuffAsync();

        var bonus = Assert.Single(vm.Bonuses);
        Assert.Equal("火球术", bonus.Name);
        Assert.Contains("未收录", bonus.Notes);
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
    public async Task Feat_buff_power_attack_resolves_to_computed_kind()
    {
        var spells = new FakeSpellService();
        var feats = new FakeFeatService();
        feats.Results.Add(new Feat { Id = 1, NameZh = "猛力攻击", NameEn = "Power Attack", Source = "CRB" });
        feats.Buffs.Add(new FeatBuff
        {
            Id = 9,
            NameEn = "Power Attack",
            NameZh = "猛力攻击",
            EffectName = "猛力攻击",
            BonusType = "None",
            Target = "Attack",
            Kind = "PowerAttack",
            Value = 0,
        });
        var vm = Create(new FakeCharacterRepository(), spells, feats);
        vm.Strength = 16; // +3
        vm.BaseAttackBonus = 4;
        ((ICommand)vm.AddWeaponCommand).Execute(null);

        await vm.LoadBuffCandidatesAsync();
        vm.SelectedFeatBuff = Assert.Single(vm.FeatBuffCandidates);
        await vm.AddFeatBuffAsync();

        var entry = Assert.Single(vm.Bonuses);
        Assert.True(entry.IsPowerAttack);
        Assert.Equal(BonusOrigin.FeatBuff, entry.Origin);
        Assert.Equal(5, vm.Sheet.Weapons[0].Attack.Total); // 4 + 3 - 2
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
            var vm = new CombatViewModel(repository, new FakeSpellService(), new FakeFeatService());
            ((ICommand)vm.AddWeaponCommand).Execute(null);
            var weapon = Assert.Single(vm.Weapons);
            ((ICommand)weapon.AddWeaponFocusCommand).Execute(null);
            ((ICommand)weapon.AddWeaponSpecializationCommand).Execute(null);
            await vm.FlushPendingSavesAsync();

            var restarted = new CombatViewModel(repository, new FakeSpellService(), new FakeFeatService());

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

    [Fact]
    public void Power_attack_preset_adds_a_computed_buff_entry()
    {
        var repository = new FakeCharacterRepository();
        var vm = Create(repository);
        vm.Strength = 16; // +3
        vm.BaseAttackBonus = 4;
        ((ICommand)vm.AddWeaponCommand).Execute(null);
        var preset = vm.Presets.First(p => p.Name == "猛力攻击");

        ((ICommand)preset.ApplyCommand).Execute(null);

        var entry = Assert.Single(vm.Bonuses);
        Assert.True(entry.IsPowerAttack);
        Assert.Equal(BonusOrigin.Preset, entry.Origin);
        Assert.Equal(5, vm.Sheet.Weapons[0].Attack.Total); // 4 + 3 - 2
        Assert.Equal("1d8+7", vm.Sheet.Weapons[0].DamageDisplay);
    }

    private static CombatViewModel Create(FakeCharacterRepository repository) =>
        Create(repository, new FakeSpellService());

    private static CombatViewModel Create(FakeCharacterRepository repository, FakeSpellService spells) =>
        new(repository, spells, new FakeFeatService());

    private static CombatViewModel Create(FakeCharacterRepository repository, FakeSpellService spells, FakeFeatService feats) =>
        new(repository, spells, feats);

    private static CombatViewModel Create(FakeCharacterRepository repository, out FakeSpellService spells)
    {
        spells = new FakeSpellService();
        return new CombatViewModel(repository, spells, new FakeFeatService());
    }
}
