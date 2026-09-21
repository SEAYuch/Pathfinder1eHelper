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
    private WeaponAbilityOption _attackAbility;
    private string _damageDice;
    private StrengthMultiplierOption _strengthMultiplier;
    private WeaponAbilityOption _damageAbility;
    private int? _enhancement;
    private string _critical;
    private WeaponResult? _result;

    public WeaponViewModel(WeaponProfile model, Action changed, Action<WeaponViewModel> remove)
    {
        ArgumentNullException.ThrowIfNull(model);

        _changed = changed;
        Id = model.Id;
        _name = model.Name;
        _attackAbility = CombatOptions.WeaponChoice(model.AttackAbility);
        _damageDice = model.DamageDice;
        _strengthMultiplier = CombatOptions.Multiplier(model.StrengthMultiplier);
        _damageAbility = CombatOptions.WeaponChoice(model.DamageAbility);
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

    /// <summary>命中属性：力量 / 敏捷。</summary>
    public WeaponAbilityOption AttackAbility
    {
        get => _attackAbility;
        set
        {
            this.RaiseAndSetIfChanged(ref _attackAbility, value);
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

    /// <summary>伤害属性：力量 / 敏捷。</summary>
    public WeaponAbilityOption DamageAbility
    {
        get => _damageAbility;
        set
        {
            this.RaiseAndSetIfChanged(ref _damageAbility, value);
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

    public string AttackDisplay => _result?.AttackDisplay ?? string.Empty;

    public string DamageDisplay => _result?.DamageDisplay ?? string.Empty;

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
        AttackAbility = _attackAbility.Value,
        DamageDice = DamageDice,
        StrengthMultiplier = _strengthMultiplier.Value,
        DamageAbility = _damageAbility.Value,
        Enhancement = Enhancement ?? 0,
        Critical = Critical,
    };
}
