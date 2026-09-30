param(
    [string]$PublishedFileId,
    [ValidateSet('0','1','2','3')][string]$Visibility = '0',
    [string]$ChangeNote
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$version = (Get-Content (Join-Path $repoRoot 'VERSION') -Raw).Trim()
$gameVersion = (Get-Content (Join-Path $repoRoot 'GAME_VERSION') -Raw).Trim()
$releaseZip = Join-Path $repoRoot "dist\GK2Plus-$version.zip"
$root = Join-Path $repoRoot 'dist\steam-workshop'
$content = Join-Path $root "GK2Plus-$version"
$itemIdFile = Join-Path $root 'item-id.txt'
$vdfFile = Join-Path $root 'GK2Plus.vdf'
$previewFile = Join-Path $root 'preview.jpg'
$previewSource = Join-Path $repoRoot 'assets\branding\gk2plus-banner.png'

if (-not (Test-Path $releaseZip)) {
    & (Join-Path $PSScriptRoot 'Build-ReleasePackage.ps1')
    if ($LASTEXITCODE -ne 0) { throw 'Release package build failed.' }
}
if (-not (Test-Path $releaseZip)) { throw "Release ZIP missing: $releaseZip" }

New-Item -ItemType Directory -Path $root -Force | Out-Null
if (Test-Path $content) { Remove-Item $content -Recurse -Force }
New-Item -ItemType Directory -Path $content -Force | Out-Null

$temp = Join-Path $root '_extract'
if (Test-Path $temp) { Remove-Item $temp -Recurse -Force }
try {
    Expand-Archive $releaseZip $temp -Force
    Copy-Item (Join-Path $temp 'BepInEx') (Join-Path $content 'BepInEx') -Recurse -Force
    foreach ($name in @('README.md','CHANGELOG.md','LICENSE')) {
        $source = Join-Path $temp $name
        if (Test-Path $source) { Copy-Item $source (Join-Path $content $name) -Force }
    }
}
finally {
    if (Test-Path $temp) { Remove-Item $temp -Recurse -Force }
}

$install = @"
GK2+ v$version - Graveyard Keeper Plus
Tested with Graveyard Keeper 2 v$gameVersion
Requires BepInEx 5.4.23.5.

Steam downloads this Workshop item under:
Steam\steamapps\workshop\content\4358690\<WorkshopItemId>

Manual install:
Copy/merge the included BepInEx folder into the Graveyard Keeper 2 game folder.
Expected DLL:
Graveyard Keeper 2\BepInEx\plugins\GK2Plus\GK2Plus.dll

Press F2 in game to open GK2+.
https://github.com/duhhbzz/GK2Plus
"@
[IO.File]::WriteAllText((Join-Path $content 'WORKSHOP_INSTALL.txt'), $install, [Text.UTF8Encoding]::new($false))

Add-Type -AssemblyName System.Drawing
$image = [Drawing.Image]::FromFile($previewSource)
try {
    $bitmap = New-Object Drawing.Bitmap 1024,512
    try {
        $g = [Drawing.Graphics]::FromImage($bitmap)
        try {
            $g.Clear([Drawing.Color]::Black)
            $g.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $scale = [Math]::Min(1024.0 / $image.Width, 512.0 / $image.Height)
            $w = [int][Math]::Round($image.Width * $scale)
            $h = [int][Math]::Round($image.Height * $scale)
            $x = [int][Math]::Floor((1024 - $w) / 2)
            $y = [int][Math]::Floor((512 - $h) / 2)
            $g.DrawImage($image, $x, $y, $w, $h)

            $codec = [Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() |
                Where-Object { $_.MimeType -eq 'image/jpeg' } |
                Select-Object -First 1
            foreach ($quality in @(90L,80L,70L,60L)) {
                $p = New-Object Drawing.Imaging.EncoderParameters 1
                $p.Param[0] = New-Object Drawing.Imaging.EncoderParameter([Drawing.Imaging.Encoder]::Quality, $quality)
                try { $bitmap.Save($previewFile, $codec, $p) } finally { $p.Dispose() }
                if ((Get-Item $previewFile).Length -lt 1MB) { break }
            }
        } finally { $g.Dispose() }
    } finally { $bitmap.Dispose() }
} finally { $image.Dispose() }

if ((Get-Item $previewFile).Length -ge 1MB) { throw 'Workshop preview must be smaller than 1 MB.' }

if ([string]::IsNullOrWhiteSpace($PublishedFileId)) {
    $PublishedFileId = if (Test-Path $itemIdFile) { (Get-Content $itemIdFile -Raw).Trim() } else { '0' }
}
if ($PublishedFileId -notmatch '^\d+$') { throw 'PublishedFileId must contain only digits.' }
if ([string]::IsNullOrWhiteSpace($ChangeNote)) { $ChangeNote = "GK2+ v$version" }

function Escape-Vdf([string]$value) {
    if ($null -eq $value) { return '' }
    return $value.Replace('\','\\').Replace('"','\"').Replace([Environment]::NewLine,'\n')
}

$description = @"
GK2+ - one mod, your way.

Modular Graveyard Keeper 2 QoL/gameplay suite.

v$version highlights:
- Shared Storage with Current Zone / Global scope
- Character Inventory Access
- Use supported consumables from Shared Storage
- Craft/build from Global Shared Storage
- Bigger Item Stacks
- Spawn Item cheat
- Manual Save and save safety

Requires BepInEx 5.4.23.5.
Tested with GK2 v$gameVersion.

See WORKSHOP_INSTALL.txt for installation.
https://github.com/duhhbzz/GK2Plus
"@
$vdf = @"
"workshopitem"
{
    "appid" "4358690"
    "publishedfileid" "$PublishedFileId"
    "contentfolder" "$(Escape-Vdf $content)"
    "previewfile" "$(Escape-Vdf $previewFile)"
    "visibility" "$Visibility"
    "title" "GK2+ - Graveyard Keeper Plus"
    "description" "$(Escape-Vdf $description)"
    "changenote" "$(Escape-Vdf $ChangeNote)"
}
"@
[IO.File]::WriteAllText($vdfFile, $vdf, [Text.UTF8Encoding]::new($false))

$dll = Join-Path $content 'BepInEx\plugins\GK2Plus\GK2Plus.dll'
if (-not (Test-Path $dll)) { throw "Workshop package missing DLL: $dll" }

$hash = Get-FileHash $releaseZip -Algorithm SHA256
Write-Host ''
Write-Host 'Steam Workshop package prepared.'
Write-Host "Content: $content"
Write-Host "VDF: $vdfFile"
Write-Host "Preview: $previewFile"
Write-Host "PublishedFileId: $PublishedFileId"
Write-Host "Canonical release ZIP SHA256: $($hash.Hash)"
