using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Pathfinder1eHelper.Models;
using Pathfinder1eHelper.Services;
using ReactiveUI;
using ReactiveUI.Primitives; // Throttle, ObserveOn, DistinctUntilChanged, DisposeWith, Subscribe

namespace Pathfinder1eHelper.ViewModels.Pages;

/// <summary>
/// Spell browser page: a reactive, debounced search over <see cref="ISpellService"/> feeding a
/// master-detail view (list + selected-spell detail).
/// </summary>
public sealed class SpellsViewModel : ViewModelBase, IPageViewModel
{
    /// <summary>ReactiveUI 路由：宿主屏幕（由 shell 在导航前赋值）。</summary>
    public IScreen HostScreen { get; set; } = null!;

    /// <summary>ReactiveUI 路由标识。</summary>
    public string UrlPathSegment => "spells";

    /// <summary>Sentinel shown in the source filter that means "no source filter".</summary>
    public const string AllSources = "（全部）";

    /// <summary>Sentinel shown in the first-letter filter that means "all letters".</summary>
    public const string AllLetters = "全部";

    /// <summary>Sentinel shown in the class/domain filter that means "all classes/domains".</summary>
    public const string AllClasses = "全部职业/领域";

    /// <summary>Sentinel shown in the level filter that means "all levels".</summary>
    public const string AllLevels = "全部环位";

    /// <summary>默认每页条数；“加载更多”按 <see cref="PageSizeStep"/> 递增。</summary>
    public const int DefaultPageSize = 200;

    private const int PageSizeStep = 200;

    private readonly ISpellService _spells;
    private readonly ObservableAsPropertyHelper<bool> _isBusy;
    private readonly HashSet<string> _knownSources = new(StringComparer.Ordinal);
    private readonly HashSet<string> _knownClasses = new(StringComparer.Ordinal);

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

        SearchCommand = ReactiveCommand.CreateFromTask<SpellQuery, SpellPage>(SearchAsync);
        _isBusy = SearchCommand.IsExecuting.ToProperty(this, x => x.IsBusy);

        LoadMoreCommand = ReactiveCommand.CreateFromTask(
            LoadMoreAsync,
            this.WhenAnyValue(x => x.HasMoreResults));

        this.WhenActivated(disposables =>
        {
            // 页面停用时取消过滤器的一次性载入与执行中的搜索。
            var loadCts = new CancellationTokenSource();
            disposables.Add(ActionDisposable.Create(loadCts.Cancel));

            // Populate the source & class filters once per activation (self-contained error handling).
            _ = LoadSourcesAsync(loadCts.Token);
            _ = LoadClassesAsync(loadCts.Token);

            // Debounced query stream: any filter change -> a normalized query -> the search command.
            // Throttle 已在任务线程池上投递，命令因此也在后台执行数据库查询；结果再切回主线程应用。
            this.WhenAnyValue(
                    x => x.SearchText,
                    x => x.SelectedSource,
                    x => x.SelectedLetter,
                    x => x.SelectedClass,
                    x => x.SelectedLevel,
                    (_, _, _, _, _) => BuildQuery())
                .Throttle(TimeSpan.FromMilliseconds(300), RxSchedulers.TaskpoolScheduler)
                .DistinctUntilChanged()
                .InvokeCommand(SearchCommand)
                .DisposeWith(disposables);

            // Apply results on the UI thread.
            SearchCommand
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(ApplyPage)
                .DisposeWith(disposables);

            // Surface async failures instead of tearing down the pipeline.
            SearchCommand.ThrownExceptions
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(ex => LastError = ex.Message)
                .DisposeWith(disposables);
        });
    }

    private SpellQuery BuildQuery() => new(
        SearchText,
        SelectedSource == AllSources ? null : SelectedSource,
        SelectedLetter == AllLetters ? null : SelectedLetter,
        Skip: 0,
        Take: PageSize,
        ClassName: SelectedClass == AllClasses ? null : SelectedClass,
        ClassLevel: int.TryParse(SelectedLevel, out var level) ? level : null);

    private async Task<SpellPage> SearchAsync(SpellQuery query, CancellationToken ct)
    {
        var items = await _spells.SearchAsync(query, ct);
        // 结果不足一页即说明已到末尾，无需再查总数；否则查询真实总数用于提示与“加载更多”。
        var total = items.Count < query.Take ? items.Count : await _spells.CountAsync(query, ct);
        return new SpellPage(items, total);
    }

    private async Task LoadMoreAsync(CancellationToken ct)
    {
        try
        {
            PageSize += PageSizeStep;
            var page = await Task.Run(() => SearchAsync(BuildQuery(), ct), ct);
            ct.ThrowIfCancellationRequested();
            ApplyPage(page);
        }
        catch (OperationCanceledException)
        {
            // 页面停用：忽略。
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }
    }

    private void ApplyPage(SpellPage page)
    {
        Spells.Clear();
        foreach (var spell in page.Items)
        {
            Spells.Add(spell);
        }

        SelectedSpell = Spells.Count > 0 ? Spells[0] : null;
        HasMoreResults = Spells.Count < page.Total;
        ResultSummary = HasMoreResults
            ? $"共 {page.Total} 条法术，已显示前 {Spells.Count} 条"
            : $"共 {page.Total} 条法术";
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

    public ReactiveCommand<SpellQuery, SpellPage> SearchCommand { get; }

    /// <summary>“加载更多”：按 <see cref="PageSizeStep"/> 扩大页大小后重新查询（仅在还有更多时可用）。</summary>
    public ICommand LoadMoreCommand { get; }

    /// <summary>当前页大小；“加载更多”每次递增 <see cref="PageSizeStep"/>。</summary>
    public int PageSize { get; private set; } = DefaultPageSize;

    /// <summary>是否还有未显示的匹配结果（由 <see cref="ApplyPage"/> 更新）。</summary>
    public bool HasMoreResults
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool IsBusy => _isBusy.Value;

    public string? SearchText
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? SelectedSource
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = AllSources;

    public string? SelectedLetter
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = AllLetters;

    public string? SelectedClass
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = AllClasses;

    public string? SelectedLevel
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = AllLevels;

    public Spell? SelectedSpell
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string ResultSummary
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    public string? LastError
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    private async Task LoadSourcesAsync(CancellationToken ct)
    {
        try
        {
            var sources = await Task.Run(() => _spells.GetSourcesAsync(ct), ct);
            ct.ThrowIfCancellationRequested();
            foreach (var source in sources)
            {
                if (_knownSources.Add(source))
                {
                    Sources.Add(source);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 页面停用：忽略。
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }
    }

    private async Task LoadClassesAsync(CancellationToken ct)
    {
        try
        {
            var classes = await Task.Run(() => _spells.GetClassesAsync(ct), ct);
            ct.ThrowIfCancellationRequested();
            foreach (var className in classes)
            {
                if (_knownClasses.Add(className))
                {
                    Classes.Add(className);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 页面停用：忽略。
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }
    }
}

/// <summary>一页法术结果：当前页条目与满足筛选条件的总数。</summary>
public sealed record SpellPage(IReadOnlyList<Spell> Items, int Total);