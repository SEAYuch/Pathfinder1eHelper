using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Pathfinder1eHelper.Infrastructure;
using Pathfinder1eHelper.Models;
using Pathfinder1eHelper.Services;

namespace Pathfinder1eHelper;

/// <summary>
/// 启动前的维护模式（headless CLI）分发：<c>--version</c> 打印版本、<c>--stamp-db</c> 给参考库盖章、
/// <c>--verify-db</c> 校验参考库与程序集是否匹配、<c>--print-db-path</c> 打印运行期参考库路径。
/// </summary>
/// <remarks>
/// 这些模式在 <c>Program.Main</c> 里于 <c>BuildAvaloniaApp</c> 之前短路，因此完全不初始化
/// Avalonia/ReactiveUI/Autofac 容器，可用于 CI 与打包流水线。
/// <para>
/// ⚠️ <c>--stamp-db</c> 是唯一对参考库写入的路径，务必只对<b>仓库的</b> <c>data/pathfinder1e.duckdb</c>
/// 使用（默认即为它），不要对已安装/只读分发的副本执行。
/// </para>
/// <para>
/// 用法：
/// <code>
/// dotnet run --project Pathfinder1eHelper -- --version
/// dotnet run --project Pathfinder1eHelper -- --stamp-db [--db &lt;path&gt;] [--content &lt;n&gt;] [--notes &lt;text&gt;]
/// dotnet run --project Pathfinder1eHelper -- --verify-db [--db &lt;path&gt;]
/// dotnet run --project Pathfinder1eHelper -- --print-db-path
/// </code>
/// </para>
/// </remarks>
internal static class MaintenanceMode
{
    /// <summary>退出码：参考库缺失。</summary>
    private const int ExitMissingDb = 2;

    /// <summary>退出码：盖章失败（写库时）。</summary>
    private const int ExitStampFailed = 1;

    /// <summary>退出码：校验不通过（版本/结构不符，或有残留 WAL）。</summary>
    private const int ExitMismatch = 1;

    /// <summary>
    /// 尝试处理维护模式参数。返回 true 表示已处理（调用方应直接退出，不再启动 UI）。
    /// </summary>
    public static bool TryHandle(string[] args, out int exitCode)
    {
        exitCode = 0;

        foreach (var arg in args)
        {
            switch (arg)
            {
                case "--version":
                    UseUtf8Console();
                    Console.WriteLine($"{AppInfo.ProductName} {AppInfo.DisplayVersion}");
                    return true;

                case "--stamp-db":
                    UseUtf8Console();
                    exitCode = StampDbAsync(args).GetAwaiter().GetResult();
                    return true;

                case "--verify-db":
                    UseUtf8Console();
                    exitCode = VerifyDbAsync(args).GetAwaiter().GetResult();
                    return true;

                case "--print-db-path":
                    UseUtf8Console();
                    exitCode = PrintRuntimeDbPath();
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 把 stdout/stderr 切到 UTF-8。本项目是 <c>WinExe</c>，控制台默认代码页在中文 Windows 上
    /// 是 GBK/936，直接写中文会显示成乱码；维护模式的输出全是中文，必须显式设定。
    /// 设定失败（无控制台、重定向到不支持的流）时忽略，不影响命令本身的成败。
    /// </summary>
    private static void UseUtf8Console()
    {
        try
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
        }
        catch (IOException)
        {
            // 输出流不支持切换编码时按原样输出。
        }
    }

    private static async Task<int> StampDbAsync(string[] args)
    {
        if (!TryResolveDbPath(args, out var dbPath, out var exitCode))
        {
            return exitCode;
        }

        var contentVersion = int.TryParse(
            Option(args, "--content"), NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;
        var notes = Option(args, "--notes");

        DbInfo? info;
        try
        {
            var writable = DbInfoCatalog.OpenForStamping(dbPath);
            try
            {
                await DbInfoCatalog.StampAsync(writable, contentVersion, notes, CancellationToken.None);
                info = await DbInfoCatalog.ReadAsync(writable, CancellationToken.None);
            }
            finally
            {
                // 必须 Dispose：FreeSql 靠连接池持有 DuckDB 连接，不释放就不会 checkpoint，
                // 会在库里留下 pathfinder1e.duckdb.wal。分发的参考库必须是单个自包含
                // .duckdb 文件——.wal 不会被打包，且残留的 .wal 会让后续只读打开直接失败
                // （"Failure while replaying WAL file"）。
                writable.Dispose();
            }
        }
        catch (Exception ex)
        {
            AppLog.Error("MaintenanceMode.StampDb", ex);
            Console.Error.WriteLine($"盖章失败：{ex.Message}");
            return ExitStampFailed;
        }

        // Dispose 之后仍残留 .wal 说明 checkpoint 没生效，属异常状态，必须让打包流程发现。
        var walPath = dbPath + ".wal";
        if (File.Exists(walPath))
        {
            Console.Error.WriteLine($"警告：盖章后仍存在 WAL 文件 {walPath}，分发前请确认其已合并。");
        }

        Console.WriteLine($"已盖章 {dbPath}");
        Console.WriteLine($"  产品       {info?.Product}");
        Console.WriteLine($"  应用版本   {info?.AppVersion}");
        Console.WriteLine($"  结构版本   {info?.SchemaVersion}");
        Console.WriteLine($"  内容版本   {info?.ContentVersion}");
        Console.WriteLine($"  构建时间   {info?.DataBuiltAt:u}");
        Console.WriteLine($"  行数       {info?.RowCounts}");
        return 0;
    }

    /// <summary>
    /// 解析 <c>--stamp-db</c> / <c>--verify-db</c> 的目标参考库路径并确认其存在。
    /// 两者共用：校验必须针对与盖章完全相同的那个库，否则校验形同虚设。
    /// </summary>
    private static bool TryResolveDbPath(string[] args, out string dbPath, out int exitCode)
    {
        dbPath = Option(args, "--db") ?? DefaultRepositoryDbPath() ?? string.Empty;
        exitCode = 0;

        if (dbPath.Length == 0)
        {
            Console.Error.WriteLine("找不到 data/pathfinder1e.duckdb；请用 --db <path> 显式指定参考库路径。");
            exitCode = ExitMissingDb;
            return false;
        }

        if (!File.Exists(dbPath))
        {
            Console.Error.WriteLine($"参考库不存在：{dbPath}");
            exitCode = ExitMissingDb;
            return false;
        }

        return true;
    }

    /// <summary>
    /// 打印<b>运行期实际使用</b>的参考库路径（即 <see cref="DbPathProvider"/> 的解析结果），
    /// 并在找不到时返回非零退出码。
    /// </summary>
    /// <remarks>
    /// 存在的意义是让打包流水线能验证「参考库在产物里真的可用」。
    /// 单文件发布（<c>PublishSingleFile</c> + <c>IncludeAllContentForSelfExtract</c>）下
    /// <c>data/pathfinder1e.duckdb</c> 并不以独立文件存在，而是被打进 exe、运行时解压到
    /// <c>%TEMP%\.net\&lt;app&gt;\&lt;hash&gt;\</c>，此时只有真正跑一次 exe 并看它解析到哪儿
    /// 才能确认打包正确——这正是本命令要回答的问题。
    /// </remarks>
    private static int PrintRuntimeDbPath()
    {
        // 与 DbPathProvider 完全同一条解析规则，不另立一套。
        var path = Path.Combine(AppContext.BaseDirectory, "data", "pathfinder1e.duckdb");
        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"运行期参考库不存在：{path}");
            return ExitMissingDb;
        }

        Console.WriteLine(path);
        return 0;
    }

    /// <summary>
    /// 只读校验参考库是否与当前程序集匹配：不匹配则返回非零退出码。
    /// </summary>
    /// <remarks>
    /// 这是<b>打包流水线的前置闸门</b>。缺了它，「改了 <c>&lt;Version&gt;</c> 但忘了重跑
    /// <c>--stamp-db</c>」会把版本不符的参考库打进安装包，用户装完才发现数据与程序集不配套。
    /// 三项检查：<c>db_info</c> 盖章行的应用版本与结构版本、以及有无残留 <c>.wal</c>。
    /// </remarks>
    private static async Task<int> VerifyDbAsync(string[] args)
    {
        if (!TryResolveDbPath(args, out var dbPath, out var exitCode))
        {
            return exitCode;
        }

        var readOnly = Data.FreeSqlFactory.CreateReadOnly(dbPath);
        try
        {
            var info = await DbInfoCatalog.ReadAsync(readOnly, CancellationToken.None);

            if (info is null)
            {
                Console.Error.WriteLine("参考库没有 db_info 盖章行（或是旧库）；请先运行 --stamp-db。");
                return ExitMismatch;
            }

            var problems = new List<string>();
            if (!string.Equals(info.AppVersion, AppInfo.Version, StringComparison.Ordinal))
            {
                problems.Add($"应用版本不符：参考库 {info.AppVersion}，程序集 {AppInfo.Version}（重跑 --stamp-db）");
            }

            if (info.SchemaVersion != AppInfo.ReferenceSchemaVersion)
            {
                problems.Add($"表结构版本不符：参考库 {info.SchemaVersion}，程序集 {AppInfo.ReferenceSchemaVersion}（重跑 --stamp-db）");
            }

            var walPath = dbPath + ".wal";
            if (File.Exists(walPath))
            {
                problems.Add($"存在残留 WAL 文件 {walPath}：它不会进包，且会让只读打开失败（先 --stamp-db 重新盖章）");
            }

            if (problems.Count > 0)
            {
                foreach (var problem in problems)
                {
                    Console.Error.WriteLine($"✗ {problem}");
                }

                return ExitMismatch;
            }

            Console.WriteLine($"✓ 参考库校验通过 {dbPath}");
            Console.WriteLine($"  应用版本 {info.AppVersion} / 结构版本 {info.SchemaVersion} / 内容版本 {info.ContentVersion}");
            return 0;
        }
        finally
        {
            readOnly.Dispose();
        }
    }

    private static string? Option(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.Ordinal))
            {
                return args[i + 1];
            }
        }

        return null;
    }

    /// <summary>
    /// 定位要盖章的参考库，默认取<b>仓库根</b>的 <c>data/pathfinder1e.duckdb</c>。
    /// </summary>
    /// <remarks>
    /// 依次尝试两个锚点：<c>AppContext.BaseDirectory</c>（通常在仓库内）与当前工作目录，
    /// 各自逐级上溯，用 <c>Pathfinder1eHelper.slnx</c> 作为「到仓库根了」的判据。
    /// 两个锚点都要试是因为输出目录可能被 <c>-p:BaseOutputPath=</c> 指到仓库外
    /// （本仓库就出现过这种构建），此时只有 CWD 还能定位到仓库。
    /// <para>
    /// 刻意<b>不</b>用「找到第一个 data/pathfinder1e.duckdb 就返回」：输出目录里就有一份
    /// csproj 复制过去的同名副本，那样会盖到构建产物上，而它下次构建就会被
    /// <c>PreserveNewest</c> 覆盖回去，等于白盖——真正需要盖章的是仓库里那份源库。
    /// </para>
    /// </remarks>
    private static string? DefaultRepositoryDbPath() =>
        FindRepositoryDb(AppContext.BaseDirectory) ?? FindRepositoryDb(Environment.CurrentDirectory);

    private static string? FindRepositoryDb(string startPath)
    {
        var directory = new DirectoryInfo(startPath);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Pathfinder1eHelper.slnx")))
            {
                var repositoryDb = Path.Combine(directory.FullName, "data", "pathfinder1e.duckdb");
                return File.Exists(repositoryDb) ? repositoryDb : null;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
