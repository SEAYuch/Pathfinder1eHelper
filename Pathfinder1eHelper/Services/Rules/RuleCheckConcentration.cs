using System;
using System.Collections.Generic;
using System.Linq;
using Pathfinder1eHelper.Models.Combat;

namespace Pathfinder1eHelper.Services.Rules;

/// <summary>
/// 专注与施法 DC，对应 WotR <c>RuleCalculateAbilityParams</c> / <c>RuleCheckConcentration</c> /
/// <c>RuleCheckCastingDefensively</c>。
/// </summary>
public static class RuleCheckConcentration
{
    /// <summary>专注值 = max(1, 施法者等级) + 施法属性调整值 + 专注修饰。</summary>
    public static StatResult Value(int casterLevel, int castingAbilityBonus, IReadOnlyList<Modifier> modifiers)
    {
        ArgumentNullException.ThrowIfNull(modifiers);

        var level = Math.Max(1, casterLevel);
        var contributions = new List<Contribution>
        {
            new("施法者等级", level),
            new("施法属性", castingAbilityBonus, Included: castingAbilityBonus != 0),
        };

        contributions.AddRange(ModifierEngine.Evaluate(0, modifiers)
            .Contributions.Select(RuleMath.FromModifier));

        return RuleMath.Stat(contributions);
    }

    /// <summary>法术豁免 DC = 10 + 环位 + 施法属性调整值 + 加值。</summary>
    public static int SpellDc(int spellLevel, int castingAbilityBonus, int bonus = 0) =>
        10 + spellLevel + castingAbilityBonus + bonus;

    /// <summary>防御式施法 DC = 15 + 2×环位。</summary>
    public static int DefensiveCastingDc(int spellLevel) => 15 + 2 * spellLevel;

    /// <summary>施法受伤 DC = 10 + 环位 + 伤害/2（整数除法）。</summary>
    public static int DamageWhileCastingDc(int spellLevel, int damageTaken) =>
        10 + spellLevel + Math.Max(0, damageTaken) / 2;

    /// <summary>无伤害的通用专注 DC = 15 + 环位（DLL <c>RuleCheckConcentration</c>）。</summary>
    public static int GeneralDc(int spellLevel) => 15 + spellLevel;

    /// <summary>
    /// 「施法困难」专注 DC = 15 + 环位。
    /// 对应 DLL <c>UnitUseAbility.MakeConcentrationCheckIfCastingIsDifficult</c> 中
    /// <c>UnitCondition.Entangled/SpellCastingIsDifficult</c>（擒抱即应用此条件）。
    /// </summary>
    public static int DifficultCastingDc(int spellLevel) => GeneralDc(spellLevel);

    /// <summary>
    /// 「施法极难」专注 DC = 15 + 2×环位（DLL 置 <c>AddTwiceSpellLevel</c>），
    /// 对应 <c>UnitCondition.SpellCastingIsVeryDifficult</c>（如被压制）。
    /// </summary>
    public static int VeryDifficultCastingDc(int spellLevel) => 15 + 2 * spellLevel;
}
