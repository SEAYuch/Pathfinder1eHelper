using Pathfinder1eHelper.Services;
using Pathfinder1eHelper.ViewModels.Combat;

namespace Pathfinder1eHelper.Test;

/// <summary>专注 DC 的纯计算规则。</summary>
public class ConcentrationCalculatorTests
{
    [Fact]
    public void Defensive_casting_is_15_plus_twice_spell_level() =>
        Assert.Equal(21, ConcentrationCalculator.Dc(ConcentrationSituation.DefensiveCasting, 3, null, null, null));

    [Fact]
    public void Damage_while_casting_adds_damage_and_level() =>
        Assert.Equal(25, ConcentrationCalculator.Dc(ConcentrationSituation.DamageWhileCasting, 3, 12, null, null));

    [Fact]
    public void Grappled_adds_grappler_cmb_and_level() =>
        Assert.Equal(32, ConcentrationCalculator.Dc(ConcentrationSituation.Grappled, 2, null, 20, null));

    [Fact]
    public void Custom_uses_the_custom_dc() =>
        Assert.Equal(18, ConcentrationCalculator.Dc(ConcentrationSituation.Custom, 9, null, null, 18));

    [Fact]
    public void Negative_damage_is_floored_at_zero() =>
        Assert.Equal(11, ConcentrationCalculator.Dc(ConcentrationSituation.DamageWhileCasting, 1, -5, null, null));
}
