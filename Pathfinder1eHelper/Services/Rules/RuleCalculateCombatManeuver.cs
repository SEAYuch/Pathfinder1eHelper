using System;
using System.Collections.Generic;
using System.Linq;
using Pathfinder1eHelper.Models.Combat;

namespace Pathfinder1eHelper.Services.Rules;

/// <summary>CMB 计算入参，对应 WotR <c>RuleCalculateBaseCMB</c> / <c>RuleCalculateCMB</c>。</summary>
public sealed record CmbRequest
{
    public int BaseAttackBonus { get; init; }

    /// <summary>战技属性调整值（通常力量；超小型可替换为敏捷）。</summary>
    public int ManeuverAbilityBonus { get; init; }

    public string ManeuverAbilityLabel { get; init; } = "力量";

    /// <summary>体型对 CMB/CMD 的修正（CMDAndCMD）。</summary>
    public int SizeBonus { get; init; }

    public bool TargetIsStunned { get; init; }

    public int AdditionalCmbBase { get; init; }

    public IReadOnlyList<Modifier> AdditionalCmb { get; init; } = [];

    public int AdditionalAttackBonusBase { get; init; }

    /// <summary>对应 <c>Stats.AdditionalAttackBonus</c>（体型项被排除，避免与 <see cref="SizeBonus"/> 重复）。</summary>
    public IReadOnlyList<Modifier> AdditionalAttackBonus { get; init; } = [];
}

/// <summary>CMD 计算入参，对应 WotR <c>RuleCalculateBaseCMD</c> / <c>RuleCalculateCMD</c>。</summary>
public sealed record CmdRequest
{
    public int BaseAttackBonus { get; init; }

    public int ManeuverAbilityBonus { get; init; }

    public string ManeuverAbilityLabel { get; init; } = "力量";

    public int DexterityBonus { get; init; }

    /// <summary>措手不及时不计（正值）敏捷加值。</summary>
    public bool DenyDexterityBonus { get; init; }

    public int SizeBonus { get; init; }

    public int AdditionalCmdBase { get; init; }

    public IReadOnlyList<Modifier> AdditionalCmd { get; init; } = [];

    /// <summary>AC 修饰列表，用于派生「接触 AC 允许」的加值（偏斜/洞察/幸运/闪避等）。</summary>
    public IReadOnlyList<Modifier> ArmorClassModifiers { get; init; } = [];
}

/// <summary>
/// 战技加值/防御，对应 WotR CMB / CMD 规则。
/// CMD 的参考项由 AC 修饰派生（接触 AC 允许、且非敏捷通道/体型者）。
/// </summary>
public static class RuleCalculateCombatManeuver
{
    public const int BaseCmd = 10;
    public const int StunnedCmbBonus = 4;

    public static StatResult Cmb(CmbRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contributions = new List<Contribution>
        {
            new("BAB", request.BaseAttackBonus, Included: request.BaseAttackBonus != 0),
            new(request.ManeuverAbilityLabel, request.ManeuverAbilityBonus, Included: request.ManeuverAbilityBonus != 0),
            new("体型", request.SizeBonus, Included: request.SizeBonus != 0),
        };

        if (request.TargetIsStunned)
        {
            contributions.Add(new Contribution("目标眩晕", StunnedCmbBonus));
        }

        if (request.AdditionalCmbBase != 0)
        {
            contributions.Add(new Contribution("额外战技", request.AdditionalCmbBase));
        }

        contributions.AddRange(ModifierEngine.Evaluate(0, request.AdditionalCmb)
            .Contributions.Select(RuleMath.FromModifier));

        if (request.AdditionalAttackBonusBase != 0)
        {
            contributions.Add(new Contribution("额外攻击加值", request.AdditionalAttackBonusBase));
        }

        contributions.AddRange(ModifierEngine.Evaluate(
                0,
                request.AdditionalAttackBonus,
                filter: m => m.Descriptor != ModifierDescriptor.Size)
            .Contributions.Select(RuleMath.FromModifier));

        return RuleMath.Stat(contributions);
    }

    public static StatResult Cmd(CmdRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dexAllowed = !request.DenyDexterityBonus || request.DexterityBonus < 0;
        var contributions = new List<Contribution>
        {
            new("基础", BaseCmd),
            new("BAB", request.BaseAttackBonus, Included: request.BaseAttackBonus != 0),
            new(request.ManeuverAbilityLabel, request.ManeuverAbilityBonus, Included: request.ManeuverAbilityBonus != 0),
            new("敏捷", dexAllowed ? request.DexterityBonus : 0, Included: dexAllowed && request.DexterityBonus != 0),
            new("体型", request.SizeBonus, Included: request.SizeBonus != 0),
        };

        // 从 AC 修饰派生「接触允许、非敏捷/体型」的加值（含闪避）。
        foreach (var contribution in ModifierEngine.Evaluate(0, request.ArmorClassModifiers).Contributions)
        {
            if (!contribution.Included)
            {
                continue;
            }

            var descriptor = contribution.Modifier.Descriptor;
            if (RuleMath.ExcludesFromTouch(contribution.Modifier)
                || descriptor is ModifierDescriptor.DexterityBonus or ModifierDescriptor.Size)
            {
                continue;
            }

            contributions.Add(RuleMath.FromModifier(contribution));
        }

        if (request.AdditionalCmdBase != 0)
        {
            contributions.Add(new Contribution("额外CMD", request.AdditionalCmdBase));
        }

        contributions.AddRange(ModifierEngine.Evaluate(0, request.AdditionalCmd)
            .Contributions.Select(RuleMath.FromModifier));

        return RuleMath.Stat(contributions);
    }
}
