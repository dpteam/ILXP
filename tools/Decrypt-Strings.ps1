# Decrypts the Class8 string table (simple per-entry XOR) and can inline the
# results back into the sources.
#
# Usage:
#   pwsh tools/Decrypt-Strings.ps1                 # dump mapping to tools/strings.txt
#   pwsh tools/Decrypt-Strings.ps1 -Apply          # rewrite all .cs files, delete Class8.cs
[CmdletBinding()]
param(
    [string]$Root = $(if ($PSScriptRoot) { Split-Path -Parent $PSScriptRoot } else { (Get-Location).Path }),
    [switch]$Apply
)

$ErrorActionPreference = 'Stop'
$class8 = Join-Path $Root 'Class8.cs'
$text = Get-Content -LiteralPath $class8 -Raw

# --- byte table -------------------------------------------------------------
$m = [regex]::Match($text, 'byte\[\]\s+byte_0\s*=\s*new\s+byte\[(\d+)\]\s*\{(?<body>.*?)\};', 'Singleline')
if (-not $m.Success) { throw 'byte_0 initializer not found' }
$data = [byte[]]($m.Groups['body'].Value -split ',' |
    ForEach-Object { $_.Trim() } |
    Where-Object { $_ -match '^\d+$' } |
    ForEach-Object { [byte]$_ })
$declared = [int]$m.Groups[1].Value
if ($data.Length -ne $declared) { throw "byte table size mismatch: $($data.Length) != $declared" }

# --- accessor table ---------------------------------------------------------
$entries = @{}
foreach ($mm in [regex]::Matches($text, 'string_0\[(?<idx>\d+)\]\s*\?\?\s*smethod_0\((?<i>\d+),\s*(?<start>\d+),\s*(?<len>\d+),\s*(?<key>\d+)\)')) {
    $idx = [int]$mm.Groups['idx'].Value
    $start = [int]$mm.Groups['start'].Value
    $len = [int]$mm.Groups['len'].Value
    $key = [int]$mm.Groups['key'].Value
    $buf = New-Object byte[] $len
    [Array]::Copy($data, $start, $buf, 0, $len)
    for ($i = 0; $i -lt $len; $i++) { $buf[$i] = [byte]($buf[$i] -bxor $key) }
    $entries[$idx] = [Text.Encoding]::UTF8.GetString($buf)
}

Write-Host "Decrypted $($entries.Count) strings."

$lines = foreach ($kv in $entries.GetEnumerator()) {
    '{0}`t{1}' -f $kv.Key, ($kv.Value -replace "`r", '\r' -replace "`n", '\n')
}
$out = Join-Path $Root 'tools\strings.txt'
Set-Content -LiteralPath $out -Value $lines -Encoding UTF8
Write-Host "Mapping written to $out"

if (-not $Apply) { return }

function ConvertTo-CSharpLiteral([string]$s) {
    $sb = [Text.StringBuilder]::new()
    [void]$sb.Append('"')
    foreach ($ch in $s.ToCharArray()) {
        $code = [int]$ch
        switch ($ch) {
            '"'  { [void]$sb.Append('\"'); continue }
            '\'  { [void]$sb.Append('\\'); continue }
            "`n" { [void]$sb.Append('\n'); continue }
            "`r" { [void]$sb.Append('\r'); continue }
            "`t" { [void]$sb.Append('\t'); continue }
            default {
                if ($code -lt 32 -or $code -gt 126) {
                    [void]$sb.Append(('\u{0:x4}' -f $code))
                } else {
                    [void]$sb.Append($ch)
                }
            }
        }
    }
    [void]$sb.Append('"')
    return $sb.ToString()
}

$map = @{}
foreach ($kv in $entries.GetEnumerator()) {
    $map["Class8.smethod_$($kv.Key + 1)()"] = ConvertTo-CSharpLiteral $kv.Value
}

# Longest names first so smethod_1 does not clobber smethod_10.
$names = $map.Keys | Sort-Object -Property Length -Descending
$total = 0
Get-ChildItem -LiteralPath $Root -Filter *.cs -File | Where-Object { $_.Name -ne 'Class8.cs' } | ForEach-Object {
    $content = Get-Content -LiteralPath $_.FullName -Raw
    $orig = $content
    foreach ($n in $names) {
        $content = $content.Replace($n, $map[$n])
    }
    if ($content -ne $orig) {
        Set-Content -LiteralPath $_.FullName -Value $content -NoNewline
        $total++
        Write-Host "rewrote $($_.Name)"
    }
}
Write-Host "Files rewritten: $total"
Remove-Item -LiteralPath $class8
Write-Host 'Class8.cs removed.'
