namespace Pathfinder1eHelper.Models.Combat;

/// <summary>
/// 一条加值/减值修饰（对应 WotR <c>ModifiableValue.Modifier</c>）。值自带符号。
/// </summary>
/// <param name="Descriptor">叠加分组键，决定是否可叠加。</param>
/// <param name="Value">带符号的修饰值。</param>
/// <param name="Mode">叠加模式覆盖。</param>
/// <param name="Source">可选来源标签（展示用）。</param>
public sealed record Modifier(
    ModifierDescriptor Descriptor,
    int Value,
    StackMode Mode = StackMode.Default,
    string? Source = null)
{
    /// <summary>是否参与求和（否则进入「最佳正值 / 最差负值」通道）。</summary>
    public bool Stacks => Mode switch
    {
        StackMode.ForceStack => true,
        StackMode.ForceNonStack => false,
        _ => Descriptor.IsStackable(),
    };
}
