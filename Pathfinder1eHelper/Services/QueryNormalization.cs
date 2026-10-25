namespace Pathfinder1eHelper.Services;

/// <summary>
/// 三类目录查询（法术/怪物/专长）共用的文本与分页归整规则。
/// 空白筛选值统一归 <c>null</c>（= 不筛选），避免仅含空格的输入被当成精确匹配条件。
/// </summary>
internal static class QueryNormalization
{
    /// <summary>去首尾空白；空或纯空白返回 <c>null</c>（不筛选）。</summary>
    public static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>负的分页偏移归零。</summary>
    public static int Skip(int skip) => skip < 0 ? 0 : skip;

    /// <summary>非正的页大小回落到 <paramref name="defaultPageSize"/>。</summary>
    public static int Take(int take, int defaultPageSize) => take <= 0 ? defaultPageSize : take;
}
