using Pathfinder1eHelper.Models.Combat;

namespace Pathfinder1eHelper.Services.Rules;

/// <summary>
/// 武器伤害骰体型缩放表，逐行照搬 WotR <c>WeaponDamageScaleTable</c>：
/// 按「基础骰 + 基础体型」定位所在行（取首个匹配行），再按目标体型取该行的骰式。
/// </summary>
public static class WeaponDamageScaleTable
{
    private static DiceFormula D(int rolls, int sides) => new(rolls, sides);

    private static readonly DiceFormula One = DiceFormula.One;

    // 每行 9 列：超微型、微型、超小型、小型、中型、大型、超大型、巨型、超巨型。
    private static readonly DiceFormula[][] Rows =
    [
        [One, One, One, One, One, D(1, 2), D(1, 3), D(1, 4), D(1, 6)],
        [One, One, One, One, D(1, 2), D(1, 3), D(1, 4), D(1, 6), D(1, 8)],
        [One, One, One, D(1, 2), D(1, 3), D(1, 4), D(1, 6), D(1, 8), D(2, 6)],
        [One, One, D(1, 2), D(1, 3), D(1, 4), D(1, 6), D(1, 8), D(2, 6), D(3, 6)],
        [One, D(1, 2), D(1, 3), D(1, 4), D(1, 6), D(1, 8), D(2, 6), D(3, 6), D(4, 6)],
        [D(1, 2), D(1, 3), D(1, 4), D(1, 6), D(1, 8), D(2, 6), D(3, 6), D(4, 6), D(6, 6)],
        [D(1, 2), D(1, 3), D(1, 4), D(1, 6), D(2, 4), D(2, 6), D(3, 6), D(4, 6), D(6, 6)],
        [D(1, 3), D(1, 4), D(1, 6), D(1, 8), D(1, 10), D(2, 8), D(3, 8), D(4, 8), D(6, 8)],
        [D(1, 4), D(1, 6), D(1, 8), D(1, 10), D(1, 12), D(3, 6), D(4, 6), D(6, 6), D(8, 6)],
        [D(1, 4), D(1, 6), D(1, 8), D(1, 10), D(2, 6), D(3, 6), D(4, 6), D(6, 6), D(8, 6)],
        [D(1, 4), D(1, 6), D(1, 10), D(1, 10), D(2, 8), D(3, 8), D(4, 8), D(6, 8), D(8, 8)],
        [D(1, 6), D(1, 8), D(1, 10), D(2, 6), D(3, 6), D(4, 6), D(6, 6), D(8, 6), D(12, 6)],
        [D(1, 8), D(1, 10), D(2, 6), D(2, 8), D(2, 10), D(4, 8), D(6, 8), D(8, 8), D(12, 8)],
        [D(1, 8), D(1, 10), D(2, 6), D(3, 6), D(4, 6), D(6, 6), D(8, 6), D(12, 6), D(16, 6)],
        [D(1, 10), D(1, 10), D(3, 6), D(2, 12), D(3, 10), D(4, 10), D(6, 10), D(8, 10), D(12, 10)],
        [D(1, 2), D(1, 3), D(1, 4), D(1, 6), D(1, 8), D(1, 10), D(2, 8), D(3, 8), D(4, 8)],
        [One, D(1, 2), D(1, 3), D(1, 4), D(1, 6), D(1, 8), D(1, 10), D(2, 8), D(3, 8)],
    ];

    /// <summary>按目标体型缩放骰式；找不到匹配行时原样返回。</summary>
    public static DiceFormula Scale(DiceFormula baseDice, SizeCategory size, SizeCategory baseDiceSize = SizeCategory.Medium)
    {
        if (size == baseDiceSize)
        {
            return baseDice;
        }

        var baseIndex = (int)baseDiceSize;
        var targetIndex = (int)size;
        foreach (var row in Rows)
        {
            if (row[baseIndex] == baseDice)
            {
                return row[targetIndex];
            }
        }

        return baseDice;
    }

    /// <summary>文本版缩放：解析失败或不应缩放时返回原文。</summary>
    public static string ScaleToText(
        string? baseDamageText,
        SizeCategory size,
        SizeCategory baseDiceSize = SizeCategory.Medium,
        bool doNotScale = false)
    {
        if (doNotScale || !DiceFormula.TryParse(baseDamageText, out var parsed))
        {
            return baseDamageText ?? string.Empty;
        }

        var scaled = Scale(parsed, size, baseDiceSize);
        return scaled == parsed ? (baseDamageText ?? string.Empty) : scaled.ToString();
    }
}
