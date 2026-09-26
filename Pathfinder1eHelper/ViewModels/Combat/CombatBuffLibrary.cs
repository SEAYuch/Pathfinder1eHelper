using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pathfinder1eHelper.Models;
using Pathfinder1eHelper.Models.Combat;
using Pathfinder1eHelper.Services;

namespace Pathfinder1eHelper.ViewModels.Combat;

/// <summary>一次 Buff 解析结果：修饰条目，以及命中的效果表行数（0 表示未收录）。</summary>
public sealed record BuffResolution(IReadOnlyList<ModifierEntry> Entries, int MatchedCount);

/// <summary>
/// Buff 候选库：惰性、一次性（并缓存）载入法术与专长目录，分别供战斗页的“法术 Buff”“专长 Buff”
/// 两个模块本地搜索；并把 <c>spell_buffs</c> / <c>feat_buffs</c> 行解析为战斗加值条目。
/// 载入与查询均在后台线程执行。
/// </summary>
public sealed class CombatBuffLibrary(ISpellService spells, IFeatService feats)
{
    private const int CandidateLimit = 10000;

    private readonly ISpellService _spells = spells;
    private readonly IFeatService _feats = feats;

    /// <summary>“法术 Buff”模块的候选（载入后填充）。</summary>
    public ObservableCollection<Spell> SpellCandidates { get; } = [];

    /// <summary>“专长 Buff”模块的候选（载入后填充）。</summary>
    public ObservableCollection<Feat> FeatCandidates { get; } = [];

    public bool IsLoaded { get; private set; }

    /// <summary>首次调用时载入全部法术与专长候选；后续调用直接返回。</summary>
    public async Task EnsureLoadedAsync(CancellationToken ct = default)
    {
        if (IsLoaded)
        {
            return;
        }

        var spellList = await Task.Run(
            () => _spells.SearchAsync(new SpellQuery(null, null, null, 0, CandidateLimit), ct),
            ct).ConfigureAwait(true);
        var featList = await Task.Run(
            () => _feats.SearchAsync(new FeatQuery(null, null, null, null, 0, CandidateLimit), ct),
            ct).ConfigureAwait(true);

        ct.ThrowIfCancellationRequested();

        SpellCandidates.Clear();
        foreach (var spell in spellList)
        {
            SpellCandidates.Add(spell);
        }

        FeatCandidates.Clear();
        foreach (var feat in featList)
        {
            FeatCandidates.Add(feat);
        }

        IsLoaded = true;
    }

    /// <summary>把所选法术在 <c>spell_buffs</c> 中的效果解析为加值条目；未收录时给出一条空白可编辑条目。</summary>
    public async Task<BuffResolution> ResolveSpellAsync(Spell spell, int casterLevel, CancellationToken ct = default)
    {
        var sourceLabel = $"《{spell.Source}》{spell.NameEn}";
        var buffs = await Task.Run(
            () => _spells.GetBuffsForSpellAsync(spell.NameEn, spell.NameZh, ct),
            ct).ConfigureAwait(true);
        if (buffs.Count == 0)
        {
            return Fallback(spell.NameZh ?? spell.NameEn, sourceLabel, BonusOrigin.SpellBuff, "spell_buffs 未收录");
        }

        var entries = buffs
            .Select(b => SpellBuffResolver.ToEntry(b, casterLevel, sourceLabel, BonusOrigin.SpellBuff))
            .ToList();
        return new BuffResolution(entries, buffs.Count);
    }

    /// <summary>把所选专长在 <c>feat_buffs</c> 中的效果解析为加值条目；未收录时给出一条空白可编辑条目。</summary>
    public async Task<BuffResolution> ResolveFeatAsync(Feat feat, int casterLevel, CancellationToken ct = default)
    {
        var sourceLabel = $"《{feat.Source}》{feat.NameEn}";
        var buffs = await Task.Run(
            () => _feats.GetBuffsForFeatAsync(feat.NameEn, feat.NameZh, ct),
            ct).ConfigureAwait(true);
        if (buffs.Count == 0)
        {
            return Fallback(feat.NameZh ?? feat.NameEn, sourceLabel, BonusOrigin.FeatBuff, "feat_buffs 未收录");
        }

        var entries = buffs
            .Select(b => SpellBuffResolver.ToEntry(b, casterLevel, sourceLabel, BonusOrigin.FeatBuff))
            .ToList();
        return new BuffResolution(entries, buffs.Count);
    }

    private static BuffResolution Fallback(string? name, string sourceLabel, BonusOrigin origin, string reason)
    {
        var display = string.IsNullOrWhiteSpace(name) ? "未命名" : name;
        return new BuffResolution(
            [
                new ModifierEntry
                {
                    Origin = origin,
                    Name = display,
                    Descriptor = ModifierDescriptor.None,
                    Stat = CombatStat.ArmorClass,
                    Value = 1,
                    Notes = $"{sourceLabel}：{reason}，请手动设置类型/目标/数值",
                },
            ],
            0);
    }
}
