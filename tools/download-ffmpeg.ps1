# Downloads ffmpeg 8.x essentials (ffmpeg.exe + ffprobe.exe) for Windows x64.
# Source: https://www.gyan.dev/ffmpeg/builds/ (official Windows builds recommended by ffmpeg.org)
#
# Usage: pwsh tools/download-ffmpeg.ps1

$ErrorActionPreference = "Stop"

$targetDir = "$PSScriptRoot\ffmpeg\win-x64"
$zip       = "$env:TEMP\ffmpeg-essentials.zip"
$extract   = "$env:TEMP\ffmpeg-extract"

Write-Host "Creating $targetDir..."
New-Item -ItemType Directory -Force -Path $targetDir | Out-Null

Write-Host "Downloading ffmpeg essentials from gyan.dev..."
Invoke-WebRequest -Uri "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip" `
    -OutFile $zip -UseBasicParsing

Write-Host "Extracting..."
if (Test-Path $extract) { Remove-Item $extract -Recurse -Force }
Expand-Archive -Path $zip -DestinationPath $extract

$binDir = Get-ChildItem -Path $extract -Recurse -Filter "bin" -Directory | Select-Object -First 1
if (-not $binDir) { throw "Could not find bin/ inside the zip." }

Copy-Item "$($binDir.FullName)\ffmpeg.exe"  $targetDir -Force
Copy-Item "$($binDir.FullName)\ffprobe.exe" $targetDir -Force

Remove-Item $zip     -Force
Remove-Item $extract -Recurse -Force

Write-Host "Done. Binaries written to $targetDir"
Get-ChildItem $targetDir | ForEach-Object { "  $($_.Name)  $([math]::Round($_.Length/1MB,1)) MB" }
