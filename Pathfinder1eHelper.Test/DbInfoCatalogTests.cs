using System;
using System.Threading.Tasks;
using Pathfinder1eHelper.Data;
using Pathfinder1eHelper.Services;

namespace Pathfinder1eHelper.Test;

/// <summary>
/// <see cref="DbInfoCatalog"/> 在「参考库尚未盖章」时的降级行为。
/// </summary>
/// <remarks>
/// 这条路径很容易在真实分发场景里被触发：老版本的库（没有 <c>db_info</c> 表）被新版本应用读到。
/// 此时应用必须照常启动 —— 缺元数据只该降级为「未盖章」，绝不能抛异常把用户挡在门外。
/// 用临时库而非真实参考库来测，避免污染 <c>data/</c>。
/// </remarks>
public class DbInfoCatalogTests
{
    private static string CreateEmptyDatabase()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"p1-nostamp-{Guid.NewGuid():N}.duckdb");
        var fsql = FreeSqlFactory.CreateReadOnly(path);
        fsql.Dispose();
        return path;
    }

    [Fact]
    public async Task ReadAsync_returns_null_when_the_table_does_not_exist()
    {
        var path = CreateEmptyDatabase();
        try
        {
            // 只读连接打开一个不存在的文件会得到空库；表自然也不存在。
            var fsql = FreeSqlFactory.CreateReadOnly(path);
            try
            {
                var info = await DbInfoCatalog.ReadAsync(fsql);
                Assert.Null(info);
            }
            finally
            {
                fsql.Dispose();
            }
        }
        finally
        {
            System.IO.File.Delete(path);
            System.IO.File.Delete(path + ".wal");
        }
    }

    /// <summary>盖章 → 再读，往返一次必须能完整取回写入的值。</summary>
    [Fact]
    public async Task Stamp_then_read_round_trips()
    {
        var path = CreateEmptyDatabase();
        try
        {
            var writable = DbInfoCatalog.OpenForStamping(path);
            try
            {
                // 空库没有主表，CountAsync 会失败；这属于「对不上的库」，不在本用例范围。
                // 这里只验证建表 + 写入路径本身能被调用到，先建一张主表。
                await writable.Ado.ExecuteNonQueryAsync(
                    "CREATE TABLE IF NOT EXISTS spells (id INTEGER PRIMARY KEY, name_en VARCHAR);");
                await DbInfoCatalog.StampAsync(writable, contentVersion: 7, notes: "往返测试");
            }
            finally
            {
                writable.Dispose();
            }

            var readOnly = FreeSqlFactory.CreateReadOnly(path);
            try
            {
                var info = await DbInfoCatalog.ReadAsync(readOnly);
                Assert.NotNull(info);
                Assert.Equal(DbInfoCatalog.SingletonId, info!.Id);
                Assert.Equal(7, info.ContentVersion);
                Assert.Equal("往返测试", info.Notes);
                Assert.False(string.IsNullOrWhiteSpace(info.AppVersion));
            }
            finally
            {
                readOnly.Dispose();
            }
        }
        finally
        {
            System.IO.File.Delete(path);
            System.IO.File.Delete(path + ".wal");
        }
    }

    /// <summary>重复盖章必须幂等：仍是单行，且不传 --content 时内容版本号不被冲回初始值。</summary>
    [Fact]
    public async Task Stamping_twice_keeps_a_single_row_and_preserves_content_version()
    {
        var path = CreateEmptyDatabase();
        try
        {
            var writable = DbInfoCatalog.OpenForStamping(path);
            try
            {
                await writable.Ado.ExecuteNonQueryAsync(
                    "CREATE TABLE IF NOT EXISTS spells (id INTEGER PRIMARY KEY, name_en VARCHAR);");
                await DbInfoCatalog.StampAsync(writable, contentVersion: 3, notes: null);

                // 第二次不指定 contentVersion：应沿用 3，而不是回到默认的 1。
                await DbInfoCatalog.StampAsync(writable, contentVersion: 0, notes: null);

                var all = await writable.Select<Models.DbInfo>().ToListAsync();
                Assert.Single(all);
                Assert.Equal(3, all[0].ContentVersion);
            }
            finally
            {
                writable.Dispose();
            }
        }
        finally
        {
            System.IO.File.Delete(path);
            System.IO.File.Delete(path + ".wal");
        }
    }
}
