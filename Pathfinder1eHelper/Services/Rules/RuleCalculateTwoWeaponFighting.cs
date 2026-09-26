using System.Linq;
using Pathfinder1eHelper.Models.Combat;

namespace Pathfinder1eHelper.Services.Rules;

/// <summary>
/// 双武器战斗的攻击减值，复刻 WotR
/// <c>TwoWeaponFightingAttackPenalty</c>（<c>TwoWeaponFightingBasicMechanics</c> 蓝图组件）。
/// <para>
/// DLL 逻辑：按 fact 的 rank 取基准减值（rank ≤ 1 → 主手 −4 / 副手 −8；rank &gt; 1 且非神话 → −2/−2；
/// rank &gt; 1 且神话 → 0/0），随后若「副手非轻型 且 主手非双头 且 非 EffortlessDualWielding」
/// 则当前这只手再 −2。描述符为 <c>UntypedStackable</c>。
/// </para>
/// <para>
/// ⚠️ 本方法返回**正数减值量**（与 <see cref="RuleCalculateAttackBonus.AttackPenalty"/> 的约定一致，
/// 由它统一取负）；DLL 里对应的是负的 modifier 值。
/// </para>
/// <para>
/// 仅在「用两把武器攻击」（DLL 中即全力攻击）时生效；本应用没有多段攻击模型，
/// 只要角色处于双持状态即视为该情形。Shield Master + 持盾的豁免属战斗风格，
/// 本应用无盾牌/战斗风格模型，未建模；EffortlessDualWielding（职业特性）同样未建模。
/// </para>
/// </summary>
public static class RuleCalculateTwoWeaponFighting
{
    /// <summary>fact rank ≤ 1（普通「双武器格斗」）时的主手攻击减值量。</summary>
    public const int MainHandPenalty = 4;

    /// <summary>fact rank ≤ 1 时的副手攻击减值量。</summary>
    public const int OffHandPenalty = 8;

    /// <summary>rank &gt; 1 且非神话版本（高等双武器格斗）的主/副手减值量。</summary>
    public const int MainHandPenaltyGreater = 2;

    public const int OffHandPenaltyGreater = 2;

    /// <summary>rank &gt; 1 且为神话版本时不再有减值。</summary>
    public const int MythicPenalty = 0;

    /// <summary>副手非轻型且主手非双头时的追加减值量。</summary>
    public const int NonLightOffHandExtraPenalty = 2;

    /// <summary>是否处于双持状态（对应 DLL <c>RestrictionsHelper.CheckHasTwoWeapon</c>）。</summary>
    public static bool IsTwoWeaponing(CharacterProfile profile) =>
        (profile.Weapons ?? []).Any(w => w.IsSecondary);

    /// <summary>
    /// 计算某把武器在双武器战斗下的攻击减值量（正数；0 表示无减值）。
    /// </summary>
    /// <param name="isSecondary">该武器是否在副手。</param>
    /// <param name="category">武器分类；副手为轻型时豁免追加减值。</param>
    /// <param name="mainHandIsDouble">主手武器是否为双头武器；为 true 时豁免追加减值（DLL <c>Blueprint.Double</c>）。</param>
    /// <param name="rank">「双武器战斗」fact 的等级（普通=1，高等=2+）。</param>
    /// <param name="isMythic">是否已取得神话版本（无视全部双武器减值）。</param>
    public static int AttackPenalty(
        bool isSecondary,
        WeaponCategory category,
        bool mainHandIsDouble = false,
        int rank = 1,
        bool isMythic = false)
    {
        var penalty = (rank, isMythic) switch
        {
            (_, true) => MythicPenalty,
            (> 1, _) => isSecondary ? OffHandPenaltyGreater : MainHandPenaltyGreater,
            _ => isSecondary ? OffHandPenalty : MainHandPenalty,
        };

        // 副手非轻型且主手非双头：再 +2
        if (penalty != 0 && isSecondary && category != WeaponCategory.Light && !mainHandIsDouble)
        {
            penalty += NonLightOffHandExtraPenalty;
        }

        return penalty;
    }
}
