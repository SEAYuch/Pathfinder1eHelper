using System.Collections.Generic;
using System.Linq;
using Pathfinder1eHelper.Models.Combat;

namespace Pathfinder1eHelper.Services;

/// <summary>
/// 共用加值叠加引擎：同类型取高；闪避/无类型/种族/环境求和（环境同源取高）；
/// 减值总是叠加。战斗数值与技能检定共用同一套规则。
/// </summary>
internal static class BonusEngine
{
    internal static IReadOnlyList<ResolvedBonus> Resolve(IEnumerable<BonusEntry> entries)
    {
        var considered = entries.Where(e => e.IsEnabled).ToList();
        var result = new List<ResolvedBonus>(considered.Count);

        foreach (var penalty in considered.Where(e => e.Type == BonusType.Penalty))
        {
            result.Add(new ResolvedBonus(penalty, true, -System.Math.Abs(penalty.Value), "减值"));
        }

        var bonuses = considered.Where(e => e.Type != BonusType.Penalty);

        foreach (var group in bonuses.GroupBy(StackKey))
        {
            var policy = group.Key.Type == BonusType.Circumstance && group.Key.Variant is not null
                ? StackingPolicy.Highest
                : PolicyFor(group.Key.Type);

            if (policy == StackingPolicy.Sum)
            {
                foreach (var entry in group)
                {
                    result.Add(new ResolvedBonus(entry, true, entry.Value, null));
                }

                continue;
            }

            var winner = group.OrderByDescending(e => e.Value).First();
            foreach (var entry in group)
            {
                var included = ReferenceEquals(entry, winner);
                result.Add(new ResolvedBonus(
                    entry,
                    included,
                    entry.Value,
                    included ? null : $"同类型取高，受「{Describe(winner)}」压制"));
            }
        }

        return result
            .OrderByDescending(r => r.Included)
            .ThenBy(r => r.Entry.Type)
            .ThenByDescending(r => r.Value)
            .ToList();
    }

    internal static IEnumerable<Contribution> ToContributions(IEnumerable<ResolvedBonus> resolved) =>
        resolved.Select(ToContribution);

    internal static Contribution Base(string label, int value) => new(label, value);

    internal static StatResult Stat(IEnumerable<Contribution> contributions)
    {
        var list = contributions.ToList();
        return new StatResult(list.Where(c => c.Included).Sum(c => c.Value), list);
    }

    private static Contribution ToContribution(ResolvedBonus resolved)
    {
        var entry = resolved.Entry;
        var excludedFromTouch = entry.Type is BonusType.Armor or BonusType.Shield or BonusType.NaturalArmor
            || (entry.Type == BonusType.Enhancement
                && entry.Enhancement is EnhancementSubject.Armor or EnhancementSubject.Shield or EnhancementSubject.NaturalArmor);
        var label = string.IsNullOrWhiteSpace(entry.Name) ? CombatText.BonusType(entry.Type) : entry.Name;

        return new Contribution(
            label,
            resolved.Value,
            resolved.Included,
            resolved.Note,
            excludedFromTouch,
            entry.Type == BonusType.Dodge);
    }

    private static (BonusType Type, string? Variant) StackKey(BonusEntry entry)
    {
        if (entry.Type == BonusType.Enhancement && entry.Enhancement != EnhancementSubject.None)
        {
            return (entry.Type, entry.Enhancement.ToString());
        }

        if (entry.Type == BonusType.Circumstance && !string.IsNullOrWhiteSpace(entry.SourceGroup))
        {
            return (entry.Type, entry.SourceGroup.Trim());
        }

        return (entry.Type, null);
    }

    private static StackingPolicy PolicyFor(BonusType type) => type switch
    {
        BonusType.Dodge or BonusType.Untyped or BonusType.Racial or BonusType.Circumstance => StackingPolicy.Sum,
        _ => StackingPolicy.Highest,
    };

    private static string Describe(BonusEntry entry) =>
        string.IsNullOrWhiteSpace(entry.Name) ? CombatText.BonusType(entry.Type) : entry.Name;

    private enum StackingPolicy
    {
        Highest,
        Sum,
    }
}

internal readonly record struct ResolvedBonus(BonusEntry Entry, bool Included, int Value, string? Note);
