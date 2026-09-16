using System;
using System.Collections.Generic;

namespace Pathfinder1eHelper.Models.Combat;

/// <summary>
/// 角色战斗档案。基础值（BAB、基础豁免、属性）与手动加值/武器分开存放，计算时合成。
/// </summary>
public sealed class CharacterProfile
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "新角色";

    public int Level { get; set; } = 1;

    public SizeCategory Size { get; set; } = SizeCategory.Medium;

    public AbilityScores Abilities { get; set; } = new();

    public int BaseAttackBonus { get; set; }

    public int BaseFortitude { get; set; }

    public int BaseReflex { get; set; }

    public int BaseWill { get; set; }

    /// <summary>护甲允许的最大敏捷加值；null 表示不限制。</summary>
    public int? MaxDexBonus { get; set; }

    public int CasterLevel { get; set; }

    public Ability CastingAbility { get; set; } = Ability.Intelligence;

    /// <summary>超小型及以下生物可改用敏捷计算 CMB/CMD。</summary>
    public bool UseDexForManeuvers { get; set; }

    public List<BonusEntry> Bonuses { get; set; } = [];

    public List<WeaponProfile> Weapons { get; set; } = [];

    /// <summary>补齐旧档案缺少的字段/列表。</summary>
    public void ApplyDefaults()
    {
        Abilities ??= new AbilityScores();
        Bonuses ??= [];
        Weapons ??= [];
    }
}
