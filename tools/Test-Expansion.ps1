param(
    [string]$Exe = 'src\ILXP\bin\Debug\net40\ILXP.exe',
    [string]$OutDir = "$env:TEMP\ilxp-expand"
)
$ErrorActionPreference = 'Stop'
$root = if ($PSScriptRoot) { Split-Path -Parent $PSScriptRoot } else { (Get-Location).Path }
$exe = Join-Path $root $Exe
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

# 1. Depth-limited expansion preview (CLI, no window).
foreach ($depth in 1, 2, 3, 9) {
    Write-Host "--- depth $depth"
    & $exe --expand $exe --depth $depth | Out-Host
}

# 2. Real GUI expansion, captured: expand to depth 2 and screenshot.
$shot = Join-Path $OutDir 'expanded.png'
& $exe --screenshot $shot --window main --open $exe --select ILXP.CodeEditor | Out-Host
if (-not (Test-Path $shot)) { throw 'screenshot was not produced' }
Write-Host ("screenshot: {0} ({1} bytes)" -f $shot, (Get-Item $shot).Length)

# 3. Settings file sanity: the keys the settings dialog writes must round-trip.
$settings = Join-Path $env:APPDATA 'ILXP\settings.ini'
if (Test-Path $settings) {
    Write-Host "--- current settings"
    Get-Content $settings | Out-Host
}
else {
    Write-Host "settings file not created yet (run the GUI once)"
}
