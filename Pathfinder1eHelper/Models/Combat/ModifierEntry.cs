using System;

namespace Pathfinder1eHelper.Models.Combat;

/// <summary>
/// 一条玩家录入/联动生成的加值修饰。值是带符号的；叠加由
/// <see cref="Descriptor"/> 与 <see cref="StackMode"/> 决定，作用通道由 <see cref="Stat"/> 决定。
/// 取代旧模型中的 BonusType/BonusTarget/EnhancementSubject 三元组。
/// </summary>
public sealed class ModifierEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>来源：手动 / 法术 Buff / 专长 Buff / 状态预设。</summary>
    public BonusOrigin Origin { get; set; } = BonusOrigin.Manual;

    public string Name { get; set; } = string.Empty;

    /// <summary>条目类型（普通 / 猛力攻击）。</summary>
    public ModifierKind Kind { get; set; } = ModifierKind.Normal;

    /// <summary>叠加分组键（对应 DLL <c>ModifierDescriptor</c>）。</summary>
    public ModifierDescriptor Descriptor { get; set; } = ModifierDescriptor.None;

    /// <summary>作用通道（对应 DLL <c>StatType</c> 的战斗子集）。</summary>
    public CombatStat Stat { get; set; } = CombatStat.ArmorClass;

    /// <summary>仅当 <see cref="Stat"/> 为 <see cref="CombatStat.AbilityScore"/> 时有意义。</summary>
    public Ability? Ability { get; set; }

    /// <summary>武器归属：仅作用于该武器（用于武器专攻/专精等）；null 表示作用于全部武器。</summary>
    public Guid? WeaponId { get; set; }

    public int Value { get; set; } = 1;

    /// <summary>叠加模式覆盖（默认按描述符规则）。</summary>
    public StackMode StackMode { get; set; } = StackMode.Default;

    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 战斗机动标记：非空表示本条是姿态条目（防御式战斗/全防御），
    /// 计算层据此处理姿态互斥（CHM：全防御时无法从「寓守于攻」获益）。普通条目为 null。
    /// </summary>
    public CombatStance? Stance { get; set; }

    public string? Notes { get; set; }

    /// <summary>转换为计算层修饰。</summary>
    public Modifier ToModifier() => new(Descriptor, Value, StackMode, Name);
}
