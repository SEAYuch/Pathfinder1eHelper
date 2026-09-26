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

    /// <summary>条目类型（<c>ModifierKind</c> 枚举名，可空；空=Normal）。用于「猛力攻击」这类按 BAB/握法计算的 buff。</summary>
    string? Kind { get; }

    int? Value { get; }

    /// <summary>旧「增强对象」列（<c>enhancement_subject</c>）；新模型已折叠进 <see cref="BonusType"/>，运行时不使用。</summary>
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
