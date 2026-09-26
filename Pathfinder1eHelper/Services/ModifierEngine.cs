using System;
using System.Collections.Generic;
using System.Linq;
using Pathfinder1eHelper.Models.Combat;

namespace Pathfinder1eHelper.Services;

/// <summary>
/// 加值叠加引擎，忠实复刻 WotR <c>ModifiableValue.ApplyModifiersFiltered</c>：
/// 按 <see cref="ModifierDescriptor"/> 分组；
/// 组内可叠加项求和，非叠加项分别取「最大正值」与「最小负值」且**两者同时生效**；
/// <see cref="ModifierDescriptor.Armor"/> / <see cref="ModifierDescriptor.Encumbrance"/> 的负值
/// 全局只取一次 <c>min</c>；最后夹到下限。
/// </summary>
public static class ModifierEngine
{
    /// <summary>按叠加规则计算最终值。</summary>
    public static int Apply(
        int baseValue,
        IReadOnlyList<Modifier> modifiers,
        int minValueRaw = int.MinValue,
        bool zeroOnHelpless = false,
        Func<Modifier, bool>? filter = null) =>
        Evaluate(baseValue, modifiers, minValueRaw, zeroOnHelpless, filter).Total;

    /// <summary>按叠加规则求值，并返回全部修饰的明细。</summary>
    public static ModifierEvaluation Evaluate(
        int baseValue,
        IReadOnlyList<Modifier> modifiers,
        int minValueRaw = int.MinValue,
        bool zeroOnHelpless = false,
        Func<Modifier, bool>? filter = null)
    {
        ArgumentNullException.ThrowIfNull(modifiers);

        var contributions = new List<ModifierContribution>(modifiers.Count);

        if (zeroOnHelpless && modifiers.Any(m => Passes(m, filter) && m.Descriptor == ModifierDescriptor.Helpless))
        {
            foreach (var modifier in modifiers)
            {
                var helpless = modifier.Descriptor == ModifierDescriptor.Helpless;
                contributions.Add(new ModifierContribution(modifier, helpless, helpless ? "无助：属性归零" : null));
            }

            return new ModifierEvaluation(0, contributions);
        }

        // 护甲 / 负重的负值：全局只取更差的一个。
        var armorPenalty = LowestNegative(modifiers, ModifierDescriptor.Armor, filter);
        var encumbrancePenalty = LowestNegative(modifiers, ModifierDescriptor.Encumbrance, filter);
        var globalPenalty = Math.Min(armorPenalty?.Value ?? 0, encumbrancePenalty?.Value ?? 0);
        Modifier? globalPenaltyWinner = null;
        if (globalPenalty < 0)
        {
            globalPenaltyWinner = armorPenalty?.Value == globalPenalty ? armorPenalty : encumbrancePenalty;
        }

        foreach (var group in modifiers.GroupBy(static m => m.Descriptor))
        {
            Modifier? highestPositive = null;
            Modifier? lowestNegative = null;
            var stackables = new List<Modifier>();
            var positives = new List<Modifier>();
            var negatives = new List<Modifier>();

            foreach (var modifier in group)
            {
                if (!Passes(modifier, filter))
                {
                    continue;
                }

                if (modifier.Value < 0
                    && modifier.Descriptor is ModifierDescriptor.Armor or ModifierDescriptor.Encumbrance)
                {
                    var isWinner = ReferenceEquals(modifier, globalPenaltyWinner);
                    contributions.Add(new ModifierContribution(
                        modifier,
                        isWinner,
                        isWinner ? "护甲/负重取更差值" : "护甲/负重取更差值，被压制"));
                    continue;
                }

                if (modifier.Stacks)
                {
                    stackables.Add(modifier);
                    continue;
                }

                if (modifier.Value > 0 && (highestPositive is null || modifier.Value > highestPositive.Value))
                {
                    highestPositive = modifier;
                }

                if (modifier.Value < 0 && (lowestNegative is null || modifier.Value < lowestNegative.Value))
                {
                    lowestNegative = modifier;
                }

                (modifier.Value > 0 ? positives : negatives).Add(modifier);
            }

            foreach (var modifier in stackables)
            {
                contributions.Add(new ModifierContribution(modifier, true, null));
            }

            foreach (var modifier in positives)
            {
                var included = ReferenceEquals(modifier, highestPositive);
                contributions.Add(new ModifierContribution(modifier, included, included ? null : "同类型取高，被更高者压制"));
            }

            foreach (var modifier in negatives)
            {
                var included = ReferenceEquals(modifier, lowestNegative);
                contributions.Add(new ModifierContribution(modifier, included, included ? null : "同类型取低，被更低者压制"));
            }
        }

        var total = baseValue + contributions.Where(c => c.Included).Sum(c => c.Modifier.Value);
        total = Math.Max(minValueRaw, total);
        return new ModifierEvaluation(total, contributions);
    }

    private static bool Passes(Modifier modifier, Func<Modifier, bool>? filter) => filter is null || filter(modifier);

    private static Modifier? LowestNegative(
        IReadOnlyList<Modifier> modifiers,
        ModifierDescriptor descriptor,
        Func<Modifier, bool>? filter)
    {
        Modifier? lowest = null;
        foreach (var modifier in modifiers)
        {
            if (modifier.Descriptor != descriptor || modifier.Value >= 0 || !Passes(modifier, filter))
            {
                continue;
            }

            if (lowest is null || modifier.Value < lowest.Value)
            {
                lowest = modifier;
            }
        }

        return lowest;
    }
}
