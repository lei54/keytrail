$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

dotnet publish (Join-Path $root "src\KeyTrail\KeyTrail.csproj") `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o (Join-Path $root "artifacts\publish")

Write-Host "Published to $root\artifacts\publish\KeyTrail.exe"

