using Pathfinder1eHelper.Services.Rules;

namespace Pathfinder1eHelper.Test;

public class RuleCalculateCombatExpertiseTests
{
    [Theory]
    // 档位 = 1 + BAB/4；攻击减值 = −档位，AC 闪避 = +档位
    [InlineData(0, -1, 1)]
    [InlineData(3, -1, 1)]
    [InlineData(4, -2, 2)]
    [InlineData(7, -2, 2)]
    [InlineData(8, -3, 3)]
    [InlineData(11, -3, 3)]
    [InlineData(12, -4, 4)]
    [InlineData(16, -5, 5)]
    public void Compute_scales_both_sides_with_bab(int bab, int expectedPenalty, int expectedAc)
    {
        var (penalty, ac) = RuleCalculateCombatExpertise.Compute(bab);

        Assert.Equal(expectedPenalty, penalty);
        Assert.Equal(expectedAc, ac);
    }

    [Fact]
    public void Negative_bab_is_treated_as_zero()
    {
        var (penalty, ac) = RuleCalculateCombatExpertise.Compute(-3);

        Assert.Equal(-1, penalty);
        Assert.Equal(1, ac);
    }

    [Fact]
    public void Non_melee_attack_loses_the_penalty_but_keeps_the_ac_bonus()
    {
        var (penalty, ac) = RuleCalculateCombatExpertise.Compute(baseAttackBonus: 8, isMeleeWeaponAttack: false);

        Assert.Equal(0, penalty);
        Assert.Equal(3, ac);
    }
}
