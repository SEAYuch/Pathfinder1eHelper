using System;
using System.Globalization;
using System.Reflection;

namespace Pathfinder1eHelper.Infrastructure;

/// <summary>
/// 运行期版本读取口。版本号的唯一来源是 <c>Directory.Build.props</c> 的 <c>&lt;Version&gt;</c>
/// （由 SDK 派生成程序集四件套），代码里不再出现第二份硬编码版本。
/// </summary>
/// <remarks>
/// 取值优先级：<c>InformationalVersion</c>（<c>0.1.0</c>，可含 <c>+commit</c> 之类的 SemVer 元数据，
/// 这里剥掉后缀只留主体）→ <c>AssemblyVersion</c>（四段，截到三段）→ <c>0.0.0</c>。
/// 之所以不用 <c>FileVersion</c>：它与 AssemblyVersion 同步但同样可能带元数据后缀，且
/// <c>0.1.0.0</c> 这种四段形式不适合塞进参考库的版本列。
/// </remarks>
public static class AppInfo
{
    private const string FallbackVersion = "0.0.0";
    private const string FallbackProduct = "Pathfinder1eHelper";

    /// <summary>
    /// 产品名（关于对话框、写入 <c>db_info.product</c>、打包元数据用）。
    /// 取自程序集的 <c>Product</c> 属性——由 <c>Directory.Build.props</c> 的 <c>&lt;Product&gt;</c>
    /// 注入，与 exe「属性 → 详细信息」里显示的名称同源，不会两处各写一份。
    /// </summary>
    public static string ProductName { get; } =
        typeof(AppInfo).Assembly
            .GetCustomAttribute<AssemblyProductAttribute>()?.Product is { Length: > 0 } product
            ? product
            : FallbackProduct;

    /// <summary>应用版本，形如 <c>0.1.0</c>。同时写入程序集与参考库 <c>db_info.app_version</c>。</summary>
    public static string Version { get; } = ReadVersion();

    /// <summary>版本展示串，形如 <c>v0.1.0</c>，供 UI 直接绑定。</summary>
    public static string DisplayVersion => "v" + Version;

    /// <summary>参考库 <c>db_info</c> 的建表结构版本：表/列契约变更时 +1，与应用版本解耦。</summary>
    public const int ReferenceSchemaVersion = 1;

    /// <summary>参考库元数据的产品名，同样写入 <c>db_info.product</c>。</summary>
    public static string ReferenceProduct => ProductName;

    private static string ReadVersion()
    {
        var assembly = typeof(AppInfo).Assembly;

        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (TryNormalize(informational, out var fromInformational))
        {
            return fromInformational;
        }

        return TryNormalize(assembly.GetName().Version?.ToString(), out var fromAssembly)
            ? fromAssembly
            : FallbackVersion;
    }

    /// <summary>
    /// 规整为三段 SemVer：剥掉 <c>+meta</c> 与 <c>-prerelease</c> 后缀，四段补齐/截断为三段。
    /// 返回 false 表示输入不可解析，调用方应回退。
    /// </summary>
    private static bool TryNormalize(string? raw, out string normalized)
    {
        normalized = FallbackVersion;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        // SemVer 元数据（+build）与预发布标识（-beta）都不是版本主体的一部分。
        var core = raw.Split('+', '-')[0].Trim();
        if (core.Length == 0)
        {
            return false;
        }

        var parts = core.Split('.');
        if (parts.Length is < 1 or > 4)
        {
            return false;
        }

        var numbers = new int[3];
        for (var i = 0; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out var number) || number < 0)
            {
                return false;
            }

            // 四段版本的第四段是 CLR 修订号，对外统一按三段展示。
            if (i < numbers.Length)
            {
                numbers[i] = number;
            }
        }

        normalized = string.Join('.', numbers);
        return true;
    }
}
