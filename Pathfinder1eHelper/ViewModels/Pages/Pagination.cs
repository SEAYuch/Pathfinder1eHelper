using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Pathfinder1eHelper.ViewModels.Pages;

/// <summary>一页查询结果：当前页条目与满足筛选条件的总数。</summary>
/// <typeparam name="T">列表项类型（法术/怪物/专长）。</typeparam>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total)
{
    /// <summary>本批次开始前已加载的项目数；首批为零。</summary>
    public int AlreadyLoaded { get; init; }

    /// <summary>是否还有未显示的匹配结果。</summary>
    public bool HasMore => AlreadyLoaded + Items.Count < Total;
}

/// <summary>
/// 法术/专长/怪物三个列表页共用的分页机制。只收敛真正同构的部分——短页省略总数查询、
/// 集合整体替换、结果摘要文案；各页继续保留自己的强类型查询、命令、筛选哨兵与选中项逻辑。
/// </summary>
public static class Pagination
{
    /// <summary>默认每页条数。</summary>
    public const int DefaultPageSize = 200;

    /// <summary>“加载更多”每次递增的页大小。</summary>
    public const int PageSizeStep = 200;

    /// <summary>
    /// 查询一页并给出总数：结果不足一页时说明已到末尾，直接用结果数作为总数（省一次 Count 查询）；
    /// 否则才查询真实总数用于提示与“加载更多”。
    /// </summary>
    /// <param name="fetchPage">执行当前页查询。</param>
    /// <param name="countTotal">查询满足筛选条件的真实总数。</param>
    /// <param name="take">本次查询的页大小。</param>
    public static async Task<PagedResult<T>> FetchAsync<T>(
        Func<Task<IReadOnlyList<T>>> fetchPage,
        Func<Task<int>> countTotal,
        int take)
    {
        ArgumentNullException.ThrowIfNull(fetchPage);
        ArgumentNullException.ThrowIfNull(countTotal);

        var items = await fetchPage();
        var total = items.Count < take ? items.Count : await countTotal();
        return new PagedResult<T>(items, total);
    }

    /// <summary>查询下一段结果，短页时用已加载数量推导筛选结果总数。</summary>
    public static async Task<PagedResult<T>> FetchNextAsync<T>(
        Func<Task<IReadOnlyList<T>>> fetchPage,
        Func<Task<int>> countTotal,
        int take,
        int alreadyLoaded)
    {
        ArgumentNullException.ThrowIfNull(fetchPage);
        ArgumentNullException.ThrowIfNull(countTotal);

        var items = await fetchPage();
        var total = items.Count < take ? alreadyLoaded + items.Count : await countTotal();
        return new PagedResult<T>(items, total) { AlreadyLoaded = alreadyLoaded };
    }

    /// <summary>将一页结果追加到列表尾部，不清空既有项（保留选择与滚动锚点）。</summary>
    public static void Append<T>(ObservableCollection<T> target, IReadOnlyList<T> items)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(items);

        foreach (var item in items)
        {
            target.Add(item);
        }
    }

    /// <summary>用一页结果整体替换列表内容（保持集合实例，供绑定继续使用）。</summary>
    public static void Replace<T>(ObservableCollection<T> target, IReadOnlyList<T> items)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(items);

        target.Clear();
        foreach (var item in items)
        {
            target.Add(item);
        }
    }

    /// <summary>结果摘要：未显示完时附“已显示前 N 条”。</summary>
    /// <param name="noun">条目名词，如“法术”。</param>
    /// <param name="total">满足筛选条件的总数。</param>
    /// <param name="shown">当前已显示的条目数。</param>
    public static string Summarize(string noun, int total, int shown) =>
        shown < total
            ? $"共 {total} 条{noun}，已显示前 {shown} 条"
            : $"共 {total} 条{noun}";
}
