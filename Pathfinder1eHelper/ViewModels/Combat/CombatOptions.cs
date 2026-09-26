using System;
using System.Collections.Generic;
using System.Linq;
using Pathfinder1eHelper.Models.Combat;

namespace Pathfinder1eHelper.ViewModels.Combat;

/// <summary>下拉选项（值 + 中文显示），避免枚举绑定需要转换器。</summary>
public sealed record DescriptorOption(ModifierDescriptor Value, string Display);

public sealed record CombatStatOption(CombatStat Value, string Display);

public sealed record StackModeOption(StackMode Value, string Display);

public sealed record SizeOption(SizeCategory Value, string Display);

public sealed record AbilityOption(Ability Value, string Display);

public sealed record WeaponHandOption(WeaponHand Value, string Display);

public sealed record WeaponAttackTypeOption(WeaponAttackType Value, string Display);

/// <summary>专注检定的常见情境（对应 WotR 的施法困难条件）。</summary>
public enum ConcentrationSituation
{
    Custom,
    DefensiveCasting,
    DamageWhileCasting,

    /// <summary>施法困难（含被擒抱）：15 + 环位。</summary>
    Grappled,

    /// <summary>施法极难（含被压制）：15 + 2×环位。</summary>
    Pinned,
}

public sealed record ConcentrationOption(ConcentrationSituation Value, string Display);

/// <summary>战斗页使用的全部枚举选项列表。</summary>
public static class CombatOptions
{
    /// <summary>描述符下拉：按 <see cref="ModifierDescriptorCatalog"/> 的 Tier1→Tier2→Tier3 顺序（常用在前）。</summary>
    public static IReadOnlyList<DescriptorOption> Descriptors { get; } =
        ModifierDescriptorCatalog.DisplayOrder
            .Select(v => new DescriptorOption(v, CombatText.Descriptor(v)))
            .ToList();

    public static IReadOnlyList<CombatStatOption> CombatStats { get; } =
        Enum.GetValues<CombatStat>().Select(v => new CombatStatOption(v, CombatText.CombatStat(v))).ToList();

    public static IReadOnlyList<StackModeOption> StackModes { get; } =
        Enum.GetValues<StackMode>().Select(v => new StackModeOption(v, CombatText.StackMode(v))).ToList();

    public static IReadOnlyList<SizeOption> Sizes { get; } =
        Enum.GetValues<SizeCategory>().Select(v => new SizeOption(v, CombatText.Size(v))).ToList();

    public static IReadOnlyList<AbilityOption> Abilities { get; } =
        Enum.GetValues<Ability>().Select(v => new AbilityOption(v, CombatText.Ability(v))).ToList();

    public static IReadOnlyList<WeaponHandOption> WeaponHands { get; } =
        Enum.GetValues<WeaponHand>().Select(v => new WeaponHandOption(v, CombatText.WeaponHand(v))).ToList();

    public static IReadOnlyList<WeaponAttackTypeOption> WeaponAttackTypes { get; } =
        Enum.GetValues<WeaponAttackType>().Select(v => new WeaponAttackTypeOption(v, CombatText.WeaponAttackType(v))).ToList();

    public static IReadOnlyList<ConcentrationOption> ConcentrationSituations { get; } =
    [
        new(ConcentrationSituation.Custom, "自定义 DC"),
        new(ConcentrationSituation.DefensiveCasting, "防御式施法（15 + 2×环位）"),
        new(ConcentrationSituation.DamageWhileCasting, "施法受伤（10 + 环位 + 伤害/2）"),
        new(ConcentrationSituation.Grappled, "被擒抱（施法困难：15 + 环位）"),
        new(ConcentrationSituation.Pinned, "被压制（施法极难：15 + 2×环位）"),
    ];

    public static DescriptorOption Descriptor(ModifierDescriptor value) =>
        Descriptors.FirstOrDefault(o => o.Value == value) ?? new DescriptorOption(value, CombatText.Descriptor(value));

    public static CombatStatOption Stat(CombatStat value) =>
        CombatStats.First(o => o.Value == value);

    public static StackModeOption StackMode(StackMode value) => StackModes.First(o => o.Value == value);

    public static SizeOption Size(SizeCategory value) => Sizes.First(o => o.Value == value);

    public static AbilityOption Ability(Ability value) => Abilities.First(o => o.Value == value);

    public static AbilityOption? AbilityOrNull(Ability? value) =>
        value is { } ability ? Abilities.First(o => o.Value == ability) : null;

    public static WeaponHandOption Hand(WeaponHand value) => WeaponHands.First(o => o.Value == value);

    public static WeaponAttackTypeOption AttackType(WeaponAttackType value) =>
        WeaponAttackTypes.First(o => o.Value == value);

    public static ConcentrationOption Concentration(ConcentrationSituation value) =>
        ConcentrationSituations.First(o => o.Value == value);
}
