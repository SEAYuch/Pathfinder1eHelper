using Pathfinder1eHelper.Services;
using Pathfinder1eHelper.ViewModels.Combat;

namespace Pathfinder1eHelper.Test;

/// <summary>专注 DC 的纯计算规则（对照 WotR DLL）。</summary>
public class ConcentrationCalculatorTests
{
    [Fact]
    public void Defensive_casting_is_15_plus_twice_spell_level() =>
        Assert.Equal(21, ConcentrationCalculator.Dc(ConcentrationSituation.DefensiveCasting, 3, null, null));

    [Fact]
    public void Damage_while_casting_is_10_plus_level_plus_half_damage() =>
        Assert.Equal(19, ConcentrationCalculator.Dc(ConcentrationSituation.DamageWhileCasting, 3, 12, null));

    [Fact]
    public void Difficult_castings_includes_grappled_at_15_plus_level() =>
        Assert.Equal(20, ConcentrationCalculator.Dc(ConcentrationSituation.Grappled, 5, null, null));

    [Fact]
    public void Very_difficult_castings_includes_pinned_at_15_plus_twice_level() =>
        Assert.Equal(21, ConcentrationCalculator.Dc(ConcentrationSituation.Pinned, 3, null, null));

    [Fact]
    public void Custom_uses_the_custom_dc() =>
        Assert.Equal(18, ConcentrationCalculator.Dc(ConcentrationSituation.Custom, 9, null, 18));

    [Fact]
    public void Negative_damage_is_floored_at_zero() =>
        Assert.Equal(11, ConcentrationCalculator.Dc(ConcentrationSituation.DamageWhileCasting, 1, -5, null));
}
