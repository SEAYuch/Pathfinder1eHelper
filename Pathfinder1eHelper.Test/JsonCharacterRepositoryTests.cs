using Pathfinder1eHelper.Models.Combat;
using Pathfinder1eHelper.Services;

namespace Pathfinder1eHelper.Test;

/// <summary>多角色 JSON 仓储：往返、删除与旧版单文件迁移。</summary>
public class JsonCharacterRepositoryTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"pf1e-characters-{Guid.NewGuid():N}");

    [Fact]
    public void Empty_directory_loads_no_characters()
    {
        var repository = new JsonCharacterRepository(_directory);

        Assert.Empty(repository.LoadAll());
    }

    [Fact]
    public void Save_then_load_round_trips_and_supports_multiple_characters()
    {
        var repository = new JsonCharacterRepository(_directory);
        var first = new CharacterProfile
        {
            Name = "阿兰",
            Level = 9,
            Size = SizeCategory.Large,
            Abilities = new AbilityScores { Strength = 22, Wisdom = 14 },
            MaxDexBonus = 3,
            CastingAbility = Ability.Wisdom,
            UseDexForManeuvers = true,
            Modifiers =
            [
                new ModifierEntry
                {
                    Name = "树皮术",
                    Descriptor = ModifierDescriptor.NaturalArmorEnhancement,
                    Stat = CombatStat.ArmorClass,
                    Value = 3,
                },
            ],
            Weapons =
            [
                new WeaponProfile
                {
                    Name = "巨剑",
                    BaseDamage = "2d6",
                    Hand = WeaponHand.TwoHanded,
                    Enhancement = 2,
                    CriticalThreatLow = 19,
                    CriticalMultiplier = 2,
                },
            ],
        };
        var second = new CharacterProfile { Name = "贝拉", Level = 3 };

        repository.Save(first);
        repository.Save(second);

        var loaded = repository.LoadAll();

        Assert.Equal(2, loaded.Count);
        Assert.Contains(loaded, p => p.Name == "贝拉");
        var roundTripped = loaded.Single(p => p.Id == first.Id);
        Assert.Equal(9, roundTripped.Level);
        Assert.Equal(SizeCategory.Large, roundTripped.Size);
        Assert.Equal(22, roundTripped.Abilities.Strength);
        Assert.Equal(3, roundTripped.MaxDexBonus);
        Assert.Equal(Ability.Wisdom, roundTripped.CastingAbility);
        Assert.True(roundTripped.UseDexForManeuvers);
        Assert.Single(roundTripped.Modifiers);
        var weapon = Assert.Single(roundTripped.Weapons);
        Assert.Equal("2d6", weapon.BaseDamage);
        Assert.Equal(WeaponHand.TwoHanded, weapon.Hand);
        Assert.Equal(19, weapon.CriticalThreatLow);
        Assert.Equal(2, weapon.CriticalMultiplier);
    }

    [Fact]
    public void Weapon_scoped_modifier_survives_restart()
    {
        var repository = new JsonCharacterRepository(_directory);
        var weapon = new WeaponProfile { Name = "长剑", BaseDamage = "1d8" };
        var profile = new CharacterProfile
        {
            Name = "专攻",
            Weapons = [weapon],
            Modifiers =
            [
                new ModifierEntry
                {
                    Name = "武器专攻（长剑）",
                    Descriptor = ModifierDescriptor.UntypedStackable,
                    Stat = CombatStat.Attack,
                    Value = 1,
                    WeaponId = weapon.Id,
                },
            ],
        };

        repository.Save(profile);

        var loaded = Assert.Single(repository.LoadAll());
        var sheet = CombatCalculator.Calculate(loaded);

        Assert.Equal(1, sheet.Weapons[0].Attack.Total);
    }

    [Fact]
    public void Delete_removes_only_the_requested_character()
    {
        var repository = new JsonCharacterRepository(_directory);
        var first = new CharacterProfile { Name = "甲" };
        var second = new CharacterProfile { Name = "乙" };
        repository.Save(first);
        repository.Save(second);

        repository.Delete(first.Id);

        var remaining = repository.LoadAll();
        Assert.Single(remaining);
        Assert.Equal(second.Id, remaining[0].Id);
    }

    [Fact]
    public void Legacy_single_file_is_migrated_and_renamed()
    {
        Directory.CreateDirectory(_directory);
        var legacy = new CharacterProfile { Name = "旧角色", Level = 5 };
        File.WriteAllText(
            Path.Combine(_directory, "character.json"),
            System.Text.Json.JsonSerializer.Serialize(legacy));

        var repository = new JsonCharacterRepository(_directory);
        var loaded = repository.LoadAll();

        var profile = Assert.Single(loaded);
        Assert.Equal("旧角色", profile.Name);
        Assert.Equal(5, profile.Level);
        Assert.True(File.Exists(Path.Combine(_directory, "character.json.bak")));
        Assert.True(File.Exists(repository.FilePathFor(profile.Id)));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
