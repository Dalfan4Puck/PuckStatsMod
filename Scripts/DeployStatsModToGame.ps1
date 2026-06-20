# Deploy the built Stats/Tooltip mod DLL to the Puck game's Plugins folder.
# Run this after building the solution.

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir

$sourceDll = Join-Path $projectRoot "Stats\bin\Debug\StatsTooltip.dll"
$destDir = "C:\Program Files (x86)\Steam\steamapps\common\Puck\Plugins\Tooltip"

if (-not (Test-Path $sourceDll)) {
    Write-Host "ERROR: Built DLL not found at $sourceDll"
    Write-Host "Run: dotnet build StatsTooltip.sln -c Debug"
    exit 1
}

if (-not (Test-Path $destDir)) {
    Write-Host "ERROR: Game Plugins\Tooltip folder not found at $destDir"
    Write-Host "Adjust destDir in this script if your Puck install is elsewhere."
    exit 1
}

$destDll = Join-Path $destDir "StatsTooltip.dll"
Copy-Item -Path $sourceDll -Destination $destDll -Force
Write-Host "Deployed: $sourceDll -> $destDll"
Write-Host "Restart Puck for changes to take effect."
