param(
    [string]$GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Graveyard Keeper 2",
    [string]$ReconRoot = "D:\GK2-Recon",
    [ValidateSet("Core","All")]
    [string]$Scope = "Core",
    [switch]$Force
)

$ErrorActionPreference = "Stop"

$ManagedDir = Join-Path $GameDir "GraveyardKeeper2_Data\Managed"
$IndexDir   = Join-Path $ReconRoot "_Index"
$OutDir     = Join-Path $ReconRoot "_DeepDive"
$Manifest   = Join-Path $OutDir "deep-dive-manifest.csv"

$TypesCsv = Join-Path $IndexDir "types.csv"

if (-not (Test-Path $ManagedDir)) {
    throw "Managed directory not found: $ManagedDir"
}

if (-not (Test-Path $TypesCsv)) {
    throw "Missing type index: $TypesCsv`nRun GK2-Recon-All.ps1 first."
}

if (-not (Get-Command ilspycmd -ErrorAction SilentlyContinue)) {
    throw "ilspycmd was not found in PATH."
}

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$Types = Import-Csv $TypesCsv

# Only first-party gameplay/framework assemblies.
$FirstPartyAssemblies = @(
    "Assembly-CSharp.dll",
    "LazyBearTechnology.dll"
)

$Types = $Types | Where-Object {
    $FirstPartyAssemblies -contains $_.Assembly -and
    $_.Kind -eq "Class"
}

# Curated architecture groups. These are discovery-focused and intentionally
# broad enough to expose relationships without decompiling every type in the game.
$Groups = [ordered]@{
    "01-Save-Persistence" = @(
        '^GameSave$',
        '^GameSaveVersion$',
        '^SaveSystem$',
        '^SaveSlotData$',
        'SaveFix',
        'Serializer',
        'Deserialize',
        'Persistence',
        '^PlayerData$',
        '^WgoData$',
        '^WgoDataCache$',
        'ConveyorSave'
    )

    "02-World-WGO-Events" = @(
        '^Wgo',
        'InteractionHandler',
        '^IInteraction',
        '^IWGOInteraction',
        '^PlayerInteraction',
        '^GameScene',
        '^GlobalEventsSystem$',
        'DelayedEvent',
        'WorldZone',
        'SceneWaypoint'
    )

    "03-Mod-Hooks-Localization-Input" = @(
        '^Mods',
        'ModHook',
        'LanguageMod',
        '^LLBase$',
        '^LazyInput$',
        '^GameBindings$',
        '^KeyboardController$',
        '^GamepadController$',
        '^GameKey$',
        '^LazyWindowInputController$',
        '^SteamWorkshop',
        '^VoiceOverMod'
    )

    "04-Zombies-Workers" = @(
        '^Zombie',
        'Worker',
        '^IWorker$'
    )

    "05-Quest-Map" = @(
        '^Quest',
        '^Map',
        'Waypoint',
        'Milestone',
        '^UIMap',
        '^Flow_OpenMap'
    )

    "06-Inventory-Crafting" = @(
        '^Inventory$',
        '^MultiInventory$',
        '^InventoryWidget$',
        '^MultiInventoryWidget$',
        '^InventoryHeaderWidget$',
        'InventoryUIItem',
        '^ChestInteractionHandler$',
        '^UIBaseChestWindow$',
        '^UIChestWindow$',
        '^CraftDef',
        '^CraftElement',
        '^CraftSystem$',
        '^CraftComponent$',
        '^ICraftable$',
        '^PlayerCraftActivity$',
        '^CraftInteractionHandler$',
        '^UIBaseCraft',
        '^UICraft',
        '^UIAlchemy'
    )

    "07-Farming" = @(
        '^Garden',
        '^Plant',
        '^Seed',
        '^Crop',
        '^Harvest',
        '^UIGarden',
        'Gardener'
    )

    "08-Economy-Time-Debug" = @(
        '^Vendor',
        'Money',
        'Currency',
        'Trade',
        '^Weather',
        '^TimeOfDay',
        '^Flow_Time',
        '^Flow_GetTime',
        '^Flow_SetTime',
        '^Flow_SetWeather',
        '^Dev_',
        '^DevUtils$',
        '^DevConsts$',
        'Debug',
        'Cheat'
    )

    "09-UI-Framework" = @(
        '^LazyUI$',
        '^LazyWindow',
        '^LazyWidget',
        '^LazyWindowsStackController$',
        '^LazyWidgetPrefabContainer$',
        '^EasySpritesCollection$',
        '^GamepadNavigationController$',
        '^LocalizedLabel$',
        '^TextStyle$',
        '^TextStyleComponent$',
        '^LazyButton$',
        '^CharacterWindow$',
        '^CharPageTabButton$',
        '^UITabButton$',
        '^ToggleButton$',
        '^SmartSlider$'
    )
}

if ($Scope -eq "Core") {
    # All groups currently listed are considered core architecture groups.
    $SelectedGroups = $Groups
} else {
    $SelectedGroups = $Groups
}

function Matches-AnyPattern {
    param(
        [string]$Name,
        [string[]]$Patterns
    )
    foreach ($Pattern in $Patterns) {
        if ($Name -match $Pattern) {
            return $true
        }
    }
    return $false
}

$ManifestRows = New-Object System.Collections.Generic.List[object]
$Seen = @{}

foreach ($GroupName in $SelectedGroups.Keys) {
    $Patterns = $SelectedGroups[$GroupName]
    $GroupDir = Join-Path $OutDir $GroupName

    if ((Test-Path $GroupDir) -and $Force) {
        Remove-Item -Recurse -Force $GroupDir
    }

    New-Item -ItemType Directory -Force -Path $GroupDir | Out-Null

    $Matches = $Types | Where-Object {
        Matches-AnyPattern -Name $_.ShortName -Patterns $Patterns
    } | Sort-Object Assembly, TypeName -Unique

    Write-Host ""
    Write-Host "[$GroupName] $($Matches.Count) candidate types" -ForegroundColor Cyan

    $GroupIndex = Join-Path $GroupDir "_index.txt"

    @(
        "=== $GroupName ===",
        "Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')",
        "Candidate types: $($Matches.Count)",
        ""
    ) | Set-Content $GroupIndex -Encoding UTF8

    foreach ($Type in $Matches) {
        $AssemblyPath = Join-Path $ManagedDir $Type.Assembly

        if (-not (Test-Path $AssemblyPath)) {
            continue
        }

        # Avoid collisions from nested types / generic notation.
        $SafeName = $Type.TypeName `
            -replace '[\\/:*?"<>|]', '_' `
            -replace '`', '_'

        $OutFile = Join-Path $GroupDir ($SafeName + ".cs.txt")

        "$($Type.Assembly) :: $($Type.TypeName)" |
            Add-Content $GroupIndex -Encoding UTF8

        $Key = "$($Type.Assembly)|$($Type.TypeName)"

        if (-not $Seen.ContainsKey($Key)) {
            $Seen[$Key] = $true

            try {
                & ilspycmd -t $Type.TypeName $AssemblyPath 2>&1 |
                    Set-Content -Path $OutFile -Encoding UTF8

                $Status = "OK"
            }
            catch {
                "ERROR: $($_.Exception.Message)" |
                    Set-Content -Path $OutFile -Encoding UTF8

                $Status = "ERROR"
            }
        }
        else {
            $Status = "DUPLICATE"
        }

        $ManifestRows.Add([pscustomobject]@{
            Group     = $GroupName
            Assembly  = $Type.Assembly
            TypeName  = $Type.TypeName
            ShortName = $Type.ShortName
            Output    = $OutFile
            Status    = $Status
        })
    }
}

$ManifestRows |
    Export-Csv -NoTypeInformation -Encoding UTF8 -Path $Manifest

$SummaryPath = Join-Path $OutDir "GK2-DeepDive-Summary.txt"

@(
    "=== GK2+ CURATED DEEP DIVE ===",
    "Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')",
    "Scope: $Scope",
    "Output: $OutDir",
    "",
    "Groups:"
) | Set-Content $SummaryPath -Encoding UTF8

foreach ($Group in $SelectedGroups.Keys) {
    $Count = ($ManifestRows | Where-Object Group -eq $Group).Count
    "- $Group : $Count types" | Add-Content $SummaryPath -Encoding UTF8
}

@(
    "",
    "IMPORTANT:",
    "- This directory contains decompiled/reverse-engineered game internals.",
    "- Keep it private/local.",
    "- Do NOT commit _DeepDive output to the public GK2Plus repository.",
    "",
    "The tooling script itself is repo-safe."
) | Add-Content $SummaryPath -Encoding UTF8

Write-Host ""
Write-Host "Deep recon complete." -ForegroundColor Green
Write-Host "Summary:  $SummaryPath" -ForegroundColor Cyan
Write-Host "Manifest: $Manifest" -ForegroundColor Cyan
Write-Host ""
Write-Host "Keep D:\GK2-Recon\_DeepDive private/local." -ForegroundColor Yellow
