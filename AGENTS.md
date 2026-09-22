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

## 专长（Feats）功能

> 状态：**已实施 v1**（主章节 9 个出处，约 1178 条；MA 神话专长与根目录 54 个 `专长*.htm` 系列留待二期）。
> 入口在「法术」下方（导航顺序：法术 → **专长** → 战斗 → 怪物）。
> 数据源：`C:\Users\sea_y\OneDrive\Documents\跑团\Pathfinder v2.20 SC.chm`（34MB，GBK，Word 导出 HTML）。
> 以下「勘察」小节为已核实的事实，其余为设计约定。

### 源数据勘察（已核实）

- **解包**：`hh.exe -decompile` 在当前环境不产文件；用 NanaZip/7z 可解：`7z x -o<dir> "Pathfinder v2.20 SC.chm"`（约 2100 个文件）。目录树在 `Pathfinder v2.20 SC.hhc`（GBK）。
- **统一格式的主章节页面**（“专长”章节按书分页，v1 范围）：

  | 书 | 页面 | 简表行数（勘察值） |
  | --- | --- | --- |
  | CRB 核心规则手册 | `page_195.html` | 180 |
  | APG 进阶玩家手册 | `page_196.html` | 171 |
  | ARG 进阶种族手册 | `page_197.html` | 185（表头为“种族专长”） |
  | UM 极限魔法 | `page_198.html` | 待编脚本时核对 |
  | UC 极限战斗 | `page_199.html` | 同上 |
  | UCa 极限战役 | `page_200.html` | 同上 |
  | ACG 进阶职业手册 | `page_203.html` | 同上 |
  | UI 极限诡道 | `page_856.html` | 118（外层多包一层表，真正表头在**第 2 行**） |
  | B1 怪物图鉴 | `page_201.html` | 同上 |

- **页面结构**（上述页面一致）：① 简表 `<table>`，列 = `[专长名称(EN+ZH), 先决条件, 专长效果简述]`，英文名尾部 `*` = 战士奖励专长；② 详述段落序列：`中文名（English）〔类型〕` → 风味句 →（可选）`先决条件：…` → `专长效果：…`（可多段，或含 `特殊/普通` 段）。类型标签如 `〔战斗〕〔团队〕〔流派〕〔超魔〕〔造物〕`，无标签 = 通用。
- **暂缓到二期**：MA 神话专长（`page_623/624.html`，嵌套表 + 纯文本详述，格式异构）；根目录 54 个 `专长*.htm`（后续书籍：惧怖冒险、恶棍志等，标记混用 `〔〕/【】/（）`、`先决条件/前置条件` 不一，出处需从“出自《…》”内联文本或 TOC 父节点取）。

### 数据库设计（并入现有 `data/pathfinder1e.duckdb`）

```sql
-- feats 主表（同源专长可能多行，沿用 spells 的“按出处保留”策略）
CREATE TABLE feats (
  id INTEGER PRIMARY KEY,
  source TEXT,            -- CRB/APG/...（同 spells.source 代码）
  name_zh TEXT, name_en TEXT,
  first_letter TEXT,      -- 由 name_en 推导 A–Z，取不到为 '#'
  feat_type TEXT,         -- 〔类型〕标签，无标签存 '通用'
  is_fighter_bonus BOOLEAN, -- 简表中英文名尾部 '*'
  prerequisites TEXT,     -- 先决条件（可空）
  summary TEXT,           -- 简表一句话效果
  benefit TEXT,           -- 详述“专长效果”全文（含特殊/普通段）
  flavor TEXT             -- 风味句
);
CREATE INDEX idx_feats_first_letter ON feats(first_letter);
CREATE INDEX idx_feats_name_en ON feats(name_en);
CREATE INDEX idx_feats_name_zh ON feats(name_zh);
-- 按需求不建出处索引（同 spells/monsters 约定）

-- feat_buffs：供战斗模块联动，列与 spell_buffs 完全同构
CREATE TABLE feat_buffs (
  id INTEGER PRIMARY KEY,
  feat_id INTEGER,        -- 可空，由 name_en 关联 feats.id
  name_en TEXT, name_zh TEXT,   -- 匹配键（忽略大小写 / 中文精确）
  effect_name TEXT, bonus_type TEXT, target TEXT, value INTEGER,
  enhancement_subject TEXT, ability TEXT,
  scale_base INTEGER, scale_offset INTEGER, scale_step INTEGER,
  scale_min INTEGER, scale_max INTEGER,
  notes TEXT, sort_order INTEGER
);
```

- `feat_buffs` 只收录**能映射到现有 `BonusType/BonusTarget/EnhancementSubject/Ability` 枚举**的专长（首版建议：闪避→AC+1 闪避、精通先攻→先攻+4 无类型、强韧加强/闪电反射/钢铁意志→对应豁免+2 等，规模类似 spell_buffs 的 20 法术/47 行起步）；不能映射的（技能加值、HP、按武器生效如武器专攻）不入表，走“未收录→手动加值”兜底，与 `spell_buffs` 行为一致。

### 构建管线（`scripts/build-feat-db/`，沿用 build-monster-db 模式）

`extract_feats.mjs <解包目录> <输出.jsonl>`：GBK 解码 + 命名实体解码（可复用 extract_monsters.mjs 的 `decodeEntities`）；按“页面结构”解析：简表行给 `name_en/name_zh/is_fighter_bonus/prerequisites/summary`，详述段落按 `中文名（English）〔类型〕` 分段、`先决条件：/专长效果：` 前缀切字段，两表按 `中文名` 对齐；`schema.sql` + `load.sql` 建表装载数据；`feat_buffs.sql` 手工策展行；最后把表**并入**现有 `pathfinder1e.duckdb`（应用只认这一个文件）。注意 UI 页表头在第 2 行、ARG 表头叫“种族专长”。

### 应用侧改动清单

- **导航（三处同步，见上文架构要点）**：`ViewLocator.Map` 加 `FeatsViewModel → FeatsView`；`AppModule` 注册 `FeatRepository/FeatService/FeatsViewModel/FeatsView`；`MainWindowViewModel` 构造注入 `Func<FeatsViewModel>` 并把 `NavItem("专长", Icon.Medal, featsFactory)` 插到法术之后（同时改无参设计时构造与 `MainWindowViewModelTests`）。
- **Models**：`Feat`、`FeatBuff` 实体，均带 `[Table(DisableSyncStructure = true)]` 与显式 `[Column(Name=...)]`（只读三重防护）；`IBuffEffect` 为 `SpellBuff`/`FeatBuff` 的共享契约，供 `SpellBuffResolver` 统一解析。
- **Services**：`FeatQuery(Term, Source, FirstLetter, Type, Skip, Take)` record；`IFeatRepository/FeatRepository`（`NameZh.Contains || NameEn.ToLower().Contains`，`OrderBy(NameEn)`，`DISTINCT` 下推用于出处/类型下拉）；`IFeatService/FeatService`（归一化 + 默认页大小 200）。
- **ViewModels**：`FeatsViewModel : ViewModelBase, IPageViewModel`，完整复刻 `SpellsViewModel` 形态：搜索防抖管道（Taskpool 执行、结果回主线程）、`PageSize/HasMoreResults/LoadMoreCommand` + 总数优化、`HashSet` 去重的懒加载过滤器（出处/首字母/类型）、`DesignTime` 无参构造。
- **Views**：`FeatsView.axaml` 复刻 `SpellsView` 布局（列表 + 详情 + “加载更多”）；详情字段：名称/类型/是否战士奖励专长/先决条件/简述/专长效果/出处。
- **战斗联动**：`BonusOrigin` 增加 `FeatBuff` 成员（旧存档枚举为字符串序列化，不受影响）；`CombatBuffLibrary(ISpellService, IFeatService)` 把法术与专长统一为 `BuffCandidate`（标注 `Kind`）候选，`AddBuffAsync` 按 `Kind` 查 `spell_buffs` / `feat_buffs`；「清除法术 Buff」按钮已改为「清除 Buff 联动」（同时清 `SpellBuff+FeatBuff` origin）。
- **测试**：`FeatDatabaseSmokeTests`（`TestDatabase.SkipIfUnavailable()`；行数阈值、猛力攻击/Power Attack 命中、首字母/出处/类型筛选收窄、`feat_buffs` 闪避→AC 映射）；`FeatsViewModelTests`（哨兵、管道映射、分页）；战斗联动新 origin 的增删测试。Fake 加 `FakeFeatService/FakeFeatRepository`（按 Skip/Take 切片）进 `TestDoubles.cs`。

### 实施顺序建议

1. 管线先行：解包 → `extract_feats.mjs` → 并入 duckdb → 冒烟测试锁行为（行数阈值防回归）。
2. 查询页：Models/Services/VM/View + 导航三处同步 + VM 测试。
3. 战斗联动：`feat_buffs.sql` 策展 + `BonusOrigin.FeatBuff` + 库合并与清除语义 + 测试。
4. 二期（可选）：MA 神话专长、54 个 `专长*.htm`（异构解析器 + 出处回填）。

## 已知待办 / 暂缓项

- **专长功能二期**：MA 神话专长（`page_623/624.html`，嵌套表+纯文本）、根目录 54 个 `专长*.htm`（异构标记/字段名、出处需内联或 TOC 取）；`UI` 页详述缺失（仅简表），如需补全需另解析其结构。
- `CombatViewModel` 仍有约 20 个表单标量属性（表单 VM 固有形态）。若继续瘦身，可抽 `CombatEditorViewModel` 并同步改 `CombatView.axaml` 绑定路径（编译期绑定会校验）。
- Buff 候选（`CombatBuffLibrary.Candidates`）仍载入完整 `Spell` 实体；改投影 DTO 需同步改 `CombatView.axaml` 与测试的 `SelectedBuffSpell` 类型。
- 英文搜索用 `ToLower().Contains(...)`（无索引可利用）；若改 DuckDB `ILIKE` 需裸 SQL 与转义。
- 低价值增强未做：`MonsterTextView` 表格可视化不虚拟化；`ObservableCollection` 已在筛选处改用 `HashSet` 去重。
