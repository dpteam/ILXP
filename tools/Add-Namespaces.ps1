# Wraps the deobfuscated source files into the ILXP namespace, keeping their
# using directives at the top of the file.
[CmdletBinding()]
param(
    [string]$Root = $(if ($PSScriptRoot) { Join-Path (Split-Path -Parent $PSScriptRoot) 'src\ILXP' } else { Join-Path (Get-Location) 'src\ILXP' }),
    [string]$Namespace = 'ILXP'
)

$ErrorActionPreference = 'Stop'

foreach ($file in Get-ChildItem -LiteralPath $Root -Filter *.cs -File) {
    $text = Get-Content -LiteralPath $file.FullName -Raw
    if ($text -match '(?m)^\s*namespace\s+') {
        continue
    }

    $lines = $text -split "\r?\n"
    $header = New-Object System.Collections.Generic.List[string]
    $body = New-Object System.Collections.Generic.List[string]
    $inHeader = $true
    foreach ($line in $lines) {
        if ($inHeader -and ($line -match '^\s*$' -or $line -match '^\s*using\s' -or $line -match '^\s*//')) {
            $header.Add($line)
        }
        else {
            $inHeader = $false
            $body.Add($line)
        }
    }

    $sb = New-Object System.Text.StringBuilder
    foreach ($line in $header) { [void]$sb.AppendLine($line) }
    [void]$sb.AppendLine("namespace $Namespace")
    [void]$sb.AppendLine("{")
    foreach ($line in $body) {
        if ($line.Trim().Length -eq 0) { [void]$sb.AppendLine() }
        else { [void]$sb.AppendLine("    " + $line) }
    }
    [void]$sb.AppendLine("}")

    Set-Content -LiteralPath $file.FullName -Value $sb.ToString() -NoNewline
    Write-Host "namespaced $($file.Name)"
}
