<#
.SYNOPSIS
    Downloads the pinned ffmpeg build (ffmpeg + ffprobe) into tools/ffmpeg/<platform>/.

.DESCRIPTION
    Version, URL and SHA-256 come from tools/ffmpeg.json; the archive is rejected if its hash
    does not match. Nothing here is committed to git or shipped in the NuGet packages - these
    binaries only make local CLI builds and CI self-contained. The CLI falls back to
    ffmpeg/ffprobe on PATH when they are absent, so this script is a convenience, not a
    prerequisite.

    Re-running is cheap: if the target folder already holds the pinned version, it exits
    immediately. Pass -Force to re-download anyway.

.PARAMETER Platform
    Runtime identifier to fetch. Only the platforms listed in tools/ffmpeg.json are available;
    other platforms are expected to install ffmpeg through their package manager.

.PARAMETER Force
    Re-download and overwrite even when the pinned version is already present.

.EXAMPLE
    pwsh tools/download-ffmpeg.ps1
#>
[CmdletBinding()]
param(
    [string] $Platform = 'win-x64',
    [switch] $Force
)

$ErrorActionPreference = 'Stop'

# Invoke-WebRequest renders a progress bar per chunk on Windows PowerShell, which dominates the
# runtime of a 100 MB download.
$ProgressPreference = 'SilentlyContinue'

$manifestPath = Join-Path $PSScriptRoot 'ffmpeg.json'
if (-not (Test-Path $manifestPath)) { throw "Manifest not found: $manifestPath" }

$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
$version = $manifest.version
$spec = $manifest.platforms.$Platform

if (-not $spec) {
    $known = ($manifest.platforms.PSObject.Properties.Name) -join ', '
    throw "No pinned build for platform '$Platform'. Pinned platforms: $known. " +
          'On Linux/macOS install ffmpeg with your package manager instead (apt install ffmpeg, brew install ffmpeg).'
}

$targetDir = Join-Path (Join-Path $PSScriptRoot 'ffmpeg') $Platform
$stampFile = Join-Path $targetDir '.version'

function Test-UpToDate {
    if (-not (Test-Path $stampFile)) { return $false }
    if ((Get-Content $stampFile -Raw).Trim() -ne $version) { return $false }

    foreach ($binary in $spec.binaries) {
        if (-not (Test-Path (Join-Path $targetDir $binary))) { return $false }
    }

    return $true
}

if (-not $Force -and (Test-UpToDate)) {
    Write-Host "ffmpeg $version already present in $targetDir (use -Force to re-download)."
    return
}

# [IO.Path]::GetTempPath() rather than $env:TEMP, so the script also runs under pwsh on Linux/macOS.
$scratch = Join-Path ([System.IO.Path]::GetTempPath()) "vcs-ffmpeg-$Platform"
$archive = Join-Path $scratch 'ffmpeg-archive.zip'
$extract = Join-Path $scratch 'extract'

if (Test-Path $scratch) { Remove-Item $scratch -Recurse -Force }
New-Item -ItemType Directory -Force -Path $scratch | Out-Null

try {
    Write-Host "Downloading ffmpeg $version for $Platform..."
    Write-Host "  $($spec.url)"
    try {
        Invoke-WebRequest -Uri $spec.url -OutFile $archive -UseBasicParsing
    }
    catch {
        # Catch-all rather than typed: the HTTP exception type differs between Windows
        # PowerShell 5.1 and pwsh 7, and the advice is the same either way.
        throw "Download failed: $($_.Exception.Message)`n" +
              "The pinned package may have been pruned upstream - bump version/url/sha256 in tools/ffmpeg.json."
    }

    $actual = (Get-FileHash $archive -Algorithm SHA256).Hash.ToLowerInvariant()
    $expected = $spec.sha256.ToLowerInvariant()
    if ($actual -ne $expected) {
        throw "SHA-256 mismatch for $($spec.url)`n  expected: $expected`n  actual:   $actual`n" +
              'Refusing to install. Either the download was corrupted or the pinned artifact changed.'
    }

    Write-Host "Checksum verified ($expected)."
    Expand-Archive -Path $archive -DestinationPath $extract -Force

    $binDir = Get-ChildItem -Path $extract -Recurse -Filter 'bin' -Directory | Select-Object -First 1
    if (-not $binDir) { throw "Could not find bin/ inside $($spec.url)." }

    New-Item -ItemType Directory -Force -Path $targetDir | Out-Null
    foreach ($binary in $spec.binaries) {
        $source = Join-Path $binDir.FullName $binary
        if (-not (Test-Path $source)) { throw "Expected '$binary' in the archive, but it is not there." }
        Copy-Item $source $targetDir -Force
    }

    Set-Content -Path $stampFile -Value $version -Encoding utf8 -NoNewline

    Write-Host "Done. ffmpeg $version written to $targetDir"
    Get-ChildItem $targetDir -File |
        Where-Object { $_.Name -ne '.version' } |
        ForEach-Object { "  $($_.Name)  $([math]::Round($_.Length / 1MB, 1)) MB" }
}
finally {
    if (Test-Path $scratch) { Remove-Item $scratch -Recurse -Force }
}
