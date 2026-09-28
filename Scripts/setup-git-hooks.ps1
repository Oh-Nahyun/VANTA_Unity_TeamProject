param(
    [switch]$SkipToolRestore
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $repoRoot

if (-not $SkipToolRestore) {
    dotnet tool restore
}

dotnet husky install

$hooksPath = git config --get core.hooksPath
$huskyShim = Join-Path $repoRoot ".husky/_/husky.sh"

if ($hooksPath -ne ".husky" -or -not (Test-Path $huskyShim)) {
    throw "Git hooks installation failed. Expected core.hooksPath='.husky' and .husky/_/husky.sh to exist."
}

Write-Host "Git hooks are installed for this repository."
