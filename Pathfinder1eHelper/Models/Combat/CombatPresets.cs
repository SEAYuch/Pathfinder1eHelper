using System.Collections.Generic;
using System.Linq;

namespace Pathfinder1eHelper.Models.Combat;

/// <summary>一组常用状态/战术加值，一键加入修饰明细（同描述符自会按规则叠加）。</summary>
public sealed record CombatPreset(string Name, IReadOnlyList<ModifierEntry> Entries)
{
    /// <summary>
    /// 产出一份可安全改动的条目副本。<see cref="Entries"/> 是全局共享的实例
    /// （<see cref="ModifierEntry"/> 可变），直接改动会污染所有预设与已保存的档案，
    /// 因此应用预设时必须走这里复制。
    /// </summary>
    public ModifierEntry[] CreateEntries() =>
        [.. Entries.Select(e => new ModifierEntry
        {
            Id = e.Id,
            Origin = BonusOrigin.Preset,
            Name = e.Name,
            Kind = e.Kind,
            Descriptor = e.Descriptor,
            Stat = e.Stat,
            Ability = e.Ability,
            WeaponId = e.WeaponId,
            Value = e.Value,
            StackMode = e.StackMode,
            IsEnabled = e.IsEnabled,
            Stance = e.Stance,
            Notes = e.Notes,
        })];
}

public static class CombatPresets
{
    private static ModifierEntry Bonus(
        string name,
        ModifierDescriptor descriptor,
        CombatStat stat,
        int value,
        string? notes = null) =>
        new() { Name = name, Descriptor = descriptor, Stat = stat, Value = value, Notes = notes };

    public static IReadOnlyList<CombatPreset> All { get; } =
    [
        new("冲锋", [
            Bonus("冲锋", ModifierDescriptor.None, CombatStat.Attack, 2),
            Bonus("冲锋（AC 减值）", ModifierDescriptor.Penalty, CombatStat.ArmorClass, -2),
        ]),
        new("夹击", [
            Bonus("夹击", ModifierDescriptor.None, CombatStat.Attack, 2),
        ]),
        new("居高临下", [
            Bonus("居高临下", ModifierDescriptor.None, CombatStat.Attack, 1),
        ]),
        new("掩蔽", [
            Bonus("掩蔽", ModifierDescriptor.Circumstance, CombatStat.ArmorClass, 4),
        ]),
        new("援助他人", [
            Bonus("援助他人", ModifierDescriptor.None, CombatStat.Attack, 2),
        ]),
        new("隐形", [
            Bonus("隐形（攻击）", ModifierDescriptor.None, CombatStat.Attack, 2),
        ]),
        new("战栗", [
            Bonus("战栗（攻击）", ModifierDescriptor.Penalty, CombatStat.Attack, -2),
            Bonus("战栗（强韧）", ModifierDescriptor.Penalty, CombatStat.Fortitude, -2),
            Bonus("战栗（反射）", ModifierDescriptor.Penalty, CombatStat.Reflex, -2),
            Bonus("战栗（意志）", ModifierDescriptor.Penalty, CombatStat.Will, -2),
        ]),
        new("俯卧", [
            Bonus("俯卧（攻击）", ModifierDescriptor.Penalty, CombatStat.Attack, -4),
            Bonus("俯卧（AC 对近战）", ModifierDescriptor.Penalty, CombatStat.ArmorClass, -4, "对远程 AC +4 需手动处理"),
        ]),
        new("防御式战斗", [
            new ModifierEntry
            {
                Name = "防御式战斗（AC）",
                Descriptor = ModifierDescriptor.Dodge,
                Stat = CombatStat.ArmorClass,
                Value = 2,
                Stance = CombatStance.DefensiveFighting,
            },
            new ModifierEntry
            {
                Name = "防御式战斗（攻击）",
                Descriptor = ModifierDescriptor.Penalty,
                Stat = CombatStat.Attack,
                Value = -4,
                Stance = CombatStance.DefensiveFighting,
            },
        ]),
        new("全防御", [
            new ModifierEntry
            {
                Name = "全防御（AC）",
                Descriptor = ModifierDescriptor.Dodge,
                Stat = CombatStat.ArmorClass,
                Value = 4,
                Stance = CombatStance.TotalDefense,
                Notes = "CHM：标准动作、1 轮内 AC +4 闪避；无法同时进行防御式攻击、借机攻击，也无法从「寓守于攻」获益",
            },
        ]),
        new("勇气激励", [
            Bonus("勇气激励（攻击）", ModifierDescriptor.Competence, CombatStat.Attack, 2, "数值随等级变化，请自行调整"),
            Bonus("勇气激励（伤害）", ModifierDescriptor.Competence, CombatStat.Damage, 2, "数值随等级变化，请自行调整"),
        ]),
        new("狂暴", [
            Bonus("狂暴（攻击）", ModifierDescriptor.Morale, CombatStat.Attack, 2),
            Bonus("狂暴（伤害）", ModifierDescriptor.Morale, CombatStat.Damage, 2),
            Bonus("狂暴（意志）", ModifierDescriptor.Morale, CombatStat.Will, 2),
            Bonus("狂暴（AC 减值）", ModifierDescriptor.Penalty, CombatStat.ArmorClass, -2),
        ]),
        new("猛力攻击", [
            new ModifierEntry
            {
                Name = "猛力攻击",
                Kind = ModifierKind.PowerAttack,
                Descriptor = ModifierDescriptor.UntypedStackable,
                Stat = CombatStat.Attack,
                Value = 0,
                Notes = "按 BAB 与握法自动计算（近战攻击减值换伤害）；停用/删除即取消",
            },
        ]),
        new("寓守于攻", [
            new ModifierEntry
            {
                Name = "寓守于攻",
                Kind = ModifierKind.CombatExpertise,
                Descriptor = ModifierDescriptor.Dodge,
                Stat = CombatStat.ArmorClass,
                Value = 0,
                Notes = "近战攻击与战技 −1 换 AC +1 闪避，两者随 BAB 每 +4 同步放大；停用/删除即取消",
            },
        ]),
        new("双武器格斗", [
            new ModifierEntry
            {
                Name = "双武器格斗",
                Kind = ModifierKind.TwoWeaponFighting,
                Descriptor = ModifierDescriptor.UntypedStackable,
                Stat = CombatStat.Attack,
                Value = 0,
                Notes = "双持（存在副手武器）时主手 −4 / 副手 −8，副手非轻型武器再 −2；额外攻击未建模",
            },
        ]),
    ];
}
