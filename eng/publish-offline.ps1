$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$packages = Join-Path $root ".packages"

dotnet publish (Join-Path $root "src\KeyTrail\KeyTrail.csproj") `
    -c Release `
    -r win-x64 `
    --self-contained true `
    --packages $packages `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o (Join-Path $root "artifacts\publish")

Write-Host "Offline publish ready: $root\artifacts\publish\KeyTrail.exe"

