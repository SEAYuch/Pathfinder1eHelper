using System;
using System.Collections.Generic;
using Pathfinder1eHelper.Models.Combat;

namespace Pathfinder1eHelper.Services;

/// <summary>
/// 可叠加数值：基础值 + 一组按描述符叠加的 <see cref="Modifier"/>。
/// 语义对应 WotR <c>ModifiableValue</c>；AC / 属性等派生数值将在此之上扩展。
/// </summary>
public class ModifiableValue
{
    private readonly List<Modifier> _modifiers = [];

    public ModifiableValue(int baseValue = 0)
    {
        BaseValue = baseValue;
    }

    /// <summary>基础值。</summary>
    public int BaseValue { get; set; }

    /// <summary>叠加结果的下限（AC / 属性为 1，其余不限制）。</summary>
    public virtual int MinValueRaw => int.MinValue;

    /// <summary>出现无助时是否把结果归零（仅属性值语义为 true）。</summary>
    protected virtual bool ZeroOnHelpless => false;

    public IReadOnlyList<Modifier> Modifiers => _modifiers;

    public void AddModifier(Modifier modifier)
    {
        ArgumentNullException.ThrowIfNull(modifier);
        _modifiers.Add(modifier);
    }

    public void AddModifiers(IEnumerable<Modifier> modifiers)
    {
        ArgumentNullException.ThrowIfNull(modifiers);
        foreach (var modifier in modifiers)
        {
            AddModifier(modifier);
        }
    }

    public bool RemoveModifier(Modifier modifier) => _modifiers.Remove(modifier);

    public void Clear() => _modifiers.Clear();

    /// <summary>按叠加规则求值；可选过滤器（如接触 / 措手不及）。</summary>
    public virtual int GetValue(Func<Modifier, bool>? filter = null) =>
        ModifierEngine.Apply(BaseValue, _modifiers, MinValueRaw, ZeroOnHelpless, filter);

    /// <summary>仅统计某一描述符贡献（对应 WotR <c>ModifiableValue.GetDescriptorBonus</c>）。</summary>
    public int GetDescriptorBonus(ModifierDescriptor descriptor) =>
        ModifierEngine.Apply(0, _modifiers, int.MinValue, filter: m => m.Descriptor == descriptor);
}
