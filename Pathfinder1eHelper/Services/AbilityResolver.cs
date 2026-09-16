using System;
using System.Linq;
using Pathfinder1eHelper.Models.Combat;

namespace Pathfinder1eHelper.Services;

/// <summary>
/// 把作用目标为 <see cref="BonusTarget.AbilityScore"/> 的加值（如牛之力量、猫之优雅、
/// 属性增强物品）按叠加规则作用到属性值上，供战斗/技能计算使用。
/// </summary>
internal static class AbilityResolver
{
    public static AbilityScores Effective(CharacterProfile profile)
    {
        var baseScores = profile.Abilities ?? new AbilityScores();
        var bonuses = profile.Bonuses ?? [];
        var result = new AbilityScores();

        foreach (var ability in Enum.GetValues<Ability>())
        {
            var entries = bonuses.Where(e => e.Target == BonusTarget.AbilityScore && e.Ability == ability);
            var delta = BonusEngine.Stat(BonusEngine.ToContributions(BonusEngine.Resolve(entries))).Total;
            result.SetScore(ability, baseScores.GetScore(ability) + delta);
        }

        return result;
    }
}
