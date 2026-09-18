# Pathfinder1eHelper

Pathfinder 1e 中文助手 —— 一款基于 Avalonia 的桌面法术查询工具。内置只读法术数据库,支持中英文名搜索与出处/首字母筛选,采用 MVVM + 响应式管道架构。

[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](Pathfinder1eHelper/Pathfinder1eHelper.csproj)

## 功能

- **法术浏览**:只读参考库约 3373 条法术,覆盖 101 个出处(由 pf_searcher_v1.0 数据重建,补充旧库独有法术,以英文名为准去重/补缺)
- **怪物浏览**:怪物图鉴 1-3 约 838 条怪物(含中/英文名、CR、体型、类型、阵营、属性、生态与原文数据块);支持中/英文名搜索与英文首字母/生物类型筛选;概览页(如“龙类绪论”)与所属怪物互相跳转
- **实时搜索**:按中文名/英文名模糊搜索,300ms 防抖
- **筛选**:按出处(`source`)、英文首字母 A–Z 过滤
- **主从详情**:列表 + 详情双栏,展示学派/环位/施法时间/成分/距离/效果/范围/目标/持续时间/豁免/法术抗力/描述/出处等字段
- **可折叠导航**:侧边栏 48×48 图标态 ↔ 展开态,展开宽度自动跟随标题按钮
- **只读安全**:DuckDB `ACCESS_MODE=READ_ONLY` + FreeSql `AutoSyncStructure(false)` + 实体 `DisableSyncStructure` 三重防护,运行时绝不改动参考库

## 技术栈

| 类别 | 选型 |
| --- | --- |
| 运行时/UI | .NET 10 · Avalonia 12 |
| 控件库/主题 | Avalonia Fluent(官方主题)· FluentIcons(FluentUI System Icons) |
| MVVM | ReactiveUI 12(含 `ReactiveUI.Avalonia.Autofac`) |
| DI | Autofac(兼作 Splat/ReactiveUI 定位器) |
| 数据访问 | FreeSql + DuckDB(只读) |
| 测试 | xUnit |

## 项目结构

```
Pathfinder1eHelper.slnx
├── Pathfinder1eHelper/            # Avalonia 主程序
│   ├── Program.cs                 # 入口:构建 AppBuilder + 注册 Autofac 模块
│   ├── App.axaml(.cs)             # 从 DI 解析主窗口
│   ├── Infrastructure/            # AppModule(组合根)、DbPathProvider
│   ├── Data/                      # FreeSqlFactory(只读 DuckDB 连接)
│   ├── Models/                    # Spell 实体(spells 表映射)
│   ├── Services/                  # SpellQuery / ISpellRepository / ISpellService
│   ├── ViewModels/                # MainWindowViewModel、NavItemViewModel、Pages/
│   ├── Views/                     # MainWindow、Pages/SpellsView
│   └── ViewLocator.cs             # ViewModel→View 命名约定解析
└── Pathfinder1eHelper.Test/       # xUnit:服务单测 + 数据库冒烟测试 + VM 测试
```

### 架构要点

- **组合根**:`Program.cs` 通过 `UseReactiveUIWithAutofac` 一次性完成 Autofac 注册、Splat 定位器接管与 ReactiveUI 钩子安装;`Infrastructure/AppModule` 集中注册全部依赖(单例共享 `IFreeSql`,页面惰性工厂)。
- **MVVM + ViewLocator**:命名约定 `*.ViewModels.*ViewModel → *.Views.*View` 解析视图,优先走 DI 容器,支持视图注入。
- **响应式数据流**:`SpellsViewModel` 用 `WhenAnyValue + Throttle(300ms) + DistinctUntilChanged + ObserveOn(MainThread)` 驱动搜索命令,`ThrownExceptions` 兜底展示错误;设计时用示例服务支持 XAML 预览器。

## 数据说明

`data/pathfinder1e.duckdb` 为只读参考数据,**该文件(及数据构建管线 `scripts/`)不在 Git 仓库中**(见 `.gitignore`)。当前库含两部分:**法术**由 **pf_searcher_v1.0** 按出处的法术 JSON 重建(约 3373 条、101 个出处,`first_letter` 由英文名推导;部分来源另提供 `spell_type` 字段);**怪物**由「Pathfinder 怪物图鉴 1-3」CHM 提取(约 838 条,名称照抄原文,`first_letter` 由英文名推导,并保留概览页与所属怪物的关联)。数据文本版权归原著作权人所有;本仓库仅发布代码。

克隆后如未放置数据文件,启动时 `DbPathProvider` 会提示 `Reference database not found`。自行准备数据:将构建好的 `pathfinder1e.duckdb` 放到仓库根 `data/` 目录并重新构建即可(`csproj` 会以 `CopyToOutputDirectory=PreserveNewest` 复制到输出目录)。重建脚本临时存放于本机(依赖 Node.js 与 duckdb CLI),未纳入版本控制。

### 参考库表结构

| 表 | 说明 |
| --- | --- |
| `spells` | 法术主表(约 3373 行):出处、中/英文名、首字母,以及学派/环位/施法时间/成分/距离/效果/范围/目标/持续时间/豁免/法术抗力/描述/附加/`spell_type` 等字段 |
| `spell_levels` | 法术按职业/领域拆分的环位:`spell_id`、`class_name`、`level`、`kind`(`class` = 主职业,`domain` = 领域/子域),供“职业 + 环位”筛选 |
| `spell_buffs` | 常见 Buff 法术的结构化加值效果,供战斗页“法术 Buff 联动”使用 |
| `monsters` | 怪物主表(约 838 行):`source`(B1/B2/B3)、`page`、中/英文名、`first_letter`、CR、体型、类型/亚种、阵营、六属性、环境/组织/宝物、描述、`stat_block`/`special_abilities`/`raw_text`(原文兜底) |
| `monster_groups` | 概览页(“绪论/概述”,如 龙类绪论):`name_zh`/`name_en`、`description`(风味描述)、`content`(可渲染正文:规则段落 + GFM 管道表)、`raw_text` |
| `monster_group_members` | 概览页 ↔ 怪物 的多对多关系(`group_id`、`monster_id`) |

`monsters` 索引:`idx_monsters_first_letter`(首字母)、`idx_monsters_name_en`/`idx_monsters_name_zh`;概览关系索引:`idx_mgm_group`、`idx_mgm_monster`,以及 `monster_groups` 的名称索引。按需求**不建出处索引**。怪物数据由 `scripts/build-monster-db/`(extract_monsters.mjs + schema.sql + load.sql + build.sh)提取;概览页与成员的关联在 CHM 中没有显式链接,由“英文名前缀 + 龙类/发条/特里埃人工种子”生成。

`spell_buffs` 列定义:

| 列 | 说明 |
| --- | --- |
| `id` / `spell_id` | 主键 / 由 `name_en` 关联 `spells.id`(可空) |
| `name_en` / `name_zh` | 匹配键:法术英文名(忽略大小写)/ 中文名 |
| `effect_name` | 生成的加值条目名称 |
| `bonus_type` / `target` | 加值类型 / 作用目标(对应代码中的 `BonusType` / `BonusTarget` 枚举名) |
| `value` | 固定加值(存在 `scale_*` 时作为备用) |
| `enhancement_subject` / `ability` | 增强对象(`EnhancementSubject`)/ 属性(`Ability`),可空 |
| `scale_base` `scale_offset` `scale_step` `scale_min` `scale_max` | 按施法者等级线性缩放的参数 |
| `notes` / `sort_order` | 备注 / 排序 |

缩放公式:`value = clamp(scale_base + floor((CL − scale_offset) / scale_step), scale_min, scale_max)`;`scale_step` 为空时直接使用 `value`。例如树皮术 `scale = 2,3,3,2,5`,CL9 → +4 天生护甲(增强)。

`spell_buffs` 由 `scripts/build-spell-db/spell_buffs.sql` 建表并写入(当前 20 个法术 / 47 条效果),运行时只读;应用端实体为 `Pathfinder1eHelper/Models/SpellBuff.cs`,展开逻辑见 `Pathfinder1eHelper/Services/SpellBuffResolver.cs`。

## 构建与运行

需要 .NET 10 SDK。

```bash
dotnet restore
dotnet build Pathfinder1eHelper.slnx

# 运行(需先准备 data/spells.duckdb)
dotnet run --project Pathfinder1eHelper
```

## 测试

```bash
dotnet test Pathfinder1eHelper.slnx
```

测试包含三类:服务层单元测试(Fake 仓储)、`SpellDatabaseSmokeTests` 连真实 DuckDB 的集成冒烟测试、`SpellsViewModelTests` 视图模型数据流测试。

## 许可证

[MIT](LICENSE) © 红色海鱼(SEA_Yuch)
