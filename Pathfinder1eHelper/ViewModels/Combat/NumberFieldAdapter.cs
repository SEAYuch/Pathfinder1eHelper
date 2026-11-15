using System;
using ReactiveUI;

namespace Pathfinder1eHelper.ViewModels.Combat;

/// <summary>
/// 把战斗档案里的 <c>int?</c> 数值接到 Arc <c>NumberField</c>（其 <c>Value</c> 是 <c>decimal</c>、不可为空）上。
/// <para>
/// 只做「显示取整、写回取整」：模型仍是 <c>int?</c>，可空与持久化语义不变。
/// 未设置时显示 <c>fallback</c>（与 <c>ToModel</c> 里的默认值一致），避免空档案显示成 0。
/// </para>
/// </summary>
public sealed class NumberFieldAdapter : ReactiveObject
{
    private readonly Func<int?> _getValue;
    private readonly Action<int?> _setValue;
    private readonly int _fallback;

    public NumberFieldAdapter(Func<int?> getValue, Action<int?> setValue, int fallback)
    {
        _getValue = getValue;
        _setValue = setValue;
        _fallback = fallback;
    }

    public decimal Value
    {
        get => _getValue() ?? _fallback;
        set
        {
            var rounded = (int)decimal.Round(value, 0, MidpointRounding.AwayFromZero);
            _setValue(rounded);

            // 源属性可能被限幅或归一，把控件里的临时输入拉回真实值。
            this.RaisePropertyChanged(nameof(Value));
        }
    }

    /// <summary>源属性被别处改动（读档、预设、重算）时刷新控件显示。</summary>
    public void NotifySourceChanged() => this.RaisePropertyChanged(nameof(Value));
}
