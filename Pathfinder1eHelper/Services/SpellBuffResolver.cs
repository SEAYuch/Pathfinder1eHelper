using System;
using System.Linq;
using Pathfinder1eHelper.Models;
using Pathfinder1eHelper.Models.Combat;

namespace Pathfinder1eHelper.Services;

/// <summary>
/// 把 <c>spell_buffs</c> / <c>feat_buffs</c> 表中的一行转成战斗修饰条目；<c>scale_*</c> 非空时按施法者等级缩放：
/// value = clamp(scale_base + floor((CL - scale_offset) / scale_step), scale_min, scale_max)。
/// </summary>
internal static class SpellBuffResolver
{
    public static ModifierEntry ToEntry(IBuffEffect buff, int casterLevel, string sourceLabel, BonusOrigin origin) => new()
    {
        Origin = origin,
        Name = buff.EffectName,
        Kind = Parse(buff.Kind, ModifierKind.Normal),
        Descriptor = Parse(buff.BonusType, ModifierDescriptor.None),
        Stat = Parse(buff.Target, CombatStat.ArmorClass),
        Ability = ParseNullable<Ability>(buff.Ability),
        Value = ResolveValue(buff, casterLevel),
        Notes = Compose(sourceLabel, buff.Notes),
    };

    private static int ResolveValue(IBuffEffect buff, int casterLevel)
    {
        if (buff.ScaleStep is int step and > 0)
        {
            var baseValue = buff.ScaleBase ?? buff.Value ?? 0;
            var offset = buff.ScaleOffset ?? 0;
            var value = baseValue + (int)Math.Floor((casterLevel - offset) / (double)step);
            return Clamp(value, buff.ScaleMin, buff.ScaleMax);
        }

        return buff.Value ?? 0;
    }

    /// <summary>
    /// 按显式上下界夹取；未提供界时不限制（保留符号，允许减值法术）。
    /// 当数据异常出现 <paramref name="min"/> &gt; <paramref name="max"/> 时不夹取，避免 <see cref="Math.Clamp"/> 抛错。
    /// </summary>
    private static int Clamp(int value, int? min, int? max)
    {
        if (min is { } lower && max is { } upper)
        {
            return lower <= upper ? Math.Clamp(value, lower, upper) : value;
        }

        if (min is { } floor)
        {
            return Math.Max(floor, value);
        }

        if (max is { } ceiling)
        {
            return Math.Min(ceiling, value);
        }

        return value;
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
