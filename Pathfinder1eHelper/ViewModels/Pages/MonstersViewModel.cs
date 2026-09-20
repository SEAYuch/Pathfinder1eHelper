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
/// Monster browser page: debounced search + first-letter / creature-type filters feeding a
/// master-detail view; the detail pane also exposes related “绪论/概述” links and their members.
/// </summary>
public sealed class MonstersViewModel : ViewModelBase, IPageViewModel
{
    /// <summary>ReactiveUI 路由：宿主屏幕（由 shell 在导航前赋值）。</summary>
    public IScreen HostScreen { get; set; } = null!;

    /// <summary>ReactiveUI 路由标识。</summary>
    public string UrlPathSegment => "monsters";

    /// <summary>Sentinel shown in the creature-type filter that means "all types".</summary>
    public const string AllTypes = "全部类型";

    /// <summary>Sentinel shown in the first-letter filter that means "all letters".</summary>
    public const string AllLetters = "全部";

    /// <summary>默认每页条数；“加载更多”按 <see cref="PageSizeStep"/> 递增。</summary>
    public const int DefaultPageSize = 200;

    private const int PageSizeStep = 200;

    private readonly IMonsterService _monsters;
    private readonly ObservableAsPropertyHelper<bool> _isBusy;
    private CancellationTokenSource? _relatedGroupsCts;
    private readonly HashSet<string> _knownCreatureTypes = new(StringComparer.Ordinal);

    public MonstersViewModel(IMonsterService monsters)
    {
        _monsters = monsters;

        Monsters = [];
        RelatedGroups = [];
        GroupMembers = [];
        Letters = [.. new[] { AllLetters }.Concat(Enumerable.Range('A', 26).Select(c => ((char)c).ToString()))];
        CreatureTypes = [AllTypes];

        SearchCommand = ReactiveCommand.CreateFromTask<MonsterQuery, MonsterPage>(SearchAsync);
        _isBusy = SearchCommand.IsExecuting.ToProperty(this, x => x.IsBusy);

        LoadMoreCommand = ReactiveCommand.CreateFromTask(
            LoadMoreAsync,
            this.WhenAnyValue(x => x.HasMoreResults));

        OpenGroupCommand = ReactiveCommand.CreateFromTask<MonsterGroup>((group, ct) => OpenGroupAsync(group, ct));
        CloseGroupCommand = ReactiveCommand.Create(() => SelectedGroup = null);
        OpenMonsterCommand = ReactiveCommand.Create<Monster>(OpenMonster);

        this.WhenActivated(disposables =>
        {
            // 页面停用：取消一次性载入与进行中的关联加载。
            var loadCts = new CancellationTokenSource();
            disposables.Add(ActionDisposable.Create(() =>
            {
                loadCts.Cancel();
                _relatedGroupsCts?.Cancel();
            }));

            _ = LoadCreatureTypesAsync(loadCts.Token);

            this.WhenAnyValue(
                    x => x.SearchText,
                    x => x.SelectedLetter,
                    x => x.SelectedCreatureType,
                    (_, _, _) => BuildQuery())
                .Throttle(TimeSpan.FromMilliseconds(300), RxSchedulers.TaskpoolScheduler)
                .DistinctUntilChanged()
                .InvokeCommand(SearchCommand)
                .DisposeWith(disposables);

            SearchCommand
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(ApplyPage)
                .DisposeWith(disposables);

            SearchCommand.ThrownExceptions
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(ex => LastError = ex.Message)
                .DisposeWith(disposables);

            this.WhenAnyValue(x => x.SelectedMonster)
                .Subscribe(OnSelectedMonsterChanged)
                .DisposeWith(disposables);
        });
    }

    private MonsterQuery BuildQuery() => new(
        SearchText,
        SelectedLetter == AllLetters ? null : SelectedLetter,
        SelectedCreatureType == AllTypes ? null : SelectedCreatureType,
        Skip: 0,
        Take: PageSize);

    private async Task<MonsterPage> SearchAsync(MonsterQuery query, CancellationToken ct)
    {
        var items = await _monsters.SearchAsync(query, ct);
        // 结果不足一页即说明已到末尾，无需再查总数；否则查询真实总数用于提示与“加载更多”。
        var total = items.Count < query.Take ? items.Count : await _monsters.CountAsync(query, ct);
        return new MonsterPage(items, total);
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

    private void ApplyPage(MonsterPage page)
    {
        Monsters.Clear();
        foreach (var monster in page.Items)
        {
            Monsters.Add(monster);
        }

        SelectedMonster = Monsters.Count > 0 ? Monsters[0] : null;
        HasMoreResults = Monsters.Count < page.Total;
        ResultSummary = HasMoreResults
            ? $"共 {page.Total} 条怪物，已显示前 {Monsters.Count} 条"
            : $"共 {page.Total} 条怪物";
    }

    /// <summary>Design-time constructor: sample data so the XAML previewer renders without DI.</summary>
    public MonstersViewModel() : this(DesignTimeMonsterService.Instance)
    {
        var sample = new Monster
        {
            Id = 1,
            Source = "B1",
            NameZh = "彩色龙，黑龙",
            NameEn = "Chromatic Dragon, Black",
            FirstLetter = "C",
            Cr = "3",
            Size = "超小型",
            CreatureType = "龙类",
            Alignment = "混乱邪恶",
            Description = "巨龙身披黑色鳞片，头上生角；绿色酸液自齿间滴落。",
        };
        Monsters.Add(sample);
        SelectedMonster = sample;
        CreatureTypes.Add("龙类");
        ResultSummary = "共 1 条怪物";
    }

    public ObservableCollection<Monster> Monsters { get; }

    public ObservableCollection<string> Letters { get; }

    public ObservableCollection<string> CreatureTypes { get; }

    /// <summary>绪论(概述)链接：当前怪物所属的概论页。</summary>
    public ObservableCollection<MonsterGroup> RelatedGroups { get; }

    /// <summary>当前所展开概论页的成员。</summary>
    public ObservableCollection<Monster> GroupMembers { get; }

    public ReactiveCommand<MonsterQuery, MonsterPage> SearchCommand { get; }

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

    public ICommand OpenGroupCommand { get; }

    public ICommand CloseGroupCommand { get; }

    public ICommand OpenMonsterCommand { get; }

    public bool IsBusy => _isBusy.Value;

    public string? SearchText
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? SelectedLetter
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = AllLetters;

    public string? SelectedCreatureType
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = AllTypes;

    public Monster? SelectedMonster
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>非空时详情区切换为“概论”视图（描述 + 成员链接）。</summary>
    public MonsterGroup? SelectedGroup
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string ResultSummary
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    } = "";

    public string? LastError
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    private async Task LoadCreatureTypesAsync(CancellationToken ct)
    {
        try
        {
            var types = await Task.Run(() => _monsters.GetCreatureTypesAsync(ct), ct);
            ct.ThrowIfCancellationRequested();
            foreach (var type in types)
            {
                if (_knownCreatureTypes.Add(type))
                {
                    CreatureTypes.Add(type);
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

    /// <summary>选中项变化：取消上一次未完成的关联加载，再启动新的。</summary>
    private void OnSelectedMonsterChanged(Monster? monster)
    {
        _relatedGroupsCts?.Cancel();
        _relatedGroupsCts?.Dispose();
        _relatedGroupsCts = new CancellationTokenSource();

        RelatedGroups.Clear();
        if (monster is not null)
        {
            _ = LoadRelatedGroupsAsync(monster, _relatedGroupsCts.Token);
        }
    }

    private async Task LoadRelatedGroupsAsync(Monster monster, CancellationToken ct)
    {
        try
        {
            var groups = await Task.Run(() => _monsters.GetGroupsForMonsterAsync(monster.Id, ct), ct);
            ct.ThrowIfCancellationRequested();
            foreach (var group in groups)
            {
                RelatedGroups.Add(group);
            }
        }
        catch (OperationCanceledException)
        {
            // 选中项已变化：忽略过期结果。
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }
    }

    private async Task OpenGroupAsync(MonsterGroup? group, CancellationToken ct)
    {
        if (group is null)
        {
            return;
        }

        try
        {
            var members = await Task.Run(() => _monsters.GetGroupMembersAsync(group.Id, ct), ct);
            ct.ThrowIfCancellationRequested();
            GroupMembers.Clear();
            foreach (var member in members)
            {
                GroupMembers.Add(member);
            }

            SelectedGroup = group;
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

    private void OpenMonster(Monster monster)
    {
        SelectedGroup = null;

        // 左侧列表（DataGrid）只能选中集合内的实例；从绪论成员跳转时该实例可能不在列表里，
        // 若直接赋值会被 DataGrid 的 SelectedItem 双向绑定回写成 null（详情空白）。
        // 因此确保目标在集合中存在（同 Id 复用既有实例，否则插入）。
        var target = Monsters.FirstOrDefault(m => m.Id == monster.Id);
        if (target is null)
        {
            Monsters.Insert(0, monster);
            target = monster;
        }

        SelectedMonster = target;
    }
}

/// <summary>一页怪物结果：当前页条目与满足筛选条件的总数。</summary>
public sealed record MonsterPage(IReadOnlyList<Monster> Items, int Total);
