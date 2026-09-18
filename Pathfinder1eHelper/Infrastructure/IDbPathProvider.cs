namespace Pathfinder1eHelper.Infrastructure;

/// <summary>Resolves filesystem paths to the app's databases.</summary>
public interface IDbPathProvider
{
    /// <summary>Absolute path to the read-only <c>pathfinder1e.duckdb</c> reference database.</summary>
    string DbPath { get; }
}
