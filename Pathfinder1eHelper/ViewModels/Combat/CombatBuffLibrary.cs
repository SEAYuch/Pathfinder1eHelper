using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pathfinder1eHelper.Models;
using Pathfinder1eHelper.Models.Combat;
using Pathfinder1eHelper.Services;

namespace Pathfinder1eHelper.ViewModels.Combat;

/// <summary>一次 Buff 解析结果：加值条目，以及命中的效果表行数（0 表示未收录）。</summary>
public sealed record BuffResolution(IReadOnlyList<BonusEntry> Entries, int MatchedCount);

/// <summary>
/// Buff 候选库：惰性、一次性（并缓存）载入法术与专长目录供本地搜索，并把
/// <c>spell_buffs</c> / <c>feat_buffs</c> 行解析为战斗加值条目。载入与查询均在后台线程执行。
/// </summary>
public sealed class CombatBuffLibrary(ISpellService spells, IFeatService feats)
{
    private const int CandidateLimit = 10000;

    private readonly ISpellService _spells = spells;
    private readonly IFeatService _feats = feats;

    /// <summary>供 AutoCompleteBox 本地过滤的候选（法术 + 专长，载入后填充）。</summary>
    public ObservableCollection<BuffCandidate> Candidates { get; } = [];

    public bool IsLoaded { get; private set; }

    /// <summary>首次调用时载入全部法术与专长候选；后续调用直接返回。</summary>
    public async Task EnsureLoadedAsync(CancellationToken ct = default)
    {
        if (IsLoaded)
        {
            return;
        }

        var spells = await Task.Run(
            () => _spells.SearchAsync(new SpellQuery(null, null, null, 0, CandidateLimit), ct),
            ct).ConfigureAwait(true);
        var feats = await Task.Run(
            () => _feats.SearchAsync(new FeatQuery(null, null, null, null, 0, CandidateLimit), ct),
            ct).ConfigureAwait(true);

        ct.ThrowIfCancellationRequested();

        Candidates.Clear();
        foreach (var spell in spells)
        {
            Candidates.Add(new BuffCandidate(spell.NameEn, spell.NameZh, spell.Source, BuffCandidateKind.Spell));
        }

        foreach (var feat in feats)
        {
            Candidates.Add(new BuffCandidate(feat.NameEn, feat.NameZh, feat.Source, BuffCandidateKind.Feat));
        }

        IsLoaded = true;
    }

    /// <summary>把所选候选的效果解析为加值条目；未收录时给出一条空白可编辑条目。</summary>
    public async Task<BuffResolution> ResolveAsync(BuffCandidate candidate, int casterLevel, CancellationToken ct = default)
    {
        var sourceLabel = $"《{candidate.Source}》{candidate.NameEn}";
        return candidate.Kind switch
        {
            BuffCandidateKind.Feat => await ResolveFeatAsync(candidate, casterLevel, sourceLabel, ct),
            _ => await ResolveSpellAsync(candidate, casterLevel, sourceLabel, ct),
        };
    }

    private async Task<BuffResolution> ResolveSpellAsync(BuffCandidate c, int casterLevel, string sourceLabel, CancellationToken ct)
    {
        var buffs = await Task.Run(() => _spells.GetBuffsForSpellAsync(c.NameEn, c.NameZh, ct), ct).ConfigureAwait(true);
        if (buffs.Count == 0)
        {
            return Fallback(c, sourceLabel, BonusOrigin.SpellBuff, "spell_buffs 未收录");
        }

        var entries = buffs.Select(b => SpellBuffResolver.ToEntry(b, casterLevel, sourceLabel, BonusOrigin.SpellBuff)).ToList();
        return new BuffResolution(entries, buffs.Count);
    }

    private async Task<BuffResolution> ResolveFeatAsync(BuffCandidate c, int casterLevel, string sourceLabel, CancellationToken ct)
    {
        var buffs = await Task.Run(() => _feats.GetBuffsForFeatAsync(c.NameEn, c.NameZh, ct), ct).ConfigureAwait(true);
        if (buffs.Count == 0)
        {
            return Fallback(c, sourceLabel, BonusOrigin.FeatBuff, "feat_buffs 未收录");
        }

        var entries = buffs.Select(b => SpellBuffResolver.ToEntry(b, casterLevel, sourceLabel, BonusOrigin.FeatBuff)).ToList();
        return new BuffResolution(entries, buffs.Count);
    }

    private static BuffResolution Fallback(BuffCandidate c, string sourceLabel, BonusOrigin origin, string reason)
    {
        var name = string.IsNullOrWhiteSpace(c.NameZh) ? c.NameEn ?? string.Empty : c.NameZh;
        return new BuffResolution(
            [
                new BonusEntry
                {
                    Origin = origin,
                    Name = name,
                    Type = BonusType.Untyped,
                    Target = BonusTarget.ArmorClass,
                    Value = 1,
                    Notes = $"{sourceLabel}：{reason}，请手动设置类型/目标/数值",
                },
            ],
            0);
    }
}
