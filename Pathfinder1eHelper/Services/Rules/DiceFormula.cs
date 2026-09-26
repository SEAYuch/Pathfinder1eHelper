using System;

namespace Pathfinder1eHelper.Services.Rules;

/// <summary>
/// 伤害骰表示（对应 WotR <c>DiceFormula</c>）。<see cref="Sides"/> 为 0/1 时表示固定值。
/// </summary>
public readonly record struct DiceFormula(int Rolls, int Sides)
{
    /// <summary>固定 1 点（对应 <c>DiceFormula.One</c>）。</summary>
    public static readonly DiceFormula One = new(1, 0);

    public bool IsFlat => Sides <= 1;

    /// <summary>解析形如 <c>2d6</c>、<c>1d8</c>、<c>d4</c>、<c>1</c> 的文本；失败返回 false。</summary>
    public static bool TryParse(string? text, out DiceFormula formula)
    {
        formula = One;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var value = text.Trim().ToLowerInvariant();
        var index = value.IndexOf('d');
        if (index < 0)
        {
            if (int.TryParse(value, out var flat) && flat >= 0)
            {
                formula = new DiceFormula(flat, 0);
                return true;
            }

            return false;
        }

        var rollsText = value[..index];
        var sidesText = value[(index + 1)..];
        var rolls = string.IsNullOrEmpty(rollsText) ? 1 : int.Parse(rollsText, System.Globalization.CultureInfo.InvariantCulture);

        // 截掉骰面后可能存在的修饰符（如 1d8+1）。
        var end = 0;
        while (end < sidesText.Length && char.IsDigit(sidesText[end]))
        {
            end++;
        }

        if (end == 0)
        {
            // 处理 1dOne 这类命名骰面。
            if (sidesText.StartsWith("one", StringComparison.OrdinalIgnoreCase))
            {
                formula = new DiceFormula(rolls, 0);
                return true;
            }

            return false;
        }

        var sides = int.Parse(sidesText[..end], System.Globalization.CultureInfo.InvariantCulture);
        formula = new DiceFormula(rolls, sides);
        return true;
    }

    public override string ToString() => IsFlat ? Rolls.ToString(System.Globalization.CultureInfo.InvariantCulture) : $"{Rolls}d{Sides}";
}
