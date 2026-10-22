param(
    [string]$Root = $(if ($PSScriptRoot) { Join-Path (Split-Path -Parent $PSScriptRoot) 'src\ILXP' } else { Join-Path (Get-Location) 'src\ILXP' })
)
$ErrorActionPreference = 'Stop'

$map = [ordered]@{
    'VbDecompiler' = 'StatementDecompiler'
    'smethod_0'    = 'WriteVisualBasicBody'
    'smethod_2'    = 'WriteCSharpBody'
}

foreach ($file in Get-ChildItem -LiteralPath $Root -Filter *.cs -File) {
    $text = Get-Content -LiteralPath $file.FullName -Raw
    $orig = $text
    if ($file.Name -eq 'VbDecompiler.cs') {
        foreach ($key in $map.Keys) {
            $text = [regex]::Replace($text, "\b$key\b", $map[$key])
        }
    }
    else {
        $text = $text.Replace('VbDecompiler.smethod_0(', 'StatementDecompiler.WriteVisualBasicBody(')
        $text = $text.Replace('VbDecompiler.smethod_2(', 'StatementDecompiler.WriteCSharpBody(')
    }
    if ($text -ne $orig) {
        Set-Content -LiteralPath $file.FullName -Value $text -NoNewline
        Write-Host "updated $($file.Name)"
    }
}

Move-Item -LiteralPath (Join-Path $Root 'VbDecompiler.cs') -Destination (Join-Path $Root 'StatementDecompiler.cs') -Force
Write-Host 'renamed VbDecompiler.cs -> StatementDecompiler.cs'
