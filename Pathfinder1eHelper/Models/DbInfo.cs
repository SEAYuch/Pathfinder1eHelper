using System;
using FreeSql.DataAnnotations;

namespace Pathfinder1eHelper.Models;

/// <summary>
/// FreeSql entity mapped to the single-row <c>db_info</c> table：参考库自身的元数据（盖章行）。
/// </summary>
/// <remarks>
/// 该表回答「这份 duckdb 是谁、给哪个应用版本生成的、数据截止何时」，用于：
/// <list type="bullet">
///   <item><description>安装/更新后自检——DB 与 <c>AppInfo.Version</c> 不匹配时提示重建数据；</description></item>
///   <item><description>排查「数据不对」——比对 <c>data_built_at</c> 与实际数据来源批次。</description></item>
/// </list>
/// 与其他表不同，本表**不是**由 <c>scripts/build-*-db</c> 产出的，而是由应用自身的
/// <c>--stamp-db</c> 维护模式写入（见 <see cref="Services.DbInfoCatalog"/>）；建表用
/// <c>CREATE TABLE IF NOT EXISTS</c>，所以重复盖章是幂等的。
/// <para>
/// ⚠️ 写库只允许发生在该离线维护模式；UI 启动路径仍然只读（见 <c>FreeSqlFactory</c> 的只读三重防护）。
/// </para>
/// </remarks>
[Table(Name = "db_info", DisableSyncStructure = true)]
public sealed class DbInfo
{
    /// <summary>恒为 1：本表是单行表（与 <c>CombatStat</c> 一样靠约定而非 schema 约束）。</summary>
    [Column(Name = "id", IsPrimary = true)]
    public int Id { get; set; }

    /// <summary>产品名，与 <c>AppInfo.ReferenceProduct</c> 一致。</summary>
    [Column(Name = "product")]
    public string Product { get; set; } = string.Empty;

    /// <summary>生成该库的应用程序版本（SemVer 三段，如 <c>0.1.0</c>），与 <c>AppInfo.Version</c> 对齐。</summary>
    [Column(Name = "app_version")]
    public string AppVersion { get; set; } = string.Empty;

    /// <summary>参考库表结构版本（<c>AppInfo.ReferenceSchemaVersion</c>）：表/列契约变更时 +1。</summary>
    [Column(Name = "schema_version")]
    public int SchemaVersion { get; set; }

    /// <summary>数据内容版本（<c>content_version</c>）：重建数据时按批次递增，与应用版本解耦。</summary>
    [Column(Name = "content_version")]
    public int ContentVersion { get; set; }

    /// <summary>数据构建时间（UTC）。用于判断参考库是否陈旧。</summary>
    [Column(Name = "data_built_at", IsNullable = true)]
    public DateTime? DataBuiltAt { get; set; }

    /// <summary>各主表行数快照（JSON 或逗号分隔），便于离线核对数据完整性。</summary>
    [Column(Name = "row_counts", IsNullable = true)]
    public string? RowCounts { get; set; }

    /// <summary>备注（如数据来源、已知缺失）。</summary>
    [Column(Name = "notes", IsNullable = true)]
    public string? Notes { get; set; }
}
