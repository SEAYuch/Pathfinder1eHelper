using Pathfinder1eHelper.Services.Rules;

namespace Pathfinder1eHelper.Test;

public class RuleCalculateCraneStyleTests
{
    [Fact]
    public void Crane_style_relaxes_defensive_fighting_to_minus_two()
    {
        var relief = RuleCalculateCraneStyle.AttackRelief(
            hasCraneStyle: true,
            defensiveFightingActive: true);

        Assert.Equal(2, relief);
        Assert.Equal(
            RuleCalculateCraneStyle.DefensiveFightingAttackPenaltyWithCraneStyle,
            RuleCalculateCraneStyle.DefensiveFightingAttackPenalty + relief);
    }

    [Theory]
    // 没有白鹤拳，或没有防御式战斗条目，都不该凭空多出 +2
    [InlineData(false, true, 0)]
    [InlineData(true, false, 0)]
    [InlineData(false, false, 0)]
    public void No_relief_without_both_conditions(bool hasCraneStyle, bool defensiveFighting, int expected)
    {
        Assert.Equal(expected, RuleCalculateCraneStyle.AttackRelief(hasCraneStyle, defensiveFighting));
    }
}
