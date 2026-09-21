namespace Pathfinder1eHelper.Models.Combat;

/// <summary>六项属性。</summary>
public enum Ability
{
    Strength,
    Dexterity,
    Constitution,
    Intelligence,
    Wisdom,
    Charisma,
}

/// <summary>武器在命中/伤害上使用的能力调整值：力量 / 敏捷。</summary>
public enum WeaponAbility
{
    /// <summary>力量（近战/投掷命中；常规力量上伤）。</summary>
    Strength,

    /// <summary>敏捷（远程命中；灵巧类敏上伤）。</summary>
    Dexterity,
}

/// <summary>生物体型（由小到大）。</summary>
public enum SizeCategory
{
    Fine,
    Diminutive,
    Tiny,
    Small,
    Medium,
    Large,
    Huge,
    Gargantuan,
    Colossal,
}

/// <summary>体型对攻击检定/AC 与战技（CMB/CMD）的修正。</summary>
public static class SizeModifiers
{
    /// <summary>攻击检定与 AC 的体型修正（超微型 +8 … 超巨型 -8）。</summary>
    public static int AttackAndAc(SizeCategory size) => size switch
    {
        SizeCategory.Fine => 8,
        SizeCategory.Diminutive => 4,
        SizeCategory.Tiny => 2,
        SizeCategory.Small => 1,
        SizeCategory.Large => -1,
        SizeCategory.Huge => -2,
        SizeCategory.Gargantuan => -4,
        SizeCategory.Colossal => -8,
        _ => 0,
    };

    /// <summary>CMB/CMD 的体型修正（与攻击/AC 相反：超微型 -8 … 超巨型 +8）。</summary>
    public static int Maneuver(SizeCategory size) => -AttackAndAc(size);
}

/// <summary>加值类型；<see cref="Penalty"/> 为无类型减值（总是叠加，计算时取负）。</summary>
public enum BonusType
{
    Alchemical,
    Armor,
    Circumstance,
    Competence,
    Deflection,
    Dodge,
    Enhancement,
    Inherent,
    Insight,
    Luck,
    Morale,
    NaturalArmor,
    Profane,
    Racial,
    Resistance,
    Sacred,
    Shield,
    Trait,
    Untyped,
    Penalty,
}

/// <summary>“增强加值”所增强的对象；决定其与哪个基础加值层相加。</summary>
public enum EnhancementSubject
{
    None,
    Armor,
    Shield,
    NaturalArmor,
}

/// <summary>加值作用的检定/统计项。</summary>
public enum BonusTarget
{
    MeleeAttack,
    RangedAttack,
    /// <summary>已废弃：不再提供接触专用加值，仅为兼容旧存档保留。</summary>
    MeleeTouchAttack,
    /// <summary>已废弃：不再提供接触专用加值，仅为兼容旧存档保留。</summary>
    RangedTouchAttack,
    ArmorClass,
    Fortitude,
    Reflex,
    Will,
    Cmb,
    Cmd,
    Concentration,
    Damage,
    Initiative,
    AbilityScore,
}
