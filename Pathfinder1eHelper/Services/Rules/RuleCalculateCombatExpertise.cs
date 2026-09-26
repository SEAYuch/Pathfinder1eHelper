using System;

namespace Pathfinder1eHelper.Services.Rules;

/// <summary>
/// 寓守于攻（CRB）的数值：近战攻击与战技检定每 −1 减值，换 AC +1 闪避加值；
/// 每当 BAB 达到 +4 及之后每 +4，两者各再加一档。
/// CHM 原文：「你可以选择用近战攻击和战技检定的-1减值来换取AC的+1闪避加值。
/// 每当BAB+4，攻击减值会再-1，AC加值也会再+1。」
/// 缩放档位与猛力攻击同源（1 + BAB/4），故沿用同一取整方式。
/// 生效前提：以近战武器攻击或作全力攻击（远程/接触不适用）。
/// </summary>
public static class RuleCalculateCombatExpertise
{
    /// <summary>
    /// 计算寓守于攻的攻击减值与 AC 闪避加值。
    /// 档位 = <c>1 + BAB/4</c>，攻击减值 = −档位，AC 闪避 = +档位。
    /// </summary>
    public static (int AttackPenalty, int ArmorClassDodgeBonus) Compute(int baseAttackBonus) =>
        Compute(baseAttackBonus, isMeleeWeaponAttack: true);

    /// <summary>
    /// 计算寓守于攻的攻击减值与 AC 闪避加值。
    /// 非近战武器攻击（远程或接触）时减值不适用，但只要专长已启用，AC 闪避加值仍生效
    /// （AC 加值不区分攻击方式，战斗中选定姿态后对一整轮有效）。
    /// </summary>
    public static (int AttackPenalty, int ArmorClassDodgeBonus) Compute(
        int baseAttackBonus,
        bool isMeleeWeaponAttack)
    {
        var step = 1 + Math.Max(0, baseAttackBonus) / 4;
        return isMeleeWeaponAttack ? (-step, step) : (0, step);
    }
}
