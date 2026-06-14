# setup.ps1 — Instantiate the dotnet-clean-arch-template by replacing placeholder tokens.
#
# Usage:
#   pwsh ./setup.ps1 -Name MyApp
#   pwsh ./setup.ps1 -Name MyApp -Description "My project description."
#
# Operates only on git-tracked files (via `git ls-files`), so bin/obj/binaries are never touched.
# Safe to re-run — warns instead of crashing if tokens are already replaced.

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Name,

    [Parameter(Mandatory = $false)]
    [string]$Description = "A clean-architecture .NET 10 application."
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ── Sanity check: is the name reasonable? ────────────────────────────────────
if ($Name -notmatch '^[A-Za-z][A-Za-z0-9._-]*$') {
    Write-Error "ProjectName must start with a letter and contain only letters, digits, '.', '_', or '-'."
    exit 1
}

# ── Idempotency check ─────────────────────────────────────────────────────────
$trackedFiles = git ls-files | Where-Object { $_ -ne "" }
if (-not $trackedFiles) {
    Write-Error "No git-tracked files found. Is this a git repository?"
    exit 1
}

$nameTokenFiles = $trackedFiles | Where-Object {
    (Get-Content -Raw -Path $_ -ErrorAction SilentlyContinue) -like '*{{ProjectName}}*'
}
$descTokenFiles = $trackedFiles | Where-Object {
    (Get-Content -Raw -Path $_ -ErrorAction SilentlyContinue) -like '*{{ProjectDescription}}*'
}

if (($nameTokenFiles.Count -eq 0) -and ($descTokenFiles.Count -eq 0)) {
    Write-Warning "No {{ProjectName}} or {{ProjectDescription}} tokens found in git-tracked files."
    Write-Warning "The template may have already been instantiated. Skipping replacement."
    exit 0
}

Write-Host "Instantiating template..."
Write-Host "  ProjectName  : $Name"
Write-Host "  Description  : $Description"
Write-Host ""

# ── Replace tokens in file contents ───────────────────────────────────────────
$totalReplaced = 0
foreach ($file in $trackedFiles) {
    if (-not (Test-Path $file -PathType Leaf)) { continue }

    $raw = $null
    try {
        $raw = Get-Content -Raw -Path $file -Encoding UTF8
    } catch {
        # Skip binary or unreadable files
        continue
    }

    if ($null -eq $raw) { continue }

    $updated = $raw `
        -replace [regex]::Escape('{{ProjectName}}'), $Name `
        -replace [regex]::Escape('{{ProjectDescription}}'), $Description

    if ($updated -ne $raw) {
        Set-Content -Path $file -Value $updated -Encoding UTF8 -NoNewline
        $totalReplaced++
    }
}

Write-Host "  [1/2] Token replacement complete ($totalReplaced file(s) updated)."

# ── Rename the solution file ───────────────────────────────────────────────────
$slnxOld = "{{ProjectName}}.slnx"
$slnxNew = "${Name}.slnx"

if (Test-Path $slnxOld) {
    Rename-Item -Path $slnxOld -NewName $slnxNew
    Write-Host "  [2/2] Renamed: $slnxOld -> $slnxNew"
} elseif (Test-Path $slnxNew) {
    Write-Host "  [2/2] Solution file already named $slnxNew -- skipping rename."
} else {
    Write-Warning "  [2/2] Could not find $slnxOld or $slnxNew -- skipping rename."
}

# ── Done ───────────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "Done! Your project is ready as '$Name'."
Write-Host ""
Write-Host "Next steps:"
Write-Host "  1. Start the dev database:"
Write-Host "       docker compose -f docker-compose.devdb.yml up -d"
Write-Host "  2. Run the API:"
Write-Host "       dotnet run --project Applications/Api"
Write-Host "  3. Run the frontend:"
Write-Host "       dotnet run --project Applications/Web"
Write-Host "  4. Run the tests:"
Write-Host "       dotnet test"
