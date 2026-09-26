using System;
using System.Collections.Generic;
using System.Linq;
using Pathfinder1eHelper.Models.Combat;

namespace Pathfinder1eHelper.Services.Rules;

/// <summary>
/// 先攻加值，对应 WotR <c>ModifiableValueInitiative</c>：
/// 敏捷调整值作为 <see cref="ModifierDescriptor.DexterityBonus"/> 修饰，与其余修饰一并叠加。
/// </summary>
public static class RuleCalculateInitiative
{
    public static StatResult Compute(int dexterityBonus, IReadOnlyList<Modifier> modifiers)
    {
        ArgumentNullException.ThrowIfNull(modifiers);

        var all = new List<Modifier>(modifiers)
        {
            new(ModifierDescriptor.DexterityBonus, dexterityBonus, Source: "敏捷"),
        };

        var evaluation = ModifierEngine.Evaluate(0, all);
        return RuleMath.Stat(evaluation.Contributions.Select(RuleMath.FromModifier));
    }
}
