param(
    [switch]$Draft
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$version = (Get-Content (Join-Path $repoRoot 'VERSION') -Raw).Trim()
$tag = "v$version"
$zipPath = Join-Path $repoRoot "dist\GK2Plus-$version.zip"
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
}
finally {
    Pop-Location
}
