param(
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$versionPath = Join-Path $repoRoot 'VERSION'
$projectPath = Join-Path $repoRoot 'src\GK2Plus\GK2Plus.csproj'
$buildDll = Join-Path $repoRoot 'src\GK2Plus\bin\Release\netstandard2.1\GK2Plus.dll'
$distDir = Join-Path $repoRoot 'dist'

if (-not (Test-Path $versionPath)) {
    throw "VERSION file not found: $versionPath"
}

$version = (Get-Content $versionPath -Raw).Trim()

if ([string]::IsNullOrWhiteSpace($version)) {
    throw 'VERSION is empty.'
}

if (-not $SkipBuild) {
    Write-Host "Building GK2+ v$version (Release)..."
    dotnet build $projectPath -c Release

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE"
    }
}

if (-not (Test-Path $buildDll)) {
    throw "Release DLL not found: $buildDll"
}

$stageRoot = Join-Path $distDir "GK2Plus-$version"
$pluginDir = Join-Path $stageRoot 'BepInEx\plugins\GK2Plus'
$zipPath = Join-Path $distDir "GK2Plus-$version.zip"

if (Test-Path $stageRoot) {
    Remove-Item $stageRoot -Recurse -Force
}

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

New-Item -ItemType Directory -Path $pluginDir -Force | Out-Null

Copy-Item $buildDll (Join-Path $pluginDir 'GK2Plus.dll')

foreach ($file in @('README.md', 'CHANGELOG.md', 'LICENSE')) {
    $source = Join-Path $repoRoot $file

    if (-not (Test-Path $source)) {
        throw "Required release file missing: $source"
    }

    Copy-Item $source (Join-Path $stageRoot $file)
}

$unexpected = Get-ChildItem $stageRoot -Recurse -File |
    Where-Object {
        $_.Extension -in @('.pdb', '.dll') -and
        $_.Name -ne 'GK2Plus.dll'
    }

if ($unexpected) {
    $names = ($unexpected.FullName -join [Environment]::NewLine)
    throw "Unexpected binary file(s) in release staging:$([Environment]::NewLine)$names"
}

Compress-Archive -Path (Join-Path $stageRoot '*') -DestinationPath $zipPath -CompressionLevel Optimal

$hash = Get-FileHash $zipPath -Algorithm SHA256

Write-Host ''
Write-Host "Release package created:"
Write-Host "  $zipPath"
Write-Host ''
Write-Host "SHA256:"
Write-Host "  $($hash.Hash)"
Write-Host ''
Write-Host 'Archive layout:'
Write-Host '  BepInEx/plugins/GK2Plus/GK2Plus.dll'
Write-Host '  README.md'
Write-Host '  CHANGELOG.md'
Write-Host '  LICENSE'
Write-Host ''
Write-Host 'Use this same ZIP for GitHub Releases and Nexus Mods.'
