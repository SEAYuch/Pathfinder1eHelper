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
- **持久化**：用户数据（战斗档案）与只读参考库分离，`JsonCharacterRepository` 每角色一个 JSON（`%AppData%\Pathfinder1eHelper\characters`）；历史单文件 `character.json` 首次运行自动迁移为 `.bak`。档案 `SchemaVersion` 现为 **2**（不保留 v1 兼容：旧字段无迁移代码，`bonuses`/旧武器字段会被忽略）。后台写入是 fire-and-forget（页面停用时 flush）。
- **领域计算（已对齐《开拓者：正义之怒》DLL）**：
  - `Models/Combat/ModifierDescriptor` = 游戏 `ModifierDescriptor` 全量（去 `Difficulty`/`DLC3_Stackable`）；可叠加集合见 `ModifierDescriptorHelper`（10 项，与 DLL 一致，去 `DLC3_Stackable`）。`CombatStat` = 游戏 `StatType` 战斗子集（攻击命中统一为 `Attack`，近战/远程/接触共用，一切按描述符叠加判定）；`ModifierEntry`（Descriptor + Stat + Ability + Value + StackMode）取代旧 `BonusEntry`。
  - `Services/ModifierEngine.Evaluate` 复刻 `ModifiableValue.ApplyModifiersFiltered`：按描述符分组，可叠加求和、非叠加「最大正值 + 最小负值」同时生效，护甲/负重负值全局只取一次 `min`，`StackMode` 可强制覆盖。`ModifiableValue`/`AttributeValue`（`Bonus=值/2-5`）为可叠加数值基类。
  - `Services/Rules/RuleCalculate*`（攻击/AC/豁免/先攻/CMB/CMD/武器/猛力攻击）、`RuleCheckConcentration` 复刻游戏同名规则；`CombatCalculator` 按 `ModifierEntry.Stat` 路由修饰后调用之。均无状态、可单测。武器伤害骰经 `WeaponDamageScaleTable`（逐行照搬 DLL）按 `WeaponProfile.WeaponSize`（可 `DamageDiceSizeShift` 偏移）缩放。
  - **武器专属修饰**：`ModifierEntry.WeaponId`（null=全部武器）用于武器专攻/专精等；`CombatCalculator` 按「全局 + 该武器专属」拆分后送给武器规则。⚠️ 构造 `WeaponViewModel` 的**每一处**都必须传 `addFocus`/`addSpecialization` 回调（`AddWeapon`、预设、`LoadProfile`）——少传则快捷按钮变空操作，表现为「重启后点不动，删掉武器再加才恢复」，回归测试 `Weapon_focus_buttons_work_on_weapons_loaded_from_disk`。
  - **规则计算型专长（`ModifierKind`）**：数值不进 `feat_buffs.value`，由 `Services/Rules/Rule*` 按 BAB/握法/姿态展开，UI 卡片只读（`BonusEntryViewModel.IsRuleComputed` + `RuleHint`）。现三种：
    - `PowerAttack`（猛力攻击）——`RuleCalculatePowerAttack`：攻击 `-(1+BAB/4)`、伤害 `2×(1+BAB/4)`（副手 ÷2、双手 ×3/2、描述符 `UntypedStackable`），**仅近战攻击检定、不入 CMB**。
    - `CombatExpertise`（寓守于攻）——`RuleCalculateCombatExpertise`：档位 `1+BAB/4`，近战攻击与**战技（CMB）**各 `-档位`、AC `+档位` 闪避（描述符 `Dodge`，故与「闪避」专长求和、也受措手不及 AC 的闪避过滤影响）。CHM 限定「只能以近战武器攻击或全力攻击时」，故减值只加在非远程武器上，AC 加值整轮生效。**神话/高等猛力攻击未实现**。
    - `CraneStyle`（白鹤拳）——`RuleCalculateCraneStyle`：CHM「你在进行防御式战斗时攻击骰只受 -2 减值」。检测到启用的 `DefensiveFighting` 条目时，把它的 −4 攻击减值**补偿 +2** 抬回 −2；补偿走**同一条 `Attack` 通道**，故武器攻击与 CMB 一致抵消（CHM 说的是「攻击骰」）。没有防御式战斗条目时**不生效**（否则凭空 +2）。白鹤拳的「AC 闪避 +1」与基准无关，作为**普通**条目单列一行。
    - `TwoWeaponFighting`（双武器格斗）——`RuleCalculateTwoWeaponFighting`：**照游戏 DLL 的 `TwoWeaponFightingAttackPenalty` 实现**（rank 1 → 主手 −4 / 副手 −8；副手非轻型武器再 −2；rank>1 非神话 → −2/−2；rank>1 神话 → 0）。需同时存在**副手武器**（`IsTwoWeaponing`）才生效。⚠️ 该方法返回**正数减值量**——`RuleCalculateAttackBonus.AttackPenalty` 的约定是「正数、由它取负」，而 DLL 里是负的 modifier 值，别把符号搞反（曾因此把减值变成加值）。武器轻型/中型/重型由新字段 `WeaponProfile.Category`（`WeaponCategory`）提供，**与 `WeaponSize`（`SizeCategory` 体型，用于伤害骰缩放）是两回事**；双头武器由 `WeaponProfile.IsDouble`（DLL `Blueprint.Double`）提供——主手持双头时副手不再因非轻型追加减值。另复刻 `TwoWeaponFightingDamagePenalty`：**副手伤害加值减半**（`RuleCalculateWeaponStats.HalfDamageBonus`，只减半属性×倍率+增强+修饰，基础骰不减半，合计向下取整减半并补一行差额使明细仍对得上；`DoubleSlice` 职业特性豁免未建模）。
  - 专注 DC：`ConcentrationCalculator`/`RuleCheckConcentration`——防御式 `15+2×环位`、受伤 `10+环位+伤害/2`、施法困难（含擒抱）`15+环位`、施法极难（含被压制）`15+2×环位`（对应 DLL `UnitCondition.SpellCastingIsDifficult/VeryDifficult`；游戏无「擒抱者 CMB」项）。
  - `SpellBuffResolver` 把 `bonus_type`/`target` 字符串直接解析为 `ModifierDescriptor`/`CombatStat`（数据表已用规范命名，解析失败回退 `None`/`ArmorClass`）。
- **战斗页协作类**（`ViewModels/Combat/`）：`CharacterRoster`（列表增删改+命名）、`CombatBuffLibrary`（法术 Buff 候选惰性载入与解析）、`CharacterAutoSaver`（后台写回）、`BonusEntryViewModel`/`WeaponViewModel` 等编辑项 VM。页面（`Views/Pages/CombatView.axaml`）按分区排布：角色档案 / 基础数值 / 战斗数值卡 / 状态预设 / 修饰明细 / 武器 / Buff 联动 / 专注；修饰与武器为带字段标签的紧凑编辑器，武器卡内含攻击/伤害组成明细与「＋武器专攻/＋武器专精」快捷按钮（生成带 `WeaponId` 的 scoped 条目）；「猛力攻击」作为状态预设的 buff 条目（`ModifierKind.PowerAttack`）加入修饰明细，勾选启用/取消即失效；其卡片沿用普通卡样式，仅把描述符/通道/叠加/数值几栏置为只读并加一行自动计算说明。`spell_buffs`/`feat_buffs` 的 `kind` 列（`ModifierKind`）可让专长/法术 Buff 直接产出该类型条目（如 `feat_buffs` 的猛力攻击行）。战斗数值卡不含通用攻击卡（攻击按武器单独计算）。
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

> 状态：**已实施 v1 + v2**（主章节 9 个出处 + MA 神话专长 + 根目录 54 个 `专长*.htm` 后续书籍，合计约 1639 条 / 47 个出处；`UI` 页详述仍缺失，见待办）。
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
- **二期已实施**：
  - **MA 神话专长**：取 `page_624.html` 详述（行结构：`中文名（神话/超魔）` → `English` → `(Mythic)` → 风味 → `先决条件：` → `好处：`），source = `MA`，约 160 条。
  - **根目录 54 个 `专长*.htm`**（后续书籍）：单行化后全局扫描表头（兼容全角/半角括号、`〔〕/【】/(战斗专长)`、`中文名 English` 空格与无空格混排）；字段标记兼容 `先决条件 ：`（冒号前有空格）等变体；出处取 CHM `.hhc` 目录父节点（有缩写代码用代码，否则用书名）。异构建条目经质量过滤（英文名首字母大写、无括号/数字/出处残留、正文须含字段标记），约 309 条、cross-source 37 个。
  - 字段标记：先决条件兼容 `先决条件：/前置条件：/前提条件：/先决：/需求：`（部分专长在灰色描述里以 `前提条件：` 给出先决条件）；故事专长（`〔故事〕`）的 `即时收益/专长目标/完成收益` 归入 `benefit`（详述），不留在 `prerequisites`。
  - 名称清洗：清除全角括号碎片与悬空括号（en 去全角 `（）`、去首尾半角 `)`；zh 去尾悬 `（`），`first_letter` 随之重算——冒烟测试 `Feat_names_have_no_dangling_brackets` 锁定。
  - 后续书籍页面格式差异极大，部分页面可解析条目较少或为 0，属预期（best-effort）。

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

- `feat_buffs` 只收录**能映射到现有 `ModifierDescriptor/CombatStat/Ability` 枚举**的专长（首版建议：闪避→AC+1 闪避、精通先攻→先攻+4 无类型、强韧加强/闪电反射/钢铁意志→对应豁免+2 等，规模类似 spell_buffs 的 20 法术/47 行起步）；不能映射的（技能加值、HP、按武器生效如武器专攻）不入表，走“未收录→手动加值”兜底，与 `spell_buffs` 行为一致。

### 构建管线（`scripts/build-feat-db/`，沿用 build-monster-db 模式）

`extract_feats.mjs <解包目录> <输出.jsonl>`：GBK 解码 + 命名实体解码（可复用 extract_monsters.mjs 的 `decodeEntities`）；按“页面结构”解析：简表行给 `name_en/name_zh/is_fighter_bonus/prerequisites/summary`，详述段落按 `中文名（English）〔类型〕` 分段、`先决条件：/专长效果：` 前缀切字段，两表按 `中文名` 对齐；`schema.sql` + `load.sql` 建表装载数据；`feat_buffs.sql` 手工策展行；最后把表**并入**现有 `pathfinder1e.duckdb`（应用只认这一个文件）。注意 UI 页表头在第 2 行、ARG 表头叫“种族专长”。

### 应用侧改动清单

- **导航（三处同步，见上文架构要点）**：`ViewLocator.Map` 加 `FeatsViewModel → FeatsView`；`AppModule` 注册 `FeatRepository/FeatService/FeatsViewModel/FeatsView`；`MainWindowViewModel` 构造注入 `Func<FeatsViewModel>` 并把 `NavItem("专长", Icon.Medal, featsFactory)` 插到法术之后（同时改无参设计时构造与 `MainWindowViewModelTests`）。
- **Models**：`Feat`、`FeatBuff` 实体，均带 `[Table(DisableSyncStructure = true)]` 与显式 `[Column(Name=...)]`（只读三重防护）；`IBuffEffect` 为 `SpellBuff`/`FeatBuff` 的共享契约，供 `SpellBuffResolver` 统一解析为 `ModifierEntry`。
- **Services**：`FeatQuery(Term, Source, FirstLetter, Type, Skip, Take)` record；`IFeatRepository/FeatRepository`（`NameZh.Contains || NameEn.ToLower().Contains || Prerequisites.ToLower().Contains`，`OrderBy(NameEn)`，`DISTINCT` 下推用于出处/类型下拉）；`IFeatService/FeatService`（归一化 + 默认页大小 200）。
- **ViewModels**：`FeatsViewModel : ViewModelBase, IPageViewModel`，完整复刻 `SpellsViewModel` 形态：搜索防抖管道（Taskpool 执行、结果回主线程）、`PageSize/HasMoreResults/LoadMoreCommand` + 总数优化、`HashSet` 去重的懒加载过滤器（出处/首字母/类型）、`DesignTime` 无参构造。
- **Views**：`FeatsView.axaml` 复刻 `SpellsView` 布局（列表 + 详情 + “加载更多”）；详情字段：名称/类型/是否战士奖励专长/先决条件/简述/专长效果/出处。
- **战斗联动**：`BonusOrigin` 增加 `FeatBuff` 成员（旧存档枚举为字符串序列化，不受影响）；战斗页含**两个并列 Buff 模块**——「法术 Buff 联动」（`spell_buffs`）与「专长 Buff 联动」（`feat_buffs`），各自由 `CombatBuffLibrary.SpellCandidates` / `FeatCandidates` 提供候选，`AddSpellBuffAsync` / `AddFeatBuffAsync` 解析为修饰条目（可逐条 ✕ 删除；不再提供「清除 Buff 联动」批量按钮）。
- **测试**：`FeatDatabaseSmokeTests`（`TestDatabase.SkipIfUnavailable()`；行数阈值、猛力攻击/Power Attack 命中、首字母/出处/类型筛选收窄、`feat_buffs` 闪避→AC 映射）；`FeatsViewModelTests`（哨兵、管道映射、分页）；战斗联动新 origin 的增删测试。Fake 加 `FakeFeatService/FakeFeatRepository`（按 Skip/Take 切片）进 `TestDoubles.cs`。

### 实施顺序建议

1. 管线先行：解包 → `extract_feats.mjs` → 并入 duckdb → 冒烟测试锁行为（行数阈值防回归）。
2. 查询页：Models/Services/VM/View + 导航三处同步 + VM 测试。
3. 战斗联动：`feat_buffs.sql` 策展 + `BonusOrigin.FeatBuff` + 库合并与清除语义 + 测试。
4. 二期（已实施）：MA 神话专长、54 个 `专长*.htm`（异构解析器 + 出处回填）。

## 已知待办 / 暂缓项

- **神话武器专攻/专精、神话（高等）猛力攻击未实现**：现只做 CRB 三个（武器专攻/专精/猛力攻击）；神话版需额外字段（神话阶层、伤害翻倍/取整规则）与 `MythicPowerAttack`/`GreaterPowerAttack` 分支。
- **专长 `UI` 页详述缺失**（`page_856.html` 仅简表；其正文非标准段落结构，如需补全须另解析）。
- **`extract_feats.mjs` 已升级 v2（本地脚本；仓库无 scripts/）**：补齐名称清洗（en 去全角 `（）` 碎片、去首尾半角 `)`；zh 去尾悬 `（`，`first_letter` 重算）、MA 神话专长解析（`page_624`）、54 个 `专长*.htm` 异构解析（出处取 `.hhc` 父节点，代码缺失时回退书名）。冒烟测试 `Feat_names_have_no_dangling_brackets` + MA/后续书籍用例锁定；重建 `feats` 表须用该版脚本。
- `CombatViewModel` 仍有约 20 个表单标量属性（表单 VM 固有形态）。若继续瘦身，可抽 `CombatEditorViewModel` 并同步改 `CombatView.axaml` 绑定路径（编译期绑定会校验）。
- Buff 候选（`CombatBuffLibrary.SpellCandidates` / `FeatCandidates`）仍载入完整 `Spell`/`Feat` 实体；改投影 DTO 需同步改 `CombatView.axaml` 与测试的 `SelectedSpellBuff`/`SelectedFeatBuff` 类型。
- `spell_buffs`/`feat_buffs` 的 `bonus_type`/`target` 已改用 `ModifierDescriptor`/`CombatStat` 命名（含攻击统一为 `Attack`、同源 Melee/Ranged 行已合并；`scripts/*/*_buffs.sql` 与本地 `data/pathfinder1e.duckdb` 已同步）。
- **规则计算型专长待办**：遍历 `feats` 表（1639 条 / 47 出处）后判定，尚需规则代码的核心战斗专长为：震慑拳（额外震慑豁免 + 自定义 DC，需新增「额外豁免」模型）、旋风攻击（全力攻击→每人一次满 BAB 攻击，需多段攻击模型）。**双武器防御不做**（CHM「+1 盾牌，防御姿态 +2」，游戏未实现，收益小）。**精通重击**（逐武器重击范围）与**武器娴熟**（攻击改用敏捷）**不做**——两者都能在武器卡上直接调整（武器已有 `CriticalThreatLow` 与 `AttackBonusStat`），无需规则代码。**职业专长**（ACG 剑客、UC 火枪手等）依赖本应用没有的职业等级模型，暂不做。**MA 神话层**（160 条）全部按神话阶层缩放，需新增阶层输入。规则数值一律以 CHM（`feats.benefit`）为准，DLL 的 `UnitFact` 只提供机制（per-feat 逻辑在 Unity 蓝图里，代码中查不到），且 DLL 含大量游戏原创专长（KPoP/Kingdom），不可作为专长清单来源。
- **双武器战斗（CHM `rpage_0008.htm`）与 DLL 的数值冲突**：CHM 的「表：双持武器战斗」给出**有专长时主手 −4 / 副手 −4**（副手轻型 −2/−2），而 DLL 的 `TwoWeaponFightingAttackPenalty` 是**主手 −4 / 副手 −8**（副手非轻型再 −2）。已按用户指示**以 DLL 为准**。CHM 该页散文（基准 −6/−10）与它自己的表也算不出 −4/−4，故以表/DLL 为准。此外 CHM 与 DLL 都还有这些**未建模**项：每轮额外副手攻击（`TwoWeaponFightingAttacks`，需多段攻击模型；且它按 fact rank>2 才生效，而 CHM 只有 CRB 的 rank 1）、Shield Master 持盾豁免（本应用无盾牌/战斗风格模型）、EffortlessDualWielding（职业特性）、双头武器的「另一头算一次副手攻击」。
- **闪避/Dodge 的已知缺口**：`feat_buffs` 中「闪避」的备注写了「失去 AC 的敏捷加值时同时失去此加值」，但 `RuleCalculateArmorClass` 目前只按 `MaxDexBonusFromArmor` 截断敏捷加值，**未**连带剔除 `Dodge` 描述符的加值。寓守于攻的 AC 加值也走 `Dodge`，故同样不受此规则影响。修这条会改变既有闪避/盾牌专攻行为，需单独评估。
- **战斗姿态（防御式战斗/全防御）**：按 **CHM「战斗机动」章节原文**做成**预设 buff 条目**（`CombatPresets`），不是档案状态——`防御式战斗` = AC **+2 闪避**（`Dodge`）/ 攻击 **−4**（`Penalty`）；`全防御` = AC **+4 闪避**，**无攻击减值**（CHM 只写 AC，且明确「无法同时进行防御式攻击、借机攻击，也无法从『寓守于攻』获益」）。姿态之间/姿态与专长的互斥靠 `ModifierEntry.Stance`（`CombatStance?` 标签）识别：`CombatCalculator` 见启用的 `TotalDefense` 条目即**完全跳过** `CombatExpertise`（CHM 明文），而 `DefensiveFighting` **不**压制寓守于攻。⚠️ 这套基准**不是**桌面版 CRB 的 +1/−1 与 +3/−3——本 CHM（KPoP/Kingdom 口径）已重平衡，**数值只能取自 CHM 原文**：`rpage_0003.htm`（GBK，需 `iconv`）。该页的战斗机动表也**不含防具检定减值**，别再照抄桌面版的 −2/−4。游戏 DLL 的 `FightingDefensively*Property` 基准同样是 **−4 攻击 / +2 AC**（与 CHM 一致，只是额外按 `Mobility技能≥3` 再 +1 AC），可作交叉验证但非来源。
  - ⚠️ **`CombatPresets.All` 的条目是全局共享的可变实例**：应用预设必须走 `CombatPreset.CreateEntries()`（深拷贝，含 `Stance`/`WeaponId` 等全部字段），**不要**在 `ApplyPreset` 里手写字段拷贝——曾漏拷 `Stance` 导致互斥失效。测试也不得直接改动 `preset.Entries`。
- **白鹤流派（UC 流浪者）与先决条件**：流派专长（白鹤拳/白鹤亮翅/白鹤弹腿）**一律不校验先决条件**，交使用者判断——档案没有职业/等级字段，而 CHM 的先决多为「BAB+N **或** 武僧 N 级」，本就无法判定。收录策略：白鹤拳按上文拆两行（`CraneStyle` 规则行 + 普通 `Dodge/ArmorClass +1` 行），白鹤亮翅取 `Dodge/ArmorClass +4`，CHM 的限定条件（仅对近战、需空手、近战失手 ≤4 点即失去等）写进 `notes`。**白鹤弹腿不入表**：它没有任何静态加值（借机攻击的反应动作），按「不能映射则不收录」惯例跳过。⚠️ 一个专长可对应**多行** `feat_buffs`（`ResolveFeatAsync` 按行全加），`FeatBuffResolver` 亦按行解析，故混排 `kind` 与普通行是安全的。
- 英文搜索用 `ToLower().Contains(...)`（无索引可利用）；若改 DuckDB `ILIKE` 需裸 SQL 与转义。
- 低价值增强未做：`MonsterTextView` 表格可视化不虚拟化；`ObservableCollection` 已在筛选处改用 `HashSet` 去重。
