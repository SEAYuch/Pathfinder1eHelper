<#
.SYNOPSIS
    打包 Pathfinder1eHelper：publish 自包含产物 → 校验 → 便携 zip / Velopack 安装包。

.DESCRIPTION
    统一入口。所有版本号与产品名都从 Directory.Build.props 读取，不在此处硬编码；
    参考库在打包前必须通过 `--verify-db` 闸门（版本/结构与程序集一致且无残留 .wal），
    防止「改了版本忘了重跑 --stamp-db」把不配套的数据打进安装包。

    产物（默认写入 artifacts/，该目录已 gitignore）：
      artifacts/<rid>/                              publish 目录（自包含，已剔除 *.pdb）
      artifacts/Pathfinder1eHelper-<ver>-<rid>.zip  便携版，双击即用
      artifacts/releases/                           Velopack：Setup.exe + *.nupkg（+ 可选 *.msi）

.PARAMETER Target
    要打包的目标。可多选；'portable' 额外产出单文件 exe。
      win-x64 / win-arm64 / linux-x64 / linux-arm64 / osx-arm64 / osx-x64 / portable

.PARAMETER Version
    覆盖版本号（默认取 Directory.Build.props 的 <Version>）。仅用于预发布试打包，
    正式发布请改 props 后重跑，以保证与程序集/db_info 同源。

.PARAMETER SkipInstaller
    跳过 Velopack（只出 zip）。未安装 vpk 工具时会自动跳过并提示。

.PARAMETER WithMsi
    额外让 Velopack 生成机器级 MSI 引导包（Velopack 的 --msi）。

.EXAMPLE
    # 多个目标要用 -Command（pwsh -File 会把逗号列表当成一个字符串传入）
    pwsh -NoProfile -Command "& ./packaging/pack.ps1 -Target win-x64,portable -WithMsi"
    pwsh -NoProfile -File ./packaging/pack.ps1 -Target win-x64
    pwsh -NoProfile -Command "& ./packaging/pack.ps1 -Target linux-x64,osx-arm64 -SkipInstaller"
#>
[CmdletBinding()]
param(
    [ValidateSet('win-x64', 'win-arm64', 'linux-x64', 'linux-arm64', 'osx-arm64', 'osx-x64', 'portable')]
    [string[]] $Target = @('win-x64'),

    [string] $Version,

    [string] $OutputDir = 'artifacts',

    [switch] $SkipInstaller,

    [switch] $WithMsi
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# 本脚本输出全是中文，控制台默认代码页（中文 Windows 为 GBK/936）会显示成乱码。
# 与应用侧 MaintenanceMode 的 UseUtf8Console 同一个理由。
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$ProjectPath = Join-Path $RepoRoot 'Pathfinder1eHelper\Pathfinder1eHelper.csproj'
$PropsPath = Join-Path $RepoRoot 'Directory.Build.props'
$ExeName = 'Pathfinder1eHelper'

function Write-Step { param([string]$Message) Write-Host "==> $Message" -ForegroundColor Cyan }
function Write-Ok { param([string]$Message) Write-Host "    $Message" -ForegroundColor Green }
function Write-Warn { param([string]$Message) Write-Host "    $Message" -ForegroundColor Yellow }
function Fail { param([string]$Message) throw $Message }

# ---------------------------------------------------------------- 版本与元数据

# 版本号与产品名的唯一来源是 Directory.Build.props（勿在此处硬编码第二份）。
# 用 XPath 取值而非 XML 的属性适配器（$node.Version）：后者在 Set-StrictMode 下，
# 遇到不含该元素的 PropertyGroup 会直接抛错，且 VersionPrefix 之类易被误匹配。
[xml] $Props = Get-Content -Raw -LiteralPath $PropsPath
function Get-Prop([string]$Name) {
    $node = $Props.SelectSingleNode("//*[local-name()='PropertyGroup']/*[local-name()='$Name']")
    if ($node -and $node.InnerText) { return $node.InnerText.Trim() }
    return $null
}

if (-not $Version) { $Version = Get-Prop 'Version' }
if (-not $Version) { Fail '未能从 Directory.Build.props 读到 <Version>。' }
$ProductName = Get-Prop 'Product'
if (-not $ProductName) { $ProductName = $ExeName }

# Velopack 要求 SemVer；剥离 Directory.Build.props 里可能出现的元数据后缀。
$PackVersion = ($Version -split '[+\-]')[0]
if ($PackVersion -notmatch '^\d+\.\d+\.\d+$') {
    Fail "版本号 '$PackVersion' 不是三段 SemVer，Velopack/MSI 无法接受。"
}

Write-Ok "版本 $Version（Velopack 打包用 $PackVersion）"
Write-Ok "产品 $ProductName"

# ---------------------------------------------------------------- 参考库闸门

# 只读校验：不匹配就中止。这条闸门是打包流水线的核心保护。
Write-Step '校验参考库（--verify-db）'
& dotnet run --project $ProjectPath -c Release --no-launch-profile -- --verify-db | Out-Host
if ($LASTEXITCODE -ne 0) {
    Fail "参考库校验未通过（退出码 $LASTEXITCODE）。请先修好参考库或重跑 --stamp-db。"
}

# ---------------------------------------------------------------- publish 参数

# 自包含：用户机器上没有 .NET 也能跑（macOS 上更是必须，Windows 可选但一致体验更好）。
# 刻意**不开** PublishTrimmed：ReactiveUI 12 靠反射扫属性、FreeSql 靠表达式树拼 SQL、
# DuckDB 驱动靠 P/Invoke，全是 trimmer 盲区，裁剪后编译能过、运行时随机崩。
$CommonPublishArgs = @(
    '--configuration', 'Release',
    '--self-contained', 'true'
)

# 单文件变体：IncludeAllContentForSelfExtract 是必须的，否则 28MB 的
# data/pathfinder1e.duckdb 不会进 bundle，程序启动就会因找不到参考库而失败。
$PortablePublishArgs = @(
    '-p:PublishSingleFile=true',
    '-p:IncludeNativeLibrariesForSelfExtract=true',
    '-p:IncludeAllContentForSelfExtract=true',
    '-p:EnableCompressionInSingleFile=true',
    '-p:DebugType=none'
)

# publish 目录里会残留运行库/原生包自带的 .pdb（libSkiaSharp.pdb 单独就 81MB），
# 它们对分发无用且极大，必须剔除——Velopack 的默认 --exclude 也会再挡一层。
function Remove-DebugSymbols {
    param([string]$PublishDir)
    # 必须用 @(...) 包起来：Get-ChildItem 只命中 1 个文件时返回的是单个对象而非数组，
    # 在 Set-StrictMode 下访问 .Count 会直接抛错。
    $pdbs = @(Get-ChildItem -Path $PublishDir -Filter '*.pdb' -File -Recurse -ErrorAction SilentlyContinue)
    if ($pdbs.Count -gt 0) {
        $bytes = ($pdbs | Measure-Object -Property Length -Sum).Sum
        $pdbs | Remove-Item -Force
        Write-Ok ("剔除 {0} 个 .pdb（{1:N0} MB）" -f $pdbs.Count, ($bytes / 1MB))
    }
}

function Publish-Target {
    param([string]$Rid, [string]$Label, [string[]]$ExtraArgs = @(), [switch]$SingleFile)

    $outDir = Join-Path $OutputDir $Label
    if (Test-Path $outDir) { Remove-Item -Recurse -Force $outDir }

    Write-Step "publish $Label（$Rid）"
    # | Out-Host 很关键：原生命令的 stdout 若留在管道里，会被算进函数的返回值，
    # 污染下面 $published[$rid]（变成「日志行 + 路径」的数组）。
    & dotnet publish $ProjectPath @CommonPublishArgs -r $Rid -o $outDir @ExtraArgs | Out-Host
    if ($LASTEXITCODE -ne 0) { Fail "publish $Label 失败（退出码 $LASTEXITCODE）。" }

    Remove-DebugSymbols -PublishDir $outDir

    $db = Join-Path $outDir 'data\pathfinder1e.duckdb'
    $wal = Join-Path $outDir 'data\pathfinder1e.duckdb.wal'

    if ($SingleFile) {
        # 单文件发布下参考库不以独立文件存在，而是打进 exe、运行时解压到
        # %TEMP%\.net\<app>\<hash>\。所以只能真跑一次 exe，让它报出自己解析到的路径——
        # 这才是「包对了」的真正判据（也正是 DbPathProvider 的运行期契约）。
        $exe = Join-Path $outDir "$ExeName.exe"
        if (-not (Test-Path $exe)) { Fail "portable 产物里没有 $ExeName.exe。" }

        $resolved = (& $exe --print-db-path | Out-String).Trim()
        if ($LASTEXITCODE -ne 0 -or -not $resolved) {
            Fail "portable 单文件里取不到参考库：exe 没能解析出 data\pathfinder1e.duckdb（IncludeAllContentForSelfExtract 是否生效？）"
        }
        Write-Ok "portable 运行期参考库 $resolved"
    }
    else {
        # 参考库必须随包分发（AppContext.BaseDirectory\data\pathfinder1e.duckdb）。
        if (-not (Test-Path $db)) { Fail "$Label 产物里没有 data\pathfinder1e.duckdb —— 参考库没被打进去。" }
        # 残留 .wal 同理：它不会进包，且会让只读打开直接失败。
        if (Test-Path $wal) { Fail "$Label 产物里有残留 .wal —— 重新跑 --stamp-db 生成干净的库。" }
    }

    $size = (Get-ChildItem -Path $outDir -File -Recurse | Measure-Object -Property Length -Sum).Sum
    Write-Ok ("{0} 完成，{1:N0} MB" -f $Label, ($size / 1MB))
    return $outDir
}

function Compress-Portable {
    param([string]$PublishDir, [string]$Label)

    $zip = Join-Path $OutputDir "$ExeName-$Version-$Label.zip"
    if (Test-Path $zip) { Remove-Item -Force $zip }

    Write-Step "压缩便携包 $Label"
    $sevenZip = Get-Command '7z' -ErrorAction SilentlyContinue
    if ($sevenZip) {
        # 7z 对 180MB 目录比 Compress-Archive 快一个量级，且不受 2GB 条目限制。
        & $sevenZip.Source a -tzip -mx=6 -bso0 -bsp0 $zip (Join-Path $PublishDir '*') | Out-Null
    }
    else {
        Compress-Archive -Path (Join-Path $PublishDir '*') -DestinationPath $zip -Force
    }
    if ($LASTEXITCODE -ne 0) { Fail "压缩 $Label 失败（退出码 $LASTEXITCODE）。" }

    $zipSize = (Get-Item $zip).Length
    Write-Ok ("{0}  {1:N1} MB" -f (Split-Path -Leaf $zip), ($zipSize / 1MB))
    return $zip
}

# ---------------------------------------------------------------- 目标分发

$RidTargets = $Target | Where-Object { $_ -ne 'portable' }
$wantPortable = $Target -contains 'portable'

$published = @{}
foreach ($rid in $RidTargets) {
    $published[$rid] = Publish-Target -Rid $rid -Label $rid
    $null = Compress-Portable -PublishDir $published[$rid] -Label $rid
}

if ($wantPortable) {
    $portableDir = Publish-Target -Rid 'win-x64' -Label 'portable' -ExtraArgs $PortablePublishArgs -SingleFile
    $exe = Join-Path $portableDir "$ExeName.exe"
    Write-Ok ("单文件 {0:N1} MB（免安装，双击即用）" -f ((Get-Item $exe).Length / 1MB))
}

# ---------------------------------------------------------------- Velopack

# vpk 是仓库级工具（dotnet-tools.json 固定版本），只对 Windows 目标产出安装包。
# Linux/macOS 的 deb/dmg 由对应平台的 runner 跑同一条命令生成（见 AGENTS.md）。
if ($SkipInstaller) {
    Write-Step '跳过 Velopack（-SkipInstaller）'
}
elseif (-not $RidTargets) {
    Write-Step '仅便携目标，无 Velopack 安装包'
}
else {
    $vpk = $null
    try { $null = & dotnet tool run vpk --help 2>&1; $vpkAvailable = $LASTEXITCODE -eq 0 } catch { $vpkAvailable = $false }

    if (-not $vpkAvailable) {
        Write-Warn '未找到 vpk 工具，跳过安装包。请先执行：dotnet tool restore'
    }
    else {
        $iconCandidate = Join-Path $RepoRoot "Pathfinder1eHelper\Assets\$ExeName.ico"
        if (-not (Test-Path $iconCandidate)) {
            Write-Warn "未找到正式图标 Assets\$ExeName.ico，安装包将使用默认图标（avalonia-logo.ico 是占位图，不要用它）。"
        }

        foreach ($rid in $RidTargets) {
            $releaseDir = Join-Path $OutputDir "releases\$rid"
            $os = ($rid -split '-')[0]

            # Velopack 的三个平台命令集并不相同，脚本必须分派：
            #   win   → pack    产出 Setup.exe（+ --msi 时附带 MSI）+ nupkg
            #   linux → pack    产出 AppImage + nupkg（注意：**不产 .deb**）
            #   osx   → bundle  产出 .app（**不产 .dmg**，见下）
            $verb = if ($os -eq 'osx') { 'bundle' } else { 'pack' }
            Write-Step "Velopack $verb $rid"

            $vpkArgs = @(
                $verb,
                '--packId', $ExeName,
                '--packVersion', $PackVersion,
                '--packDir', $published[$rid],
                '--runtime', $rid,
                '--channel', $rid,
                '--outputDir', $releaseDir,
                '--packTitle', $ProductName,
                '--packAuthors', $ProductName
            )

            if (Test-Path $iconCandidate) { $vpkArgs += @('--icon', $iconCandidate) }

            # apphost 只有 Windows 带 .exe 后缀；Linux/macOS 上是裸名。
            # 写死 "$ExeName.exe" 会让 Linux 打包报 "Could not find main application executable"。
            $mainExeName = if ($os -eq 'win') { "$ExeName.exe" } else { $ExeName }
            $vpkArgs += @('--mainExe', $mainExeName)

            if ($os -eq 'win') {
                # 跳过「Program.Main 里必须有 VelopackApp.Build().Run()」的检查。
                # 那是**应用内自动更新**的接线（需引用 Velopack 包并包住 AppBuilder），
                # 与「打个安装包」是两件事；本项目暂不启用自动更新，故显式跳过。
                # 将来要启用时：给 csproj 加 Velopack 包引用、在 Program.Main 里包一层
                # VelopackApp，并删掉这个开关。
                # ⚠️ 该开关只被 Windows 的 pack 接受，Linux/macOS 传了会直接报
                # "Unrecognized command or argument"，故必须按平台区分。
                $vpkArgs += '--skipVeloAppCheck'

                if ($WithMsi) { $vpkArgs += '--msi' }
            }

            # OS 指令前缀让 Velopack 允许从 Windows 交叉打包；少了它会报
            # "To build packages for Windows, the target rid must be Windows"。
            & dotnet tool run vpk "[$os]" @vpkArgs | Out-Host
            if ($LASTEXITCODE -ne 0) { Fail "Velopack $verb $rid 失败（退出码 $LASTEXITCODE）。" }

            if ($os -eq 'osx') {
                Write-Warn 'macOS 只产出了 .app：.dmg 必须用 macOS 的 hdiutil 组装（且需 Apple 签名与公证），本脚本无法在 Windows 上代劳。'
            }

            Get-ChildItem -Path $releaseDir -File -Recurse |
                ForEach-Object { Write-Ok ("{0}  {1:N1} MB" -f $_.Name, ($_.Length / 1MB)) }
        }
    }
}

Write-Step '打包完成'
Write-Host "产物目录：$((Resolve-Path $OutputDir).Path)" -ForegroundColor Green
