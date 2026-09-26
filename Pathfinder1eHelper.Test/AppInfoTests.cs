using System;
using System.Linq;
using System.Reflection;
using Pathfinder1eHelper.Infrastructure;

namespace Pathfinder1eHelper.Test;

/// <summary>
/// 版本号契约测试。版本是「程序集 ↔ 参考库」两侧共享的事实，一旦漂移（比如改了
/// <c>&lt;Version&gt;</c> 忘了重新盖章）安装后的自检就会误报，所以这里把它钉死。
/// </summary>
public class AppInfoTests
{
    /// <summary>
    /// 从测试程序集的 <c>RepositoryVersion</c> 元数据读出版本源的值。该元数据由 csproj 里的
    /// <c>&lt;AssemblyMetadata Include="RepositoryVersion" Value="$(Version)" /&gt;</c> 从
    /// <c>Directory.Build.props</c> 注入——不在测试里再写一份字面量（否则每升一次版都要改两处，
    /// 单一版本源名存实亡），也不必在运行时回溯文件系统去找 props 文件。
    /// </summary>
    private static string? VersionFromBuildProps() =>
        typeof(AppInfoTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "RepositoryVersion")
            ?.Value;

    [Fact]
    public void Version_matches_the_single_source_in_Directory_Build_props()
    {
        var expected = VersionFromBuildProps();

        // 元数据缺失说明 csproj 的 <AssemblyMetadata> 被删了，先修构建配置再谈断言。
        Assert.NotNull(expected);
        Assert.False(string.IsNullOrWhiteSpace(expected));
        Assert.Equal(expected, AppInfo.Version);
    }

    [Fact]
    public void Version_is_a_three_segment_semver_and_is_parsable()
    {
        Assert.Matches(@"^\d+\.\d+\.\d+$", AppInfo.Version);

        // 能被 System.Version 解析，保证后续写进 MSI/MSIX 的数值版本与写进 db_info 的一致。
        Assert.True(Version.TryParse(AppInfo.Version, out _));
    }

    [Fact]
    public void DisplayVersion_is_prefixed_with_v()
    {
        Assert.Equal("v" + AppInfo.Version, AppInfo.DisplayVersion);
    }

    /// <summary>
    /// SDK 由 <c>&lt;Version&gt;</c> 派生的三个程序集属性必须彼此一致：打包工具
    /// （Velopack/WiX/MSIX）读的是 AssemblyVersion/FileVersion，UI 与 db_info 读的是
    /// InformationalVersion。一旦不一致，发行出去的文件属性就会与应用内显示对不上。
    /// </summary>
    [Fact]
    public void Assembly_File_and_Informational_versions_are_consistent()
    {
        var assembly = typeof(AppInfo).Assembly;

        var assemblyVersion = assembly.GetName().Version!;
        Assert.Equal(AppInfo.Version, $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}");

        var fileVersion = SingleAttribute<AssemblyFileVersionAttribute>(assembly).Version;
        Assert.Equal(AppInfo.Version, string.Join('.', fileVersion.Split('.').Take(3)));

        // InformationalVersion 可能带 +commit 之类的 SemVer 元数据后缀，主体须等于版本号。
        var informational = SingleAttribute<AssemblyInformationalVersionAttribute>(assembly).InformationalVersion;
        Assert.Equal(AppInfo.Version, informational.Split('+', '-')[0].Trim());
    }

    [Fact]
    public void Reference_schema_version_is_positive_and_bounded()
    {
        // schema_version 用于判断参考库表结构是否与本版应用兼容，必须从 1 起单调递增。
        Assert.InRange(AppInfo.ReferenceSchemaVersion, 1, 999_999);
    }

    private static T SingleAttribute<T>(Assembly assembly)
        where T : Attribute =>
        (T)assembly.GetCustomAttributes(typeof(T), false).Single();
}
