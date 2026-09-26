using System.Collections.Generic;
using System.Linq;

namespace Pathfinder1eHelper.Models.Combat;

/// <summary>一项数值的组成条目；<see cref="Included"/> 为 false 表示被同描述符更高/更低者压制。</summary>
public sealed record Contribution(
    string Label,
    int Value,
    bool Included = true,
    string? Note = null,
    bool ExcludedFromTouch = false,
    bool ExcludedFromFlatFooted = false,
    ModifierDescriptor? Descriptor = null)
{
    public string ValueDisplay => Value >= 0 ? $"+{Value}" : Value.ToString();

    public double RowOpacity => Included ? 1.0 : 0.4;
}

/// <summary>一项战斗数值的合计与全部组成明细。</summary>
public sealed record StatResult(int Total, IReadOnlyList<Contribution> Contributions)
{
    public string TotalDisplay => Total >= 0 ? $"+{Total}" : Total.ToString();

    /// <summary>剔除不适用的条目后重新合计（用于接触 AC / 措手不及 AC）。</summary>
    public StatResult Excluding(bool touch)
    {
        var total = Contributions
            .Where(c => c.Included && !(touch ? c.ExcludedFromTouch : c.ExcludedFromFlatFooted))
            .Sum(c => c.Value);
        return this with { Total = total };
    }
}

/// <summary>一把武器的攻击/伤害计算结果。</summary>
public sealed record WeaponResult(
    WeaponProfile Weapon,
    StatResult Attack,
    StatResult Damage,
    int AttacksCount,
    int CriticalThreatLow,
    int CriticalMultiplier,
    string DamageDice)
{
    public string AttackDisplay => Attack.Total >= 0 ? $"+{Attack.Total}" : Attack.Total.ToString();

    public string DamageDisplay
    {
        get
        {
            var dice = DamageDice?.Trim() ?? string.Empty;
            if (Damage.Total == 0)
            {
                return dice;
            }

            var modifier = Damage.Total > 0 ? $"+{Damage.Total}" : Damage.Total.ToString();
            return dice.Length == 0 ? modifier : dice + modifier;
        }
    }

    /// <summary>伤害骰 + 属性/增强合计后的展示（含多打数）。</summary>
    public string DamageDisplayWithDice => DamageDisplay;

    public string FullAttackDisplay => AttacksCount <= 1
        ? AttackDisplay
        : string.Join("/", Enumerable.Range(0, AttacksCount)
            .Select(i => (Attack.Total - i * 5 >= 0 ? $"+{Attack.Total - i * 5}" : (Attack.Total - i * 5).ToString()))
            .Reverse());

    public string CriticalDisplay =>
        $"{(CriticalThreatLow >= 20 ? "20" : $"{CriticalThreatLow}–20")}/×{CriticalMultiplier}";
}

/// <summary>整个战斗档案的计算结果。</summary>
public sealed record CombatSheet(
    StatResult ArmorClass,
    StatResult TouchArmorClass,
    StatResult FlatFootedArmorClass,
    StatResult FlatFootedTouchArmorClass,
    StatResult Fortitude,
    StatResult Reflex,
    StatResult Will,
    StatResult Cmb,
    StatResult Cmd,
    StatResult Concentration,
    StatResult Initiative,
    int SpellDc,
    IReadOnlyList<WeaponResult> Weapons);
