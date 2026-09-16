namespace Pathfinder1eHelper.Models.Combat;

/// <summary>战斗域枚举的中文显示文本。</summary>
public static class CombatText
{
    public static string Ability(Ability ability) => ability switch
    {
        Models.Combat.Ability.Strength => "力量",
        Models.Combat.Ability.Dexterity => "敏捷",
        Models.Combat.Ability.Constitution => "体质",
        Models.Combat.Ability.Intelligence => "智力",
        Models.Combat.Ability.Wisdom => "感知",
        Models.Combat.Ability.Charisma => "魅力",
        _ => ability.ToString(),
    };

    public static string Size(SizeCategory size) => size switch
    {
        SizeCategory.Fine => "超微型",
        SizeCategory.Diminutive => "微型",
        SizeCategory.Tiny => "超小型",
        SizeCategory.Small => "小型",
        SizeCategory.Medium => "中型",
        SizeCategory.Large => "大型",
        SizeCategory.Huge => "超大型",
        SizeCategory.Gargantuan => "巨型",
        SizeCategory.Colossal => "超巨型",
        _ => size.ToString(),
    };

    public static string BonusType(BonusType type) => type switch
    {
        Models.Combat.BonusType.Alchemical => "炼金",
        Models.Combat.BonusType.Armor => "护甲",
        Models.Combat.BonusType.Circumstance => "环境",
        Models.Combat.BonusType.Competence => "表现",
        Models.Combat.BonusType.Deflection => "偏斜",
        Models.Combat.BonusType.Dodge => "闪避",
        Models.Combat.BonusType.Enhancement => "增强",
        Models.Combat.BonusType.Inherent => "内在",
        Models.Combat.BonusType.Insight => "洞察",
        Models.Combat.BonusType.Luck => "幸运",
        Models.Combat.BonusType.Morale => "士气",
        Models.Combat.BonusType.NaturalArmor => "天生护甲",
        Models.Combat.BonusType.Profane => "亵渎",
        Models.Combat.BonusType.Racial => "种族",
        Models.Combat.BonusType.Resistance => "抗力",
        Models.Combat.BonusType.Sacred => "崇圣",
        Models.Combat.BonusType.Shield => "盾牌",
        Models.Combat.BonusType.Trait => "训练",
        Models.Combat.BonusType.Untyped => "无类型",
        Models.Combat.BonusType.Penalty => "减值",
        _ => type.ToString(),
    };

    public static string EnhancementSubject(EnhancementSubject subject) => subject switch
    {
        Models.Combat.EnhancementSubject.None => "—",
        Models.Combat.EnhancementSubject.Armor => "护甲",
        Models.Combat.EnhancementSubject.Shield => "盾牌",
        Models.Combat.EnhancementSubject.NaturalArmor => "天生护甲",
        _ => subject.ToString(),
    };

    public static string BonusTarget(BonusTarget target) => target switch
    {
        Models.Combat.BonusTarget.MeleeAttack => "近战攻击",
        Models.Combat.BonusTarget.RangedAttack => "远程攻击",
        Models.Combat.BonusTarget.MeleeTouchAttack => "近战接触攻击",
        Models.Combat.BonusTarget.RangedTouchAttack => "远程接触攻击",
        Models.Combat.BonusTarget.ArmorClass => "防御等级(AC)",
        Models.Combat.BonusTarget.Fortitude => "强韧豁免",
        Models.Combat.BonusTarget.Reflex => "反射豁免",
        Models.Combat.BonusTarget.Will => "意志豁免",
        Models.Combat.BonusTarget.Cmb => "战技加值(CMB)",
        Models.Combat.BonusTarget.Cmd => "战技防御(CMD)",
        Models.Combat.BonusTarget.Concentration => "专注",
        Models.Combat.BonusTarget.Damage => "伤害",
        Models.Combat.BonusTarget.Initiative => "先攻",
        Models.Combat.BonusTarget.AbilityScore => "属性值",
        _ => target.ToString(),
    };
}
