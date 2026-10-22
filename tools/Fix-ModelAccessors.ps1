param(
    [string]$Root = $(if ($PSScriptRoot) { Join-Path (Split-Path -Parent $PSScriptRoot) 'src\ILXP' } else { Join-Path (Get-Location) 'src\ILXP' })
)
$ErrorActionPreference = 'Stop'

function Replace-Cast([string]$text, [string]$type, [string]$method, [string]$property) {
    $pattern = '\(\({0}\)([^()]+)\)\.{1}\(\)' -f $type, $method
    return [regex]::Replace($text, $pattern, {
        param($m)
        '(({0}){1}).{2}' -f $type, $m.Groups[1].Value, $property
    })
}

foreach ($file in Get-ChildItem -LiteralPath $Root -Filter *.cs -File) {
    $text = Get-Content -LiteralPath $file.FullName -Raw
    $orig = $text

    $text = Replace-Cast $text 'AssemblyItem' 'method_0' 'FilePath'
    $text = Replace-Cast $text 'AssemblyItem' 'method_1' 'Assembly'
    $text = Replace-Cast $text 'ModuleItem' 'method_0' 'Namespace'
    $text = Replace-Cast $text 'ModuleItem' 'method_1' 'Module'

    $text = $text.Replace('gclass0_0.method_0()', 'gclass0_0.FilePath')
    $text = $text.Replace('gclass0_0.method_1()', 'gclass0_0.Assembly')
    $text = $text.Replace('gclass6_0.method_0()', 'gclass6_0.Namespace')
    $text = $text.Replace('gclass6_0.method_1()', 'gclass6_0.Module')
    $text = $text.Replace('assemblyItem.method_0()', 'assemblyItem.FilePath')
    $text = $text.Replace('assemblyItem.method_1()', 'assemblyItem.Assembly')
    $text = $text.Replace('moduleItem.method_0()', 'moduleItem.Namespace')
    $text = $text.Replace('moduleItem.method_1()', 'moduleItem.Module')

    if ($text -ne $orig) {
        Set-Content -LiteralPath $file.FullName -Value $text -NoNewline
        Write-Host "fixed model accessors in $($file.Name)"
    }
}
