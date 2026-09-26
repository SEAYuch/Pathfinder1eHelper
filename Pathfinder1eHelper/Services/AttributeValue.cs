namespace Pathfinder1eHelper.Services;

/// <summary>
/// 属性值（力量/敏捷/…）。对应 WotR <c>ModifiableValueAttributeStat</c>：
/// 基础值即属性值，修正 = <c>ModifiedValue / 2 - 5</c>，下限 1，且无助时归零。
/// </summary>
public sealed class AttributeValue : ModifiableValue
{
    public AttributeValue(int score = 10)
        : base(score)
    {
    }

    public override int MinValueRaw => 1;

    protected override bool ZeroOnHelpless => true;

    /// <summary>属性调整值 = floor((属性值 - 10) / 2)。</summary>
    public int Bonus => GetValue() / 2 - 5;
}
