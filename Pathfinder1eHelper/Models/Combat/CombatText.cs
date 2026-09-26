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

    public static string CombatStat(Models.Combat.CombatStat stat) => stat switch
    {
        Models.Combat.CombatStat.ArmorClass => "防御等级(AC)",
        Models.Combat.CombatStat.Attack => "攻击命中",
        Models.Combat.CombatStat.Damage => "伤害",
        Models.Combat.CombatStat.Fortitude => "强韧豁免",
        Models.Combat.CombatStat.Reflex => "反射豁免",
        Models.Combat.CombatStat.Will => "意志豁免",
        Models.Combat.CombatStat.Initiative => "先攻",
        Models.Combat.CombatStat.Cmb => "战技加值(CMB)",
        Models.Combat.CombatStat.Cmd => "战技防御(CMD)",
        Models.Combat.CombatStat.AdditionalCMB => "额外 CMB",
        Models.Combat.CombatStat.AdditionalCMD => "额外 CMD",
        Models.Combat.CombatStat.Concentration => "专注",
        Models.Combat.CombatStat.SpellDC => "法术 DC",
        Models.Combat.CombatStat.AbilityScore => "属性值",
        _ => stat.ToString(),
    };

    public static string WeaponHand(Models.Combat.WeaponHand hand) => hand switch
    {
        Models.Combat.WeaponHand.Primary => "主手 ×1",
        Models.Combat.WeaponHand.TwoHanded => "双手 ×1.5",
        Models.Combat.WeaponHand.OffHand => "副手 ×0.5",
        _ => hand.ToString(),
    };

    public static string WeaponCategory(Models.Combat.WeaponCategory category) => category switch
    {
        Models.Combat.WeaponCategory.Light => "轻型",
        Models.Combat.WeaponCategory.Medium => "中型",
        Models.Combat.WeaponCategory.Heavy => "重型",
        _ => category.ToString(),
    };

    public static string WeaponAttackType(Models.Combat.WeaponAttackType type) => type switch    {
        Models.Combat.WeaponAttackType.Melee => "近战",
        Models.Combat.WeaponAttackType.Touch => "近战接触",
        Models.Combat.WeaponAttackType.Ranged => "远程",
        Models.Combat.WeaponAttackType.RangedTouch => "远程接触",
        _ => type.ToString(),
    };

    public static string Descriptor(ModifierDescriptor descriptor) => descriptor switch
    {
        ModifierDescriptor.None => "无类型",
        ModifierDescriptor.Racial => "种族",
        ModifierDescriptor.Dodge => "闪避",
        ModifierDescriptor.Competence => "表现",
        ModifierDescriptor.Armor => "护甲",
        ModifierDescriptor.Shield => "盾牌",
        ModifierDescriptor.Alchemical => "炼金",
        ModifierDescriptor.Circumstance => "环境",
        ModifierDescriptor.Deflection => "偏斜",
        ModifierDescriptor.Enhancement => "增强",
        ModifierDescriptor.ArmorEnhancement => "护甲增强",
        ModifierDescriptor.ShieldEnhancement => "盾牌增强",
        ModifierDescriptor.Inherent => "内在",
        ModifierDescriptor.Insight => "洞察",
        ModifierDescriptor.Luck => "幸运",
        ModifierDescriptor.Morale => "士气",
        ModifierDescriptor.NaturalArmor => "天生护甲",
        ModifierDescriptor.NaturalArmorEnhancement => "天生护甲增强",
        ModifierDescriptor.Profane => "亵渎",
        ModifierDescriptor.Sacred => "崇圣",
        ModifierDescriptor.Size => "体型",
        ModifierDescriptor.Trait => "背景特性",
        ModifierDescriptor.Resistance => "抗力",
        ModifierDescriptor.FearPenalty => "恐惧减值",
        ModifierDescriptor.NegativeEnergyPenalty => "负能量减值",
        ModifierDescriptor.UntypedStackable => "无类型",
        ModifierDescriptor.DexterityBonus => "敏捷加值",
        ModifierDescriptor.ConstitutionBonus => "体质加值",
        ModifierDescriptor.Fatigued => "疲乏",
        ModifierDescriptor.Crippled => "残废",
        ModifierDescriptor.Feat => "专长",
        ModifierDescriptor.StatDamage => "属性伤害",
        ModifierDescriptor.StatDrain => "属性吸取",
        ModifierDescriptor.Focus => "专攻",
        ModifierDescriptor.BaseStatBonus => "基础加值",
        ModifierDescriptor.Penalty => "减值",
        ModifierDescriptor.ArmorFocus => "护甲专攻",
        ModifierDescriptor.Cooking => "烹饪",
        ModifierDescriptor.Polymorph => "变形",
        ModifierDescriptor.Helpless => "无助",
        ModifierDescriptor.Encumbrance => "负重",
        ModifierDescriptor.FavoredEnemy => "宿敌",
        ModifierDescriptor.Other => "其他",
        ModifierDescriptor.Prone => "俯卧",
        ModifierDescriptor.Mythic => "神话",
        ModifierDescriptor.DemonBonus => "恶魔加值",
        ModifierDescriptor.Rage => "狂暴",
        ModifierDescriptor.UniqueItem => "独特物品",
        ModifierDescriptor.LockpickersKit => "开锁工具",
        ModifierDescriptor.FavouredClassBonus => "天赋职业加值",
        ModifierDescriptor.NaturalArmorForm => "天生护甲形态",
        ModifierDescriptor.Anomaly => "异常",
        ModifierDescriptor.WeaponTraining => "武器训练",
        ModifierDescriptor.MasterShapeshifter => "变形大师",
        ModifierDescriptor.SelfBonus => "自身加值",
        ModifierDescriptor.Haste => "加速",
        _ => descriptor.ToString(),
    };

    public static string StackMode(Models.Combat.StackMode mode) => mode switch
    {
        Models.Combat.StackMode.Default => "默认",
        Models.Combat.StackMode.ForceStack => "强制叠加",
        Models.Combat.StackMode.ForceNonStack => "强制不叠加",
        _ => mode.ToString(),
    };
}
