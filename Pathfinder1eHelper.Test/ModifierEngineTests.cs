using Pathfinder1eHelper.Models.Combat;
using Pathfinder1eHelper.Services;

namespace Pathfinder1eHelper.Test;

/// <summary>加值叠加引擎测试：对照 WotR <c>ModifiableValue.ApplyModifiersFiltered</c> 语义。</summary>
public class ModifierEngineTests
{
    private static Modifier M(ModifierDescriptor descriptor, int value, StackMode mode = StackMode.Default) =>
        new(descriptor, value, mode);

    [Fact]
    public void Untyped_bonuses_stack()
    {
        var total = ModifierEngine.Apply(10, [M(ModifierDescriptor.None, 1), M(ModifierDescriptor.None, 2)]);

        Assert.Equal(13, total);
    }

    [Fact]
    public void Dodge_bonuses_stack()
    {
        var total = ModifierEngine.Apply(10, [M(ModifierDescriptor.Dodge, 1), M(ModifierDescriptor.Dodge, 2)]);

        Assert.Equal(13, total);
    }

    [Fact]
    public void Penalty_descriptor_sums_signed_values()
    {
        var total = ModifierEngine.Apply(10, [M(ModifierDescriptor.Penalty, -2), M(ModifierDescriptor.Penalty, -3)]);

        Assert.Equal(5, total);
    }

    [Fact]
    public void Non_stackable_descriptor_takes_highest_positive()
    {
        var total = ModifierEngine.Apply(10, [M(ModifierDescriptor.Morale, 2), M(ModifierDescriptor.Morale, 3)]);

        Assert.Equal(13, total);
    }

    [Fact]
    public void Non_stackable_descriptor_keeps_best_positive_and_worst_negative()
    {
        var total = ModifierEngine.Apply(
            10,
            [
                M(ModifierDescriptor.Morale, 3),
                M(ModifierDescriptor.Morale, 2),
                M(ModifierDescriptor.Morale, -4),
                M(ModifierDescriptor.Morale, -1),
            ]);

        Assert.Equal(9, total);
    }

    [Fact]
    public void Circumstance_is_non_stackable()
    {
        var total = ModifierEngine.Apply(10, [M(ModifierDescriptor.Circumstance, 2), M(ModifierDescriptor.Circumstance, 5)]);

        Assert.Equal(15, total);
    }

    [Fact]
    public void Armor_and_encumbrance_penalties_use_single_worst_min()
    {
        var total = ModifierEngine.Apply(10, [M(ModifierDescriptor.Armor, -2), M(ModifierDescriptor.Encumbrance, -5)]);

        Assert.Equal(5, total);
    }

    [Fact]
    public void Positive_armor_is_non_stackable()
    {
        var total = ModifierEngine.Apply(10, [M(ModifierDescriptor.Armor, 4), M(ModifierDescriptor.Armor, 6)]);

        Assert.Equal(16, total);
    }

    [Fact]
    public void Force_stack_overrides_non_stackable_descriptor()
    {
        var total = ModifierEngine.Apply(
            10,
            [
                M(ModifierDescriptor.Morale, 2, StackMode.ForceStack),
                M(ModifierDescriptor.Morale, 3, StackMode.ForceStack),
            ]);

        Assert.Equal(15, total);
    }

    [Fact]
    public void Force_non_stack_overrides_stackable_descriptor()
    {
        var total = ModifierEngine.Apply(
            10,
            [
                M(ModifierDescriptor.None, 2, StackMode.ForceNonStack),
                M(ModifierDescriptor.None, 3, StackMode.ForceNonStack),
            ]);

        Assert.Equal(13, total);
    }

    [Fact]
    public void Different_descriptors_apply_independently()
    {
        var total = ModifierEngine.Apply(
            10,
            [
                M(ModifierDescriptor.Dodge, 1),
                M(ModifierDescriptor.Morale, 2),
                M(ModifierDescriptor.Penalty, -1),
            ]);

        Assert.Equal(12, total);
    }

    [Fact]
    public void Filter_excludes_modifiers()
    {
        var total = ModifierEngine.Apply(
            10,
            [M(ModifierDescriptor.Armor, 5), M(ModifierDescriptor.Shield, 2)],
            filter: m => m.Descriptor != ModifierDescriptor.Armor);

        Assert.Equal(12, total);
    }

    [Fact]
    public void Result_is_clamped_to_min_value_raw()
    {
        var total = ModifierEngine.Apply(10, [M(ModifierDescriptor.Penalty, -50)], minValueRaw: 1);

        Assert.Equal(1, total);
    }

    [Fact]
    public void Helpless_zeroes_attribute_value_when_enabled()
    {
        var total = ModifierEngine.Apply(18, [M(ModifierDescriptor.Helpless, 1)], zeroOnHelpless: true);

        Assert.Equal(0, total);
    }
}
