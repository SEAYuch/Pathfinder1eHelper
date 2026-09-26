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
    private WeaponAttackTypeOption _attackType;
    private WeaponHandOption _hand;
    private bool _isSecondary;
    private WeaponCategoryOption _category;
    private bool _isDouble;
    private AbilityOption? _attackBonusStat;
    private AbilityOption? _damageBonusStat;
    private string _baseDamage;
    private SizeOption _weaponSize;
    private int? _damageDiceSizeShift;
    private int? _criticalThreatLow;
    private int? _criticalMultiplier;
    private int? _enhancement;
    private WeaponResult? _result;

    public WeaponViewModel(
        WeaponProfile model,
        Action changed,
        Action<WeaponViewModel> remove,
        Action<WeaponViewModel>? addFocus = null,
        Action<WeaponViewModel>? addSpecialization = null)
    {
        ArgumentNullException.ThrowIfNull(model);

        _changed = changed;
        Id = model.Id;
        _name = model.Name;
        _attackType = CombatOptions.AttackType(model.AttackType);
        _hand = CombatOptions.Hand(model.Hand);
        _isSecondary = model.IsSecondary;
        _category = CombatOptions.Category(model.Category);
        _isDouble = model.IsDouble;
        _attackBonusStat = CombatOptions.AbilityOrNull(model.AttackBonusStat);
        _damageBonusStat = CombatOptions.AbilityOrNull(model.DamageBonusStat);
        _baseDamage = model.BaseDamage;
        _weaponSize = CombatOptions.Size(model.WeaponSize);
        _damageDiceSizeShift = model.DamageDiceSizeShift;
        _criticalThreatLow = model.CriticalThreatLow;
        _criticalMultiplier = model.CriticalMultiplier;
        _enhancement = model.Enhancement;

        RemoveCommand = ReactiveCommand.Create(() => remove(this));
        AddWeaponFocusCommand = ReactiveCommand.Create(() => addFocus?.Invoke(this));
        AddWeaponSpecializationCommand = ReactiveCommand.Create(() => addSpecialization?.Invoke(this));
    }

    public Guid Id { get; }

    public ICommand RemoveCommand { get; }

    /// <summary>快捷添加「武器专攻」（+1 攻击，仅本武器）。</summary>
    public ICommand AddWeaponFocusCommand { get; }

    /// <summary>快捷添加「武器专精」（+2 伤害，仅本武器）。</summary>
    public ICommand AddWeaponSpecializationCommand { get; }

    public string Name
    {
        get => _name;
        set
        {
            this.RaiseAndSetIfChanged(ref _name, value);
            _changed();
        }
    }

    public WeaponAttackTypeOption AttackType
    {
        get => _attackType;
        set
        {
            this.RaiseAndSetIfChanged(ref _attackType, value);
            _changed();
        }
    }

    public WeaponHandOption Hand
    {
        get => _hand;
        set
        {
            this.RaiseAndSetIfChanged(ref _hand, value);
            _changed();
        }
    }

    public bool IsSecondary
    {
        get => _isSecondary;
        set
        {
            this.RaiseAndSetIfChanged(ref _isSecondary, value);
            _changed();
        }
    }

    /// <summary>武器分类（轻型/中型/重型）；「双武器格斗」据此判定副手是否轻型。</summary>
    public WeaponCategoryOption Category
    {
        get => _category;
        set
        {
            this.RaiseAndSetIfChanged(ref _category, value);
            _changed();
        }
    }

    /// <summary>是否双头武器（主手持双头时副手不再因非轻型追加减值）。</summary>
    public bool IsDouble
    {
        get => _isDouble;
        set
        {
            this.RaiseAndSetIfChanged(ref _isDouble, value);
            _changed();
        }
    }

    /// <summary>命中属性；null 表示自动（近战力量 / 远程敏捷）。</summary>
    public AbilityOption? AttackBonusStat
    {
        get => _attackBonusStat;
        set
        {
            this.RaiseAndSetIfChanged(ref _attackBonusStat, value);
            _changed();
        }
    }

    /// <summary>伤害属性；null 表示自动（近战力量 / 远程无）。</summary>
    public AbilityOption? DamageBonusStat
    {
        get => _damageBonusStat;
        set
        {
            this.RaiseAndSetIfChanged(ref _damageBonusStat, value);
            _changed();
        }
    }

    public string BaseDamage
    {
        get => _baseDamage;
        set
        {
            this.RaiseAndSetIfChanged(ref _baseDamage, value);
            _changed();
        }
    }

    /// <summary>武器体型（伤害骰缩放基准）。</summary>
    public SizeOption WeaponSize
    {
        get => _weaponSize;
        set
        {
            this.RaiseAndSetIfChanged(ref _weaponSize, value);
            _changed();
        }
    }

    /// <summary>武器体型偏移（+1 增大一档，如变巨/缩小）。</summary>
    public int? DamageDiceSizeShift
    {
        get => _damageDiceSizeShift;
        set
        {
            this.RaiseAndSetIfChanged(ref _damageDiceSizeShift, value);
            _changed();
        }
    }

    public int? CriticalThreatLow
    {
        get => _criticalThreatLow;
        set
        {
            this.RaiseAndSetIfChanged(ref _criticalThreatLow, value);
            _changed();
        }
    }

    public int? CriticalMultiplier
    {
        get => _criticalMultiplier;
        set
        {
            this.RaiseAndSetIfChanged(ref _criticalMultiplier, value);
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

    public string AttackDisplay => _result?.AttackDisplay ?? string.Empty;

    public string FullAttackDisplay => _result?.FullAttackDisplay ?? string.Empty;

    public string DamageDisplay => _result?.DamageDisplay ?? string.Empty;

    public string CriticalDisplay => _result?.CriticalDisplay ?? string.Empty;

    public IReadOnlyList<Contribution> AttackContributions => _result?.Attack.Contributions ?? [];

    public IReadOnlyList<Contribution> DamageContributions => _result?.Damage.Contributions ?? [];

    public void ApplyResult(WeaponResult result)
    {
        _result = result;
        this.RaisePropertyChanged(nameof(AttackDisplay));
        this.RaisePropertyChanged(nameof(FullAttackDisplay));
        this.RaisePropertyChanged(nameof(DamageDisplay));
        this.RaisePropertyChanged(nameof(CriticalDisplay));
        this.RaisePropertyChanged(nameof(AttackContributions));
        this.RaisePropertyChanged(nameof(DamageContributions));
    }

    public WeaponProfile ToModel() => new()
    {
        Id = Id,
        Name = Name,
        AttackType = _attackType.Value,
        Hand = _hand.Value,
        IsSecondary = IsSecondary,
        Category = _category.Value,
        IsDouble = IsDouble,
        AttackBonusStat = _attackBonusStat?.Value,
        DamageBonusStat = _damageBonusStat?.Value,
        BaseDamage = string.IsNullOrWhiteSpace(BaseDamage) ? "1d4" : BaseDamage.Trim(),
        WeaponSize = _weaponSize.Value,
        DamageDiceSizeShift = DamageDiceSizeShift ?? 0,
        CriticalThreatLow = CriticalThreatLow ?? 20,
        CriticalMultiplier = CriticalMultiplier ?? 2,
        Enhancement = Enhancement ?? 0,
    };
}
