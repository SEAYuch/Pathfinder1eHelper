using Pathfinder1eHelper.Services.Rules;
using Pathfinder1eHelper.ViewModels.Combat;

namespace Pathfinder1eHelper.Services;

/// <summary>专注检定 DC 的纯计算，委托给 DLL 规则 <see cref="RuleCheckConcentration"/>。</summary>
public static class ConcentrationCalculator
{
    /// <summary>
    /// 按情境返回专注 DC：
    /// 防御式施法 15 + 2×环位；施法受伤 10 + 环位 + 伤害/2；
    /// 被擒抱（施法困难）15 + 环位；被压制（施法极难）15 + 2×环位；其余使用自定义 DC。
    /// </summary>
    public static int Dc(
        ConcentrationSituation situation,
        int spellLevel,
        int? damageTaken,
        int? customDc) => situation switch
    {
        ConcentrationSituation.DefensiveCasting => RuleCheckConcentration.DefensiveCastingDc(spellLevel),
        ConcentrationSituation.DamageWhileCasting => RuleCheckConcentration.DamageWhileCastingDc(spellLevel, damageTaken ?? 0),
        ConcentrationSituation.Grappled => RuleCheckConcentration.DifficultCastingDc(spellLevel),
        ConcentrationSituation.Pinned => RuleCheckConcentration.VeryDifficultCastingDc(spellLevel),
        _ => customDc ?? 0,
    };
}
