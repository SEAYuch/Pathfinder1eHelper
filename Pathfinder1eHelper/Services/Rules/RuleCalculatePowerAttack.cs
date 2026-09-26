using System;

namespace Pathfinder1eHelper.Services.Rules;

/// <summary>
/// 猛力攻击（CRB）的数值，复刻 WotR <c>WeaponParametersAttackBonus</c> /
/// <c>WeaponParametersDamageBonus</c>（`PowerAttackBuffEffect` 蓝图：AttackBonus=-1、DamageBonus=2、
/// ScaleByBasicAttackBonus=true、PowerAttackScaling=true、仅近战、接触与远程无效）。
/// 神话/高等猛力攻击（基准 3、双手 ×5/3 或 ×2）暂未实现。
/// </summary>
public static class RuleCalculatePowerAttack
{
    public const int BaseDamageBonus = 2;

    /// <summary>
    /// 计算猛力攻击的攻击减值与伤害加值。
    /// 攻击减值 = <c>-(1 + BAB/4)</c>；伤害 = <c>2 × (1 + BAB/4)</c>，
    /// 副手减半，双手 ×1.5。远程或接触攻击返回 (0, 0)。
    /// </summary>
    public static (int AttackPenalty, int DamageBonus) Compute(
        int baseAttackBonus,
        bool isMelee,
        bool isTouch,
        bool isSecondary,
        bool holdInTwoHands)
    {
        if (!isMelee || isTouch)
        {
            return (0, 0);
        }

        var step = 1 + Math.Max(0, baseAttackBonus) / 4;
        var penalty = -step;
        var damage = BaseDamageBonus * step;

        if (isSecondary)
        {
            damage /= 2;
        }
        else if (holdInTwoHands)
        {
            damage = damage * 3 / 2;
        }

        return (penalty, damage);
    }
}
