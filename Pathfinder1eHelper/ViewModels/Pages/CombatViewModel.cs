using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Pathfinder1eHelper.Models.Combat;
using Pathfinder1eHelper.Services;
using Pathfinder1eHelper.ViewModels.Combat;
using ReactiveUI;
using ReactiveUI.Primitives;

namespace Pathfinder1eHelper.ViewModels.Pages;

/// <summary>
/// 战斗页：管理多个角色档案，编辑基础数据与修饰/武器，实时用
/// <see cref="CombatCalculator"/> 汇总，并保存到 <see cref="ICharacterRepository"/>。
/// </summary>
public sealed class CombatViewModel : ViewModelBase, IPageViewModel
{
    /// <summary>ReactiveUI 路由：宿主屏幕（由 shell 在导航前赋值）。</summary>
    public IScreen HostScreen { get; set; } = null!;

    /// <summary>ReactiveUI 路由标识。</summary>
    public string UrlPathSegment => "combat";

    private readonly CharacterRoster _roster;
    private readonly CharacterAutoSaver _autoSaver;
    private bool _loading;
    private int _batchDepth;
    private bool _batchDirty;
    private CharacterListItemViewModel? _selectedCharacter;
    private Guid _profileId;
    private int _schemaVersion;

    private SizeOption _selectedSize = CombatOptions.Size(SizeCategory.Medium);
    private AbilityOption _selectedCastingAbility = CombatOptions.Ability(Ability.Intelligence);
    private ConcentrationOption _concentrationSituation = CombatOptions.Concentration(ConcentrationSituation.DefensiveCasting);
    private PenaltySourceOption _selectedPenaltySource = CombatOptions.PenaltySources[1];
    private BonusDestinationOption _selectedBonusDestination = CombatOptions.BonusDestinations[0];
    private string _bonusMultiplierText = "1.0";
    private string? _exchangeValidationError;
    private string _exchangeEntryName = "减值换加值";

    public CombatViewModel(ICharacterRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);

        _roster = new CharacterRoster(repository);
        _autoSaver = new CharacterAutoSaver(repository);

        Characters = _roster.Characters;
        Bonuses = [];
        Weapons = [];
        Bonuses.CollectionChanged += (_, _) => OnInputChanged();
        Weapons.CollectionChanged += (_, _) => OnInputChanged();

        Presets = CombatPresets.All
            .Select(p => new PresetViewModel(p.Name, () => ApplyPreset(p)))
            .ToList();
        WeaponPresets = WeaponCatalog.All
            .Select(p => new PresetViewModel(p.Name, () => ApplyWeaponPreset(p)))
            .ToList();

        AddBonusCommand = ReactiveCommand.Create(AddBonus);
        AddWeaponCommand = ReactiveCommand.Create(AddWeapon);
        NewCharacterCommand = ReactiveCommand.Create(NewCharacter);
        DuplicateCharacterCommand = ReactiveCommand.Create(DuplicateCharacter);
        DeleteCharacterCommand = ReactiveCommand.Create(DeleteCharacter);
        ClearAllBonusesCommand = ReactiveCommand.Create(ClearAllBonuses);
        AddPenaltyBonusCommand = ReactiveCommand.Create(AddPenaltyBonus);

        _roster.Load();
        _selectedCharacter = _roster.EnsureAtLeastOne();
        LoadProfile(_selectedCharacter.Profile);

        this.WhenActivated(disposables => disposables.Add(ActionDisposable.Create(() =>
            _ = _autoSaver.FlushAsync())));
    }

    /// <summary>设计时构造：示例数据，供 XAML 预览器使用。</summary>
    public CombatViewModel() : this(new DesignTimeCharacterRepository())
    {
    }

    public ObservableCollection<CharacterListItemViewModel> Characters { get; }

    public ObservableCollection<BonusEntryViewModel> Bonuses { get; }

    public ObservableCollection<WeaponViewModel> Weapons { get; }

    public IReadOnlyList<PresetViewModel> Presets { get; }

    public IReadOnlyList<PresetViewModel> WeaponPresets { get; }

    public CharacterListItemViewModel? SelectedCharacter
    {
        get => _selectedCharacter;
        set
        {
            if (value is null || ReferenceEquals(_selectedCharacter, value))
            {
                return;
            }

            this.RaiseAndSetIfChanged(ref _selectedCharacter, value);
            LoadProfile(value.Profile);
        }
    }

    public ICommand AddBonusCommand { get; }

    public ICommand AddWeaponCommand { get; }

    public ICommand NewCharacterCommand { get; }

    public ICommand DuplicateCharacterCommand { get; }

    public ICommand DeleteCharacterCommand { get; }

    public ICommand ClearAllBonusesCommand { get; }

    public ICommand AddPenaltyBonusCommand { get; }

    public IReadOnlyList<PenaltySourceOption> PenaltySources => CombatOptions.PenaltySources;

    public IReadOnlyList<BonusDestinationOption> BonusDestinations => CombatOptions.BonusDestinations;

    public IReadOnlyList<string> BonusMultiplierOptions => CombatOptions.Multipliers;

    public int ExchangePenalty
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            this.RaisePropertyChanged(nameof(CanAddExchange));
            ExchangeValidationError = null;
        }
    } = 1;

    public PenaltySourceOption SelectedPenaltySource
    {
        get => _selectedPenaltySource;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedPenaltySource, value);
            this.RaisePropertyChanged(nameof(CanAddExchange));
            ExchangeValidationError = null;
        }
    }

    public string BonusMultiplierText
    {
        get => _bonusMultiplierText;
        set
        {
            this.RaiseAndSetIfChanged(ref _bonusMultiplierText, value);
            this.RaisePropertyChanged(nameof(CanAddExchange));
            ExchangeValidationError = null;
        }
    }

    public string ExchangeEntryName
    {
        get => _exchangeEntryName;
        set
        {
            this.RaiseAndSetIfChanged(ref _exchangeEntryName, value);
            ExchangeValidationError = null;
        }
    }

    public BonusDestinationOption SelectedBonusDestination
    {
        get => _selectedBonusDestination;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedBonusDestination, value);
            this.RaisePropertyChanged(nameof(CanAddExchange));
            ExchangeValidationError = null;
        }
    }

    public string? ExchangeValidationError
    {
        get => _exchangeValidationError;
        private set => this.RaiseAndSetIfChanged(ref _exchangeValidationError, value);
    }

    public bool CanAddExchange => ExchangePenalty is >= 0 and <= 11
        && TryGetMultiplierHalfSteps(BonusMultiplierText, out _);

    public CombatSheet Sheet
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    } = null!;

    public IReadOnlyList<StatCard> Cards
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    } = [];

    public string? SaveError
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string Name
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            if (!_loading && _selectedCharacter is not null)
            {
                _selectedCharacter.Name = value;
            }

            OnInputChanged();
        }
    } = string.Empty;

    public int? Level
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            OnInputChanged();
        }
    }

    public SizeOption SelectedSize
    {
        get => _selectedSize;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedSize, value);
            OnInputChanged();
        }
    }

    public AbilityOption SelectedCastingAbility
    {
        get => _selectedCastingAbility;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedCastingAbility, value);
            OnInputChanged();
        }
    }

    public int? Strength
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            OnInputChanged();
        }
    }

    public int? Dexterity
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            OnInputChanged();
        }
    }

    public int? Constitution
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            OnInputChanged();
        }
    }

    public int? Intelligence
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            OnInputChanged();
        }
    }

    public int? Wisdom
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            OnInputChanged();
        }
    }

    public int? Charisma
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            OnInputChanged();
        }
    }

    public int? BaseAttackBonus
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            OnInputChanged();
        }
    }

    public int? BaseFortitude
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            OnInputChanged();
        }
    }

    public int? BaseReflex
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            OnInputChanged();
        }
    }

    public int? BaseWill
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            OnInputChanged();
        }
    }

    public bool LimitDex
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            OnInputChanged();
        }
    }

    public int? MaxDexBonus
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            OnInputChanged();
        }
    }

    public int? CasterLevel
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            OnInputChanged();
        }
    }

    public bool UseDexForManeuvers
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            OnInputChanged();
        }
    }

    public int? SpellLevel
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            OnInputChanged();
        }
    }

    public int? DamageTaken
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            OnInputChanged();
        }
    }

    public int? CustomDc
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            OnInputChanged();
        }
    } = 15;

    public ConcentrationOption SelectedConcentrationSituation
    {
        get => _concentrationSituation;
        set
        {
            this.RaiseAndSetIfChanged(ref _concentrationSituation, value);
            this.RaisePropertyChanged(nameof(IsCustomDc));
            this.RaisePropertyChanged(nameof(IsDamageMode));
            OnInputChanged();
        }
    }

    public bool IsCustomDc => _concentrationSituation.Value == ConcentrationSituation.Custom;

    public bool IsDamageMode => _concentrationSituation.Value == ConcentrationSituation.DamageWhileCasting;

    public int ConcentrationDc
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string ConcentrationCheckDisplay => $"d20 {Sheet.Concentration.TotalDisplay} vs DC {ConcentrationDc}";

    private void OnInputChanged()
    {
        if (_loading)
        {
            return;
        }

        // 批量添加（预设/Buff 多行）期间只记脏标记，退出批量时统一重算一次并调度一次保存。
        if (_batchDepth > 0)
        {
            _batchDirty = true;
            return;
        }

        Refresh(save: true);
    }

    /// <summary>
    /// 把多项修改合并成一次完整重算与一次保存调度。集合仍逐项通知 UI，只是中间态不再触发
    /// <see cref="Refresh"/>；即使范围正常结束时抛异常（由调用方负责捕获），也在 finally 里补算一次，
    /// 保证界面不停留在过期数值上。
    /// </summary>
    private void RunBatch(Action mutate)
    {
        ArgumentNullException.ThrowIfNull(mutate);

        _batchDepth++;
        try
        {
            mutate();
        }
        finally
        {
            _batchDepth--;
            if (_batchDepth == 0 && _batchDirty)
            {
                _batchDirty = false;
                if (!_loading)
                {
                    Refresh(save: true);
                }
            }
        }
    }

    private void AddBonus() => Bonuses.Add(new BonusEntryViewModel(
        new ModifierEntry { Name = "新加值", Stat = CombatStat.ArmorClass, Descriptor = ModifierDescriptor.None, Value = 1 },
        OnInputChanged,
        RemoveBonus));

    private void RemoveBonus(BonusEntryViewModel entry) => Bonuses.Remove(entry);

    private void AddPenaltyBonus()
    {
        var entryName = ExchangeEntryName.Trim();
        if (ExchangePenalty is < 0 or > 11 || !TryGetMultiplierHalfSteps(BonusMultiplierText, out var halfSteps))
        {
            ExchangeValidationError = "减值需为 0–11，倍率需为 0.5–10 且按 0.5 递增。";
            return;
        }

        if (string.IsNullOrWhiteSpace(entryName))
        {
            ExchangeValidationError = "请输入词条名称。";
            return;
        }

        var penaltyStat = SelectedPenaltySource.Value switch
        {
            PenaltySource.ArmorClass => CombatStat.ArmorClass,
            PenaltySource.WeaponAttack => CombatStat.WeaponAttack,
            _ => throw new ArgumentOutOfRangeException(nameof(SelectedPenaltySource)),
        };
        var bonusStat = SelectedBonusDestination.Value switch
        {
            BonusDestination.ArmorClass => CombatStat.ArmorClass,
            BonusDestination.Damage => CombatStat.Damage,
            _ => throw new ArgumentOutOfRangeException(nameof(SelectedBonusDestination)),
        };
        var bonusValue = ExchangePenalty * halfSteps / 2;
        var multiplier = (halfSteps / 2m).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);

        RunBatch(() =>
        {
            Bonuses.Add(CreateExchangeEntry(
                $"{entryName}：{SelectedPenaltySource.Display} −{ExchangePenalty}", penaltyStat, -ExchangePenalty,
                ModifierDescriptor.Penalty));
            Bonuses.Add(CreateExchangeEntry(
                $"{entryName}：{SelectedBonusDestination.Display} +{bonusValue}（倍率 ×{multiplier}）", bonusStat,
                bonusValue, ModifierDescriptor.UntypedStackable));
        });
        ExchangeValidationError = null;
    }

    private BonusEntryViewModel CreateExchangeEntry(
        string name,
        CombatStat stat,
        int value,
        ModifierDescriptor descriptor) =>
        new(
            new ModifierEntry
            {
                Name = name,
                Kind = ModifierKind.Normal,
                Descriptor = descriptor,
                Stat = stat,
                Value = value,
                StackMode = StackMode.Default,
                IsEnabled = true,
                Origin = BonusOrigin.Manual,
            },
            OnInputChanged,
            RemoveBonus);

    private static bool TryGetMultiplierHalfSteps(string? text, out int halfSteps)
    {
        halfSteps = 0;
        if (!decimal.TryParse(text, System.Globalization.NumberStyles.AllowDecimalPoint,
                System.Globalization.CultureInfo.InvariantCulture, out var multiplier))
        {
            return false;
        }

        var scaled = multiplier * 2m;
        if (scaled is < 1m or > 20m || scaled != decimal.Truncate(scaled))
        {
            return false;
        }

        halfSteps = (int)scaled;
        return true;
    }

    /// <summary>清空全部修饰条目（含手动条目）。</summary>
    private void ClearAllBonuses() => Bonuses.Clear();

    private void AddWeapon() => Weapons.Add(new WeaponViewModel(
        new WeaponProfile { Name = "新武器" },
        OnInputChanged,
        RemoveWeapon,
        AddWeaponFocus,
        AddWeaponSpecialization));

    private void RemoveWeapon(WeaponViewModel weapon) => Weapons.Remove(weapon);

    /// <summary>武器卡快捷：为本武器添加「武器专攻」（+1 攻击，描述符 UntypedStackable）。</summary>
    private void AddWeaponFocus(WeaponViewModel weapon) =>
        AddScopedWeaponModifier(weapon, "武器专攻", ModifierDescriptor.UntypedStackable, CombatStat.Attack, 1);

    /// <summary>武器卡快捷：为本武器添加「武器专精」（+2 伤害，描述符 UntypedStackable）。</summary>
    private void AddWeaponSpecialization(WeaponViewModel weapon) =>
        AddScopedWeaponModifier(weapon, "武器专精", ModifierDescriptor.UntypedStackable, CombatStat.Damage, 2);

    private void AddScopedWeaponModifier(
        WeaponViewModel weapon,
        string feat,
        ModifierDescriptor descriptor,
        CombatStat stat,
        int value) =>
        Bonuses.Add(new BonusEntryViewModel(
            new ModifierEntry
            {
                Origin = BonusOrigin.Preset,
                Name = $"{feat}（{weapon.Name}）",
                Descriptor = descriptor,
                Stat = stat,
                Value = value,
                WeaponId = weapon.Id,
            },
            OnInputChanged,
            RemoveBonus));

    private void NewCharacter() => SelectedCharacter = _roster.Create();

    private void DeleteCharacter()
    {
        if (_selectedCharacter is null)
        {
            return;
        }

        SelectedCharacter = _roster.Delete(_selectedCharacter);
    }

    private void DuplicateCharacter()
    {
        if (_selectedCharacter is null)
        {
            return;
        }

        SelectedCharacter = _roster.Duplicate(BuildProfile());
    }

    private void LoadProfile(CharacterProfile profile)
    {
        profile.ApplyDefaults();

        _loading = true;
        try
        {
            _profileId = profile.Id;
            _schemaVersion = profile.SchemaVersion;
            ExchangePenalty = 1;
            SelectedPenaltySource = CombatOptions.PenaltySourceOption(PenaltySource.WeaponAttack);
            BonusMultiplierText = "1.0";
            SelectedBonusDestination = CombatOptions.BonusDestinationOption(BonusDestination.Damage);
            ExchangeValidationError = null;
            this.RaisePropertyChanged(nameof(CanAddExchange));
            Name = profile.Name;
            Level = profile.Level;
            SelectedSize = CombatOptions.Size(profile.Size);
            SelectedCastingAbility = CombatOptions.Ability(profile.CastingAbility);
            Strength = profile.Abilities.Strength;
            Dexterity = profile.Abilities.Dexterity;
            Constitution = profile.Abilities.Constitution;
            Intelligence = profile.Abilities.Intelligence;
            Wisdom = profile.Abilities.Wisdom;
            Charisma = profile.Abilities.Charisma;
            BaseAttackBonus = profile.BaseAttackBonus;
            BaseFortitude = profile.BaseFortitude;
            BaseReflex = profile.BaseReflex;
            BaseWill = profile.BaseWill;
            LimitDex = profile.MaxDexBonus is not null;
            MaxDexBonus = profile.MaxDexBonus ?? 99;
            CasterLevel = profile.CasterLevel;
            SpellLevel = profile.SpellLevel;
            UseDexForManeuvers = profile.UseDexForManeuvers;

            Bonuses.Clear();
            foreach (var modifier in profile.Modifiers)
            {
                Bonuses.Add(new BonusEntryViewModel(modifier, OnInputChanged, RemoveBonus));
            }

            Weapons.Clear();
            foreach (var weapon in profile.Weapons)
            {
                // 快捷按钮（＋武器专攻/专精）必须在这里也挂上回调：
                // 启动/切档都走 LoadProfile，少传则 addFocus/addSpecialization 为 null，
                // 按钮变成空操作（表现为「重启后点不动，删掉武器再加才恢复」）。
                Weapons.Add(new WeaponViewModel(
                    weapon, OnInputChanged, RemoveWeapon, AddWeaponFocus, AddWeaponSpecialization));
            }
        }
        finally
        {
            _loading = false;
        }

        Refresh(save: false);
    }

    /// <summary>等待后台挂起的档案保存完成（测试与退出前使用）。</summary>
    public async Task FlushPendingSavesAsync()
    {
        await _autoSaver.FlushAsync();
        SaveError = _autoSaver.Error;
    }

    private void ApplyWeaponPreset(WeaponPreset preset)
    {
        Weapons.Add(new WeaponViewModel(
            new WeaponProfile
            {
                Name = preset.Weapon.Name,
                AttackType = preset.Weapon.AttackType,
                Hand = preset.Weapon.Hand,
                IsSecondary = preset.Weapon.IsSecondary,
                AttackBonusStat = preset.Weapon.AttackBonusStat,
                DamageBonusStat = preset.Weapon.DamageBonusStat,
                BaseDamage = preset.Weapon.BaseDamage,
                WeaponSize = preset.Weapon.WeaponSize,
                DamageDiceSizeShift = preset.Weapon.DamageDiceSizeShift,
                CriticalThreatLow = preset.Weapon.CriticalThreatLow,
                CriticalMultiplier = preset.Weapon.CriticalMultiplier,
                Enhancement = preset.Weapon.Enhancement,
            },
            OnInputChanged,
            RemoveWeapon,
            AddWeaponFocus,
            AddWeaponSpecialization));
    }

    private void Refresh(bool save)
    {
        var profile = BuildProfile();
        ApplySheet(CombatCalculator.Calculate(profile));
        UpdateConcentration();

        if (save)
        {
            _autoSaver.Schedule(profile);
        }
    }

    private void UpdateConcentration()
    {
        ConcentrationDc = ConcentrationCalculator.Dc(
            _concentrationSituation.Value,
            SpellLevel ?? 0,
            DamageTaken,
            CustomDc);
        this.RaisePropertyChanged(nameof(ConcentrationCheckDisplay));
    }

    private void ApplyPreset(CombatPreset preset) =>
        RunBatch(() =>
        {
            foreach (var entry in preset.CreateEntries())
            {
                Bonuses.Add(new BonusEntryViewModel(entry, OnInputChanged, RemoveBonus));
            }
        });

    private void ApplySheet(CombatSheet sheet)
    {
        Sheet = sheet;
        Cards = BuildCards(sheet);

        var count = Math.Min(Weapons.Count, sheet.Weapons.Count);
        for (var i = 0; i < count; i++)
        {
            Weapons[i].ApplyResult(sheet.Weapons[i]);
        }
    }

    private CharacterProfile BuildProfile() => new()
    {
        SchemaVersion = _schemaVersion,
        Id = _profileId,
        Name = Name,
        Level = Level ?? 1,
        Size = _selectedSize.Value,
        Abilities = new AbilityScores
        {
            Strength = Strength ?? 10,
            Dexterity = Dexterity ?? 10,
            Constitution = Constitution ?? 10,
            Intelligence = Intelligence ?? 10,
            Wisdom = Wisdom ?? 10,
            Charisma = Charisma ?? 10,
        },
        BaseAttackBonus = BaseAttackBonus ?? 0,
        BaseFortitude = BaseFortitude ?? 0,
        BaseReflex = BaseReflex ?? 0,
        BaseWill = BaseWill ?? 0,
        MaxDexBonus = LimitDex ? MaxDexBonus ?? 0 : null,
        CasterLevel = CasterLevel ?? 0,
        SpellLevel = SpellLevel ?? 0,
        CastingAbility = _selectedCastingAbility.Value,
        UseDexForManeuvers = UseDexForManeuvers,
        Modifiers = Bonuses.Select(b => b.ToModel()).ToList(),
        Weapons = Weapons.Select(w => w.ToModel()).ToList(),
    };

    private static IReadOnlyList<StatCard> BuildCards(CombatSheet sheet) =>
    [
        new StatCard("防御等级 AC", sheet.ArmorClass, signed: false),
        new StatCard("接触 AC", sheet.TouchArmorClass, signed: false),
        new StatCard("措手不及 AC", sheet.FlatFootedArmorClass, signed: false),
        new StatCard("措手不及接触 AC", sheet.FlatFootedTouchArmorClass, signed: false),
        new StatCard("强韧", sheet.Fortitude),
        new StatCard("反射", sheet.Reflex),
        new StatCard("意志", sheet.Will),
        new StatCard("CMB", sheet.Cmb),
        new StatCard("CMD", sheet.Cmd, signed: false),
        new StatCard("专注", sheet.Concentration),
        new StatCard("先攻", sheet.Initiative),
        new StatCard("法术 DC", new StatResult(sheet.SpellDc, []), signed: false),
    ];
}
