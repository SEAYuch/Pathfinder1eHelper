using Pathfinder1eHelper.ViewModels.Pages;

namespace Pathfinder1eHelper.Test;

/// <summary>列表页分页公共逻辑：短页省略总数查询、结果摘要与集合替换。</summary>
public class PaginationTests
{
    [Fact]
    public async Task Short_page_skips_the_count_query()
    {
        var counted = false;
        var page = await Pagination.FetchAsync<int>(
            () => Task.FromResult<IReadOnlyList<int>>([1, 2]),
            () =>
            {
                counted = true;
                return Task.FromResult(99);
            },
            take: 200);

        Assert.False(counted);
        Assert.Equal(2, page.Total);
        Assert.False(page.HasMore);
    }

    [Fact]
    public async Task Full_page_queries_the_true_total()
    {
        var page = await Pagination.FetchAsync<int>(
            () => Task.FromResult<IReadOnlyList<int>>([1, 2]),
            () => Task.FromResult(57),
            take: 2);

        Assert.Equal(57, page.Total);
        Assert.True(page.HasMore);
    }

    [Fact]
    public async Task Empty_result_reports_zero_without_counting()
    {
        var counted = false;
        var page = await Pagination.FetchAsync<int>(
            () => Task.FromResult<IReadOnlyList<int>>([]),
            () =>
            {
                counted = true;
                return Task.FromResult(5);
            },
            take: 200);

        Assert.False(counted);
        Assert.Equal(0, page.Total);
        Assert.False(page.HasMore);
    }

    [Fact]
    public void Replace_clears_and_refills_the_existing_collection()
    {
        var target = new System.Collections.ObjectModel.ObservableCollection<int> { 9 };
        var raised = 0;
        target.CollectionChanged += (_, _) => raised++;

        Pagination.Replace(target, [1, 2, 3]);

        Assert.Equal([1, 2, 3], target);
        Assert.True(raised > 0);
    }

    [Fact]
    public void Summarize_appends_shown_count_only_when_more_remain()
    {
        Assert.Equal("共 3 条法术，已显示前 2 条", Pagination.Summarize("法术", 3, 2));
        Assert.Equal("共 3 条法术", Pagination.Summarize("法术", 3, 3));
    }
}
