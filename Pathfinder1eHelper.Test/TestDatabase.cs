using System.IO;

namespace Pathfinder1eHelper.Test;

/// <summary>
/// 真实参考库的位置与可用性。数据文件不在版本库中（见 .gitignore），因此冒烟测试在缺失时
/// 动态跳过（xUnit v3 动态跳过），使 CI 在没有数据文件时仍能跑通其余测试。
/// </summary>
internal static class TestDatabase
{
    public static string Path { get; } =
        System.IO.Path.Combine(AppContext.BaseDirectory, "data", "pathfinder1e.duckdb");

    public static bool IsAvailable => File.Exists(Path);

    /// <summary>数据库缺失时跳过当前测试。</summary>
    public static void SkipIfUnavailable()
    {
        if (!IsAvailable)
        {
            Assert.Skip($"Reference database not found at '{Path}'; skipping integration smoke test.");
        }
    }
}
