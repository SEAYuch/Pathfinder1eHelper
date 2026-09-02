# Pathfinder1eHelper

Pathfinder 1e 中文助手 —— 一款基于 Avalonia 的桌面法术查询工具。内置只读法术数据库,支持中英文名搜索与出处/首字母筛选,采用 MVVM + 响应式管道架构。

[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](Pathfinder1eHelper/Pathfinder1eHelper.csproj)

## 功能

- **法术浏览**:只读参考库约 2965 条法术(来源含 Pathfinder 中文规则与社区 wiki 补充)
- **实时搜索**:按中文名/英文名模糊搜索,300ms 防抖
- **筛选**:按出处(`source`)、英文首字母 A–Z 过滤
- **主从详情**:列表 + 详情双栏,展示学派/环位/施法时间/成分/距离/效果/范围/目标/持续时间/豁免/法术抗力/描述/出处等字段
- **可折叠导航**:侧边栏 48×48 图标态 ↔ 展开态,展开宽度自动跟随标题按钮
- **只读安全**:DuckDB `ACCESS_MODE=READ_ONLY` + FreeSql `AutoSyncStructure(false)` + 实体 `DisableSyncStructure` 三重防护,运行时绝不改动参考库

## 技术栈

| 类别 | 选型 |
| --- | --- |
| 运行时/UI | .NET 10 · Avalonia 12 |
| 控件库/主题 | Ursa · Semi.Avalonia · Material.Icons |
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

`data/spells.duckdb` 为只读参考数据,**该文件(及数据构建管线 `scripts/`)不在 Git 仓库中**(见 `.gitignore`)。数据文本来源于 Pathfinder 中文规则资料与社区 wiki,版权归原著作权人所有;本仓库仅发布代码。

克隆后如未放置数据文件,启动时 `DbPathProvider` 会提示 `Reference database not found`。自行准备数据:将构建好的 `spells.duckdb` 放到仓库根 `data/` 目录并重新构建即可(`csproj` 会以 `CopyToOutputDirectory=PreserveNewest` 复制到输出目录)。

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
