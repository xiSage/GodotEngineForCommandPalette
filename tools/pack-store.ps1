#requires -Version 7
<#
.SYNOPSIS
    构建 GodotEngineForCommandPalette 的 Microsoft Store 提交包 (.msixupload)。

.DESCRIPTION
    一次构建产出 x64 + arm64 主包、语言/scale 资源包并打包成 .msixupload，
    供 Partner Center 提交。产物不签名（Store 提交时由微软签名）。

.PARAMETER Version
    必填。Store 版本号，格式 x.y.z.w，且必须高于 Store 中已有的最高版本。
    脚本会同步写入 GodotEngineForCommandPalette.csproj 的 <AppxPackageVersion>、
    Package.appxmanifest 的 <Identity Version> 与 app.manifest 中应用自身
    <assemblyIdentity version>（按 name="GodotEngineForCommandPalette.app" 定位；
    Common Controls 依赖的 version="6.0.0.0" 属于 OS 定义，不能被改），
    并在写入后校验三处一致。

.PARAMETER OutputDir
    可选。.msixupload 输出目录，默认 <仓库根>\artifacts（每次运行先清空）。

.EXAMPLE
    .\pack-store.ps1 -Version 1.4.0.0
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+\.\d+$')]
    [string]$Version,

    [string]$OutputDir
)

$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent $PSScriptRoot
$ProjectDir = Join-Path $RepoRoot 'GodotEngineForCommandPalette'
$CsprojPath = Join-Path $ProjectDir 'GodotEngineForCommandPalette.csproj'
$ManifestPath = Join-Path $ProjectDir 'Package.appxmanifest'
$AppManifestPath = Join-Path $ProjectDir 'app.manifest'
$AppPackagesDir = Join-Path $ProjectDir 'AppPackages'

if (-not $OutputDir) {
    $OutputDir = Join-Path $RepoRoot 'artifacts'
}
$OutputDir = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDir)

foreach ($p in @($CsprojPath, $ManifestPath, $AppManifestPath)) {
    if (-not (Test-Path -LiteralPath $p)) {
        throw "找不到文件: $p"
    }
}

function Update-VersionInFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Pattern,
        [Parameter(Mandatory = $true)][string]$NewVersion
    )

    $content = [System.IO.File]::ReadAllText($Path)
    if ($content -notmatch $Pattern) {
        throw "在 $Path 中未找到版本占位符（Pattern: $Pattern）"
    }
    $updated = [regex]::Replace($content, $Pattern, "`${1}$NewVersion`${2}")
    if ($updated -eq $content) {
        Write-Verbose "版本已是 $NewVersion，无需修改: $Path"
        return
    }
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    [System.IO.File]::WriteAllText($Path, $updated, [System.Text.UTF8Encoding]::new($hasBom))
    Write-Host "已更新版本为 $NewVersion : $Path"
}

Write-Host "==> 更新版本号到 $Version"
Update-VersionInFile -Path $CsprojPath -Pattern '(<AppxPackageVersion>)[^<]*(</AppxPackageVersion>)' -NewVersion $Version
Update-VersionInFile -Path $ManifestPath -Pattern '(<Identity[^>]*Version=")\d+\.\d+\.\d+\.\d+(")' -NewVersion $Version
# app.manifest 里有两个 <assemblyIdentity>：应用自身（version 在前、name 指向 *.app）和 Common
# Controls 依赖（version="6.0.0.0" 由 OS 定义）。必须锚定前者，否则依赖版本会被一起改成应用
# 版本号，exe 启动即报"并行配置不正确"（SxS / 事件日志 SideBySide Id 33）。
Update-VersionInFile -Path $AppManifestPath -Pattern '(<assemblyIdentity\s+version=")\d+\.\d+\.\d+\.\d+("\s+name="GodotEngineForCommandPalette\.app")' -NewVersion $Version

Write-Host "==> 校验三处版本号"
$versionChecks = @(
    @{ Path = $CsprojPath;      Pattern = '<AppxPackageVersion>([^<]*)</AppxPackageVersion>' }
    @{ Path = $ManifestPath;    Pattern = '<Identity[^>]*Version="([^"]*)"' }
    @{ Path = $AppManifestPath; Pattern = '<assemblyIdentity\s+version="([^"]*)"\s+name="GodotEngineForCommandPalette\.app"' }
)
foreach ($check in $versionChecks) {
    $match = [regex]::Match([System.IO.File]::ReadAllText($check.Path), $check.Pattern)
    if (-not $match.Success) {
        throw "版本号校验失败: 在 $($check.Path) 中未找到版本号"
    }
    if ($match.Groups[1].Value -ne $Version) {
        throw "版本号校验失败: $($check.Path) 中为 $($match.Groups[1].Value)，期望 $Version"
    }
    Write-Host "    OK  $Version  $($check.Path)"
}

# 回归护栏：Common Controls 依赖的版本必须保持 OS 定义的 6.0.0.0。
$commonControls = [regex]::Match(
    [System.IO.File]::ReadAllText($AppManifestPath),
    '<assemblyIdentity[^>]*name="Microsoft\.Windows\.Common-Controls"[^>]*version="([^"]*)"')
if ($commonControls.Success -and $commonControls.Groups[1].Value -ne '6.0.0.0') {
    throw "app.manifest 中 Common Controls 依赖的版本是 $($commonControls.Groups[1].Value)，应为 6.0.0.0，否则 exe 无法启动"
}

if ($OutputDir -eq $RepoRoot -or $OutputDir -eq $ProjectDir) {
    throw "拒绝清空危险目录: $OutputDir"
}

Write-Host "==> 清空输出目录: $OutputDir"
if (Test-Path -LiteralPath $OutputDir) {
    Remove-Item -Path (Join-Path $OutputDir '*') -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

# Appx 打包会把项目目录里已有的 AppPackages 内容一并打进新包：上一次运行留下的
# .msixupload 会被塞进本次的每个架构包里，包体从约 27 MB 膨胀到约 88 MB，所以先清掉。
if (Test-Path -LiteralPath $AppPackagesDir) {
    Write-Host "==> 清空上一次的 AppPackages: $AppPackagesDir"
    Remove-Item -LiteralPath $AppPackagesDir -Recurse -Force
}

Write-Host "==> 构建 MSIX 上传包 (x64 + arm64, Release, AOT)"
# -restore: dotnet msbuild 的 -t:Build 不会像 dotnet build 那样隐式还原，
# 冷环境（CI runner）下缺少 obj/project.assets.json 会直接报 NETSDK1004。
$msbuildArgs = @(
    'msbuild',
    $CsprojPath,
    '-restore',
    '-t:Build',
    '-v:m',
    '-p:Configuration=Release',
    '-p:Platform=X64',
    '-p:RuntimeIdentifier=win-x64',
    '-p:GenerateAppxPackageOnBuild=true',
    '-p:AppxBundlePlatforms=X64|ARM64',
    '-p:AppxBundle=Always',
    '-p:AppxPackageSigningEnabled=false',
    '-p:UapAppxPackageBuildMode=CI'
)
& dotnet @msbuildArgs
if ($LASTEXITCODE -ne 0) {
    throw "MSBuild 打包失败 (exit code $LASTEXITCODE)"
}

Write-Host "==> 收集 .msixupload 产物"
$upload = Get-ChildItem -LiteralPath $AppPackagesDir -Recurse -Filter "*$Version*.msixupload" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1
if (-not $upload) {
    throw "未在 $AppPackagesDir 下找到版本 $Version 的 .msixupload，打包可能未按预期产出"
}

$destination = Join-Path $OutputDir $upload.Name
Copy-Item -LiteralPath $upload.FullName -Destination $destination -Force

Write-Host ""
Write-Host "打包完成（未签名，Store 提交时由微软签名）"
Write-Host "  版本:   $Version"
Write-Host "  架构:   x64 + arm64 + 资源包"
Write-Host "  产物:   $destination  ($([math]::Round((Get-Item $destination).Length / 1MB, 1)) MB)"
