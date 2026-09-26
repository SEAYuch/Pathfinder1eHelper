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

/// <summary>武器攻击方式，对应 WotR <c>AttackType</c>。</summary>
public enum WeaponAttackType
{
    Melee,
    Touch,
    Ranged,
    RangedTouch,
}

/// <summary>武器持握方式，决定伤害属性倍率。</summary>
public enum WeaponHand
{
    /// <summary>主手（×1）。</summary>
    Primary,

    /// <summary>双手（×1.5）。</summary>
    TwoHanded,

    /// <summary>副手（×0.5）。</summary>
    OffHand,
}

/// <summary>加值作用的战斗数值通道（对应 WotR <c>StatType</c> 战斗子集）。</summary>
public enum CombatStat
{
    ArmorClass,

    /// <summary>攻击命中（对应 WotR <c>Stats.AdditionalAttackBonus</c>）；近战/远程/接触共用，一切按描述符叠加判定。</summary>
    Attack,

    /// <summary>对应 WotR <c>Stats.AdditionalDamage</c>（附加伤害）。</summary>
    Damage,

    Fortitude,
    Reflex,
    Will,
    Initiative,
    Cmb,
    Cmd,

    /// <summary>对应 WotR <c>Stats.AdditionalCMB</c>。</summary>
    AdditionalCMB,

    /// <summary>对应 WotR <c>Stats.AdditionalCMD</c>。</summary>
    AdditionalCMD,

    Concentration,

    /// <summary>法术豁免 DC（10 + 环位 + 施法属性）。</summary>
    SpellDC,

    /// <summary>属性值（需配合 <see cref="ModifierEntry.Ability"/>）。</summary>
    AbilityScore,
}

/// <summary>生物体型（由小到大），对应 WotR <c>Size</c>。</summary>
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

/// <summary>
/// 体型对攻击检定/AC 与战技（CMB/CMD）的修正，对应 WotR <c>WeaponSizeExtension.SizeModifiers</c>。
/// </summary>
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

    /// <summary>
    /// 体型偏移（对应 WotR <c>WeaponSizeExtension.Shift</c>）；默认夹在超小型…超巨型之间。
    /// </summary>
    public static SizeCategory Shift(
        this SizeCategory size,
        int shift,
        SizeCategory min = SizeCategory.Tiny,
        SizeCategory max = SizeCategory.Colossal)
    {
        var value = (int)size + shift;
        if (value < (int)min)
        {
            return min;
        }

        return value > (int)max ? max : (SizeCategory)value;
    }
}
