using System;

namespace Pathfinder1eHelper.Models.Combat;

/// <summary>
/// 一条手动录入的加值/减值。同类型加值按 PF1 规则叠加（取高或求和，见计算引擎）；
/// <see cref="BonusType.Penalty"/> 表示无类型减值，<see cref="Value"/> 存正数，计算时取负。
/// </summary>
public sealed class BonusEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>来源：手动 / 法术 Buff / 状态预设（见 <see cref="BonusOrigin"/>）。</summary>
    public BonusOrigin Origin { get; set; } = BonusOrigin.Manual;

    public string Name { get; set; } = "";

    public BonusType Type { get; set; } = BonusType.Untyped;

    /// <summary>仅当 <see cref="Type"/> 为 <see cref="BonusType.Enhancement"/> 时有意义。</summary>
    public EnhancementSubject Enhancement { get; set; } = EnhancementSubject.None;

    public BonusTarget Target { get; set; } = BonusTarget.ArmorClass;

    /// <summary>仅当 <see cref="Target"/> 为 <see cref="BonusTarget.AbilityScore"/> 时指定属性。</summary>
    public Ability? Ability { get; set; }

    public int Value { get; set; } = 1;

    public bool IsEnabled { get; set; } = true;

    /// <summary>环境加值的“同源”标记：同源取高，异源叠加。</summary>
    public string? SourceGroup { get; set; }

    public string? Notes { get; set; }
}
