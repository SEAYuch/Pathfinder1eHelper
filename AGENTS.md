# AGENTS.md

面向在本仓库工作的智能体/开发者的工程说明：架构、施工约定与已知坑。

## 项目概览

Pathfinder 1e 中文助手：基于 Avalonia 的桌面参考工具，含**法术查询**、**怪物图鉴**、**战斗助手**三个页面。数据来自随包的只读 DuckDB 参考库，运行时绝不写入。

## 技术栈

| 类别 | 选型 |
| --- | --- |
| 运行时/UI | .NET 10 · Avalonia 12.1.2 |
| 控件库/主题 | FluentAvaloniaUI 3.1.0（`FluentAvaloniaTheme`）· FluentIcons.Avalonia.Fluent |
| MVVM | ReactiveUI 12（`ReactiveUI.Avalonia` / `ReactiveUI.Avalonia.Autofac`） |
| DI | Autofac（经 `UseReactiveUIWithAutofac` 兼作 Splat/ReactiveUI 定位器） |
| 数据访问 | FreeSql + DuckDB（只读） |
| 测试 | xUnit v3 + Microsoft.Testing.Platform |

## 目录结构

```
Pathfinder1eHelper.slnx
├─ Directory.Build.props        # TreatWarningsAsErrors
├─ Directory.Packages.props     # 中央包版本管理（CPM）
├─ .github/workflows/ci.yml     # CI：restore → Release build → 测试宿主
├─ data/                        # 只读参考库（.gitignore，不入库）
├─ scripts/                     # 数据构建管线（.gitignore，不入库）
├─ Pathfinder1eHelper/
│  ├─ Program.cs                # 入口：AppLog 兜底 + AppBuilder + Autofac 模块
│  ├─ App.axaml(.cs)            # 主题资源；从 DI 解析主窗口
│  ├─ Infrastructure/           # AppModule(组合根)、DbPathProvider、AppLog
│  ├─ Data/FreeSqlFactory.cs    # 只读 IFreeSql 单例
│  ├─ Models/                   # FreeSql 实体 + Combat/ 领域模型
│  ├─ Services/                 # Repository/Service、计算、持久化
│  ├─ ViewModels/               # Shell + Pages + Combat/ 协作类
│  └─ Views/                    # MainWindow、Pages、Controls、ViewLocator
└─ Pathfinder1eHelper.Test/     # xUnit v3（MTP 宿主）
```

## 架构要点

- **组合根**：`Program.BuildAvaloniaApp()` 调用 `UseReactiveUIWithAutofac(builder => builder.RegisterModule<AppModule>())`，一次性完成 Autofac 注册、Splat 定位器接管与 ReactiveUI 钩子安装。
  - ⚠️ **不要**再调用 `.UseReactiveUI()`，否则钩子注册到默认定位器、被 Autofac 定位器遮蔽。
  - 新增依赖统一在 `Infrastructure/AppModule.cs` 注册。
- **导航/路由**：`MainWindowViewModel : IScreen` 持有 `RoutingState`；`NavItems`（`NavItemViewModel`）用 `Func<IPageViewModel>` **惰性创建并缓存**页面 VM；`RoutedViewHost` 渲染。页面 VM 实现 `IPageViewModel`（可写 `HostScreen`）。
- **ViewLocator**：`Views/ViewLocator.cs` 为**显式映射**（`Type → Func<IViewFor>`，无反射，AOT/裁剪安全）。**新增可路由页面时必须同步三处**：`ViewLocator.Map` 加一行、`AppModule` 注册 VM/View、`MainWindowViewModel` 加 `NavItem`。
- **数据访问**：`IFreeSql` 单例、线程安全且只读；Repository/Service 为 `InstancePerDependency`。
  - **只读三重防护**：连接串 `ACCESS_MODE=READ_ONLY` + `UseAutoSyncStructure(false)` + 实体 `[Table(DisableSyncStructure = true)]`。任何新实体都要带该特性。
- **持久化**：用户数据（战斗档案）与只读参考库分离，`JsonCharacterRepository` 每角色一个 JSON（`%AppData%\Pathfinder1eHelper\characters`）；历史单文件 `character.json` 首次运行自动迁移为 `.bak`。
- **领域计算**：`CombatCalculator`（纯静态）依赖 `BonusEngine`（叠加规则）、`AbilityResolver`（属性加值）、`SpellBuffResolver`（`spell_buffs` → 加值）、`ConcentrationCalculator`（专注 DC）。均为无状态、可单测。
- **战斗页协作类**（`ViewModels/Combat/`）：`CharacterRoster`（列表增删改+命名）、`CombatBuffLibrary`（法术 Buff 候选惰性载入与解析）、`CharacterAutoSaver`（后台写回）、`BonusEntryViewModel`/`WeaponViewModel` 等编辑项 VM。

## 施工约定（Key Conventions）

### 线程与调度
- **数据库查询不在 UI 线程执行**。搜索用管道 `WhenAnyValue(...).Throttle(300ms, TaskpoolScheduler).DistinctUntilChanged().InvokeCommand(cmd)`；命令在任务线程池运行，结果与异常再 `.ObserveOn(RxSchedulers.MainThreadScheduler)` 回主线程更新集合。
- **一次性载入**（筛选下拉、关联数据、法术候选）用 `Task.Run(...)` 移到后台，并配 `CancellationTokenSource`：页面停用时取消；选中项切换时取消上一次过期加载。激活钩子用 `ActionDisposable`（见下）。
- 页面 VM 继承 `ViewModelBase`（`ReactiveObject + IActivatableViewModel`），生命周期逻辑放 `this.WhenActivated(disposables => ...)`。
- 不依赖 `System.Reactive` 的 `Disposable.Create`（未直接引用）：用 `ViewModels/ActionDisposable.cs`。

### 保存
- 档案编辑**不**同步落盘：`CombatViewModel` 经 `CharacterAutoSaver.Schedule(snapshot)` 合并为“最多一次在途保存”，后台线程写盘；显式操作（新建/复制/删除）仍同步保存。
- 测试改值后需 `await vm.FlushPendingSavesAsync()` 再断言 `LastSaved`；页面停用时会自动 flush。

### 分页
- 法术/怪物列表使用 `PageSize`（默认 200、步长 200）、`HasMoreResults`、`LoadMoreCommand` 与 `SpellPage`/`MonsterPage`（含真实总数）。
- 总数优化：结果数 `< Take` 时直接以 `items.Count` 为总数，否则才调 `CountAsync`。不要恢复“始终查总数”。

### XAML / Avalonia
- `AvaloniaUseCompiledBindingsByDefault=true`：绑定路径写错是**编译错误**，改动 VM 属性名会连带编译失败。
- 设计时预览：页面 VM 提供无参构造，使用 `ViewModels/DesignTimeData.cs` 的 `DesignTime*` 假实现；XAML 用 `<Design.DataContext>`。
- **主题坑**：`FluentAvaloniaTheme` 已内置 DataGrid 样式。**不要**再 `StyleInclude` `avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml`——两套 DataGrid 主题叠加会让列表**列标题（表头）变透明/不可见**。
- 自定义主题色（如 `AppDividerBrush`、`AppDangerBrush`）在 `App.axaml` 的 `ThemeDictionaries` 里按 Light/Dark 定义。

### 数据/查询
- 去重与排序尽量下推 SQL（`.Distinct().OrderBy(...)`），不要把整列拉进内存再处理。
- 实体字段多为可空字符串；映射列名用 `[Column(Name = "...")]` 显式声明。

### 依赖与告警
- **中央包版本管理**：版本写在 `Directory.Packages.props`，csproj 的 `PackageReference` 不写 `Version`。
- `Directory.Build.props` 开启 `TreatWarningsAsErrors=true`，主干必须零告警。分析器级别保持 SDK 默认，**不要**上调到 `latest-recommended`（会引入大量 CA1304/CA1305/CA1859 噪声）。

### 测试
- **运行方式**：仓库用 Microsoft.Testing.Platform 宿主（`global.json` 指定）。用
  `dotnet run --project Pathfinder1eHelper.Test`
  —— `dotnet test` 在当前配置下会“运行了零个测试”。
- 程序集级初始化在 `TestAppFixture`（xUnit v3 assembly fixture）：启动 headless Avalonia + 以 `ImmediateSequencer` 作主线程调度器（命令输出同步送达）。
  - ⚠️ 重型初始化**不要**放 `[ModuleInitializer]`，会在 MTP 发现阶段卡住。
- Fake 实现集中 `TestDoubles.cs`；`FakeSpellService`/`FakeMonsterService` 按 `Skip/Take` 切片以支持分页断言。
- **冒烟测试**连真实 DuckDB；数据文件不在版本库，缺失时 `TestDatabase.SkipIfUnavailable()` 动态跳过（CI 可无库通过）。需要真实库的测试都用它，不要直接 `new DbPathProvider()`。
- `xUnit1051` 已统一关闭（样式提示，非正确性问题）。

## 常用命令

```bash
# 构建（0 警告要求）
dotnet build Pathfinder1eHelper.slnx

# 运行（需先准备 data/pathfinder1e.duckdb）
dotnet run --project Pathfinder1eHelper

# 测试（MTP 宿主；缺参考库时冒烟测试自动跳过）
dotnet run --project Pathfinder1eHelper.Test
```

## 已知待办 / 暂缓项

- `CombatViewModel` 仍有约 20 个表单标量属性（表单 VM 固有形态）。若继续瘦身，可抽 `CombatEditorViewModel` 并同步改 `CombatView.axaml` 绑定路径（编译期绑定会校验）。
- Buff 候选（`CombatBuffLibrary.Candidates`）仍载入完整 `Spell` 实体；改投影 DTO 需同步改 `CombatView.axaml` 与测试的 `SelectedBuffSpell` 类型。
- 英文搜索用 `ToLower().Contains(...)`（无索引可利用）；若改 DuckDB `ILIKE` 需裸 SQL 与转义。
- 低价值增强未做：`MonsterTextView` 表格可视化不虚拟化；`ObservableCollection` 已在筛选处改用 `HashSet` 去重。
