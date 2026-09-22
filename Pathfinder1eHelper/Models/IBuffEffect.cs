namespace Pathfinder1eHelper.Models;

/// <summary>
/// 加值效果表行的共享契约（<c>spell_buffs</c> / <c>feat_buffs</c> 列同构），
/// 供 <c>SpellBuffResolver</c> 统一解析为战斗加值条目。
/// </summary>
public interface IBuffEffect
{
    string EffectName { get; }

    /// <summary>加值类型（<c>BonusType</c> 枚举名）。</summary>
    string BonusType { get; }

    /// <summary>作用目标（<c>BonusTarget</c> 枚举名）。</summary>
    string Target { get; }

    int? Value { get; }

    /// <summary>增强对象（<c>EnhancementSubject</c> 枚举名，可空）。</summary>
    string? EnhancementSubject { get; }

    /// <summary>属性（<c>Ability</c> 枚举名，可空）。</summary>
    string? Ability { get; }

    int? ScaleBase { get; }
    int? ScaleOffset { get; }
    int? ScaleStep { get; }
    int? ScaleMin { get; }
    int? ScaleMax { get; }
    string? Notes { get; }
}
