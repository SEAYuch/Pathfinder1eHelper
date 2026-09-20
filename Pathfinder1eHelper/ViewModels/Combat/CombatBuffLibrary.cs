using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pathfinder1eHelper.Models;
using Pathfinder1eHelper.Models.Combat;
using Pathfinder1eHelper.Services;

namespace Pathfinder1eHelper.ViewModels.Combat;

/// <summary>一次法术 Buff 解析结果：加值条目，以及命中的 <c>spell_buffs</c> 行数（0 表示未收录）。</summary>
public sealed record BuffResolution(IReadOnlyList<BonusEntry> Entries, int MatchedCount);

/// <summary>
/// 法术 Buff 候选库：惰性、一次性（并缓存）载入法术目录供本地搜索，
/// 并把 <c>spell_buffs</c> 行解析为战斗加值条目。载入与查询均在后台线程执行。
/// </summary>
public sealed class CombatBuffLibrary(ISpellService spells)
{
    private const int CandidateLimit = 10000;

    private readonly ISpellService _spells = spells;

    /// <summary>供 AutoCompleteBox 本地过滤的法术候选（载入后填充）。</summary>
    public ObservableCollection<Spell> Candidates { get; } = [];

    public bool IsLoaded { get; private set; }

    /// <summary>首次调用时载入全部法术候选；后续调用直接返回。</summary>
    public async Task EnsureLoadedAsync(CancellationToken ct = default)
    {
        if (IsLoaded)
        {
            return;
        }

        var all = await Task.Run(
            () => _spells.SearchAsync(new SpellQuery(null, null, null, 0, CandidateLimit), ct),
            ct).ConfigureAwait(true);

        ct.ThrowIfCancellationRequested();

        Candidates.Clear();
        foreach (var spell in all)
        {
            Candidates.Add(spell);
        }

        IsLoaded = true;
    }

    /// <summary>把所选法术的 <c>spell_buffs</c> 效果解析为加值条目；未收录时给出一条空白可编辑条目。</summary>
    public async Task<BuffResolution> ResolveAsync(Spell spell, int casterLevel, CancellationToken ct = default)
    {
        var sourceLabel = $"《{spell.Source}》{spell.NameEn}";
        var buffs = await Task.Run(
            () => _spells.GetBuffsForSpellAsync(spell.NameEn, spell.NameZh, ct),
            ct).ConfigureAwait(true);

        if (buffs.Count == 0)
        {
            return new BuffResolution(
                [
                    new BonusEntry
                    {
                        Origin = BonusOrigin.SpellBuff,
                        Name = spell.NameZh,
                        Type = BonusType.Untyped,
                        Target = BonusTarget.ArmorClass,
                        Value = 1,
                        Notes = $"{sourceLabel}：spell_buffs 未收录，请手动设置类型/目标/数值",
                    },
                ],
                0);
        }

        var entries = buffs
            .Select(b => SpellBuffResolver.ToEntry(b, casterLevel, sourceLabel))
            .ToList();
        return new BuffResolution(entries, buffs.Count);
    }
}
