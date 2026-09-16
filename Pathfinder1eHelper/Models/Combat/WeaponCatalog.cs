using System.Collections.Generic;

namespace Pathfinder1eHelper.Models.Combat;

/// <summary>常用武器的快速添加模板（伤害骰/力量倍率/重击仅供参考，可再编辑）。</summary>
public sealed record WeaponPreset(string Name, WeaponProfile Weapon);

public static class WeaponCatalog
{
    private static WeaponProfile Weapon(
        string name,
        string damage,
        double strengthMultiplier = 1.0,
        string critical = "×2",
        bool ranged = false) =>
        new()
        {
            Name = name,
            DamageDice = damage,
            StrengthMultiplier = strengthMultiplier,
            Critical = critical,
            IsRanged = ranged,
        };

    public static IReadOnlyList<WeaponPreset> All { get; } =
    [
        new("巨剑", Weapon("巨剑", "2d6", 1.5, "19–20/×2")),
        new("长剑", Weapon("长剑", "1d8")),
        new("弯刀", Weapon("弯刀", "1d6", 1.0, "18–20/×2")),
        new("细剑", Weapon("细剑", "1d6", 1.0, "18–20/×2")),
        new("短剑", Weapon("短剑", "1d6")),
        new("匕首", Weapon("匕首", "1d4", 1.0, "19–20/×2")),
        new("战斗斧", Weapon("战斗斧", "1d8", 1.0, "×3")),
        new("巨斧", Weapon("巨斧", "1d12", 1.5, "×3")),
        new("战锤", Weapon("战锤", "1d8", 1.0, "×3")),
        new("巨木棒", Weapon("巨木棒", "1d10", 1.5)),
        new("长枪", Weapon("长枪", "1d8", 1.0, "×3")),
        new("矛", Weapon("矛", "1d8", 1.0, "×3")),
        new("巨镰", Weapon("巨镰", "2d4", 1.5, "×4")),
        new("戟", Weapon("戟", "1d10", 1.5, "×3")),
        new("徒手击打", Weapon("徒手击打", "1d3", 1.0, "×2")),
        new("长弓", Weapon("长弓", "1d8", 0.0, "×3", ranged: true)),
        new("复合长弓", Weapon("复合长弓", "1d8", 0.0, "×3", ranged: true)),
        new("短弓", Weapon("短弓", "1d6", 0.0, "×3", ranged: true)),
        new("轻弩", Weapon("轻弩", "1d8", 0.0, "19–20/×2", ranged: true)),
        new("重弩", Weapon("重弩", "1d10", 0.0, "19–20/×2", ranged: true)),
        new("投石索", Weapon("投石索", "1d4", 0.0, "×2", ranged: true)),
    ];
}
