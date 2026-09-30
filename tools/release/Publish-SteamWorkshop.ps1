param(
    [string]$SteamUser,
    [string]$SteamCmdPath,
    [string]$PublishedFileId
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$version = (Get-Content (Join-Path $repoRoot 'VERSION') -Raw).Trim()
$root = Join-Path $repoRoot 'dist\steam-workshop'
$vdfFile = Join-Path $root 'GK2Plus.vdf'
$itemIdFile = Join-Path $root 'item-id.txt'

if (-not (Test-Path $vdfFile)) {
    $args = @{}
    if (-not [string]::IsNullOrWhiteSpace($PublishedFileId)) { $args.PublishedFileId = $PublishedFileId }
    & (Join-Path $PSScriptRoot 'Build-SteamWorkshopPackage.ps1') @args
    if ($LASTEXITCODE -ne 0) { throw 'Steam Workshop package build failed.' }
}

if ([string]::IsNullOrWhiteSpace($SteamUser)) { $SteamUser = Read-Host 'Steam account login name' }
if ([string]::IsNullOrWhiteSpace($SteamUser)) { throw 'Steam login name is required.' }

function Resolve-SteamCmd([string]$explicit) {
    if (-not [string]::IsNullOrWhiteSpace($explicit)) {
        if (-not (Test-Path $explicit)) { throw "SteamCMD not found: $explicit" }
        return (Resolve-Path $explicit).Path
    }

    $cmd = Get-Command steamcmd.exe -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }

    $candidates = @(
        (Join-Path $repoRoot 'dist\steamcmd\steamcmd.exe'),
        'C:\steamcmd\steamcmd.exe'
    )
    $pf86 = [Environment]::GetEnvironmentVariable('ProgramFiles(x86)')
    if (-not [string]::IsNullOrWhiteSpace($pf86)) {
        $candidates += (Join-Path $pf86 'Steam\steamcmd.exe')
    }
    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) { return (Resolve-Path $candidate).Path }
    }

    $install = Join-Path $repoRoot 'dist\steamcmd'
    $zip = Join-Path $install 'steamcmd.zip'
    New-Item -ItemType Directory -Path $install -Force | Out-Null
    Write-Host 'SteamCMD not found. Downloading the official SteamCMD package...'
    Invoke-WebRequest 'https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip' -OutFile $zip
    Expand-Archive $zip $install -Force
    Remove-Item $zip -Force

    $downloaded = Join-Path $install 'steamcmd.exe'
    if (-not (Test-Path $downloaded)) { throw 'SteamCMD download did not produce steamcmd.exe.' }
    return $downloaded
}

$steamCmd = Resolve-SteamCmd $SteamCmdPath
Write-Host ''
Write-Host "Publishing GK2+ v$version to Graveyard Keeper 2 Steam Workshop..."
Write-Host 'SteamCMD may prompt for your password and Steam Guard code.'
Write-Host 'No Steam credentials are stored by GK2+.'
Write-Host ''

& $steamCmd '+login' $SteamUser '+workshop_build_item' $vdfFile '+quit'
if ($LASTEXITCODE -ne 0) { throw "SteamCMD failed with exit code $LASTEXITCODE" }

$text = Get-Content $vdfFile -Raw
$match = [regex]::Match($text, '"publishedfileid"\s+"(?<id>\d+)"')
if (-not $match.Success -or $match.Groups['id'].Value -eq '0') {
    throw 'SteamCMD completed but no non-zero publishedfileid was written to the VDF.'
}

$id = $match.Groups['id'].Value
[IO.File]::WriteAllText($itemIdFile, $id, [Text.UTF8Encoding]::new($false))

Write-Host ''
Write-Host 'Steam Workshop publish completed.'
Write-Host "PublishedFileId: $id"
Write-Host "https://steamcommunity.com/sharedfiles/filedetails/?id=$id"
Write-Warning 'On the first upload, open the item page and accept the Steam Workshop legal agreement if Steam requests it.'
