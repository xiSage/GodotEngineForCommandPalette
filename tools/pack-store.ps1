#requires -Version 7
<#
.SYNOPSIS
    构建 GodotEngineForCommandPalette 的 Microsoft Store 提交包 (.msixupload)。

.DESCRIPTION
    一次构建产出 x64 + arm64 主包、语言/scale 资源包并打包成 .msixupload，
    供 Partner Center 提交。产物不签名（Store 提交时由微软签名）。

.PARAMETER Version
    必填。Store 版本号，格式 x.y.z.w，且必须高于 Store 中已有的最高版本。
    脚本会同步写入 GodotEngineForCommandPalette.csproj 的 <AppxPackageVersion>
    与 Package.appxmanifest 的 <Identity Version>（持久化）。

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
$AppPackagesDir = Join-Path $ProjectDir 'AppPackages'

if (-not $OutputDir) {
    $OutputDir = Join-Path $RepoRoot 'artifacts'
}
$OutputDir = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDir)

foreach ($p in @($CsprojPath, $ManifestPath)) {
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

if ($OutputDir -eq $RepoRoot -or $OutputDir -eq $ProjectDir) {
    throw "拒绝清空危险目录: $OutputDir"
}

Write-Host "==> 清空输出目录: $OutputDir"
if (Test-Path -LiteralPath $OutputDir) {
    Remove-Item -Path (Join-Path $OutputDir '*') -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

Write-Host "==> 构建 MSIX 上传包 (x64 + arm64, Release, AOT)"
$msbuildArgs = @(
    'msbuild',
    $CsprojPath,
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
