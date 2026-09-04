$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$packages = Join-Path $root ".packages"

dotnet restore (Join-Path $root "KeyTrail.slnx") --packages $packages
Write-Host "Package cache ready: $packages"

