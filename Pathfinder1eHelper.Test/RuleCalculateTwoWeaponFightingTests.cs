using Pathfinder1eHelper.Models.Combat;
using Pathfinder1eHelper.Services.Rules;

namespace Pathfinder1eHelper.Test;

/// <summary>
/// 复刻 WotR <c>TwoWeaponFightingAttackPenalty</c>：rank 1 → 主手 −4 / 副手 −8，
/// 副手非轻型再 −2；rank &gt; 1 非神话 → −2/−2；rank &gt; 1 神话 → 0。
/// </summary>
public class RuleCalculateTwoWeaponFightingTests
{
    [Fact]
    public void Main_hand_takes_minus_four_at_rank_one()
    {
        Assert.Equal(4, RuleCalculateTwoWeaponFighting.AttackPenalty(
            isSecondary: false, WeaponCategory.Medium));
    }

    [Theory]
    [InlineData(WeaponCategory.Light, 8)]     // 轻型：豁免追加减值
    [InlineData(WeaponCategory.Medium, 10)]
    [InlineData(WeaponCategory.Heavy, 10)]
    public void Off_hand_takes_minus_eight_plus_two_when_not_light(WeaponCategory category, int expected)
    {
        Assert.Equal(expected, RuleCalculateTwoWeaponFighting.AttackPenalty(
            isSecondary: true, category));
    }

    [Theory]
    [InlineData(WeaponCategory.Light, 2)]
    [InlineData(WeaponCategory.Medium, 4)]
    [InlineData(WeaponCategory.Heavy, 4)]
    public void Greater_rank_reduces_both_hands(WeaponCategory category, int expected)
    {
        Assert.Equal(expected, RuleCalculateTwoWeaponFighting.AttackPenalty(
            isSecondary: true, category, rank: 2));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Mythic_version_ignores_all_penalties(bool isSecondary)
    {
        Assert.Equal(0, RuleCalculateTwoWeaponFighting.AttackPenalty(
            isSecondary, WeaponCategory.Heavy, rank: 2, isMythic: true));
    }

    [Fact]
    public void Off_hand_is_punished_even_if_the_main_hand_is_light()
    {
        // DLL 的判定是「副手非轻型」，与主手分类无关
        Assert.Equal(8, RuleCalculateTwoWeaponFighting.AttackPenalty(
            isSecondary: true, WeaponCategory.Light));
        Assert.Equal(4, RuleCalculateTwoWeaponFighting.AttackPenalty(
            isSecondary: false, WeaponCategory.Heavy));
    }

    [Fact]
    public void Two_weaponing_requires_a_secondary_weapon()
    {
        var single = new CharacterProfile { Weapons = [new WeaponProfile { Name = "长剑" }] };
        Assert.False(RuleCalculateTwoWeaponFighting.IsTwoWeaponing(single));

        var dual = new CharacterProfile
        {
            Weapons =
            [
                new WeaponProfile { Name = "长剑" },
                new WeaponProfile { Name = "短剑", IsSecondary = true },
            ],
        };
        Assert.True(RuleCalculateTwoWeaponFighting.IsTwoWeaponing(dual));
    }

    [Fact]
    public void Half_damage_bonus_halves_the_bonus_and_keeps_the_breakdown_consistent()
    {
        // 力量 16 → +3，副手 ×0.5 → 1（向下取整）；增强 +2
        var result = RuleCalculateWeaponStats.Compute(new WeaponStatsRequest
        {
            BaseAttackBonus = 6,
            DamageAbilityBonus = 3,
            DamageAbilityLabel = "力量",
            DamageAbilityMultiplier = 0.5,
            Enhancement = 2,
            BaseDamageDice = "1d6",
            HalfDamageBonus = true,
        });

        // 加值合计 1 + 2 = 3 → 减半向下取整 = 1
        Assert.Equal(1, result.Damage.Total);
        Assert.Equal(result.Damage.Total, result.Damage.Contributions.Where(c => c.Included).Sum(c => c.Value));
        Assert.Equal("1d6", result.DamageDice);
    }

    [Fact]
    public void Half_damage_bonus_does_nothing_when_the_bonus_is_zero()
    {
        var result = RuleCalculateWeaponStats.Compute(new WeaponStatsRequest
        {
            BaseAttackBonus = 6,
            DamageAbilityBonus = 0,
            BaseDamageDice = "1d6",
            HalfDamageBonus = true,
        });

        Assert.Equal(0, result.Damage.Total);
        Assert.DoesNotContain(result.Damage.Contributions, c => c.Label == "副手伤害加值减半");
    }
}
