using System;
using System.Collections.Generic;
using System.Linq;
using Pathfinder1eHelper.Models.Combat;

namespace Pathfinder1eHelper.Services.Rules;

/// <summary>攻击加值计算入参。基础值（BAB/属性/体型）由调用方预计算，状态加值走修饰列表。</summary>
public sealed record AttackBonusRequest
{
    public int BaseAttackBonus { get; init; }

    /// <summary>命中属性调整值（近战力量 / 远程敏捷，或武器指定的属性）。</summary>
    public int AbilityBonus { get; init; }

    /// <summary>额外攻击减值（正数，如多重攻击、副手）。</summary>
    public int AttackPenalty { get; init; }

    /// <summary>武器增强加值。</summary>
    public int WeaponEnhancement { get; init; }

    /// <summary>对应 WotR <c>Stats.AdditionalAttackBonus</c>（含体型、武器专攻、法术加值等）。</summary>
    public IReadOnlyList<Modifier> AdditionalAttackBonus { get; init; } = [];

    public bool IsMelee { get; init; }

    public bool TargetIsFlanked { get; init; }

    /// <summary>攻击者获得全遮蔽/目盲优势（DLL 中 <c>RuleCalculateAttackBonus</c> 的 +2）。</summary>
    public bool AttackerHasConcealmentAdvantage { get; init; }

    /// <summary>远程射击陷入近战的敌人（−4）。</summary>
    public bool ShootIntoCombatPenalty { get; init; }
}

/// <summary>
/// 攻击加值，对应 WotR <c>RuleCalculateAttackBonus[WithoutTarget]</c>：
/// BAB + 命中属性 + 武器增强 + AdditionalAttackBonus + 夹击/射入近战等情景。
/// </summary>
public static class RuleCalculateAttackBonus
{
    public const int Flanking = 2;
    public const int ConcealmentAdvantage = 2;
    public const int ShootIntoCombat = -4;

    public static StatResult Compute(AttackBonusRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contributions = new List<Contribution>
        {
            new("BAB", request.BaseAttackBonus, Included: request.BaseAttackBonus != 0),
            new("命中属性", request.AbilityBonus, Included: request.AbilityBonus != 0),
        };

        if (request.AttackPenalty != 0)
        {
            contributions.Add(new Contribution("攻击减值", -request.AttackPenalty));
        }

        if (request.IsMelee && request.TargetIsFlanked)
        {
            contributions.Add(new Contribution("夹击", Flanking));
        }

        if (request.AttackerHasConcealmentAdvantage)
        {
            contributions.Add(new Contribution("遮蔽/目盲优势", ConcealmentAdvantage));
        }

        if (request.ShootIntoCombatPenalty)
        {
            contributions.Add(new Contribution("射入近战", ShootIntoCombat));
        }

        var additional = request.AdditionalAttackBonus.ToList();
        var addWeaponEnhancement = true;

        // 对应 DLL：AdditionalAttackBonus 已有更强的非叠加增强加值时，不再叠加武器增强。
        var existing = additional.FindIndex(m => m.Descriptor == ModifierDescriptor.Enhancement && !m.Stacks);
        if (existing >= 0)
        {
            if (additional[existing].Value <= request.WeaponEnhancement)
            {
                additional.RemoveAt(existing);
            }
            else
            {
                addWeaponEnhancement = false;
            }
        }

        if (addWeaponEnhancement && request.WeaponEnhancement > 0)
        {
            contributions.Add(new Contribution("武器增强", request.WeaponEnhancement, Descriptor: ModifierDescriptor.Enhancement));
        }

        var evaluation = ModifierEngine.Evaluate(0, additional);
        contributions.AddRange(evaluation.Contributions.Select(RuleMath.FromModifier));

        return RuleMath.Stat(contributions);
    }
}
