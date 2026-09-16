using System;
using System.Collections.Generic;
using System.Windows.Input;
using Pathfinder1eHelper.Models.Combat;
using ReactiveUI;

namespace Pathfinder1eHelper.ViewModels.Combat;

/// <summary>武器行的可编辑视图模型，附最近一次计算出的攻击/伤害。</summary>
public sealed class WeaponViewModel : ReactiveObject
{
    private readonly Action _changed;
    private string _name;
    private bool _isRanged;
    private string _damageDice;
    private StrengthMultiplierOption _strengthMultiplier;
    private int? _enhancement;
    private string _critical;
    private WeaponResult? _result;

    public WeaponViewModel(WeaponProfile model, Action changed, Action<WeaponViewModel> remove)
    {
        ArgumentNullException.ThrowIfNull(model);

        _changed = changed;
        Id = model.Id;
        _name = model.Name;
        _isRanged = model.IsRanged;
        _damageDice = model.DamageDice;
        _strengthMultiplier = CombatOptions.Multiplier(model.StrengthMultiplier);
        _enhancement = model.Enhancement;
        _critical = model.Critical;

        RemoveCommand = ReactiveCommand.Create(() => remove(this));
    }

    public Guid Id { get; }

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

    public bool IsRanged
    {
        get => _isRanged;
        set
        {
            this.RaiseAndSetIfChanged(ref _isRanged, value);
            _changed();
        }
    }

    public string DamageDice
    {
        get => _damageDice;
        set
        {
            this.RaiseAndSetIfChanged(ref _damageDice, value);
            _changed();
        }
    }

    public StrengthMultiplierOption StrengthMultiplier
    {
        get => _strengthMultiplier;
        set
        {
            this.RaiseAndSetIfChanged(ref _strengthMultiplier, value);
            _changed();
        }
    }

    public int? Enhancement
    {
        get => _enhancement;
        set
        {
            this.RaiseAndSetIfChanged(ref _enhancement, value);
            _changed();
        }
    }

    public string Critical
    {
        get => _critical;
        set
        {
            this.RaiseAndSetIfChanged(ref _critical, value);
            _changed();
        }
    }

    public string AttackDisplay => _result?.AttackDisplay ?? "";

    public string DamageDisplay => _result?.DamageDisplay ?? "";

    public IReadOnlyList<Contribution> AttackContributions => _result?.Attack.Contributions ?? [];

    public IReadOnlyList<Contribution> DamageContributions => _result?.Damage.Contributions ?? [];

    public void ApplyResult(WeaponResult result)
    {
        _result = result;
        this.RaisePropertyChanged(nameof(AttackDisplay));
        this.RaisePropertyChanged(nameof(DamageDisplay));
        this.RaisePropertyChanged(nameof(AttackContributions));
        this.RaisePropertyChanged(nameof(DamageContributions));
    }

    public WeaponProfile ToModel() => new()
    {
        Id = Id,
        Name = Name,
        IsRanged = IsRanged,
        DamageDice = DamageDice,
        StrengthMultiplier = _strengthMultiplier.Value,
        Enhancement = Enhancement ?? 0,
        Critical = Critical,
    };
}
