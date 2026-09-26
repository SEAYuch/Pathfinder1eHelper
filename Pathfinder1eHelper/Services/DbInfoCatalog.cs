using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FreeSql;
using Pathfinder1eHelper.Infrastructure;
using Pathfinder1eHelper.Models;

namespace Pathfinder1eHelper.Services;

/// <summary>
/// 参考库 <c>db_info</c> 元数据表的建表、盖章（写）与读取（只读）。
/// </summary>
/// <remarks>
/// 这是全应用<b>唯一</b>允许对参考库写入的代码路径，且只在 <c>--stamp-db</c> 维护模式下被调用
/// （见 <c>MaintenanceMode</c>）。UI 启动路径只经 <see cref="ReadAsync"/> 读，不写。
/// <para>
/// 之所以用应用自身而不是 <c>scripts/build-*-db</c> 来盖章：版本号是应用侧的事实
/// （来自 <c>Directory.Build.props</c> 的 <c>&lt;Version&gt;</c>），让 DB 与程序集从同一处取值，
/// 才能保证 <c>db_info.app_version == AppInfo.Version</c> 恒成立，不会出现「改了程序集忘了改脚本」的漂移。
/// </para>
/// </remarks>
public static class DbInfoCatalog
{
    /// <summary>单行表主键值。</summary>
    public const int SingletonId = 1;

    /// <summary>
    /// 建表 DDL。<c>IF NOT EXISTS</c> 保证可重复执行；不 <c>DROP</c>，避免误删已写入的盖章信息。
    /// </summary>
    private const string CreateTableSql = """
        CREATE TABLE IF NOT EXISTS db_info (
          id              INTEGER PRIMARY KEY,
          product         VARCHAR NOT NULL,
          app_version     VARCHAR NOT NULL,
          schema_version  INTEGER NOT NULL,
          content_version INTEGER NOT NULL,
          data_built_at   TIMESTAMP,
          row_counts      VARCHAR,
          notes           VARCHAR
        );
        """;

    /// <summary>
    /// 打开一个<b>可写</b>的连接，仅供维护模式盖章使用。刻意不复用
    /// <see cref="Data.FreeSqlFactory"/>：那条路径带 <c>ACCESS_MODE=READ_ONLY</c> 三重防护，
    /// 不应被任何可能误用的地方拿到。
    /// </summary>
    public static IFreeSql OpenForStamping(string dbPath) =>
        new FreeSqlBuilder()
            .UseConnectionString(DataType.DuckDB, $"DataSource={dbPath}")
            .UseAutoSyncStructure(false)
            .Build();

    /// <summary>
    /// 把当前应用版本盖到参考库里（幂等）。<paramref name="contentVersion"/> 为 0 时沿用库中已有值，
    /// 这样重复执行不会把内容版本号冲回初始值。
    /// </summary>
    public static async Task StampAsync(
        IFreeSql writable,
        int contentVersion,
        string? notes,
        CancellationToken ct = default)
    {
        await writable.Ado.ExecuteNonQueryAsync(CreateTableSql, ct).ConfigureAwait(false);

        var existing = await writable.Select<DbInfo>()
            .Where(a => a.Id == SingletonId)
            .FirstAsync(ct)
            .ConfigureAwait(false);

        // 幂等写入：先删后插，包在一个事务里。
        // 刻意用 FreeSql 的实体 API（Delete/Insert）而不是拼裸 SQL 带参数——本版本的
        // FreeSql+DuckDB 组合下，Ado 的 object/DbParameter[] 重载不会把占位符绑定到
        // DuckDB 的 $n 语法上（会报 "Referenced column not found" 或 "Invalid number of
        // parameters"）。实体 API 自行生成正确的参数化语句，且免去手写列名转义。
        var row = new DbInfo
        {
            Id = SingletonId,
            Product = AppInfo.ReferenceProduct,
            AppVersion = AppInfo.Version,
            SchemaVersion = AppInfo.ReferenceSchemaVersion,
            ContentVersion = contentVersion > 0 ? contentVersion : existing?.ContentVersion ?? 1,
            DataBuiltAt = DateTime.UtcNow,
            RowCounts = await CollectRowCountsAsync(writable, ct).ConfigureAwait(false),
            Notes = notes ?? existing?.Notes,
        };

        // fsql.Transaction 只提供同步重载，内部已含 Begin/Commit/Dispose 异常处理；
        // 这里在事务外 await 计数查询，事务内只做同步 IO，避免在事务中混用两种模型。
        writable.Transaction(() =>
        {
            writable.Delete<DbInfo>().Where(a => a.Id == SingletonId).ExecuteAffrows();
            writable.Insert(row).ExecuteAffrows();
        });

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    /// 读取盖章行。表不存在（老库未盖章）或读失败时返回 <c>null</c>，而不是抛异常——
    /// 元数据缺失不应让应用起不来，调用方按「未盖章」降级处理。
    /// </summary>
    public static async Task<DbInfo?> ReadAsync(IFreeSql readOnly, CancellationToken ct = default)
    {
        try
        {
            return await readOnly.Select<DbInfo>()
                .Where(a => a.Id == SingletonId)
                .FirstAsync(ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AppLog.Error("DbInfoCatalog.ReadAsync", ex);
            return null;
        }
    }

    /// <summary>
    /// 行数快照，形如 <c>spells=3373; feats=1639; monsters=838; ...</c>，供离线核对数据完整性。
    /// </summary>
    /// <remarks>
    /// 走各表已映射实体的 <c>CountAsync()</c> 而非拼裸 SQL：表名全部来自下方常量表，
    /// 不存在注入面，同时保持与 <c>SpellRepository</c> 等一致的实体映射路径。
    /// <para>
    /// 单表计数失败（表不存在等）只记录该项为 <c>missing</c> 而不中断整体——行数快照是
    /// <b>诊断辅助</b>，不该反过来把盖章流程搞失败：部分构建的库（例如只导了法术没导专长）
    /// 同样需要能盖上版本章。
    /// </para>
    /// </remarks>
    private static async Task<string> CollectRowCountsAsync(IFreeSql writable, CancellationToken ct)
    {
        var parts = new List<string>(8)
        {
            await CountAsync<Spell>("spells", writable, fsql => fsql.Select<Spell>(), ct).ConfigureAwait(false),
            await CountAsync<SpellLevel>("spell_levels", writable, fsql => fsql.Select<SpellLevel>(), ct).ConfigureAwait(false),
            await CountAsync<SpellBuff>("spell_buffs", writable, fsql => fsql.Select<SpellBuff>(), ct).ConfigureAwait(false),
            await CountAsync<Feat>("feats", writable, fsql => fsql.Select<Feat>(), ct).ConfigureAwait(false),
            await CountAsync<FeatBuff>("feat_buffs", writable, fsql => fsql.Select<FeatBuff>(), ct).ConfigureAwait(false),
            await CountAsync<Monster>("monsters", writable, fsql => fsql.Select<Monster>(), ct).ConfigureAwait(false),
            await CountAsync<MonsterGroup>("monster_groups", writable, fsql => fsql.Select<MonsterGroup>(), ct).ConfigureAwait(false),
            await CountAsync<MonsterGroupMember>("monster_group_members", writable, fsql => fsql.Select<MonsterGroupMember>(), ct).ConfigureAwait(false),
        };

        return string.Join("; ", parts);
    }

    private static async Task<string> CountAsync<T>(string table, IFreeSql writable, Func<IFreeSql, ISelect<T>> selector, CancellationToken ct)
        where T : class
    {
        try
        {
            var count = await selector(writable).CountAsync(ct).ConfigureAwait(false);
            return $"{table}={count.ToString(CultureInfo.InvariantCulture)}";
        }
        catch (Exception ex)
        {
            AppLog.Error($"DbInfoCatalog.CountAsync({table})", ex);
            return $"{table}=missing";
        }
    }
}
