using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pathfinder1eHelper.Models;
using Pathfinder1eHelper.Services;
using ReactiveUI;
using ReactiveUI.Primitives; // Throttle, ObserveOn, DistinctUntilChanged, DisposeWith, Subscribe

namespace Pathfinder1eHelper.ViewModels.Pages;

/// <summary>
/// Spell browser page: a reactive, debounced search over <see cref="ISpellService"/> feeding a
/// master-detail view (list + selected-spell detail).
/// </summary>
public sealed class SpellsViewModel : ViewModelBase
{
    /// <summary>Sentinel shown in the source filter that means "no source filter".</summary>
    public const string AllSources = "（全部）";

    /// <summary>Sentinel shown in the first-letter filter that means "all letters".</summary>
    public const string AllLetters = "全部";

    /// <summary>Sentinel shown in the class/domain filter that means "all classes/domains".</summary>
    public const string AllClasses = "全部职业/领域";

    /// <summary>Sentinel shown in the level filter that means "all levels".</summary>
    public const string AllLevels = "全部环位";

    private readonly ISpellService _spells;
    private readonly ObservableAsPropertyHelper<bool> _isBusy;

    private string? _searchText;
    private string? _selectedSource = AllSources;
    private string? _selectedLetter = AllLetters;
    private string? _selectedClass = AllClasses;
    private string? _selectedLevel = AllLevels;
    private Spell? _selectedSpell;
    private string _resultSummary = "";
    private string? _lastError;

    public SpellsViewModel(ISpellService spells)
    {
        _spells = spells;

        Spells = [];
        Sources = [AllSources];
        Letters =
        [
            .. new[] { AllLetters }.Concat(Enumerable.Range('A', 26).Select(c => ((char)c).ToString()))
        ];
        Classes = [AllClasses];
        // 环位 0–9（PF 1e：0 环戏法 ~ 9 环）。
        Levels =
        [
            .. new[] { AllLevels }.Concat(Enumerable.Range(0, 10).Select(i => i.ToString()))
        ];

        SearchCommand =
            ReactiveCommand.CreateFromTask<SpellQuery, IReadOnlyList<Spell>>((query, ct) =>
                _spells.SearchAsync(query, ct));

        _isBusy = SearchCommand.IsExecuting.ToProperty(this, x => x.IsBusy);

        this.WhenActivated(disposables =>
        {
            // Populate the source & class filters once per activation (self-contained error handling).
            _ = LoadSourcesAsync();
            _ = LoadClassesAsync();

            // Debounced query stream: any filter change -> a normalized query -> the search command.
            this.WhenAnyValue(
                    x => x.SearchText,
                    x => x.SelectedSource,
                    x => x.SelectedLetter,
                    x => x.SelectedClass,
                    x => x.SelectedLevel,
                    (term, source, letter, className, level) => new SpellQuery(
                        term,
                        source == AllSources ? null : source,
                        letter == AllLetters ? null : letter,
                        Skip: 0,
                        Take: 200,
                        ClassName: className == AllClasses ? null : className,
                        ClassLevel: int.TryParse(level, out var parsedLevel) ? parsedLevel : null))
                .Throttle(TimeSpan.FromMilliseconds(300), RxSchedulers.TaskpoolScheduler)
                .DistinctUntilChanged()
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .InvokeCommand(SearchCommand)
                .DisposeWith(disposables);

            // Apply results on the UI thread (ReactiveCommand delivers on the main scheduler).
            SearchCommand.Subscribe(list =>
            {
                Spells.Clear();
                foreach (var spell in list)
                {
                    Spells.Add(spell);
                }

                SelectedSpell = Spells.Count > 0 ? Spells[0] : null;
                ResultSummary = list.Count >= 200
                    ? "显示前 200 条法术（可用筛选缩小范围）"
                    : $"共 {list.Count} 条法术";
            }).DisposeWith(disposables);

            // Surface async failures instead of tearing down the pipeline.
            SearchCommand.ThrownExceptions
                .Subscribe(ex => LastError = ex.Message)
                .DisposeWith(disposables);
        });
    }

    /// <summary>Design-time constructor: sample data so the XAML previewer renders without DI.</summary>
    public SpellsViewModel() : this(DesignTimeSpellService.Instance)
    {
        Sources.Add("CRB");
        Classes.Add("术士/法师");
        Classes.Add("牧师");
        Spells.Add(new Spell
        {
            Id = 705,
            NameEn = "Fireball",
            NameZh = "火球术",
            Source = "CRB",
            FirstLetter = "F",
            School = "塑能系[火]",
            Level = "术士/法师 3",
            CastingTime = "1 标准动作",
            Components = "V, S, M",
            Range = "远距（120尺 + 10尺/等级）",
            Duration = "即时",
            SavingThrow = "反射，取半",
            SpellResistance = "是",
            Description = "一道能量在你的指尖迸发，飞向目标位置炸开成烈焰……",
        });
        SelectedSpell = Spells[0];
        ResultSummary = "共 1 条法术";
    }

    public ObservableCollection<Spell> Spells { get; }

    public ObservableCollection<string> Sources { get; }

    /// <summary>英文首字母筛选项（“全部” + A–Z）。</summary>
    public ObservableCollection<string> Letters { get; }

    /// <summary>职业/领域筛选项（“全部” + spell_levels.class_name 去重）。</summary>
    public ObservableCollection<string> Classes { get; }

    /// <summary>环位筛选项（“全部” + 0–9）。</summary>
    public ObservableCollection<string> Levels { get; }

    public ReactiveCommand<SpellQuery, IReadOnlyList<Spell>> SearchCommand { get; }

    public bool IsBusy => _isBusy.Value;

    public string? SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    public string? SelectedSource
    {
        get => _selectedSource;
        set => this.RaiseAndSetIfChanged(ref _selectedSource, value);
    }

    public string? SelectedLetter
    {
        get => _selectedLetter;
        set => this.RaiseAndSetIfChanged(ref _selectedLetter, value);
    }

    public string? SelectedClass
    {
        get => _selectedClass;
        set => this.RaiseAndSetIfChanged(ref _selectedClass, value);
    }

    public string? SelectedLevel
    {
        get => _selectedLevel;
        set => this.RaiseAndSetIfChanged(ref _selectedLevel, value);
    }

    public Spell? SelectedSpell
    {
        get => _selectedSpell;
        set => this.RaiseAndSetIfChanged(ref _selectedSpell, value);
    }

    public string ResultSummary
    {
        get => _resultSummary;
        private set => this.RaiseAndSetIfChanged(ref _resultSummary, value);
    }

    public string? LastError
    {
        get => _lastError;
        private set => this.RaiseAndSetIfChanged(ref _lastError, value);
    }

    private async Task LoadSourcesAsync()
    {
        try
        {
            var sources = await _spells.GetSourcesAsync();
            foreach (var source in sources)
            {
                if (!Sources.Contains(source))
                {
                    Sources.Add(source);
                }
            }
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }
    }

    private async Task LoadClassesAsync()
    {
        try
        {
            var classes = await _spells.GetClassesAsync();
            foreach (var className in classes)
            {
                if (!Classes.Contains(className))
                {
                    Classes.Add(className);
                }
            }
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }
    }

    /// <summary>Minimal no-op service used only by the design-time constructor.</summary>
    private sealed class DesignTimeSpellService : ISpellService
    {
        public static readonly DesignTimeSpellService Instance = new();

        public Task<IReadOnlyList<Spell>> SearchAsync(SpellQuery query, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Spell>>(Array.Empty<Spell>());

        public Task<int> CountAsync(SpellQuery query, CancellationToken ct = default) => Task.FromResult(0);

        public Task<IReadOnlyList<string>> GetSourcesAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        public Task<IReadOnlyList<string>> GetClassesAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        public Task<Spell?> GetByIdAsync(int id, CancellationToken ct = default) => Task.FromResult<Spell?>(null);

        public Task<IReadOnlyList<SpellBuff>> GetBuffsForSpellAsync(string? nameEn, string? nameZh, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<SpellBuff>>(Array.Empty<SpellBuff>());
    }
}