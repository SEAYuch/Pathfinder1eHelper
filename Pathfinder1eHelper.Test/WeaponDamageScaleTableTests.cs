using Pathfinder1eHelper.Models.Combat;
using Pathfinder1eHelper.Services.Rules;

namespace Pathfinder1eHelper.Test;

/// <summary>武器伤害骰体型缩放表（对照 WotR <c>WeaponDamageScaleTable</c>）。</summary>
public class WeaponDamageScaleTableTests
{
    [Theory]
    [InlineData("1d8", SizeCategory.Large, "2d6")]
    [InlineData("1d8", SizeCategory.Small, "1d6")]
    [InlineData("2d6", SizeCategory.Large, "3d6")]
    [InlineData("2d6", SizeCategory.Huge, "4d6")]
    [InlineData("1d10", SizeCategory.Large, "2d8")]
    [InlineData("1d12", SizeCategory.Large, "3d6")]
    [InlineData("2d4", SizeCategory.Large, "2d6")]
    [InlineData("1d4", SizeCategory.Gargantuan, "2d6")]
    public void Scales_dice_by_size(string baseDice, SizeCategory size, string expected) =>
        Assert.Equal(expected, WeaponDamageScaleTable.ScaleToText(baseDice, size));

    [Fact]
    public void Medium_size_is_unchanged()
    {
        Assert.Equal("1d8", WeaponDamageScaleTable.ScaleToText("1d8", SizeCategory.Medium));
    }

    [Fact]
    public void Unparsable_text_is_returned_unchanged()
    {
        Assert.Equal("+3", WeaponDamageScaleTable.ScaleToText("+3", SizeCategory.Large));
    }

    [Theory]
    [InlineData(SizeCategory.Medium, 1, SizeCategory.Large)]
    [InlineData(SizeCategory.Tiny, -5, SizeCategory.Tiny)]
    [InlineData(SizeCategory.Colossal, 5, SizeCategory.Colossal)]
    [InlineData(SizeCategory.Small, 2, SizeCategory.Large)]
    public void Size_shift_clamps_to_supported_range(SizeCategory size, int shift, SizeCategory expected) =>
        Assert.Equal(expected, size.Shift(shift));
}
