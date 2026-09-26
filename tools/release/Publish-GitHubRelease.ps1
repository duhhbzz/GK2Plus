param(
    [switch]$Draft
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$version = (Get-Content (Join-Path $repoRoot 'VERSION') -Raw).Trim()
$tag = "v$version"
$zipPath = Join-Path $repoRoot "dist\GK2Plus-$version.zip"
$thunderstoreZipPath = Join-Path $repoRoot "dist\GK2Plus-$version-Thunderstore.zip"
$notesPath = Join-Path $repoRoot "docs\RELEASE_NOTES_$version.md"

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw 'GitHub CLI (gh) is not installed or not available in PATH.'
}

gh auth status
if ($LASTEXITCODE -ne 0) {
    throw 'GitHub CLI is not authenticated. Run: gh auth login'
}

if (-not (Test-Path $zipPath)) {
    throw "Release ZIP not found: $zipPath. Run Build-ReleasePackage.ps1 first."
}

if (-not (Test-Path $thunderstoreZipPath)) {
    throw "Thunderstore ZIP not found: $thunderstoreZipPath. Run Build-ThunderstorePackage.ps1 first."
}

if (-not (Test-Path $notesPath)) {
    throw "Release notes not found: $notesPath"
}

Push-Location $repoRoot

try {
    $status = git status --porcelain

    if ($status) {
        throw 'Working tree is not clean. Commit/stash changes before publishing.'
    }

    $branch = (git branch --show-current).Trim()

    if ($branch -ne 'main') {
        throw "Publish from main after the release PR is merged. Current branch: $branch"
    }

    git pull --ff-only origin main
    if ($LASTEXITCODE -ne 0) {
        throw 'Failed to update main from origin.'
    }

    $args = @(
        'release', 'create', $tag,
        $zipPath,
        $thunderstoreZipPath,
        '--target', 'main',
        '--title', "GK2+ $tag - First Gameplay Release",
        '--notes-file', $notesPath
    )

    if ($Draft) {
        $args += '--draft'
    }

    & gh @args

    if ($LASTEXITCODE -ne 0) {
        throw "GitHub Release creation failed. If $tag already exists, inspect it with: gh release view $tag"
    }

    Write-Host ''
    Write-Host "Published GitHub Release $tag with:"
    Write-Host "  $zipPath"
    Write-Host "  $thunderstoreZipPath"
    Write-Host ''
    Write-Host 'Publishing the release will trigger GitHub Actions for Nexus Mods and Thunderstore.'
    Write-Host ''
    Write-Warning 'MAINTAINER CHECK: verify/update the live Nexus Mods main-page description.'
    Write-Host '  Source copy: docs/NEXUS_PAGE.md'
    Write-Host '  Automated file upload metadata is separate from the full public mod-page description.'
}
finally {
    Pop-Location
}
