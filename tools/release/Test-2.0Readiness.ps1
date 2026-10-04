param(
    [string]$ExpectedVersion = '2.0.0',
    [string]$ExpectedGameVersion = '1.008',
    [switch]$SkipWorkshop
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$projectPath = Join-Path $repoRoot 'src\GK2Plus\GK2Plus.csproj'
$version = (Get-Content (Join-Path $repoRoot 'VERSION') -Raw).Trim()
$gameVersion = (Get-Content (Join-Path $repoRoot 'GAME_VERSION') -Raw).Trim()
$distDir = Join-Path $repoRoot 'dist'

function Assert-Equal([string]$label, [string]$actual, [string]$expected) {
    if ($actual -ne $expected) {
        throw "$label mismatch. Expected '$expected' but found '$actual'."
    }
}

function Assert-File([string]$path) {
    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Required file missing: $path"
    }
}

function Invoke-Checked([scriptblock]$command, [string]$label) {
    Write-Host ''
    Write-Host "== $label =="
    & $command
    if ($LASTEXITCODE -ne 0) {
        throw "$label failed with exit code $LASTEXITCODE."
    }
}

Write-Host 'GK2+ v2.0 release-readiness preflight'
Write-Host "Repository: $repoRoot"
Write-Host "VERSION: $version"
Write-Host "GAME_VERSION: $gameVersion"

Assert-Equal 'VERSION' $version $ExpectedVersion
Assert-Equal 'GAME_VERSION' $gameVersion $ExpectedGameVersion

Invoke-Checked { dotnet build $projectPath -c Debug --warnaserror } 'Debug build (warnings are errors)'
Invoke-Checked { dotnet build $projectPath -c Release --warnaserror } 'Release build (warnings are errors)'

Invoke-Checked { & (Join-Path $PSScriptRoot 'Build-ReleasePackage.ps1') -SkipBuild } 'Canonical release package'
Invoke-Checked { & (Join-Path $PSScriptRoot 'Build-ThunderstorePackage.ps1') -SkipBuild } 'Thunderstore package'

if (-not $SkipWorkshop) {
    Invoke-Checked { & (Join-Path $PSScriptRoot 'Build-SteamWorkshopPackage.ps1') } 'Steam Workshop staging package'
}

$releaseZip = Join-Path $distDir "GK2Plus-$version.zip"
$thunderZip = Join-Path $distDir "GK2Plus-$version-Thunderstore.zip"
$releaseDll = Join-Path $repoRoot 'src\GK2Plus\bin\Release\netstandard2.1\GK2Plus.dll'

Assert-File $releaseDll
Assert-File $releaseZip
Assert-File $thunderZip

Add-Type -AssemblyName System.IO.Compression.FileSystem

$releaseArchive = [System.IO.Compression.ZipFile]::OpenRead($releaseZip)
try {
    $entries = @($releaseArchive.Entries | ForEach-Object { $_.FullName.Replace('/', '\') })
    foreach ($required in @(
        'BepInEx\plugins\GK2Plus\GK2Plus.dll',
        'README.md',
        'CHANGELOG.md',
        'LICENSE'
    )) {
        if ($entries -notcontains $required) {
            throw "Canonical release ZIP is missing '$required'."
        }
    }

    $unexpectedDlls = @($entries | Where-Object {
        $_ -like '*.dll' -and $_ -ne 'BepInEx\plugins\GK2Plus\GK2Plus.dll'
    })

    if ($unexpectedDlls.Count -gt 0) {
        throw "Canonical release ZIP contains unexpected DLL(s): $($unexpectedDlls -join ', ')"
    }
}
finally {
    $releaseArchive.Dispose()
}

$thunderArchive = [System.IO.Compression.ZipFile]::OpenRead($thunderZip)
try {
    $manifestEntry = $thunderArchive.GetEntry('manifest.json')
    if ($null -eq $manifestEntry) {
        throw 'Thunderstore ZIP is missing manifest.json.'
    }

    $reader = New-Object System.IO.StreamReader($manifestEntry.Open())
    try {
        $manifest = ($reader.ReadToEnd() | ConvertFrom-Json)
    }
    finally {
        $reader.Dispose()
    }

    Assert-Equal 'Thunderstore manifest version' $manifest.version_number $version

    if ($manifest.dependencies -notcontains 'BepInEx-BepInExPack-5.4.2305') {
        throw 'Thunderstore manifest is missing the required BepInEx dependency.'
    }
}
finally {
    $thunderArchive.Dispose()
}

if (-not $SkipWorkshop) {
    $workshopDll = Join-Path $distDir "steam-workshop\GK2Plus-$version\BepInEx\plugins\GK2Plus\GK2Plus.dll"
    $workshopVdf = Join-Path $distDir 'steam-workshop\GK2Plus.vdf'
    Assert-File $workshopDll
    Assert-File $workshopVdf
}

$hash = (Get-FileHash $releaseZip -Algorithm SHA256).Hash

Write-Host ''
Write-Host 'Preflight passed.'
Write-Host '  Debug build:       clean'
Write-Host '  Release build:     clean'
Write-Host '  Release ZIP:       verified'
Write-Host '  Thunderstore ZIP:  verified'
if (-not $SkipWorkshop) {
    Write-Host '  Workshop staging:  verified'
}
Write-Host "  Canonical SHA256:  $hash"
Write-Host ''
Write-Host 'Next: run the in-game v2.0 one-pass checklist in docs\RELEASE_CHECKLIST.md.'
