using System.Windows.Input;
using Pathfinder1eHelper.Models;
using Pathfinder1eHelper.Models.Combat;
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

        Assert.Equal(10, vm.Sheet.MeleeAttack.Total);
        Assert.Equal(13, vm.Cards.Count);
    }

    [Fact]
    public async Task Changing_strength_recalculates_and_saves_in_background()
    {
        var repository = new FakeCharacterRepository();
        repository.Data.Add(new CharacterProfile { Name = "测试" });
        var vm = Create(repository);

        vm.Strength = 20;
        Assert.Equal(5, vm.Sheet.MeleeAttack.Total); // 重算仍同步生效

        await vm.FlushPendingSavesAsync(); // 落盘在后台合并进行

        Assert.NotNull(repository.LastSaved);
        Assert.Equal(20, repository.LastSaved!.Abilities.Strength);
    }

    [Fact]
    public void Legacy_touch_bonus_targets_are_migrated_on_load()
    {
        var repository = new FakeCharacterRepository();
        repository.Data.Add(new CharacterProfile
        {
            Name = "旧档",
            Bonuses =
            [
                new BonusEntry
                {
                    Name = "旧接触加值",
                    Type = BonusType.Untyped,
                    Target = BonusTarget.MeleeTouchAttack,
                    Value = 2,
                },
            ],
        });

        var vm = Create(repository);

        Assert.Equal(2, vm.Sheet.MeleeAttack.Total);
        Assert.Equal(BonusTarget.MeleeAttack, vm.Bonuses[0].Target.Value);
    }

    [Fact]
    public void Ability_score_bonus_is_applied_before_deriving_modifiers()
    {
        var repository = new FakeCharacterRepository();
        repository.Data.Add(new CharacterProfile { Name = "测试" });
        var vm = Create(repository);

        vm.Bonuses.Add(new BonusEntryViewModel(
            new BonusEntry
            {
                Name = "公牛之力",
                Type = BonusType.Enhancement,
                Target = BonusTarget.AbilityScore,
                Ability = Ability.Strength,
                Value = 4,
            },
            () => { },
            _ => { }));
        vm.Strength = 14; // 14 + 4 = 18 → +4

        Assert.Equal(4, vm.Sheet.MeleeAttack.Total);
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
    public void Applying_a_preset_adds_its_bonus_entries()
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
        Assert.Equal(WeaponAbility.Strength, weapon.DamageAbility.Value);

        weapon.DamageAbility = CombatOptions.WeaponChoice(WeaponAbility.Dexterity);

        Assert.Equal(WeaponAbility.Dexterity, weapon.DamageAbility.Value);
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
        Assert.Equal("2d6", weapon.DamageDice);
        Assert.Equal("19–20/×2", weapon.Critical);
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
        Assert.Equal(10, vm.Sheet.MeleeAttack.Total);
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
        Assert.Equal(5, vm.Sheet.MeleeAttack.Total);
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
            Target = "MeleeAttack",
            Value = 1,
            SortOrder = 1,
        });
        var vm = Create(new FakeCharacterRepository(), spells);

        await vm.LoadBuffCandidatesAsync();
        vm.SelectedSpellBuff = Assert.Single(vm.SpellBuffCandidates);
        await vm.AddSpellBuffAsync();

        var bonus = Assert.Single(vm.Bonuses);
        Assert.Equal("祝福术·攻击", bonus.Name);
        Assert.Equal(BonusType.Morale, bonus.Type.Value);
        Assert.Equal(BonusTarget.MeleeAttack, bonus.Target.Value);
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
            BonusType = "Untyped",
            Target = "MeleeAttack",
            Value = -1,
        });
        var vm = Create(new FakeCharacterRepository(), spells);

        await vm.LoadBuffCandidatesAsync();
        vm.SelectedSpellBuff = Assert.Single(vm.SpellBuffCandidates);
        await vm.AddSpellBuffAsync();

        var bonus = Assert.Single(vm.Bonuses);
        Assert.Equal(-1, bonus.Value);
        Assert.Equal(-1, vm.Sheet.MeleeAttack.Total);
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
            BonusType = "Untyped",
            Target = "MeleeAttack",
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
        Assert.Equal(BonusType.Dodge, bonus.Type.Value);
        Assert.Equal(BonusTarget.ArmorClass, bonus.Target.Value);
        Assert.Equal(1, bonus.Value);
        Assert.Equal(BonusOrigin.FeatBuff, bonus.Origin);
        Assert.Contains("CRB", bonus.Notes);
    }

    [Fact]
    public async Task Clear_buffs_removes_spell_and_feat_linkage_entries()
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
            Target = "MeleeAttack",
            Value = 1,
        });
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

        ((ICommand)vm.AddBonusCommand).Execute(null); // 手动条目
        var charge = vm.Presets.First(p => p.Name == "冲锋");
        ((ICommand)charge.ApplyCommand).Execute(null); // 2 条预设条目
        await vm.LoadBuffCandidatesAsync();
        vm.SelectedSpellBuff = Assert.Single(vm.SpellBuffCandidates);
        await vm.AddSpellBuffAsync(); // 1 条法术 Buff
        vm.SelectedFeatBuff = Assert.Single(vm.FeatBuffCandidates);
        await vm.AddFeatBuffAsync(); // 1 条专长 Buff

        Assert.Equal(5, vm.Bonuses.Count);

        ((ICommand)vm.ClearBuffsCommand).Execute(null);

        Assert.Equal(3, vm.Bonuses.Count);
        Assert.DoesNotContain(vm.Bonuses, b => b.Origin is BonusOrigin.SpellBuff or BonusOrigin.FeatBuff);
        Assert.Contains(vm.Bonuses, b => b.Origin == BonusOrigin.Manual);
        Assert.Contains(vm.Bonuses, b => b.Origin == BonusOrigin.Preset);
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
            BonusType = "Enhancement",
            Target = "ArmorClass",
            EnhancementSubject = "NaturalArmor",
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
        Assert.Equal(EnhancementSubject.NaturalArmor, bonus.Enhancement.Value);
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
