using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Pathfinder1eHelper.Models;
using Pathfinder1eHelper.Models.Combat;
using Pathfinder1eHelper.Services;
using Pathfinder1eHelper.ViewModels.Combat;
using ReactiveUI;

namespace Pathfinder1eHelper.ViewModels.Pages;

/// <summary>
/// 战斗页：管理多个角色档案，编辑基础数据与手动加值/武器，实时用
/// <see cref="CombatCalculator"/> 汇总，并保存到 <see cref="ICharacterRepository"/>。
/// </summary>
public sealed class CombatViewModel : ViewModelBase, IPageViewModel
{
    /// <summary>ReactiveUI 路由：宿主屏幕（由 shell 在导航前赋值）。</summary>
    public IScreen HostScreen { get; set; } = null!;

    /// <summary>ReactiveUI 路由标识。</summary>
    public string UrlPathSegment => "combat";

    private readonly ICharacterRepository _repository;
    private readonly ISpellService _spells;
    private bool _loading;
    private CharacterListItemViewModel? _selectedCharacter;
    private Guid _profileId;
    private int _schemaVersion;

    private SizeOption _selectedSize = CombatOptions.Size(SizeCategory.Medium);
    private AbilityOption _selectedCastingAbility = CombatOptions.Ability(Ability.Intelligence);
    private ConcentrationOption _concentrationSituation = CombatOptions.Concentration(ConcentrationSituation.DefensiveCasting);
    private CharacterProfile? _lastProfile;
    private Spell? _selectedBuffSpell;

    public CombatViewModel(ICharacterRepository repository, ISpellService spells)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _spells = spells ?? throw new ArgumentNullException(nameof(spells));

        BuffSpellCandidates = [];
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
        AddBuffCommand = ReactiveCommand.CreateFromTask(AddBuffAsync);
        ClearBuffsCommand = ReactiveCommand.Create(ClearBuffs);
        ClearAllBonusesCommand = ReactiveCommand.Create(ClearAllBonuses);

        _ = LoadBuffCandidatesAsync();

        Characters = new ObservableCollection<CharacterListItemViewModel>(
            repository.LoadAll().Select(p => new CharacterListItemViewModel(p)));
        if (Characters.Count == 0)
        {
            var profile = CreateNewProfile();
            _repository.Save(profile);
            Characters.Add(new CharacterListItemViewModel(profile));
        }

        _selectedCharacter = Characters[0];
        LoadProfile(_selectedCharacter.Profile);
    }

    /// <summary>设计时构造：示例数据，供 XAML 预览器使用。</summary>
    public CombatViewModel() : this(new DesignTimeCharacterRepository(), DesignTimeSpellService.Instance)
    {
    }

    public ObservableCollection<CharacterListItemViewModel> Characters { get; }

    public ObservableCollection<BonusEntryViewModel> Bonuses { get; }

    public ObservableCollection<WeaponViewModel> Weapons { get; }

    public IReadOnlyList<PresetViewModel> Presets { get; }

    public IReadOnlyList<PresetViewModel> WeaponPresets { get; }

    /// <summary>法术 Buff 搜索候选（一次性载入法术库，由 AutoCompleteBox 在本地过滤）。</summary>
    public ObservableCollection<Spell> BuffSpellCandidates { get; }

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

    public ICommand AddBuffCommand { get; }

    public ICommand ClearBuffsCommand { get; }

    public ICommand ClearAllBonusesCommand { get; }

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

    public int? GrapplerCmb
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
            this.RaisePropertyChanged(nameof(IsGrappleMode));
            OnInputChanged();
        }
    }

    public bool IsCustomDc => _concentrationSituation.Value == ConcentrationSituation.Custom;

    public bool IsDamageMode => _concentrationSituation.Value == ConcentrationSituation.DamageWhileCasting;

    public bool IsGrappleMode => _concentrationSituation.Value == ConcentrationSituation.Grappled;

    public Spell? SelectedBuffSpell
    {
        get => _selectedBuffSpell;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedBuffSpell, value);
            UpdateBuffHint();
        }
    }

    public string BuffHint
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

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

        Refresh(save: true);
    }

    private void AddBonus() => Bonuses.Add(new BonusEntryViewModel(
        new BonusEntry { Name = "新加值", Target = BonusTarget.ArmorClass, Value = 1 },
        OnInputChanged,
        RemoveBonus));

    private void RemoveBonus(BonusEntryViewModel entry) => Bonuses.Remove(entry);

    /// <summary>只清除由“法术 Buff 联动”添加的条目，保留手动与状态预设条目。</summary>
    private void ClearBuffs()
    {
        for (var i = Bonuses.Count - 1; i >= 0; i--)
        {
            if (Bonuses[i].Origin == BonusOrigin.SpellBuff)
            {
                Bonuses.RemoveAt(i);
            }
        }
    }

    /// <summary>清空全部加值条目（含手动条目）。</summary>
    private void ClearAllBonuses() => Bonuses.Clear();

    private void AddWeapon() => Weapons.Add(new WeaponViewModel(
        new WeaponProfile { Name = "新武器" },
        OnInputChanged,
        RemoveWeapon));

    private void RemoveWeapon(WeaponViewModel weapon) => Weapons.Remove(weapon);

    private void NewCharacter()
    {
        var profile = CreateNewProfile();
        _repository.Save(profile);

        var item = new CharacterListItemViewModel(profile);
        Characters.Add(item);
        SelectedCharacter = item;
    }

    private void DeleteCharacter()
    {
        if (_selectedCharacter is null)
        {
            return;
        }

        _repository.Delete(_selectedCharacter.Profile.Id);
        Characters.Remove(_selectedCharacter);

        if (Characters.Count == 0)
        {
            var profile = CreateNewProfile();
            _repository.Save(profile);
            Characters.Add(new CharacterListItemViewModel(profile));
        }

        SelectedCharacter = Characters[0];
    }

    private void DuplicateCharacter()
    {
        if (_selectedCharacter is null)
        {
            return;
        }

        var clone = CharacterCloner.Duplicate(BuildProfile());
        clone.Name = NextDuplicateName(_selectedCharacter.Name);
        _repository.Save(clone);

        var item = new CharacterListItemViewModel(clone);
        Characters.Add(item);
        SelectedCharacter = item;
    }

    private string NextDuplicateName(string baseName)
    {
        var index = 1;
        string name;
        do
        {
            index++;
            name = index == 2 ? $"{baseName} 副本" : $"{baseName} 副本 {index - 1}";
        }
        while (Characters.Any(c => c.Name == name));

        return name;
    }

    private CharacterProfile CreateNewProfile()
    {
        var profile = new CharacterProfile { Name = NextCharacterName() };
        profile.ApplyDefaults();
        return profile;
    }

    private string NextCharacterName()
    {
        var index = Characters.Count + 1;
        string name;
        do
        {
            name = $"新角色 {index++}";
        }
        while (Characters.Any(c => c.Name == name));

        return name;
    }

    private void LoadProfile(CharacterProfile profile)
    {
        profile.ApplyDefaults();

        _loading = true;
        try
        {
            _profileId = profile.Id;
            _schemaVersion = profile.SchemaVersion;
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
            UseDexForManeuvers = profile.UseDexForManeuvers;

            Bonuses.Clear();
            foreach (var bonus in profile.Bonuses)
            {
                Bonuses.Add(new BonusEntryViewModel(bonus, OnInputChanged, RemoveBonus));
            }

            Weapons.Clear();
            foreach (var weapon in profile.Weapons)
            {
                Weapons.Add(new WeaponViewModel(weapon, OnInputChanged, RemoveWeapon));
            }
        }
        finally
        {
            _loading = false;
        }

        Refresh(save: false);
    }

    /// <summary>载入全部法术作为 Buff 搜索候选（AutoCompleteBox 本地过滤，无需服务端搜索）。</summary>
    public async Task LoadBuffCandidatesAsync()
    {
        try
        {
            var all = await _spells.SearchAsync(new SpellQuery(null, null, null, 0, 10000));
            BuffSpellCandidates.Clear();
            foreach (var spell in all)
            {
                BuffSpellCandidates.Add(spell);
            }
        }
        catch (Exception ex)
        {
            SaveError = ex.Message;
        }
    }

    private void UpdateBuffHint()
    {
        if (_selectedBuffSpell is null)
        {
            BuffHint = string.Empty;
            return;
        }

        BuffHint = $"已选择：{_selectedBuffSpell.NameZh}（点击“添加为 Buff”）";
    }

    /// <summary>把所选法术在 <c>spell_buffs</c> 中的效果加入加值明细（供面板与测试调用）。</summary>
    public async Task AddBuffAsync()
    {
        if (_selectedBuffSpell is not { } spell)
        {
            return;
        }

        var sourceLabel = $"《{spell.Source}》{spell.NameEn}";
        IReadOnlyList<SpellBuff> buffs;
        try
        {
            buffs = await _spells.GetBuffsForSpellAsync(spell.NameEn, spell.NameZh);
        }
        catch (Exception ex)
        {
            SaveError = ex.Message;
            return;
        }

        var entries = buffs.Count == 0
            ? new List<BonusEntry>
            {
                new()
                {
                    Origin = BonusOrigin.SpellBuff,
                    Name = spell.NameZh,
                    Type = BonusType.Untyped,
                    Target = BonusTarget.ArmorClass,
                    Value = 1,
                    Notes = $"{sourceLabel}：spell_buffs 未收录，请手动设置类型/目标/数值",
                },
            }
            : buffs.Select(b => SpellBuffResolver.ToEntry(b, CasterLevel ?? 0, sourceLabel)).ToList();

        foreach (var entry in entries)
        {
            Bonuses.Add(new BonusEntryViewModel(entry, OnInputChanged, RemoveBonus));
        }

        BuffHint = buffs.Count == 0
            ? "spell_buffs 未收录：已添加 1 条空白加值"
            : $"已添加 {buffs.Count} 条加值（按 CL {CasterLevel ?? 0}）";
    }

    private void ApplyWeaponPreset(WeaponPreset preset)
    {
        Weapons.Add(new WeaponViewModel(
            new WeaponProfile
            {
                Name = preset.Weapon.Name,
                IsRanged = preset.Weapon.IsRanged,
                DamageDice = preset.Weapon.DamageDice,
                StrengthMultiplier = preset.Weapon.StrengthMultiplier,
                Enhancement = preset.Weapon.Enhancement,
                Critical = preset.Weapon.Critical,
            },
            OnInputChanged,
            RemoveWeapon));
    }

    private void Refresh(bool save)
    {
        var profile = BuildProfile();
        var sheet = CombatCalculator.Calculate(profile);
        ApplySheet(sheet);
        UpdateConcentration();
        _lastProfile = profile;

        if (save)
        {
            TrySave();
        }
    }

    private void UpdateConcentration()
    {
        var spellLevel = SpellLevel ?? 0;
        ConcentrationDc = _concentrationSituation.Value switch
        {
            ConcentrationSituation.DefensiveCasting => 15 + 2 * spellLevel,
            ConcentrationSituation.DamageWhileCasting => 10 + Math.Max(0, DamageTaken ?? 0) + spellLevel,
            ConcentrationSituation.Grappled => 10 + Math.Max(0, GrapplerCmb ?? 0) + spellLevel,
            _ => CustomDc ?? 0,
        };
        this.RaisePropertyChanged(nameof(ConcentrationCheckDisplay));
    }

    private void ApplyPreset(CombatPreset preset)
    {
        foreach (var template in preset.Entries)
        {
            Bonuses.Add(new BonusEntryViewModel(
                new BonusEntry
                {
                    Origin = BonusOrigin.Preset,
                    Name = template.Name,
                    Type = template.Type,
                    Enhancement = template.Enhancement,
                    Target = template.Target,
                    Value = template.Value,
                    IsEnabled = template.IsEnabled,
                    SourceGroup = template.SourceGroup,
                    Notes = template.Notes,
                },
                OnInputChanged,
                RemoveBonus));
        }
    }

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
        CastingAbility = _selectedCastingAbility.Value,
        UseDexForManeuvers = UseDexForManeuvers,
        Bonuses = Bonuses.Select(b => b.ToModel()).ToList(),
        Weapons = Weapons.Select(w => w.ToModel()).ToList(),
    };

    private void TrySave()
    {
        try
        {
            _repository.Save(_lastProfile ?? BuildProfile());
            SaveError = null;
        }
        catch (Exception ex)
        {
            SaveError = ex.Message;
        }
    }

    private static IReadOnlyList<StatCard> BuildCards(CombatSheet sheet) =>
    [
        new StatCard("近战攻击", sheet.MeleeAttack),
        new StatCard("远程攻击", sheet.RangedAttack),
        new StatCard("远程接触攻击", sheet.RangedTouchAttack),
        new StatCard("防御等级 AC", sheet.ArmorClass, signed: false),
        new StatCard("接触 AC", sheet.TouchArmorClass, signed: false),
        new StatCard("措手不及 AC", sheet.FlatFootedArmorClass, signed: false),
        new StatCard("强韧", sheet.Fortitude),
        new StatCard("反射", sheet.Reflex),
        new StatCard("意志", sheet.Will),
        new StatCard("CMB", sheet.Cmb),
        new StatCard("CMD", sheet.Cmd, signed: false),
        new StatCard("专注", sheet.Concentration),
        new StatCard("先攻", sheet.Initiative),
    ];

    private sealed class DesignTimeCharacterRepository : ICharacterRepository
    {
        public string DirectoryPath => "(design-time)";

        public IReadOnlyList<CharacterProfile> LoadAll() => [SampleProfile()];

        public void Save(CharacterProfile profile)
        {
        }

        public void Delete(Guid id)
        {
        }

        private static CharacterProfile SampleProfile()
        {
            var profile = new CharacterProfile
            {
                Name = "示例角色",
                Level = 10,
                Size = SizeCategory.Medium,
                Abilities = new AbilityScores
                {
                    Strength = 20,
                    Dexterity = 14,
                    Constitution = 16,
                    Intelligence = 10,
                    Wisdom = 12,
                    Charisma = 8,
                },
                BaseAttackBonus = 10,
                BaseFortitude = 7,
                BaseReflex = 3,
                BaseWill = 3,
                CasterLevel = 10,
                CastingAbility = Ability.Intelligence,
                Bonuses =
                [
                    new BonusEntry { Name = "武器专攻", Type = BonusType.Untyped, Target = BonusTarget.MeleeAttack, Value = 1 },
                    new BonusEntry { Name = "天生护甲", Type = BonusType.NaturalArmor, Target = BonusTarget.ArmorClass, Value = 1 },
                    new BonusEntry
                    {
                        Name = "树皮术",
                        Type = BonusType.Enhancement,
                        Enhancement = EnhancementSubject.NaturalArmor,
                        Target = BonusTarget.ArmorClass,
                        Value = 3,
                    },
                    new BonusEntry { Name = "掩护", Type = BonusType.Circumstance, Target = BonusTarget.ArmorClass, Value = 4 },
                    new BonusEntry { Name = "勇气激励", Type = BonusType.Competence, Target = BonusTarget.MeleeAttack, Value = 2 },
                    new BonusEntry { Name = "勇气激励", Type = BonusType.Competence, Target = BonusTarget.Damage, Value = 2 },
                ],
                Weapons =
                [
                    new WeaponProfile { Name = "长剑", DamageDice = "1d8", StrengthMultiplier = 1, Enhancement = 1 },
                    new WeaponProfile
                    {
                        Name = "复合长弓",
                        IsRanged = true,
                        DamageDice = "1d8",
                        StrengthMultiplier = 0,
                        Enhancement = 2,
                    },
                ],
            };

            profile.ApplyDefaults();
            return profile;
        }
    }

    private sealed class DesignTimeSpellService : ISpellService
    {
        public static readonly DesignTimeSpellService Instance = new();

        public Task<IReadOnlyList<Spell>> SearchAsync(SpellQuery query, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Spell>>(
            [
                new Spell { Id = 1, NameZh = "祝福术", NameEn = "Bless", Source = "CRB", FirstLetter = "B" },
                new Spell { Id = 2, NameZh = "树皮术", NameEn = "Barkskin", Source = "CRB", FirstLetter = "B" },
            ]);

        public Task<int> CountAsync(SpellQuery query, CancellationToken ct = default) => Task.FromResult(2);

        public Task<IReadOnlyList<string>> GetSourcesAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<string>>([]);

        public Task<IReadOnlyList<string>> GetClassesAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<string>>([]);

        public Task<Spell?> GetByIdAsync(int id, CancellationToken ct = default) => Task.FromResult<Spell?>(null);

        public Task<IReadOnlyList<SpellBuff>> GetBuffsForSpellAsync(string? nameEn, string? nameZh, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<SpellBuff>>([]);
    }
}
