using System;
using System.Windows.Input;
using Pathfinder1eHelper.Models.Combat;
using ReactiveUI;

namespace Pathfinder1eHelper.ViewModels.Combat;

/// <summary>加值明细行的可编辑视图模型。</summary>
public sealed class BonusEntryViewModel : ReactiveObject
{
    private readonly Action _changed;
    private string _name;
    private BonusTypeOption _type;
    private EnhancementOption _enhancement;
    private BonusTargetOption _target;
    private int? _value;
    private bool _isEnabled;
    private string? _sourceGroup;
    private string? _notes;
    private AbilityOption? _ability;

    public BonusEntryViewModel(BonusEntry model, Action changed, Action<BonusEntryViewModel> remove)
    {
        ArgumentNullException.ThrowIfNull(model);

        _changed = changed;
        Id = model.Id;
        Origin = model.Origin;
        _name = model.Name;
        _type = CombatOptions.Type(model.Type);
        _enhancement = CombatOptions.Enhancement(model.Enhancement);
        _target = CombatOptions.Target(model.Target);
        _value = model.Value;
        _isEnabled = model.IsEnabled;
        _sourceGroup = model.SourceGroup;
        _notes = model.Notes;
        _ability = model.Ability is { } ability ? CombatOptions.Ability(ability) : null;

        RemoveCommand = ReactiveCommand.Create(() => remove(this));
    }

    public Guid Id { get; }

    /// <summary>来源（手动 / 法术 Buff / 状态预设），只读。</summary>
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

    public BonusTypeOption Type
    {
        get => _type;
        set
        {
            this.RaiseAndSetIfChanged(ref _type, value);
            this.RaisePropertyChanged(nameof(IsEnhancement));
            _changed();
        }
    }

    public bool IsEnhancement => _type.Value == BonusType.Enhancement;

    public EnhancementOption Enhancement
    {
        get => _enhancement;
        set
        {
            this.RaiseAndSetIfChanged(ref _enhancement, value);
            _changed();
        }
    }

    public BonusTargetOption Target
    {
        get => _target;
        set
        {
            this.RaiseAndSetIfChanged(ref _target, value);
            this.RaisePropertyChanged(nameof(IsAbilityTarget));
            _changed();
        }
    }

    public bool IsAbilityTarget => _target.Value == BonusTarget.AbilityScore;

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

    public string? SourceGroup
    {
        get => _sourceGroup;
        set
        {
            this.RaiseAndSetIfChanged(ref _sourceGroup, value);
            _changed();
        }
    }

    public string? Notes
    {
        get => _notes;
        set
        {
            this.RaiseAndSetIfChanged(ref _notes, value);
            _changed();
        }
    }

    public BonusEntry ToModel() => new()
    {
        Id = Id,
        Origin = Origin,
        Name = Name,
        Type = _type.Value,
        Enhancement = _enhancement.Value,
        Target = _target.Value,
        Ability = _ability?.Value,
        Value = Value ?? 0,
        IsEnabled = IsEnabled,
        SourceGroup = string.IsNullOrWhiteSpace(SourceGroup) ? null : SourceGroup.Trim(),
        Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
    };
}
