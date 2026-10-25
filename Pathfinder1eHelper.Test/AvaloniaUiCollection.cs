namespace Pathfinder1eHelper.Test;

/// <summary>
/// 构造真实 Avalonia 控件/XAML 的测试归入此 collection，串行执行。
/// </summary>
/// <remarks>
/// Avalonia 的属性系统（<c>WeakEvents</c> 等静态表）不是线程安全的，而 xUnit v3 默认按
/// 「每个测试类一个 collection」并行跑。两个类同时 <c>InitializeComponent()</c> 时会在
/// <c>WeakEvent.Subscription.OnEvent</c> 里偶发 <see cref="System.NullReferenceException"/>，
/// 症状是随机某个视图测试失败（包括并非本次改动的测试）。归组即可根除。
/// </remarks>
[CollectionDefinition(Name)]
public sealed class AvaloniaUiCollection
{
    public const string Name = "Avalonia UI";
}
