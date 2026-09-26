namespace Pathfinder1eHelper.Models.Combat;

/// <summary>
/// 加值/减值的「描述符」——叠加分组键。语义与《开拓者：正义之怒》反编译库中的
/// <c>Kingmaker.Enums.ModifierDescriptor</c> 一一对应，仅移除两个纯游戏内部项：
/// <c>Difficulty</c>(34) 与 <c>DLC3_Stackable</c>(53)（故数值在此留空）。
/// 是否可叠加见 <see cref="ModifierDescriptorHelper"/>；叠加运算见
/// <see cref="Services.ModifierEngine"/>。
/// </summary>
public enum ModifierDescriptor
{
    /// <summary>无类型（可叠加）。</summary>
    None = 0,

    Racial = 1,

    Dodge = 2,

    Competence = 3,

    Armor = 4,

    Shield = 5,

    Alchemical = 6,

    /// <summary>环境加值；在游戏中**不可叠加**（同描述符取高）。</summary>
    Circumstance = 7,

    Deflection = 8,

    Enhancement = 9,

    ArmorEnhancement = 10,

    ShieldEnhancement = 11,

    Inherent = 12,

    Insight = 13,

    Luck = 14,

    Morale = 15,

    NaturalArmor = 16,

    NaturalArmorEnhancement = 17,

    Profane = 18,

    Sacred = 19,

    /// <summary>体型修正。</summary>
    Size = 20,

    Trait = 21,

    Resistance = 22,

    FearPenalty = 23,

    NegativeEnergyPenalty = 24,

    /// <summary>显式「无类型可叠加」，与 <see cref="None"/> 同义但用于区分来源。</summary>
    UntypedStackable = 25,

    /// <summary>AC / 先攻的基础敏捷加值通道。</summary>
    DexterityBonus = 26,

    ConstitutionBonus = 27,

    Fatigued = 28,

    Crippled = 29,

    Feat = 30,

    StatDamage = 31,

    StatDrain = 32,

    Focus = 33,

    /// <summary>与 <see cref="Focus"/> 同值的别名（DLL 中为 <c>ShieldFocus = Focus</c>），仅用于解析字符串。</summary>
    ShieldFocus = Focus,

    BaseStatBonus = 35,

    Penalty = 36,

    ArmorFocus = 37,

    Cooking = 38,

    Polymorph = 39,

    Helpless = 40,

    Encumbrance = 41,

    FavoredEnemy = 42,

    Other = 43,

    Prone = 44,

    Mythic = 45,

    DemonBonus = 46,

    Rage = 47,

    UniqueItem = 48,

    LockpickersKit = 49,

    FavouredClassBonus = 50,

    NaturalArmorForm = 51,

    Anomaly = 52,

    WeaponTraining = 54,

    MasterShapeshifter = 55,

    SelfBonus = 56,

    Haste = 57,
}
