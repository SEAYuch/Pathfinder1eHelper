using System;
using System.Windows.Input;
using Pathfinder1eHelper.Models.Combat;
using ReactiveUI;

namespace Pathfinder1eHelper.ViewModels.Combat;

/// <summary>修饰明细行的可编辑视图模型（描述符 + 通道 + 叠加模式）。</summary>
public sealed class BonusEntryViewModel : ReactiveObject
{
    private readonly Action _changed;
    private string _name;
    private DescriptorOption _descriptor;
    private CombatStatOption _stat;
    private AbilityOption? _ability;
    private int? _value;
    private StackModeOption _stackMode;
    private bool _isEnabled;
    private string? _notes;

    public BonusEntryViewModel(ModifierEntry model, Action changed, Action<BonusEntryViewModel> remove)
    {
        ArgumentNullException.ThrowIfNull(model);

        _changed = changed;
        Id = model.Id;
        Origin = model.Origin;
        _name = model.Name;
        _descriptor = CombatOptions.Descriptor(model.Descriptor);
        _stat = CombatOptions.Stat(model.Stat);
        _ability = CombatOptions.AbilityOrNull(model.Ability);
        _value = model.Value;
        _stackMode = CombatOptions.StackMode(model.StackMode);
        _isEnabled = model.IsEnabled;
        _notes = model.Notes;
        WeaponId = model.WeaponId;
        Kind = model.Kind;
        Stance = model.Stance;

        ValueInput = new NumberFieldAdapter(() => _value, value => Value = value, 0);
        PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(Value))
            {
                ValueInput.NotifySourceChanged();
            }
        };

        RemoveCommand = ReactiveCommand.Create(() => remove(this));
    }

    public Guid Id { get; }

    /// <summary>数值的 decimal 适配器（Arc NumberField 绑定，模型仍为 int?）。</summary>
    public NumberFieldAdapter ValueInput { get; }

    /// <summary>
    /// 条目类型。保留该字段只为读回旧档（旧档的 <c>kind</c> 字符串仍要能反序列化）；
    /// 「猛力攻击/寓守于攻/白鹤拳/双武器格斗」四个规则已移除，所有条目一律按普通条目参与叠加。
    /// </summary>
    public ModifierKind Kind { get; }

    /// <summary>战斗机动标记；非空表示本条是姿态条目（防御式战斗/全防御）。</summary>
    public CombatStance? Stance { get; }

    /// <summary>武器归属（仅作用于该武器）；只读，由武器卡快捷按钮设置。</summary>
    public Guid? WeaponId { get; }

    /// <summary>来源（手动 / 法术 Buff / 专长 Buff / 状态预设），只读。</summary>
    public BonusOrigin Origin { get; }

    public ICommand RemoveCommand { get; }

    public string Name
    {
        get => _name;
        set
        {
            this.RaiseAndSetIfChanged(ref _name, value);
            _changed();
        }
    }

    /// <summary>加值类型（描述符），决定叠加规则。</summary>
    public DescriptorOption Descriptor
    {
        get => _descriptor;
        set
        {
            this.RaiseAndSetIfChanged(ref _descriptor, value);
            _changed();
        }
    }

    /// <summary>作用通道。</summary>
    public CombatStatOption Stat
    {
        get => _stat;
        set
        {
            this.RaiseAndSetIfChanged(ref _stat, value);
            this.RaisePropertyChanged(nameof(IsAbilityTarget));
            _changed();
        }
    }

    public bool IsAbilityTarget => _stat.Value == CombatStat.AbilityScore;

    public AbilityOption? Ability
    {
        get => _ability;
        set
        {
            this.RaiseAndSetIfChanged(ref _ability, value);
            _changed();
        }
    }

    public int? Value
    {
        get => _value;
        set
        {
            this.RaiseAndSetIfChanged(ref _value, value);
            _changed();
        }
    }

    public StackModeOption StackMode
    {
        get => _stackMode;
        set
        {
            this.RaiseAndSetIfChanged(ref _stackMode, value);
            _changed();
        }
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            this.RaiseAndSetIfChanged(ref _isEnabled, value);
            this.RaisePropertyChanged(nameof(CardOpacity));
            _changed();
        }
    }

    public double CardOpacity => _isEnabled ? 1.0 : 0.5;

    public string? Notes
    {
        get => _notes;
        set
        {
            this.RaiseAndSetIfChanged(ref _notes, value);
            _changed();
        }
    }

    public ModifierEntry ToModel() => new()
    {
        Id = Id,
        Origin = Origin,
        Name = Name,
        Kind = Kind,
        Descriptor = _descriptor.Value,
        Stat = _stat.Value,
        Ability = _ability?.Value,
        WeaponId = WeaponId,
        Value = Value ?? 0,
        StackMode = _stackMode.Value,
        IsEnabled = IsEnabled,
        Stance = Stance,
        Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
    };
}
