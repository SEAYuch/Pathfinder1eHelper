using System;
using System.Text.Json.Serialization;

namespace Pathfinder1eHelper.Models.Combat;

/// <summary>一把武器/一种攻击方式的伤害与攻击参数。</summary>
public sealed class WeaponProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "近战武器";

    /// <summary>命中使用的属性调整值：力量（近战/投掷）或敏捷（远程）。</summary>
    public WeaponAbility AttackAbility { get; set; } = WeaponAbility.Strength;

    /// <summary>
    /// 旧字段（“远程”勾选）。仅用于反序列化旧存档：为 true 时迁移为 <see cref="AttackAbility"/> = 敏捷。
    /// 只写不读，因此不会再写入新存档。
    /// </summary>
    [JsonInclude]
    public bool IsRanged
    {
        set
        {
            if (value)
            {
                AttackAbility = WeaponAbility.Dexterity;
            }
        }
    }

    /// <summary>伤害骰文本，如 <c>1d8</c>、<c>2d6</c>；仅作展示与拼接。</summary>
    public string DamageDice { get; set; } = "1d8";

    /// <summary>能力伤害倍率：主手 1、双手 1.5、副手 0.5、无 0。</summary>
    public double StrengthMultiplier { get; set; } = 1.0;

    /// <summary>伤害计入的属性调整值：力量（力上伤）或敏捷（敏上伤）。</summary>
    public WeaponAbility DamageAbility { get; set; } = WeaponAbility.Strength;

    /// <summary>武器增强加值（同时计入攻击与伤害）。</summary>
    public int Enhancement { get; set; }

    /// <summary>重击威胁范围/倍率，仅作展示，如 <c>19–20/×2</c>。</summary>
    public string Critical { get; set; } = string.Empty;
}
