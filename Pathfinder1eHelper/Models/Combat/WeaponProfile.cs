using System;

namespace Pathfinder1eHelper.Models.Combat;

/// <summary>一把武器/一种攻击方式的伤害与攻击参数。</summary>
public sealed class WeaponProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "近战武器";

    public bool IsRanged { get; set; }

    /// <summary>伤害骰文本，如 <c>1d8</c>、<c>2d6</c>；仅作展示与拼接。</summary>
    public string DamageDice { get; set; } = "1d8";

    /// <summary>力量伤害倍率：主手 1、双手 1.5、副手 0.5、无 0。</summary>
    public double StrengthMultiplier { get; set; } = 1.0;

    /// <summary>武器增强加值（同时计入攻击与伤害）。</summary>
    public int Enhancement { get; set; }

    /// <summary>重击威胁范围/倍率，仅作展示，如 <c>19–20/×2</c>。</summary>
    public string Critical { get; set; } = "";
}
