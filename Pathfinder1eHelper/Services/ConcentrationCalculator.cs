using System;
using Pathfinder1eHelper.ViewModels.Combat;

namespace Pathfinder1eHelper.Services;

/// <summary>PF1 专注检定 DC 的纯计算，供战斗页使用（便于单测）。</summary>
public static class ConcentrationCalculator
{
    /// <summary>
    /// 按情境返回专注 DC：
    /// 防御式施法 15 + 2×环位；施法受伤 10 + 伤害 + 环位；
    /// 被擒抱 10 + 擒抱者 CMB + 环位；其余使用自定义 DC。
    /// </summary>
    public static int Dc(
        ConcentrationSituation situation,
        int spellLevel,
        int? damageTaken,
        int? grapplerCmb,
        int? customDc) => situation switch
    {
        ConcentrationSituation.DefensiveCasting => 15 + 2 * spellLevel,
        ConcentrationSituation.DamageWhileCasting => 10 + Math.Max(0, damageTaken ?? 0) + spellLevel,
        ConcentrationSituation.Grappled => 10 + Math.Max(0, grapplerCmb ?? 0) + spellLevel,
        _ => customDc ?? 0,
    };
}
