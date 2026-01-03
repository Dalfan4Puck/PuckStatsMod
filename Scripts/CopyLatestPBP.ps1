# Copy the latest playbyplay CSV file from stats folder to root as playbyplay_latest.csv

$statsFolder = "stats"
$latestFile = "playbyplay_latest.csv"

# Check if stats folder exists
if (-not (Test-Path $statsFolder)) {
    Write-Host "Stats folder not found. No CSV files to copy."
    exit 1
}

# Find the latest playbyplay CSV file
$csvFiles = Get-ChildItem -Path $statsFolder -Filter "playbyplay_*.csv" -ErrorAction SilentlyContinue

if ($null -eq $csvFiles -or $csvFiles.Count -eq 0) {
    Write-Host "No playbyplay CSV files found in stats folder."
    exit 1
}

# Sort by last write time and get the most recent
$latestCsv = $csvFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 1

if ($null -eq $latestCsv) {
    Write-Host "Could not determine latest CSV file."
    exit 1
}

# Copy to root as playbyplay_latest.csv
try {
    Copy-Item -Path $latestCsv.FullName -Destination $latestFile -Force
    Write-Host "Successfully copied $($latestCsv.Name) to $latestFile"
    Write-Host "Source: $($latestCsv.FullName)"
    Write-Host "Destination: $(Resolve-Path $latestFile)"
} catch {
    Write-Host "Error copying file: $_"
    exit 1
}
