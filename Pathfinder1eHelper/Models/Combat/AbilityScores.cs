using System;

namespace Pathfinder1eHelper.Models.Combat;

/// <summary>六项属性值，提供 PF1 的属性修正（向下取整）。</summary>
public sealed class AbilityScores
{
    public int Strength { get; set; } = 10;

    public int Dexterity { get; set; } = 10;

    public int Constitution { get; set; } = 10;

    public int Intelligence { get; set; } = 10;

    public int Wisdom { get; set; } = 10;

    public int Charisma { get; set; } = 10;

    public int GetScore(Ability ability) => ability switch
    {
        Ability.Strength => Strength,
        Ability.Dexterity => Dexterity,
        Ability.Constitution => Constitution,
        Ability.Intelligence => Intelligence,
        Ability.Wisdom => Wisdom,
        Ability.Charisma => Charisma,
        _ => 10,
    };

    /// <summary>属性修正 = floor((属性值 - 10) / 2)。</summary>
    public int GetModifier(Ability ability) => (int)Math.Floor((GetScore(ability) - 10) / 2.0);

    public void SetScore(Ability ability, int score)
    {
        switch (ability)
        {
            case Ability.Strength:
                Strength = score;
                break;
            case Ability.Dexterity:
                Dexterity = score;
                break;
            case Ability.Constitution:
                Constitution = score;
                break;
            case Ability.Intelligence:
                Intelligence = score;
                break;
            case Ability.Wisdom:
                Wisdom = score;
                break;
            case Ability.Charisma:
                Charisma = score;
                break;
        }
    }
}
