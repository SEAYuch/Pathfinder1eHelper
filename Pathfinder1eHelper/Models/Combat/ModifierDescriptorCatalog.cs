using System.Collections.Generic;

namespace Pathfinder1eHelper.Models.Combat;

/// <summary>
/// 加值类型下拉的展示顺序：常用（Tier 1）→ 情境（Tier 2）→ 其余（Tier 3，按 DLL 顺序）。
/// 枚举声明序与 DLL 一致，仅用于追溯，不能直接拿来生成下拉，故在此显式给出排序。
/// </summary>
public static class ModifierDescriptorCatalog
{
    /// <summary>表团高频。</summary>
    public static IReadOnlyList<ModifierDescriptor> Tier1 { get; } =
    [
        ModifierDescriptor.None,
        ModifierDescriptor.Dodge,
        ModifierDescriptor.Morale,
        ModifierDescriptor.Luck,
        ModifierDescriptor.Sacred,
        ModifierDescriptor.Profane,
        ModifierDescriptor.Deflection,
        ModifierDescriptor.Competence,
        ModifierDescriptor.Insight,
        ModifierDescriptor.Resistance,
        ModifierDescriptor.Armor,
        ModifierDescriptor.Shield,
        ModifierDescriptor.NaturalArmor,
        ModifierDescriptor.Enhancement,
        ModifierDescriptor.Circumstance,
        ModifierDescriptor.Penalty,
    ];

    /// <summary>常用但偏情境。</summary>
    public static IReadOnlyList<ModifierDescriptor> Tier2 { get; } =
    [
        ModifierDescriptor.Racial,
        ModifierDescriptor.Trait,
        ModifierDescriptor.Alchemical,
        ModifierDescriptor.Inherent,
        ModifierDescriptor.UntypedStackable,
        ModifierDescriptor.Size,
        ModifierDescriptor.ArmorEnhancement,
        ModifierDescriptor.ShieldEnhancement,
        ModifierDescriptor.NaturalArmorEnhancement,
        ModifierDescriptor.Focus,
        ModifierDescriptor.ArmorFocus,
        ModifierDescriptor.Feat,
        ModifierDescriptor.NegativeEnergyPenalty,
        ModifierDescriptor.DexterityBonus,
    ];

    /// <summary>其余（按 DLL 顺序）；不含 <see cref="ModifierDescriptor.ShieldFocus"/> 别名。</summary>
    public static IReadOnlyList<ModifierDescriptor> Tier3 { get; } =
    [
        ModifierDescriptor.FearPenalty,
        ModifierDescriptor.ConstitutionBonus,
        ModifierDescriptor.Fatigued,
        ModifierDescriptor.Crippled,
        ModifierDescriptor.StatDamage,
        ModifierDescriptor.StatDrain,
        ModifierDescriptor.BaseStatBonus,
        ModifierDescriptor.Cooking,
        ModifierDescriptor.Polymorph,
        ModifierDescriptor.Helpless,
        ModifierDescriptor.Encumbrance,
        ModifierDescriptor.FavoredEnemy,
        ModifierDescriptor.Other,
        ModifierDescriptor.Prone,
        ModifierDescriptor.Mythic,
        ModifierDescriptor.DemonBonus,
        ModifierDescriptor.Rage,
        ModifierDescriptor.UniqueItem,
        ModifierDescriptor.LockpickersKit,
        ModifierDescriptor.FavouredClassBonus,
        ModifierDescriptor.NaturalArmorForm,
        ModifierDescriptor.Anomaly,
        ModifierDescriptor.WeaponTraining,
        ModifierDescriptor.MasterShapeshifter,
        ModifierDescriptor.SelfBonus,
        ModifierDescriptor.Haste,
    ];

    /// <summary>下拉展示顺序 = Tier1 + Tier2 + Tier3。</summary>
    public static IReadOnlyList<ModifierDescriptor> DisplayOrder { get; } =
        [.. Tier1, .. Tier2, .. Tier3];
}
