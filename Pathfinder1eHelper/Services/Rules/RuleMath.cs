using System.Collections.Generic;
using System.Linq;
using Pathfinder1eHelper.Models.Combat;

namespace Pathfinder1eHelper.Services.Rules;

/// <summary>规则层共用的拼装/过滤工具。</summary>
internal static class RuleMath
{
    public static StatResult Stat(IEnumerable<Contribution> contributions)
    {
        var list = contributions.ToList();
        return new StatResult(list.Where(c => c.Included).Sum(c => c.Value), list);
    }

    /// <summary>把叠加结果明细转成展示用 <see cref="Contribution"/>（并标注接触/措手不及排除）。</summary>
    public static Contribution FromModifier(ModifierContribution contribution)
    {
        var modifier = contribution.Modifier;
        var label = string.IsNullOrWhiteSpace(modifier.Source)
            ? CombatText.Descriptor(modifier.Descriptor)
            : modifier.Source!;

        return new Contribution(
            label,
            modifier.Value,
            contribution.Included,
            contribution.Note,
            ExcludedFromTouch: ExcludesFromTouch(modifier),
            ExcludedFromFlatFooted: ExcludesFromFlatFooted(modifier),
            Descriptor: modifier.Descriptor);
    }

    /// <summary>正值时不计入接触 AC（护甲/盾牌/天生护甲家族）；对应 DLL <c>FilterAllowedForTouch</c>。</summary>
    public static bool ExcludesFromTouch(Modifier modifier) =>
        modifier.Value > 0 && modifier.Descriptor is
            ModifierDescriptor.Armor or
            ModifierDescriptor.ArmorEnhancement or
            ModifierDescriptor.ArmorFocus or
            ModifierDescriptor.NaturalArmor or
            ModifierDescriptor.NaturalArmorEnhancement or
            ModifierDescriptor.NaturalArmorForm or
            ModifierDescriptor.Shield or
            ModifierDescriptor.ShieldEnhancement or
            ModifierDescriptor.Focus;

    /// <summary>正值时不计入措手不及 AC（闪避/敏捷加值）；对应 DLL <c>FilterAllowedForFlatFooted</c>。</summary>
    public static bool ExcludesFromFlatFooted(Modifier modifier) =>
        modifier.Value > 0 && modifier.Descriptor is ModifierDescriptor.Dodge or ModifierDescriptor.DexterityBonus;

    /// <summary>按接触/措手不及维度重新合计（负值不会被排除）。</summary>
    public static StatResult Derive(StatResult source, bool touch, bool flatFooted)
    {
        var total = source.Contributions
            .Where(c => c.Included
                && !(touch && c.ExcludedFromTouch)
                && !(flatFooted && c.ExcludedFromFlatFooted))
            .Sum(c => c.Value);
        return source with { Total = total };
    }
}
