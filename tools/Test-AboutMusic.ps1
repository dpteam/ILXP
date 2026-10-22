# Renders the About window, opens the "Another music" menu and captures the result.
# Requires a built ILXP.exe. Uses the same automation hooks as --screenshot.
param(
    [string]$OutDir = "$env:TEMP"
)
$ErrorActionPreference = 'Stop'
$root = if ($PSScriptRoot) { Split-Path -Parent $PSScriptRoot } else { (Get-Location).Path }
$exe = Join-Path $root 'src\ILXP\bin\Debug\net40\ILXP.exe'

& $exe --screenshot (Join-Path $OutDir 'about-music.png') --window about | Out-Host

# Also verify the music pipeline end to end: text -> wav.
$track = Join-Path $root 'src\ILXP\Music\Tracks\E1M1.txt'
$wav = Join-Path $OutDir 'e1m1.wav'
& $exe --music $track --output $wav | Out-Host
& (Join-Path $PSScriptRoot 'Test-Wav.ps1') -Path $wav
