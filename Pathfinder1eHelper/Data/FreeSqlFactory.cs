using FreeSql;

namespace Pathfinder1eHelper.Data;

/// <summary>
/// Builds the singleton <see cref="IFreeSql"/> instance for the read-only DuckDB reference data.
/// </summary>
/// <remarks>
/// Read-only safety is triple-guarded so FreeSql never issues DDL/DML against the shipped DB:
/// <list type="bullet">
///   <item><description><c>ACCESS_MODE=READ_ONLY</c> in the connection string (DuckDB engine level).</description></item>
///   <item><description><c>UseAutoSyncStructure(false)</c> (FreeSql never CREATE/ALTERs).</description></item>
///   <item><description><c>[Table(DisableSyncStructure = true)]</c> on the entity.</description></item>
/// </list>
/// <see cref="IFreeSql"/> is thread-safe, so a single instance is shared as a singleton.
/// </remarks>
public static class FreeSqlFactory
{
    public static IFreeSql CreateReadOnly(string dbPath) =>
        new FreeSqlBuilder()
            .UseConnectionString(DataType.DuckDB, $"DataSource={dbPath};ACCESS_MODE=READ_ONLY")
            .UseAutoSyncStructure(false)
            .Build();
}
