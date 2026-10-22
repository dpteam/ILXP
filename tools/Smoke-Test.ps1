# Smoke test: loads assemblies through the CLI and checks the outputs.
# Usage: powershell -File tools\Smoke-Test.ps1 [-Exe <path>]
param(
    [string]$Exe = 'src\ILXP\bin\Debug\net40\ILXP.exe'
)

$ErrorActionPreference = 'Stop'
$root = if ($PSScriptRoot) { Split-Path -Parent $PSScriptRoot } else { (Get-Location).Path }
$exe = Join-Path $root $Exe
if (-not (Test-Path $exe)) { throw "executable not found: $exe" }

$temp = Join-Path ([IO.Path]::GetTempPath()) ('ilxp-smoke-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $temp | Out-Null

$failures = 0
function Check([string]$name, [scriptblock]$action) {
    try {
        & $action
        Write-Host ("  PASS  {0}" -f $name) -ForegroundColor Green
    }
    catch {
        $script:failures++
        Write-Host ("  FAIL  {0}: {1}" -f $name, $_.Exception.Message) -ForegroundColor Red
    }
}

function Run([string[]]$arguments) {
    $process = Start-Process -FilePath $exe -ArgumentList $arguments -NoNewWindow -Wait -PassThru `
        -RedirectStandardOutput (Join-Path $temp 'out.txt') -RedirectStandardError (Join-Path $temp 'err.txt')
    return [pscustomobject]@{
        ExitCode = $process.ExitCode
        Out = (Get-Content (Join-Path $temp 'out.txt') -Raw -ErrorAction SilentlyContinue)
        Err = (Get-Content (Join-Path $temp 'err.txt') -Raw -ErrorAction SilentlyContinue)
    }
}

$target = Join-Path $root 'src\ILXP\bin\Debug\net40\ILXP.exe'
if (-not (Test-Path $target)) { $target = $exe }

# Pick a sample assembly: Mono.Cecil when it is built for the same configuration as the
# executable, otherwise fall back to the executable itself.
function Find-Sample([string]$name) {
    $configuration = 'Debug'
    if ($Exe -match '\\Release\\') { $configuration = 'Release' }
    $candidates = @(
        (Join-Path $root ("src\{0}\bin\{1}\net40\{0}.dll" -f $name, $configuration)),
        (Join-Path $root ("src\{0}\bin\Debug\net40\{0}.dll" -f $name)),
        (Join-Path $root ("src\{0}\bin\Release\net40\{0}.dll" -f $name))
    )
    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) { return $candidate }
    }
    return $null
}

$cecil = Find-Sample 'Cecil'
if (-not $cecil) { $cecil = $target }

Write-Host "Smoke test using $exe"
Write-Host "Temporary folder: $temp"

Check 'version' {
    $r = Run @('--version')
    if ($r.ExitCode -ne 0) { throw "exit $($r.ExitCode)" }
    if ($r.Out -notmatch 'ILXP') { throw 'unexpected output' }
}

Check 'list types' {
    $r = Run @('--list', $target)
    if ($r.ExitCode -ne 0) { throw "exit $($r.ExitCode)" }
    if ($r.Out -notmatch 'ILXP\.MainForm') { throw 'MainForm not listed' }
}

Check 'tree with references' {
    $r = Run @('--tree', $target)
    if ($r.ExitCode -ne 0) { throw "exit $($r.ExitCode)" }
    if ($r.Out -notmatch 'References') { throw 'no references section' }
    if ($r.Out -notmatch 'mscorlib') { throw 'mscorlib reference missing' }
}

Check 'decompile IL' {
    $out = Join-Path $temp 'il'
    $r = Run @('--decompile', $target, '--language', 'il', '--output', $out)
    if ($r.ExitCode -ne 0) { throw "exit $($r.ExitCode)" }
    if (-not (Test-Path (Join-Path $out 'manifest.txt'))) { throw 'no manifest' }
    $files = Get-ChildItem $out -Filter *.il
    if ($files.Count -lt 10) { throw "only $($files.Count) files" }
}

Check 'decompile C# with bodies' {
    $out = Join-Path $temp 'cs'
    $r = Run @('--decompile', $target, '--language', 'cs', '--dialect', 'cs6', '--bodies', '--output', $out)
    if ($r.ExitCode -ne 0) { throw "exit $($r.ExitCode)" }
    $sample = Join-Path $out 'ILXP.MainForm.cs'
    if (-not (Test-Path $sample)) { throw 'MainForm.cs missing' }
    $text = Get-Content $sample -Raw
    if ($text -notmatch 'Dialect: C# 6.0') { throw 'dialect header missing' }
}

Check 'decompile VB' {
    $out = Join-Path $temp 'vb'
    $r = Run @('--decompile', $target, '--language', 'vb', '--output', $out)
    if ($r.ExitCode -ne 0) { throw "exit $($r.ExitCode)" }
    if ((Get-ChildItem $out -Filter *.vb).Count -lt 5) { throw 'too few VB files' }
}

Check 'managed ildasm/ilasm round trip' {
    if (-not (Test-Path $cecil)) { throw 'Mono.Cecil.dll not built' }
    $il = Join-Path $temp 'cecil.il'
    $r = Run @('--ildasm', $cecil, '--output', $il)
    if ($r.ExitCode -ne 0) { throw "ildasm exit $($r.ExitCode)" }
    if ((Get-Item $il).Length -lt 100000) { throw 'IL output too small' }

    $dll = Join-Path $temp 'cecil.rt.dll'
    $r = Run @('--ilasm', $il, '--output', $dll, '--dll')
    if ($r.ExitCode -ne 0) { throw "ilasm exit $($r.ExitCode): $($r.Err)" }
    if ((Get-Item $dll).Length -lt 10000) { throw 'reassembled DLL too small' }

    $r = Run @('--list', $dll)
    if ($r.ExitCode -ne 0) { throw 'reassembled DLL cannot be read' }
}

Check 'managed x86 disassembler' {
    $out = Join-Path $temp 'bea.txt'
    $r = Run @('--disasm-x86', $target, '--offset', '0', '--length', '64', '--output', $out)
    if ($r.ExitCode -ne 0) { throw "exit $($r.ExitCode)" }
    $text = Get-Content $out -Raw
    if ($text -notmatch 'dec ebp|push|mov|add') { throw 'unexpected disassembly' }
}

Check 'clean pass' {
    if (-not (Test-Path $cecil)) { throw 'Mono.Cecil.dll not built' }
    $out = Join-Path $temp 'cecil.clean.dll'
    $r = Run @('--clean', $cecil, '--output', $out)
    if ($r.ExitCode -ne 0) { throw "exit $($r.ExitCode)" }
    if (-not (Test-Path $out)) { throw 'no output' }
}

Check 'music text -> midi -> text' {
    $track = Join-Path $root 'src\ILXP\Music\Tracks\T2JD.txt'
    if (-not (Test-Path $track)) { throw 'track missing' }
    $mid = Join-Path $temp 't2jd.mid'
    $r = Run @('--music', $track, '--output', $mid)
    if ($r.ExitCode -ne 0) { throw "midi exit $($r.ExitCode)" }
    if ((Get-Item $mid).Length -lt 1000) { throw 'midi too small' }

    $txt = Join-Path $temp 't2jd.txt'
    $r = Run @('--music', $mid, '--output', $txt)
    if ($r.ExitCode -ne 0) { throw "txt exit $($r.ExitCode)" }
    if ((Get-Item $txt).Length -lt 1000) { throw 'text too small' }
}

Check 'carved PE image' {
    $blob = Join-Path $temp 'blob.bin'
    $bytes = [IO.File]::ReadAllBytes($target)
    $prefix = New-Object byte[] 2048
    $blobBytes = New-Object byte[] ($prefix.Length + $bytes.Length)
    [Array]::Copy($prefix, 0, $blobBytes, 0, $prefix.Length)
    [Array]::Copy($bytes, 0, $blobBytes, $prefix.Length, $bytes.Length)
    [IO.File]::WriteAllBytes($blob, $blobBytes)

    $r = Run @('--list', $blob)
    if ($r.ExitCode -ne 0) { throw "exit $($r.ExitCode)" }
    if ($r.Out -notmatch 'MainForm') { throw 'carved assembly not listed' }
}

Write-Host ''
if ($failures -eq 0) {
    Write-Host 'All smoke tests passed.' -ForegroundColor Green
}
else {
    Write-Host ("{0} smoke test(s) failed." -f $failures) -ForegroundColor Red
}

Remove-Item -Recurse -Force $temp -ErrorAction SilentlyContinue
exit $failures
