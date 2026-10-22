param(
    [string]$Assembly = 'src\ILXP\bin\Debug\net40\ILXP.exe',
    [string]$Output = "$env:TEMP\ilxp-carve-test.bin"
)
$ErrorActionPreference = 'Stop'
$root = if ($PSScriptRoot) { Split-Path -Parent $PSScriptRoot } else { (Get-Location).Path }
$assemblyPath = Join-Path $root $Assembly
$bytes = [IO.File]::ReadAllBytes($assemblyPath)
$prefix = New-Object byte[] 4096
for ($i = 0; $i -lt $prefix.Length; $i++) { $prefix[$i] = [byte]($i % 251) }
$suffix = New-Object byte[] 512
for ($i = 0; $i -lt $suffix.Length; $i++) { $suffix[$i] = 0xCC }
$blob = New-Object byte[] ($prefix.Length + $bytes.Length + $suffix.Length)
[Array]::Copy($prefix, 0, $blob, 0, $prefix.Length)
[Array]::Copy($bytes, 0, $blob, $prefix.Length, $bytes.Length)
[Array]::Copy($suffix, 0, $blob, $prefix.Length + $bytes.Length, $suffix.Length)
[IO.File]::WriteAllBytes($Output, $blob)
Write-Host "wrote $Output ($($blob.Length) bytes)"
