using System.Linq;
using Pathfinder1eHelper.Models.Combat;

namespace Pathfinder1eHelper.Test;

/// <summary>描述符下拉排序与可叠加集合的测试。</summary>
public class ModifierDescriptorCatalogTests
{
    [Fact]
    public void Display_order_covers_every_declared_descriptor_once()
    {
        var declared = System.Enum.GetValues<ModifierDescriptor>().Distinct().ToArray();
        var order = ModifierDescriptorCatalog.DisplayOrder;

        Assert.Equal(declared.Length, order.Count);
        Assert.Equal(declared.OrderBy(v => v).ToArray(), order.OrderBy(v => v).ToArray());
    }

    [Fact]
    public void Display_order_has_no_duplicates()
    {
        var order = ModifierDescriptorCatalog.DisplayOrder;

        Assert.Equal(order.Count, order.Distinct().Count());
    }

    [Fact]
    public void Common_descriptors_come_first()
    {
        Assert.Equal(ModifierDescriptor.None, ModifierDescriptorCatalog.DisplayOrder[0]);
        Assert.Contains(ModifierDescriptor.Dodge, ModifierDescriptorCatalog.Tier1);
        Assert.Contains(ModifierDescriptor.Morale, ModifierDescriptorCatalog.Tier1);
    }

    [Fact]
    public void Shield_focus_alias_does_not_create_a_duplicate_entry()
    {
        // ShieldFocus 与 Focus 同值：别名不应在展示序列里额外制造一条。
        Assert.Equal(1, ModifierDescriptorCatalog.DisplayOrder.Count(d => d == ModifierDescriptor.Focus));
    }

    [Theory]
    [InlineData(ModifierDescriptor.None, true)]
    [InlineData(ModifierDescriptor.Racial, true)]
    [InlineData(ModifierDescriptor.Dodge, true)]
    [InlineData(ModifierDescriptor.UntypedStackable, true)]
    [InlineData(ModifierDescriptor.Feat, true)]
    [InlineData(ModifierDescriptor.Penalty, true)]
    [InlineData(ModifierDescriptor.ArmorFocus, true)]
    [InlineData(ModifierDescriptor.Focus, true)]
    [InlineData(ModifierDescriptor.NegativeEnergyPenalty, true)]
    [InlineData(ModifierDescriptor.NaturalArmor, true)]
    [InlineData(ModifierDescriptor.Morale, false)]
    [InlineData(ModifierDescriptor.Circumstance, false)]
    [InlineData(ModifierDescriptor.Armor, false)]
    public void Stackable_set_matches_game_minus_dlc3(ModifierDescriptor descriptor, bool stackable)
    {
        Assert.Equal(stackable, descriptor.IsStackable());
    }
}
