using System.Collections.Generic;

namespace Pathfinder1eHelper.Models.Combat;

/// <summary>一组常用状态/战术加值，一键加入加值明细（同类型自会取高）。</summary>
public sealed record CombatPreset(string Name, IReadOnlyList<BonusEntry> Entries);

public static class CombatPresets
{
    private static BonusEntry Bonus(string name, BonusType type, BonusTarget target, int value, string? notes = null) =>
        new() { Name = name, Type = type, Target = target, Value = value, Notes = notes };

    public static IReadOnlyList<CombatPreset> All { get; } =
    [
        new("冲锋", [
            Bonus("冲锋", BonusType.Untyped, BonusTarget.MeleeAttack, 2),
            Bonus("冲锋（AC 减值）", BonusType.Penalty, BonusTarget.ArmorClass, 2),
        ]),
        new("夹击", [
            Bonus("夹击", BonusType.Untyped, BonusTarget.MeleeAttack, 2),
        ]),
        new("居高临下", [
            Bonus("居高临下", BonusType.Untyped, BonusTarget.MeleeAttack, 1),
        ]),
        new("掩蔽", [
            Bonus("掩蔽", BonusType.Untyped, BonusTarget.ArmorClass, 4),
        ]),
        new("援助他人", [
            Bonus("援助他人", BonusType.Untyped, BonusTarget.MeleeAttack, 2),
        ]),
        new("隐形", [
            Bonus("隐形（攻击）", BonusType.Untyped, BonusTarget.MeleeAttack, 2),
        ]),
        new("战栗", [
            Bonus("战栗（近战）", BonusType.Penalty, BonusTarget.MeleeAttack, 2),
            Bonus("战栗（远程）", BonusType.Penalty, BonusTarget.RangedAttack, 2),
            Bonus("战栗（强韧）", BonusType.Penalty, BonusTarget.Fortitude, 2),
            Bonus("战栗（反射）", BonusType.Penalty, BonusTarget.Reflex, 2),
            Bonus("战栗（意志）", BonusType.Penalty, BonusTarget.Will, 2),
        ]),
        new("俯卧", [
            Bonus("俯卧（近战攻击）", BonusType.Penalty, BonusTarget.MeleeAttack, 4),
            Bonus("俯卧（AC 对近战）", BonusType.Penalty, BonusTarget.ArmorClass, 4, "对远程 AC +4 需手动处理"),
        ]),
        new("勇气激励", [
            Bonus("勇气激励（近战）", BonusType.Competence, BonusTarget.MeleeAttack, 2, "数值随等级变化，请自行调整"),
            Bonus("勇气激励（远程）", BonusType.Competence, BonusTarget.RangedAttack, 2, "数值随等级变化，请自行调整"),
            Bonus("勇气激励（伤害）", BonusType.Competence, BonusTarget.Damage, 2, "数值随等级变化，请自行调整"),
        ]),
        new("狂暴", [
            Bonus("狂暴（近战）", BonusType.Morale, BonusTarget.MeleeAttack, 2),
            Bonus("狂暴（伤害）", BonusType.Morale, BonusTarget.Damage, 2),
            Bonus("狂暴（意志）", BonusType.Morale, BonusTarget.Will, 2),
            Bonus("狂暴（AC 减值）", BonusType.Penalty, BonusTarget.ArmorClass, 2),
        ]),
        new("加速术", [
            Bonus("加速术（攻击）", BonusType.Untyped, BonusTarget.MeleeAttack, 1),
            Bonus("加速术（AC）", BonusType.Dodge, BonusTarget.ArmorClass, 1),
            Bonus("加速术（反射）", BonusType.Dodge, BonusTarget.Reflex, 1),
        ]),
        new("祈祷术", [
            Bonus("祈祷术（攻击）", BonusType.Luck, BonusTarget.MeleeAttack, 1),
            Bonus("祈祷术（伤害）", BonusType.Luck, BonusTarget.Damage, 1),
            Bonus("祈祷术（强韧）", BonusType.Luck, BonusTarget.Fortitude, 1),
            Bonus("祈祷术（反射）", BonusType.Luck, BonusTarget.Reflex, 1),
            Bonus("祈祷术（意志）", BonusType.Luck, BonusTarget.Will, 1),
        ]),
    ];
}
