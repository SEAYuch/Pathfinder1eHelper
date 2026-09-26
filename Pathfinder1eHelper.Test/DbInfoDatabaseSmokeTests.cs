using System;
using System.Threading.Tasks;
using Pathfinder1eHelper.Data;
using Pathfinder1eHelper.Infrastructure;
using Pathfinder1eHelper.Services;

namespace Pathfinder1eHelper.Test;

/// <summary>
/// 参考库 <c>db_info</c> 盖章行的集成冒烟测试（连真实 duckdb；文件缺失时动态跳过）。
/// </summary>
/// <remarks>
/// 重点是那条跨边界的契约：<c>db_info.app_version</c> 必须等于运行期程序集的
/// <c>AppInfo.Version</c>。二者同源于 <c>Directory.Build.props</c>，所以一旦出现不等，
/// 说明参考库是旧版本代码盖章后就没再重新执行 <c>--stamp-db</c>——这正是打包分发后
/// 最容易出现、也最难排查的漂移。
/// </remarks>
public class DbInfoDatabaseSmokeTests
{
    [Fact]
    public async Task Stamped_db_info_matches_the_running_assembly_version()
    {
        TestDatabase.SkipIfUnavailable();
        var fsql = FreeSqlFactory.CreateReadOnly(TestDatabase.Path);
        try
        {
            var info = await DbInfoCatalog.ReadAsync(fsql);

            Assert.NotNull(info);
            Assert.Equal(DbInfoCatalog.SingletonId, info!.Id);
            Assert.Equal(AppInfo.ReferenceProduct, info.Product);
            Assert.Equal(AppInfo.Version, info.AppVersion);
            Assert.Equal(AppInfo.ReferenceSchemaVersion, info.SchemaVersion);
            Assert.InRange(info.ContentVersion, 1, 999_999);
            Assert.NotNull(info.DataBuiltAt);

            // row_counts 快照：每项形如 "table=123"，用于离线核对数据完整性。
            Assert.False(string.IsNullOrWhiteSpace(info.RowCounts));
            Assert.Contains("spells=", info.RowCounts);
            Assert.Contains("feats=", info.RowCounts);
            Assert.Contains("monsters=", info.RowCounts);
        }
        finally
        {
            fsql.Dispose();
        }
    }

    /// <summary>
    /// 盖章行的行数快照必须与实际表一致——否则说明盖章之后数据又被重建过，
    /// <c>data_built_at</c> 与 <c>row_counts</c> 已失去参考价值。
    /// </summary>
    [Fact]
    public async Task Row_count_snapshot_agrees_with_the_actual_tables()
    {
        TestDatabase.SkipIfUnavailable();
        var fsql = FreeSqlFactory.CreateReadOnly(TestDatabase.Path);
        try
        {
            var info = await DbInfoCatalog.ReadAsync(fsql);
            Assert.NotNull(info);

            var snapshot = ParseRowCounts(info!.RowCounts!);

            // 与各 SmokeTests 里已锁定的主表行数下限对照（只取主表，索引表不做阈值断言）。
            Assert.Equal(await fsql.Select<Models.Spell>().CountAsync(), snapshot["spells"]);
            Assert.Equal(await fsql.Select<Models.Feat>().CountAsync(), snapshot["feats"]);
            Assert.Equal(await fsql.Select<Models.Monster>().CountAsync(), snapshot["monsters"]);
        }
        finally
        {
            fsql.Dispose();
        }
    }

    /// <summary>解析 <c>"spells=3373; feats=1639"</c> 形式的快照。</summary>
    private static System.Collections.Generic.Dictionary<string, long> ParseRowCounts(string raw)
    {
        var result = new System.Collections.Generic.Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var pair in raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = pair.IndexOf('=');
            Assert.True(separator > 0, $"malformed row_counts entry: '{pair}'");
            result[pair[..separator]] = long.Parse(pair[(separator + 1)..], System.Globalization.CultureInfo.InvariantCulture);
        }

        return result;
    }
}
