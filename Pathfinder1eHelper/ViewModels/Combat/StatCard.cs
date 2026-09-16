using System.Collections.Generic;
using Pathfinder1eHelper.Models.Combat;

namespace Pathfinder1eHelper.ViewModels.Combat;

/// <summary>战斗数值卡：总计 + 组成明细。</summary>
public sealed class StatCard(string name, StatResult result, bool signed = true)
{
    public string Name { get; } = name;

    public int Total { get; } = result.Total;

    public string TotalDisplay { get; } = signed ? result.TotalDisplay : result.Total.ToString();

    public IReadOnlyList<Contribution> Contributions { get; } = result.Contributions;
}
