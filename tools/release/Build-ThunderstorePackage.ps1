param(
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$version = (Get-Content (Join-Path $repoRoot 'VERSION') -Raw).Trim()
$gameVersion = (Get-Content (Join-Path $repoRoot 'GAME_VERSION') -Raw).Trim()

$projectPath = Join-Path $repoRoot 'src\GK2Plus\GK2Plus.csproj'
$buildDll = Join-Path $repoRoot 'src\GK2Plus\bin\Release\netstandard2.1\GK2Plus.dll'
$sourceReadme = Join-Path $repoRoot 'packaging\thunderstore\README.md'
$sourceChangelog = Join-Path $repoRoot 'CHANGELOG.md'
$sourceLogo = Join-Path $repoRoot 'assets\branding\gk2plus-logo.png'
$distDir = Join-Path $repoRoot 'dist'

if ([string]::IsNullOrWhiteSpace($version)) {
    throw 'VERSION is empty.'
}

if ([string]::IsNullOrWhiteSpace($gameVersion)) {
    throw 'GAME_VERSION is empty.'
}

if (-not $SkipBuild) {
    Write-Host "Building GK2+ v$version (Release)..."
    dotnet build $projectPath -c Release

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE"
    }
}

foreach ($required in @($buildDll, $sourceReadme, $sourceChangelog, $sourceLogo)) {
    if (-not (Test-Path $required)) {
        throw "Required Thunderstore source file missing: $required"
    }
}

$stageRoot = Join-Path $distDir "GK2Plus-$version-Thunderstore"
$pluginDir = Join-Path $stageRoot 'BepInEx\plugins\GK2Plus'
$zipPath = Join-Path $distDir "GK2Plus-$version-Thunderstore.zip"

if (Test-Path $stageRoot) {
    Remove-Item $stageRoot -Recurse -Force
}

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

New-Item -ItemType Directory -Path $pluginDir -Force | Out-Null

Copy-Item $buildDll (Join-Path $pluginDir 'GK2Plus.dll')
Copy-Item $sourceReadme (Join-Path $stageRoot 'README.md')
Copy-Item $sourceChangelog (Join-Path $stageRoot 'CHANGELOG.md')

$manifest = [ordered]@{
    name = 'GK2Plus'
    version_number = $version
    website_url = 'https://github.com/duhhbzz/GK2Plus'
    description = "Modular Graveyard Keeper 2 QoL suite with Manual Save, native-style cheats, save-safety checkpoints, and per-save achievement protection. Tested with GK2 v$gameVersion."
    dependencies = @(
        'BepInEx-BepInExPack-5.4.2305'
    )
}

$manifest |
    ConvertTo-Json -Depth 4 |
    Set-Content (Join-Path $stageRoot 'manifest.json') -Encoding UTF8

# Thunderstore requires a 256x256 PNG icon. Generate it from the existing
# GK2+ logo so package branding stays in sync without maintaining a second
# hand-edited source image.
Add-Type -AssemblyName System.Drawing

$sourceImage = [System.Drawing.Image]::FromFile($sourceLogo)

try {
    $canvas = New-Object System.Drawing.Bitmap 256, 256

    try {
        $graphics = [System.Drawing.Graphics]::FromImage($canvas)

        try {
            $graphics.Clear([System.Drawing.Color]::Transparent)
            $graphics.InterpolationMode =
                [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.SmoothingMode =
                [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
            $graphics.PixelOffsetMode =
                [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

            $scale = [Math]::Min(
                256.0 / $sourceImage.Width,
                256.0 / $sourceImage.Height
            )

            $width = [Math]::Max(
                1,
                [int][Math]::Round($sourceImage.Width * $scale)
            )

            $height = [Math]::Max(
                1,
                [int][Math]::Round($sourceImage.Height * $scale)
            )

            $x = [int][Math]::Floor((256 - $width) / 2)
            $y = [int][Math]::Floor((256 - $height) / 2)

            $graphics.DrawImage(
                $sourceImage,
                $x,
                $y,
                $width,
                $height
            )

            $canvas.Save(
                (Join-Path $stageRoot 'icon.png'),
                [System.Drawing.Imaging.ImageFormat]::Png
            )
        }
        finally {
            $graphics.Dispose()
        }
    }
    finally {
        $canvas.Dispose()
    }
}
finally {
    $sourceImage.Dispose()
}

$icon = [System.Drawing.Image]::FromFile(
    (Join-Path $stageRoot 'icon.png')
)

try {
    if ($icon.Width -ne 256 -or $icon.Height -ne 256) {
        throw "Generated Thunderstore icon is not 256x256: $($icon.Width)x$($icon.Height)"
    }
}
finally {
    $icon.Dispose()
}

$requiredRootFiles = @(
    'manifest.json',
    'README.md',
    'CHANGELOG.md',
    'icon.png'
)

foreach ($name in $requiredRootFiles) {
    if (-not (Test-Path (Join-Path $stageRoot $name))) {
        throw "Thunderstore package root file missing: $name"
    }
}

Compress-Archive -Path (Join-Path $stageRoot '*') -DestinationPath $zipPath -CompressionLevel Optimal

$hash = Get-FileHash $zipPath -Algorithm SHA256

Write-Host ''
Write-Host "Thunderstore package created:"
Write-Host "  $zipPath"
Write-Host "Tested GK2 version:"
Write-Host "  $gameVersion"
Write-Host ''
Write-Host "SHA256:"
Write-Host "  $($hash.Hash)"
Write-Host ''
Write-Host 'Upload settings:'
Write-Host '  Community: Graveyard Keeper 2'
Write-Host '  Category: Mods'
Write-Host '  NSFW: No'
Write-Host ''
Write-Host 'Manifest dependency:'
Write-Host '  BepInEx-BepInExPack-5.4.2305'
