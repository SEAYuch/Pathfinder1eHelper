using Pathfinder1eHelper.Services.Rules;

namespace Pathfinder1eHelper.Test;

/// <summary>猛力攻击数值（对照 WotR WeaponParameters* 组件）。</summary>
public class RuleCalculatePowerAttackTests
{
    [Theory]
    [InlineData(1, -1, 2)]
    [InlineData(3, -1, 2)]
    [InlineData(4, -2, 4)]
    [InlineData(7, -2, 4)]
    [InlineData(8, -3, 6)]
    [InlineData(16, -5, 10)]
    [InlineData(20, -6, 12)]
    public void Scales_penalty_and_damage_by_bab(int bab, int penalty, int damage)
    {
        var (p, d) = RuleCalculatePowerAttack.Compute(
            bab, isMelee: true, isTouch: false, isSecondary: false, holdInTwoHands: false);

        Assert.Equal(penalty, p);
        Assert.Equal(damage, d);
    }

    [Fact]
    public void Two_handed_increases_damage_by_half()
    {
        var (p, d) = RuleCalculatePowerAttack.Compute(
            4, isMelee: true, isTouch: false, isSecondary: false, holdInTwoHands: true);

        Assert.Equal(-2, p);
        Assert.Equal(6, d);
    }

    [Fact]
    public void Off_hand_halves_damage()
    {
        var (p, d) = RuleCalculatePowerAttack.Compute(
            4, isMelee: true, isTouch: false, isSecondary: true, holdInTwoHands: false);

        Assert.Equal(-2, p);
        Assert.Equal(2, d);
    }

    [Fact]
    public void Ranged_and_touch_are_unaffected()
    {
        Assert.Equal((0, 0), RuleCalculatePowerAttack.Compute(
            8, isMelee: false, isTouch: false, isSecondary: false, holdInTwoHands: false));
        Assert.Equal((0, 0), RuleCalculatePowerAttack.Compute(
            8, isMelee: true, isTouch: true, isSecondary: false, holdInTwoHands: false));
    }
}
