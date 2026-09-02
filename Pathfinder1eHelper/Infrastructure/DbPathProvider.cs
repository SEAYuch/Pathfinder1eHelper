using System;
using System.IO;

namespace Pathfinder1eHelper.Infrastructure;

/// <summary>
/// Resolves the reference DB beside the executable. This only works because the csproj links
/// <c>..\data\spells.duckdb</c> with <c>CopyToOutputDirectory=PreserveNewest</c>.
/// </summary>
public sealed class DbPathProvider : IDbPathProvider
{
    public string SpellsDbPath { get; }

    public DbPathProvider()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "data", "spells.duckdb");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Reference database not found at '{path}'. Ensure spells.duckdb is copied to the " +
                "output directory (see the <None Include=\"..\\data\\spells.duckdb\"> item in the csproj).",
                path);
        }

        SpellsDbPath = path;
    }
}
