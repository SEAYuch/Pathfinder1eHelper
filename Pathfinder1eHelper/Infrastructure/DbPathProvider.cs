using System;
using System.IO;

namespace Pathfinder1eHelper.Infrastructure;

/// <summary>
/// Resolves the reference DB beside the executable. This only works because the csproj links
/// <c>..\data\pathfinder1e.duckdb</c> with <c>CopyToOutputDirectory=PreserveNewest</c>.
/// </summary>
public sealed class DbPathProvider : IDbPathProvider
{
    public string DbPath { get; }

    public DbPathProvider()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "data", "pathfinder1e.duckdb");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Reference database not found at '{path}'. Ensure pathfinder1e.duckdb is copied to the " +
                "output directory (see the <None Include=\"..\\data\\pathfinder1e.duckdb\"> item in the csproj).",
                path);
        }

        DbPath = path;
    }
}
