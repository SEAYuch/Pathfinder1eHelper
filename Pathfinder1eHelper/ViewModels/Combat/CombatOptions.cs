using System;
using System.Collections.Generic;
using System.Linq;
using Pathfinder1eHelper.Models.Combat;

namespace Pathfinder1eHelper.ViewModels.Combat;

/// <summary>下拉选项（值 + 中文显示），避免枚举绑定需要转换器。</summary>
public sealed record BonusTypeOption(BonusType Value, string Display);

public sealed record BonusTargetOption(BonusTarget Value, string Display);

public sealed record EnhancementOption(EnhancementSubject Value, string Display);

public sealed record SizeOption(SizeCategory Value, string Display);

public sealed record AbilityOption(Ability Value, string Display);

public sealed record StrengthMultiplierOption(double Value, string Display);

public sealed record WeaponAbilityOption(WeaponAbility Value, string Display);

/// <summary>专注检定的常见情境。</summary>
public enum ConcentrationSituation
{
    Custom,
    DefensiveCasting,
    DamageWhileCasting,
    Grappled,
}

public sealed record ConcentrationOption(ConcentrationSituation Value, string Display);

/// <summary>战斗页使用的全部枚举选项列表。</summary>
public static class CombatOptions
{
    public static IReadOnlyList<BonusTypeOption> BonusTypes { get; } =
        Enum.GetValues<BonusType>().Select(v => new BonusTypeOption(v, CombatText.BonusType(v))).ToList();

    // 接触专用目标已废弃（不再提供“接触专用加值”），仅保留枚举成员以兼容旧存档，故从可选列表中排除。
    public static IReadOnlyList<BonusTargetOption> BonusTargets { get; } =
        Enum.GetValues<BonusTarget>()
            .Where(v => v is not (BonusTarget.MeleeTouchAttack or BonusTarget.RangedTouchAttack))
            .Select(v => new BonusTargetOption(v, CombatText.BonusTarget(v)))
            .ToList();

    public static IReadOnlyList<EnhancementOption> Enhancements { get; } =
        Enum.GetValues<EnhancementSubject>().Select(v => new EnhancementOption(v, CombatText.EnhancementSubject(v))).ToList();

    public static IReadOnlyList<SizeOption> Sizes { get; } =
        Enum.GetValues<SizeCategory>().Select(v => new SizeOption(v, CombatText.Size(v))).ToList();

    public static IReadOnlyList<AbilityOption> Abilities { get; } =
        Enum.GetValues<Ability>().Select(v => new AbilityOption(v, CombatText.Ability(v))).ToList();

    public static IReadOnlyList<StrengthMultiplierOption> StrengthMultipliers { get; } =
    [
        new(0, "无"),
        new(0.5, "×0.5 副手"),
        new(1, "×1 主手"),
        new(1.5, "×1.5 双手"),
    ];

    /// <summary>武器命中/伤害可选的能力属性：力量 / 敏捷。</summary>
    public static IReadOnlyList<WeaponAbilityOption> WeaponAbilities { get; } =
    [
        new(WeaponAbility.Strength, "力量"),
        new(WeaponAbility.Dexterity, "敏捷"),
    ];

    public static IReadOnlyList<ConcentrationOption> ConcentrationSituations { get; } =
    [
        new(ConcentrationSituation.Custom, "自定义 DC"),
        new(ConcentrationSituation.DefensiveCasting, "防御式施法（15 + 2×环位）"),
        new(ConcentrationSituation.DamageWhileCasting, "施法受伤（10 + 伤害 + 环位）"),
        new(ConcentrationSituation.Grappled, "被擒抱（10 + 擒抱者CMB + 环位）"),
    ];

    public static BonusTypeOption Type(BonusType value) => BonusTypes.First(o => o.Value == value);

    public static BonusTargetOption Target(BonusTarget value) =>
        BonusTargets.FirstOrDefault(o => o.Value == value)
        ?? new BonusTargetOption(value, CombatText.BonusTarget(value));

    public static EnhancementOption Enhancement(EnhancementSubject value) => Enhancements.First(o => o.Value == value);

    public static SizeOption Size(SizeCategory value) => Sizes.First(o => o.Value == value);

    public static AbilityOption Ability(Ability value) => Abilities.First(o => o.Value == value);

    public static StrengthMultiplierOption Multiplier(double value) =>
        StrengthMultipliers.FirstOrDefault(o => Math.Abs(o.Value - value) < 0.001) ?? StrengthMultipliers[2];

    public static WeaponAbilityOption WeaponChoice(WeaponAbility value) =>
        WeaponAbilities.First(o => o.Value == value);

    public static ConcentrationOption Concentration(ConcentrationSituation value) =>
        ConcentrationSituations.First(o => o.Value == value);
}
