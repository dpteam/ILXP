# Applies the first wave of deobfuscation cleanups to the ILXP sources:
#  - drops [SuppressIldasm] (attribute is assembly/module-only in C#)
#  - drops redundant "virtual" on members of sealed classes
#  - normalizes file-scoped BOM/whitespace
[CmdletBinding()]
param([string]$Root = $(if ($PSScriptRoot) { Join-Path (Split-Path -Parent $PSScriptRoot) 'src\ILXP' } else { Join-Path (Get-Location) 'src\ILXP' }))

$ErrorActionPreference = 'Stop'
$files = Get-ChildItem -LiteralPath $Root -Filter *.cs -File

foreach ($f in $files) {
    $text = Get-Content -LiteralPath $f.FullName -Raw
    $orig = $text
    $text = [regex]::Replace($text, '(?m)^[ \t]*\[SuppressIldasm\][ \t]*\r?\n', '')
    $text = [regex]::Replace($text, '\binternal virtual\b', 'internal')
    $text = [regex]::Replace($text, '\bpublic virtual\b', 'public')
    $text = [regex]::Replace($text, '\bprotected virtual\b', 'protected')
    if ($text -ne $orig) {
        Set-Content -LiteralPath $f.FullName -Value $text -NoNewline
        Write-Host "cleaned $($f.Name)"
    }
}
