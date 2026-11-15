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
using ReactiveUI.Primitives;

namespace Pathfinder1eHelper.ViewModels.Pages;

/// <summary>
/// Feat browser page: a reactive, debounced search over <see cref="IFeatService"/> feeding a
/// master-detail view (list + selected-feat detail). Mirrors <see cref="SpellsViewModel"/>.
/// </summary>
public sealed class FeatsViewModel : ViewModelBase, IPageViewModel
{
    /// <summary>ReactiveUI 路由：宿主屏幕（由 shell 在导航前赋值）。</summary>
    public IScreen HostScreen { get; set; } = null!;

    /// <summary>ReactiveUI 路由标识。</summary>
    public string UrlPathSegment => "feats";

    /// <summary>Sentinel shown in the source filter that means "no source filter".</summary>
    public const string AllSources = "（全部）";

    /// <summary>Sentinel shown in the first-letter filter that means "all letters".</summary>
    public const string AllLetters = "全部";

    /// <summary>Sentinel shown in the type filter that means "all types".</summary>
    public const string AllTypes = "全部类型";

    /// <summary>首批查询的默认条数；后续批次从当前结果末尾按页追加。</summary>
    public const int DefaultPageSize = Pagination.DefaultPageSize;

    private readonly IFeatService _feats;
    private readonly ObservableAsPropertyHelper<bool> _isBusy;
    private readonly ObservableAsPropertyHelper<bool> _isLoadingMore;
    private readonly HashSet<string> _knownSources = new(StringComparer.Ordinal);
    private readonly HashSet<string> _knownTypes = new(StringComparer.Ordinal);

    public FeatsViewModel(IFeatService feats)
    {
        _feats = feats;

        Feats = [];
        Sources = [AllSources];
        Letters = [.. new[] { AllLetters }.Concat(Enumerable.Range('A', 26).Select(c => ((char)c).ToString()))];
        Types = [AllTypes];

        SearchCommand = ReactiveCommand.CreateFromTask<FeatQuery, FeatPage>(SearchAsync);
        _isBusy = SearchCommand.IsExecuting.ToProperty(this, x => x.IsBusy);
        ClearFiltersCommand = ReactiveCommand.Create(
            ClearFilters,
            this.WhenAnyValue(
                x => x.SearchText,
                x => x.SelectedSource,
                x => x.SelectedLetter,
                x => x.SelectedType,
                (search, source, letter, type) =>
                    !string.IsNullOrWhiteSpace(search)
                    || source != AllSources
                    || letter != AllLetters
                    || type != AllTypes));

        var loadMoreCommand = ReactiveCommand.CreateFromTask(
            LoadMoreAsync,
            this.WhenAnyValue(
                x => x.HasMoreResults,
                x => x.IsBusy,
                (hasMore, isBusy) => hasMore && !isBusy));
        LoadMoreCommand = loadMoreCommand;
        _isLoadingMore = loadMoreCommand.IsExecuting.ToProperty(this, x => x.IsLoadingMore);

        this.WhenActivated(disposables =>
        {
            var loadCts = new CancellationTokenSource();
            disposables.Add(ActionDisposable.Create(loadCts.Cancel));

            _ = LoadSourcesAsync(loadCts.Token);
            _ = LoadTypesAsync(loadCts.Token);

            // Debounced query stream: any filter change -> a query -> the search command on the taskpool.
            this.WhenAnyValue(
                    x => x.SearchText,
                    x => x.SelectedSource,
                    x => x.SelectedLetter,
                    x => x.SelectedType,
                    (_, _, _, _) => BuildQuery())
                .Throttle(TimeSpan.FromMilliseconds(300), RxSchedulers.TaskpoolScheduler)
                .DistinctUntilChanged()
                .InvokeCommand(SearchCommand)
                .DisposeWith(disposables);

            SearchCommand
                .IsExecuting
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(isExecuting =>
                {
                    if (isExecuting)
                    {
                        HasNoResults = false;
                        LastError = null;
                    }
                })
                .DisposeWith(disposables);

            SearchCommand
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(ApplyPage)
                .DisposeWith(disposables);

            SearchCommand.ThrownExceptions
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(ex => LastError = ex.Message)
                .DisposeWith(disposables);
        });
    }

    /// <summary>Design-time constructor: sample data so the XAML previewer renders without DI.</summary>
    public FeatsViewModel() : this(DesignTimeFeatService.Instance)
    {
        Sources.Add("CRB");
        Types.Add("战斗");
        var sample = new Feat
        {
            Id = 1,
            Source = "CRB",
            NameEn = "Power Attack",
            NameZh = "猛力攻击",
            FirstLetter = "P",
            FeatType = "战斗",
            IsFighterBonus = true,
            Prerequisites = "力量13，基本攻击加值+1。",
            Summary = "以近战攻击加值换取伤害加值",
            Flavor = "通过牺牲准度来换取力道，你能够做出异常致命的近战攻击。",
            Benefit = "你可以选择在所有近战攻击和战技检定上承受 -1 减值，以在所有近战伤害检定上获得 +2 加值……",
        };
        Feats.Add(sample);
        SelectedFeat = sample;
        ResultSummary = "共 1 条专长";
        HasNoResults = false;
    }

    public ObservableCollection<Feat> Feats { get; }

    public ObservableCollection<string> Sources { get; }

    /// <summary>英文首字母筛选项（“全部” + A–Z）。</summary>
    public ObservableCollection<string> Letters { get; }

    /// <summary>专长类型筛选项（“全部类型” + feat_type 去重）。</summary>
    public ObservableCollection<string> Types { get; }

    public ReactiveCommand<FeatQuery, FeatPage> SearchCommand { get; }

    /// <summary>清除文本搜索与所有筛选条件。</summary>
    public ICommand ClearFiltersCommand { get; }

    /// <summary>加载下一批结果并追加到当前列表（仅在还有更多结果且当前未搜索时可用）。</summary>
    public ICommand LoadMoreCommand { get; }

    /// <summary>首批查询的结果条数；后续批次从已加载列表末尾追加。</summary>
    public int PageSize { get; private set; } = DefaultPageSize;

    /// <summary>是否还有未显示的匹配结果（由 <see cref="ApplyPage"/> 更新）。</summary>
    public bool HasMoreResults
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool IsBusy => _isBusy.Value;

    public bool IsLoadingMore => _isLoadingMore.Value;

    public bool HasNoResults
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

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

    public string? SelectedType
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = AllTypes;

    public Feat? SelectedFeat
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

    private void ClearFilters()
    {
        SearchText = null;
        SelectedSource = AllSources;
        SelectedLetter = AllLetters;
        SelectedType = AllTypes;
    }

    private FeatQuery BuildQuery() => new(
        SearchText,
        SelectedSource == AllSources ? null : SelectedSource,
        SelectedLetter == AllLetters ? null : SelectedLetter,
        SelectedType == AllTypes ? null : SelectedType,
        Skip: 0,
        Take: PageSize);

    private async Task<FeatPage> SearchAsync(FeatQuery query, CancellationToken ct)
    {
        var page = await Pagination.FetchAsync(
            () => _feats.SearchAsync(query, ct),
            () => _feats.CountAsync(query, ct),
            query.Take);
        return new FeatPage(page.Items, page.Total);
    }

    private async Task LoadMoreAsync(CancellationToken ct)
    {
        try
        {
            if (!HasMoreResults || IsBusy)
            {
                return;
            }

            var alreadyLoaded = Feats.Count;
            var currentQuery = BuildQuery();
            var query = currentQuery with { Skip = alreadyLoaded, Take = Pagination.PageSizeStep };
            var page = await Task.Run(
                () => Pagination.FetchNextAsync(
                    () => _feats.SearchAsync(query, ct),
                    () => _feats.CountAsync(query, ct),
                    query.Take,
                    alreadyLoaded),
                ct);
            ct.ThrowIfCancellationRequested();

            if (alreadyLoaded != Feats.Count || currentQuery != BuildQuery())
            {
                return;
            }

            Pagination.Append(Feats, page.Items);
            HasMoreResults = Feats.Count < page.Total;
            HasNoResults = Feats.Count == 0;
            ResultSummary = Pagination.Summarize("专长", page.Total, Feats.Count);
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

    private void ApplyPage(FeatPage page)
    {
        Pagination.Replace(Feats, page.Items);

        SelectedFeat = Feats.Count > 0 ? Feats[0] : null;
        HasMoreResults = Feats.Count < page.Total;
        HasNoResults = Feats.Count == 0;
        ResultSummary = Pagination.Summarize("专长", page.Total, Feats.Count);
    }

    private async Task LoadSourcesAsync(CancellationToken ct)
    {
        try
        {
            var sources = await Task.Run(() => _feats.GetSourcesAsync(ct), ct);
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

    private async Task LoadTypesAsync(CancellationToken ct)
    {
        try
        {
            var types = await Task.Run(() => _feats.GetTypesAsync(ct), ct);
            ct.ThrowIfCancellationRequested();
            foreach (var type in types)
            {
                if (_knownTypes.Add(type))
                {
                    Types.Add(type);
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

/// <summary>一页专长结果：当前页条目与满足筛选条件的总数。</summary>
public sealed record FeatPage(IReadOnlyList<Feat> Items, int Total);
