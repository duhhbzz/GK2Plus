param(
    [string]$GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Graveyard Keeper 2",
    [string]$ReconRoot = "D:\GK2-Recon",
    [switch]$Deep,
    [switch]$Force
)

$ErrorActionPreference = "Stop"

$ManagedDir = Join-Path $GameDir "GraveyardKeeper2_Data\Managed"
$ReportsDir = Join-Path $ReconRoot "_Reports"
$IndexDir   = Join-Path $ReconRoot "_Index"
$SourceDir  = Join-Path $ReconRoot "_Source"

if (-not (Test-Path $ManagedDir)) {
    throw "Managed directory not found: $ManagedDir"
}

if (-not (Get-Command ilspycmd -ErrorAction SilentlyContinue)) {
    throw "ilspycmd was not found in PATH. Install with: dotnet tool install --global ilspycmd"
}

New-Item -ItemType Directory -Force -Path $ReportsDir, $IndexDir, $SourceDir | Out-Null

$Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$Dlls = Get-ChildItem -Path $ManagedDir -File -Filter "*.dll" | Sort-Object Name

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

function Write-Section {
    param([string]$Path, [string]$Title)
    @(
        "",
        "================================================================",
        $Title,
        "================================================================"
    ) | Add-Content -Path $Path -Encoding UTF8
}

function Get-AssemblyVersionSafe {
    param([string]$Path)
    try {
        return [System.Reflection.AssemblyName]::GetAssemblyName($Path).Version.ToString()
    } catch {
        return ""
    }
}

function Get-TypeList {
    param([string]$AssemblyPath, [string]$Kind)
    try {
        return & ilspycmd -l $Kind $AssemblyPath 2>$null
    } catch {
        return @()
    }
}

function Normalize-TypeListLine {
    param([string]$Line)
    if ([string]::IsNullOrWhiteSpace($Line)) { return $null }
    return $Line.Trim()
}

# ---------------------------------------------------------------------------
# 1. Assembly inventory + hashes
# ---------------------------------------------------------------------------

$AssemblyCsv = Join-Path $IndexDir "assemblies.csv"

$AssemblyRows = foreach ($Dll in $Dlls) {
    $Hash = Get-FileHash -Algorithm SHA256 -Path $Dll.FullName

    [pscustomobject]@{
        Name          = $Dll.Name
        FullPath      = $Dll.FullName
        SizeBytes     = $Dll.Length
        Version       = Get-AssemblyVersionSafe $Dll.FullName
        SHA256        = $Hash.Hash
        LastWriteTime = $Dll.LastWriteTime.ToString("s")
    }
}

$AssemblyRows | Export-Csv -NoTypeInformation -Encoding UTF8 -Path $AssemblyCsv

# ---------------------------------------------------------------------------
# 2. Type inventory
# ---------------------------------------------------------------------------

$TypesCsv = Join-Path $IndexDir "types.csv"
$TypeRows = New-Object System.Collections.Generic.List[object]

$Kinds = @{
    "c" = "Class"
    "i" = "Interface"
    "s" = "Struct"
    "e" = "Enum"
    "d" = "Delegate"
}

foreach ($Dll in $Dlls) {
    Write-Host "Indexing types: $($Dll.Name)" -ForegroundColor DarkGray

    foreach ($Kind in $Kinds.Keys) {
        $Lines = Get-TypeList -AssemblyPath $Dll.FullName -Kind $Kind

        foreach ($Line in $Lines) {
            $Normalized = Normalize-TypeListLine $Line
            if (-not $Normalized) { continue }

            $TypeName = $Normalized -replace '^(Class|Interface|Struct|Enum|Delegate)\s+', ''

            $Namespace = ""
            $ShortName = $TypeName

            if ($TypeName.Contains(".")) {
                $LastDot = $TypeName.LastIndexOf(".")
                $Namespace = $TypeName.Substring(0, $LastDot)
                $ShortName = $TypeName.Substring($LastDot + 1)
            }

            $TypeRows.Add([pscustomobject]@{
                Assembly  = $Dll.Name
                Kind      = $Kinds[$Kind]
                Namespace = $Namespace
                TypeName  = $TypeName
                ShortName = $ShortName
            })
        }
    }
}

$TypeRows |
    Sort-Object Assembly, Namespace, TypeName |
    Export-Csv -NoTypeInformation -Encoding UTF8 -Path $TypesCsv

# ---------------------------------------------------------------------------
# 3. Namespace inventory
# ---------------------------------------------------------------------------

$NamespacesCsv = Join-Path $IndexDir "namespaces.csv"

$TypeRows |
    Group-Object Assembly, Namespace |
    ForEach-Object {
        $Sample = $_.Group[0]
        [pscustomobject]@{
            Assembly  = $Sample.Assembly
            Namespace = $Sample.Namespace
            TypeCount = $_.Count
        }
    } |
    Sort-Object Assembly, Namespace |
    Export-Csv -NoTypeInformation -Encoding UTF8 -Path $NamespacesCsv

# ---------------------------------------------------------------------------
# 4. Architecture keyword index
# ---------------------------------------------------------------------------

$ArchitectureCsv = Join-Path $IndexDir "architecture-keywords.csv"

$Patterns = [ordered]@{
    "UI"            = 'UI|Window|Widget|Tooltip|Dialog|Tab|Menu|HUD|Button|Slider|Toggle|InputField|Scroll'
    "Save"          = 'Save|Load|Serialize|Deserialize|Persistence|Profile'
    "Inventory"     = 'Inventory|Chest|Storage|Item|Bag|Stack'
    "Crafting"      = 'Craft|Recipe|Alchemy|Blueprint'
    "Farming"       = 'Farm|Garden|Plant|Seed|Crop|Harvest'
    "Zombie"        = 'Zombie|Worker'
    "QuestMap"      = 'Quest|Map|Marker|Waypoint|Location'
    "TechTree"      = 'Tech|Talent|Perk|Inspiration'
    "Player"        = 'Player|Character|Health|Energy|Insanity|Stamina'
    "Movement"      = 'Movement|Move|Path|Navigation'
    "World"         = 'World|Zone|Scene|Interaction|Interact|Wgo'
    "Economy"       = 'Money|Gold|Currency|Trade|Vendor|Price|Economy'
    "Time"          = 'Time|Day|Night|Calendar|Weather'
    "Input"         = 'Input|GameKey|Binding|Keyboard|Gamepad|Controller'
    "Localization"  = 'Locale|Localiz|Language|LLBase|TextStyle'
    "Assets"        = 'Addressable|Sprite|Atlas|Prefab|Asset|Resource'
    "Audio"         = 'Audio|Sound|Music|Mixer'
    "Modding"       = 'Mod|Workshop|Hook|Plugin'
    "DebugCheat"    = 'Debug|Cheat|Dev|Developer|Console'
    "Extension"     = 'Register|Registry|Provider|Factory|Manager|Service|Container|Hook'
}

$ArchitectureRows = foreach ($Row in $TypeRows) {
    foreach ($System in $Patterns.Keys) {
        if ($Row.TypeName -match $Patterns[$System]) {
            [pscustomobject]@{
                System    = $System
                Assembly  = $Row.Assembly
                Kind      = $Row.Kind
                Namespace = $Row.Namespace
                TypeName  = $Row.TypeName
            }
        }
    }
}

$ArchitectureRows |
    Sort-Object System, Assembly, TypeName |
    Export-Csv -NoTypeInformation -Encoding UTF8 -Path $ArchitectureCsv

# ---------------------------------------------------------------------------
# 5. High-value extension-point shortlist
# ---------------------------------------------------------------------------

$ExtensionCsv = Join-Path $IndexDir "extension-point-candidates.csv"

$ExtensionPattern = 'Register|Registry|Hook|Provider|Factory|Service|Manager|Container|Loader|Resolver|Adapter|Serializer|Deserializer|SaveFix|Mod|Workshop|Event|Controller'

$TypeRows |
    Where-Object { $_.TypeName -match $ExtensionPattern } |
    Sort-Object Assembly, TypeName |
    Export-Csv -NoTypeInformation -Encoding UTF8 -Path $ExtensionCsv

# ---------------------------------------------------------------------------
# 6. Human-readable summary
# ---------------------------------------------------------------------------

$Summary = Join-Path $ReportsDir "GK2-Recon-Summary.txt"

@(
    "=== GK2+ GLOBAL RECON SUMMARY ===",
    "Generated: $Timestamp",
    "GameDir: $GameDir",
    "ManagedDir: $ManagedDir",
    "ReconRoot: $ReconRoot",
    "",
    "Managed DLLs: $($Dlls.Count)",
    "Indexed Types: $($TypeRows.Count)",
    "",
    "IMPORTANT:",
    "- This output is for local reverse-engineering/research.",
    "- Do NOT commit decompiled proprietary game source to the GK2Plus repository.",
    "- Repo-safe items are the tooling scripts and your own documentation/notes.",
    "",
    "Generated index files:",
    "- _Index\assemblies.csv",
    "- _Index\types.csv",
    "- _Index\namespaces.csv",
    "- _Index\architecture-keywords.csv",
    "- _Index\extension-point-candidates.csv"
) | Set-Content -Path $Summary -Encoding UTF8

Write-Section -Path $Summary -Title "SYSTEM COUNTS"

$ArchitectureRows |
    Group-Object System |
    Sort-Object Name |
    ForEach-Object {
        "{0,-18} {1,6}" -f $_.Name, $_.Count
    } |
    Add-Content -Path $Summary -Encoding UTF8

Write-Section -Path $Summary -Title "TOP ASSEMBLIES BY TYPE COUNT"

$TypeRows |
    Group-Object Assembly |
    Sort-Object Count -Descending |
    Select-Object -First 25 |
    ForEach-Object {
        "{0,-50} {1,6}" -f $_.Name, $_.Count
    } |
    Add-Content -Path $Summary -Encoding UTF8

# ---------------------------------------------------------------------------
# 7. Optional deep decompile
# ---------------------------------------------------------------------------

if ($Deep) {
    Write-Host ""
    Write-Host "Deep mode enabled. Decompiling selected first-party assemblies..." -ForegroundColor Yellow

    $PreferredAssemblies = @(
        "Assembly-CSharp.dll",
        "LazyBearTechnology.dll"
    )

    foreach ($AssemblyName in $PreferredAssemblies) {
        $AssemblyPath = Join-Path $ManagedDir $AssemblyName

        if (-not (Test-Path $AssemblyPath)) {
            continue
        }

        $Target = Join-Path $SourceDir ([IO.Path]::GetFileNameWithoutExtension($AssemblyName))

        if ((Test-Path $Target) -and $Force) {
            Remove-Item -Recurse -Force $Target
        }

        if (-not (Test-Path $Target)) {
            New-Item -ItemType Directory -Force -Path $Target | Out-Null
            Write-Host "Decompiling $AssemblyName -> $Target" -ForegroundColor Cyan
            & ilspycmd -p -o $Target $AssemblyPath
        }
        else {
            Write-Host "Skipping existing source directory: $Target (use -Force to rebuild)" -ForegroundColor DarkYellow
        }
    }
}

Write-Host ""
Write-Host "GK2 recon complete." -ForegroundColor Green
Write-Host "Summary: $Summary" -ForegroundColor Cyan
Write-Host "Index:   $IndexDir" -ForegroundColor Cyan

if (-not $Deep) {
    Write-Host ""
    Write-Host "Tip: run with -Deep to refresh local decompiled source for selected first-party assemblies." -ForegroundColor DarkGray
}
