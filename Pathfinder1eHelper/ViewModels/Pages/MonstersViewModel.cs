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

    private readonly IMonsterService _monsters;
    private readonly ObservableAsPropertyHelper<bool> _isBusy;

    public MonstersViewModel(IMonsterService monsters)
    {
        _monsters = monsters;

        Monsters = [];
        RelatedGroups = [];
        GroupMembers = [];
        Letters = [.. new[] { AllLetters }.Concat(Enumerable.Range('A', 26).Select(c => ((char)c).ToString()))];
        CreatureTypes = [AllTypes];

        SearchCommand = ReactiveCommand.CreateFromTask<MonsterQuery, IReadOnlyList<Monster>>((query, ct) =>
            _monsters.SearchAsync(query, ct));
        _isBusy = SearchCommand.IsExecuting.ToProperty(this, x => x.IsBusy);

        OpenGroupCommand = ReactiveCommand.CreateFromTask<MonsterGroup>(OpenGroupAsync);
        CloseGroupCommand = ReactiveCommand.Create(() => SelectedGroup = null);
        OpenMonsterCommand = ReactiveCommand.Create<Monster>(OpenMonster);

        this.WhenActivated(disposables =>
        {
            _ = LoadCreatureTypesAsync();

            this.WhenAnyValue(
                    x => x.SearchText,
                    x => x.SelectedLetter,
                    x => x.SelectedCreatureType,
                    (term, letter, type) => new MonsterQuery(
                        term,
                        letter == AllLetters ? null : letter,
                        type == AllTypes ? null : type,
                        Skip: 0,
                        Take: 200))
                .Throttle(TimeSpan.FromMilliseconds(300), RxSchedulers.TaskpoolScheduler)
                .DistinctUntilChanged()
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .InvokeCommand(SearchCommand)
                .DisposeWith(disposables);

            SearchCommand.Subscribe(list =>
            {
                Monsters.Clear();
                foreach (var monster in list)
                {
                    Monsters.Add(monster);
                }

                SelectedMonster = Monsters.Count > 0 ? Monsters[0] : null;
                ResultSummary = list.Count >= 200
                    ? "显示前 200 条怪物（可用筛选缩小范围）"
                    : $"共 {list.Count} 条怪物";
            }).DisposeWith(disposables);

            SearchCommand.ThrownExceptions
                .Subscribe(ex => LastError = ex.Message)
                .DisposeWith(disposables);

            this.WhenAnyValue(x => x.SelectedMonster)
                .Subscribe(monster => _ = LoadRelatedGroupsAsync(monster))
                .DisposeWith(disposables);
        });
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

    public ReactiveCommand<MonsterQuery, IReadOnlyList<Monster>> SearchCommand { get; }

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

    private async Task LoadCreatureTypesAsync()
    {
        try
        {
            var types = await _monsters.GetCreatureTypesAsync();
            foreach (var type in types)
            {
                if (!CreatureTypes.Contains(type))
                {
                    CreatureTypes.Add(type);
                }
            }
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }
    }

    private async Task LoadRelatedGroupsAsync(Monster? monster)
    {
        RelatedGroups.Clear();
        if (monster is null)
        {
            return;
        }

        try
        {
            var groups = await _monsters.GetGroupsForMonsterAsync(monster.Id);
            foreach (var group in groups)
            {
                RelatedGroups.Add(group);
            }
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }
    }

    private async Task OpenGroupAsync(MonsterGroup? group)
    {
        if (group is null)
        {
            return;
        }

        try
        {
            var members = await _monsters.GetGroupMembersAsync(group.Id);
            GroupMembers.Clear();
            foreach (var member in members)
            {
                GroupMembers.Add(member);
            }

            SelectedGroup = group;
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

    /// <summary>Minimal no-op service used only by the design-time constructor.</summary>
    private sealed class DesignTimeMonsterService : IMonsterService
    {
        public static readonly DesignTimeMonsterService Instance = new();

        public Task<IReadOnlyList<Monster>> SearchAsync(MonsterQuery query, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Monster>>([]);

        public Task<int> CountAsync(MonsterQuery query, CancellationToken ct = default) => Task.FromResult(0);

        public Task<IReadOnlyList<string>> GetCreatureTypesAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<string>>([]);

        public Task<IReadOnlyList<MonsterGroup>> GetGroupsForMonsterAsync(int monsterId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<MonsterGroup>>([]);

        public Task<IReadOnlyList<Monster>> GetGroupMembersAsync(int groupId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Monster>>([]);
    }
}
