using System;
using System.Collections.Generic;
using System.Linq;
using Pathfinder1eHelper.Models.Combat;

namespace Pathfinder1eHelper.Services.Rules;

/// <summary>
/// 豁免值，对应 WotR <c>ModifiableValueSavingThrow</c> + <c>RuleSavingThrow</c>：
/// 基础豁免 + 属性调整值 + 修饰。
/// </summary>
public static class RuleCalculateSavingThrow
{
    public static StatResult Compute(
        int baseSave,
        int abilityBonus,
        IReadOnlyList<Modifier> modifiers,
        string abilityLabel = "属性")
    {
        ArgumentNullException.ThrowIfNull(modifiers);

        var contributions = new List<Contribution>
        {
            new("基础豁免", baseSave, Included: baseSave != 0),
            new(abilityLabel, abilityBonus, Included: abilityBonus != 0),
        };

        var evaluation = ModifierEngine.Evaluate(0, modifiers);
        contributions.AddRange(evaluation.Contributions.Select(RuleMath.FromModifier));

        return RuleMath.Stat(contributions);
    }
}
