using System;
using System.Linq;
using Pathfinder1eHelper.Models;
using Pathfinder1eHelper.Models.Combat;

namespace Pathfinder1eHelper.Services;

/// <summary>
/// 把 <c>spell_buffs</c> 表中的一行转成战斗页的加值条目；<c>scale_*</c> 非空时按施法者等级缩放：
/// value = clamp(scale_base + floor((CL - scale_offset) / scale_step), scale_min, scale_max)。
/// </summary>
internal static class SpellBuffResolver
{
    public static BonusEntry ToEntry(SpellBuff buff, int casterLevel, string sourceLabel) => new()
    {
        Origin = BonusOrigin.SpellBuff,
        Name = buff.EffectName,
        Type = Parse(buff.BonusType, BonusType.Untyped),
        Target = Parse(buff.Target, BonusTarget.ArmorClass),
        Enhancement = Parse(buff.EnhancementSubject, EnhancementSubject.None),
        Ability = ParseNullable<Ability>(buff.Ability),
        Value = ResolveValue(buff, casterLevel),
        Notes = Compose(sourceLabel, buff.Notes),
    };

    private static int ResolveValue(SpellBuff buff, int casterLevel)
    {
        if (buff.ScaleStep is int step and > 0)
        {
            var baseValue = buff.ScaleBase ?? buff.Value ?? 0;
            var offset = buff.ScaleOffset ?? 0;
            var value = baseValue + (int)Math.Floor((casterLevel - offset) / (double)step);
            return Math.Max(0, Math.Clamp(value, buff.ScaleMin ?? int.MinValue, buff.ScaleMax ?? int.MaxValue));
        }

        return Math.Max(0, buff.Value ?? 0);
    }

    private static string? Compose(string source, string? notes)
    {
        var text = string.Join("｜", new[] { source, notes }.Where(s => !string.IsNullOrWhiteSpace(s)));
        return text.Length == 0 ? null : text;
    }

    private static T Parse<T>(string? value, T fallback)
        where T : struct, Enum =>
        Enum.TryParse<T>(value, ignoreCase: true, out var parsed) ? parsed : fallback;

    private static T? ParseNullable<T>(string? value)
        where T : struct, Enum =>
        Enum.TryParse<T>(value, ignoreCase: true, out var parsed) ? parsed : null;
}
