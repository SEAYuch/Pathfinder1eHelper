using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pathfinder1eHelper.Models;
using Pathfinder1eHelper.Models.Combat;
using Pathfinder1eHelper.Services;

namespace Pathfinder1eHelper.ViewModels;

/// <summary>
/// 设计时（XAML 预览器）用的示例服务，集中于此避免在各页面 VM 内重复实现。
/// 仅在 <c>d:DesignWidth</c> 预览路径使用，运行时由 DI 提供真实实现。
/// </summary>
internal sealed class DesignTimeSpellService : ISpellService
{
    public static readonly DesignTimeSpellService Instance = new();

    private static readonly IReadOnlyList<Spell> Sample =
    [
        new Spell { Id = 1, NameZh = "祝福术", NameEn = "Bless", Source = "CRB", FirstLetter = "B" },
        new Spell { Id = 2, NameZh = "树皮术", NameEn = "Barkskin", Source = "CRB", FirstLetter = "B" },
    ];

    public Task<IReadOnlyList<Spell>> SearchAsync(SpellQuery query, CancellationToken ct = default) =>
        Task.FromResult(Sample);

    public Task<int> CountAsync(SpellQuery query, CancellationToken ct = default) => Task.FromResult(Sample.Count);

    public Task<IReadOnlyList<string>> GetSourcesAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<string>>([]);

    public Task<IReadOnlyList<string>> GetClassesAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<string>>([]);

    public Task<Spell?> GetByIdAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(Sample.FirstOrDefault(s => s.Id == id));

    public Task<IReadOnlyList<SpellBuff>> GetBuffsForSpellAsync(string? nameEn, string? nameZh, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<SpellBuff>>([]);
}

/// <summary>设计时角色仓储：返回一个示例角色、忽略写入。</summary>
internal sealed class DesignTimeCharacterRepository : ICharacterRepository
{
    public string DirectoryPath => "(design-time)";

    public IReadOnlyList<CharacterProfile> LoadAll() => [SampleProfile()];

    public void Save(CharacterProfile profile)
    {
    }

    public void Delete(Guid id)
    {
    }

    private static CharacterProfile SampleProfile()
    {
        var profile = new CharacterProfile
        {
            Name = "示例角色",
            Level = 10,
            Size = SizeCategory.Medium,
            Abilities = new AbilityScores
            {
                Strength = 20,
                Dexterity = 14,
                Constitution = 16,
                Intelligence = 10,
                Wisdom = 12,
                Charisma = 8,
            },
            BaseAttackBonus = 10,
            BaseFortitude = 7,
            BaseReflex = 3,
            BaseWill = 3,
            CasterLevel = 10,
            CastingAbility = Ability.Intelligence,
            Bonuses =
            [
                new BonusEntry { Name = "武器专攻", Type = BonusType.Untyped, Target = BonusTarget.MeleeAttack, Value = 1 },
                new BonusEntry { Name = "天生护甲", Type = BonusType.NaturalArmor, Target = BonusTarget.ArmorClass, Value = 1 },
                new BonusEntry
                {
                    Name = "树皮术",
                    Type = BonusType.Enhancement,
                    Enhancement = EnhancementSubject.NaturalArmor,
                    Target = BonusTarget.ArmorClass,
                    Value = 3,
                },
                new BonusEntry { Name = "掩护", Type = BonusType.Circumstance, Target = BonusTarget.ArmorClass, Value = 4 },
                new BonusEntry { Name = "勇气激励", Type = BonusType.Competence, Target = BonusTarget.MeleeAttack, Value = 2 },
                new BonusEntry { Name = "勇气激励", Type = BonusType.Competence, Target = BonusTarget.Damage, Value = 2 },
            ],
            Weapons =
            [
                new WeaponProfile { Name = "长剑", DamageDice = "1d8", StrengthMultiplier = 1, Enhancement = 1 },
                new WeaponProfile
                {
                    Name = "复合长弓",
                    AttackAbility = WeaponAbility.Dexterity,
                    DamageDice = "1d8",
                    StrengthMultiplier = 0,
                    Enhancement = 2,
                },
            ],
        };

        profile.ApplyDefaults();
        return profile;
    }
}

/// <summary>设计时专长服务：一条示例专长。</summary>
internal sealed class DesignTimeFeatService : IFeatService
{
    public static readonly DesignTimeFeatService Instance = new();

    private static readonly IReadOnlyList<Feat> Sample =
    [
        new Feat
        {
            Id = 1,
            Source = "CRB",
            NameZh = "猛力攻击",
            NameEn = "Power Attack",
            FirstLetter = "P",
            FeatType = "战斗",
            IsFighterBonus = true,
            Prerequisites = "力量13，基本攻击加值+1。",
            Summary = "以近战攻击加值换取伤害加值",
        },
    ];

    public Task<IReadOnlyList<Feat>> SearchAsync(FeatQuery query, CancellationToken ct = default) =>
        Task.FromResult(Sample);

    public Task<int> CountAsync(FeatQuery query, CancellationToken ct = default) => Task.FromResult(Sample.Count);

    public Task<IReadOnlyList<string>> GetSourcesAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<string>>([]);

    public Task<IReadOnlyList<string>> GetTypesAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<string>>([]);

    public Task<IReadOnlyList<FeatBuff>> GetBuffsForFeatAsync(string? nameEn, string? nameZh, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<FeatBuff>>([]);
}

/// <summary>设计时怪物服务：空实现。</summary>
internal sealed class DesignTimeMonsterService : IMonsterService
{
    public static readonly DesignTimeMonsterService Instance = new();

    public Task<IReadOnlyList<Monster>> SearchAsync(MonsterQuery query, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Monster>>([]);

    public Task<int> CountAsync(MonsterQuery query, CancellationToken ct = default) => Task.FromResult(0);

    public Task<IReadOnlyList<string>> GetCreatureTypesAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<string>>([]);

    public Task<IReadOnlyList<MonsterGroup>> GetGroupsForMonsterAsync(int monsterId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<MonsterGroup>>([]);

    public Task<IReadOnlyList<Monster>> GetGroupMembersAsync(int groupId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Monster>>([]);
}
