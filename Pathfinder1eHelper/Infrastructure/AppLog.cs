using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Pathfinder1eHelper.Infrastructure;

/// <summary>
/// 轻量应用日志：把未处理异常写入 <c>%AppData%\Pathfinder1eHelper\logs</c> 并输出到 Trace。
/// 仅用于兜底记录，不引入日志框架；写盘失败不影响应用运行。
/// </summary>
public static class AppLog
{
    private static readonly object Gate = new();

    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Pathfinder1eHelper",
        "logs");

    /// <summary>最近一次写入的日志文件路径（便于在“找不到日志”时定位）。</summary>
    public static string? LastLogPath { get; private set; }

    /// <summary>安装 AppDomain / TaskScheduler 级别的未处理异常兜底记录。</summary>
    public static void InstallGlobalHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Error("UnhandledException", e.ExceptionObject as Exception);

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Error("UnobservedTaskException", e.Exception);
            e.SetObserved();
        };
    }

    public static void Error(string context, Exception? exception)
    {
        var message = $"[{DateTimeOffset.Now:O}] {context}: {exception}";
        Trace.TraceError(message);
        WriteToFile(message);
    }

    private static void WriteToFile(string message)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(LogDirectory);
                var path = Path.Combine(LogDirectory, $"app-{DateTime.UtcNow:yyyyMMdd}.log");
                File.AppendAllText(path, message + Environment.NewLine);
                LastLogPath = path;
            }
        }
        catch
        {
            // 日志失败不应影响应用。
        }
    }
}
