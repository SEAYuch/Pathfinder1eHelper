namespace Pathfinder1eHelper.Models.Combat;

/// <summary>
/// 叠加模式的强制覆盖，对应 WotR <c>ModifiableValue.StackMode</c>。
/// <see cref="Default"/> 时由描述符自身的可叠加性决定（见 <see cref="ModifierDescriptorHelper"/>）。
/// </summary>
public enum StackMode
{
    /// <summary>按描述符默认规则（可叠加则求和，否则取高/取低）。</summary>
    Default,

    /// <summary>强制叠加（求和），无视描述符默认。</summary>
    ForceStack,

    /// <summary>强制不叠加（只保留最佳正值与最差负值），无视描述符默认。</summary>
    ForceNonStack,
}
