using System;
using System.Collections.Generic;
using System.Linq;
using Pathfinder1eHelper.Models.Combat;

namespace Pathfinder1eHelper.Services.Rules;

/// <summary>AC 计算入参。</summary>
public sealed record ArmorClassRequest
{
    /// <summary>基础属性调整值（通常为敏捷；可被替换）。</summary>
    public int BaseAttributeBonus { get; init; }

    /// <summary>护甲允许的最大敏捷加值（null 表示不限制）。</summary>
    public int? MaxDexBonusFromArmor { get; init; }

    /// <summary>AC 修饰（护甲/盾牌/偏斜/闪避/天生护甲…）。</summary>
    public IReadOnlyList<Modifier> Modifiers { get; init; } = [];

    public bool TargetIsProne { get; init; }

    public bool TargetIsHelpless { get; init; }

    public bool TargetIsBlind { get; init; }

    public bool AttackIsMelee { get; init; }
}

/// <summary>AC 的四个变体。</summary>
public sealed record ArmorClassResult(
    StatResult ArmorClass,
    StatResult Touch,
    StatResult FlatFooted,
    StatResult FlatFootedTouch);

/// <summary>
/// 防御等级，对应 WotR <c>ModifiableValueArmorClass</c> + <c>RuleCalculateAC</c>：
/// 基础 10 + 基础属性 + 修饰；接触过滤护甲/盾牌/天生护甲家族正值，措手不及过滤闪避/敏捷加值正值。
/// </summary>
public static class RuleCalculateArmorClass
{
    public const int BaseArmorClass = 10;

    public static ArmorClassResult Compute(ArmorClassRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contributions = new List<Contribution> { new("基础", BaseArmorClass) };

        var attributeBonus = request.BaseAttributeBonus;
        var attributeLabel = "基础属性";
        if (request.MaxDexBonusFromArmor is { } maxDex && attributeBonus > maxDex)
        {
            attributeBonus = maxDex;
            attributeLabel = "敏捷（护甲上限）";
        }

        contributions.Add(new Contribution(attributeLabel, attributeBonus, ExcludedFromFlatFooted: true));

        if (request.TargetIsProne)
        {
            contributions.Add(new Contribution(
                "俯卧",
                request.AttackIsMelee ? -4 : 4,
                Note: request.AttackIsMelee ? "对近战" : "对远程"));
        }

        if (request.TargetIsHelpless && request.AttackIsMelee)
        {
            contributions.Add(new Contribution("无助", -4));
        }

        if (request.TargetIsBlind)
        {
            contributions.Add(new Contribution("目盲", -2));
        }

        var evaluation = ModifierEngine.Evaluate(0, request.Modifiers);
        contributions.AddRange(evaluation.Contributions.Select(RuleMath.FromModifier));

        var armorClass = RuleMath.Stat(contributions);
        return new ArmorClassResult(
            armorClass,
            RuleMath.Derive(armorClass, touch: true, flatFooted: false),
            RuleMath.Derive(armorClass, touch: false, flatFooted: true),
            RuleMath.Derive(armorClass, touch: true, flatFooted: true));
    }
}
