using System.Collections.Generic;

namespace Pathfinder1eHelper.Models.Combat;

/// <summary>
/// 描述符的可叠加性，对应 WotR <c>ModifierDescriptorHelper.DefaultStackingDescriptors</c>。
/// 已按既定决策移除 <c>DLC3_Stackable</c>（游戏专有可叠加项）。
/// </summary>
public static class ModifierDescriptorHelper
{
    /// <summary>可叠加描述符集合（组内求和）；其余描述符仅保留最佳正值与最差负值。</summary>
    public static IReadOnlySet<ModifierDescriptor> StackableDescriptors { get; } =
        new HashSet<ModifierDescriptor>
        {
            ModifierDescriptor.None,
            ModifierDescriptor.Racial,
            ModifierDescriptor.Dodge,
            ModifierDescriptor.UntypedStackable,
            ModifierDescriptor.Feat,
            ModifierDescriptor.Penalty,
            ModifierDescriptor.ArmorFocus,
            ModifierDescriptor.Focus,
            ModifierDescriptor.NegativeEnergyPenalty,
            ModifierDescriptor.NaturalArmor,
        };

    /// <summary>该描述符是否默认可叠加。</summary>
    public static bool IsStackable(this ModifierDescriptor descriptor) =>
        StackableDescriptors.Contains(descriptor);
}
