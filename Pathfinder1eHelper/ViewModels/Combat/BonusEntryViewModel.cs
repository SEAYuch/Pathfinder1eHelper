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

        RemoveCommand = ReactiveCommand.Create(() => remove(this));
    }

    public Guid Id { get; }

    /// <summary>条目类型（普通 / 猛力攻击）。</summary>
    public ModifierKind Kind { get; }

    /// <summary>是否为「猛力攻击」特殊条目（编辑器改为只读提示）。</summary>
    public bool IsPowerAttack => Kind == ModifierKind.PowerAttack;

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
        Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
    };
}
