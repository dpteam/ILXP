# Renames the obfuscated type names of the application to meaningful names.
[CmdletBinding()]
param(
    [string]$Root = $(if ($PSScriptRoot) { Join-Path (Split-Path -Parent $PSScriptRoot) 'src\ILXP' } else { Join-Path (Get-Location) 'src\ILXP' })
)
$ErrorActionPreference = 'Stop'

$map = [ordered]@{
    'GControl0' = 'LoadingOverlay'
    'GDelegate0' = 'ObjectFunc'
    'GDelegate1' = 'TreeNodeFunc'
    'GClass0'   = 'AssemblyItem'
    'GClass1'   = 'AssemblyResolver'
    'GClass2'   = 'LanguageWriter'
    'GClass3'   = 'CSharpWriter'
    'GClass4'   = 'IlWriter'
    'GClass5'   = 'VbWriter'
    'GClass6'   = 'ModuleItem'
    'GClass7'   = 'NavigationHistory'
    'GClass8'   = 'RingStack'
    'GClass9'   = 'TreeNodeComparer'
    'GClass10'  = 'BorderPanel'
    'GClass11'  = 'CodeEditor'
    'GClass12'  = 'VbDecompiler'
    'GClass13'  = 'LineCollection'
    'GClass14'  = 'CodeLine'
    'GClass15'  = 'CodeToken'
    'GClass16'  = 'UiThread'
    'GForm0'    = 'AboutForm'
    'GForm1'    = 'MainForm'
    'Enum0'     = 'FileContentKind'
}

$files = Get-ChildItem -LiteralPath $Root -Filter *.cs -File
foreach ($file in $files) {
    $text = Get-Content -LiteralPath $file.FullName -Raw
    $orig = $text
    foreach ($key in $map.Keys) {
        $text = [regex]::Replace($text, "\b$key\b", $map[$key])
    }
    if ($text -ne $orig) {
        Set-Content -LiteralPath $file.FullName -Value $text -NoNewline
        Write-Host "renamed types in $($file.Name)"
    }
}

$moves = [ordered]@{
    'GClass0.cs'  = 'AssemblyItem.cs'
    'GClass1.cs'  = 'AssemblyResolver.cs'
    'GClass2.cs'  = 'LanguageWriter.cs'
    'GClass3.cs'  = 'CSharpWriter.cs'
    'GClass4.cs'  = 'IlWriter.cs'
    'GClass5.cs'  = 'VbWriter.cs'
    'GClass6.cs'  = 'ModuleItem.cs'
    'GClass7.cs'  = 'NavigationHistory.cs'
    'GClass9.cs'  = 'TreeNodeComparer.cs'
    'GClass10.cs' = 'BorderPanel.cs'
    'GClass11.cs' = 'CodeEditor.cs'
    'GClass12.cs' = 'VbDecompiler.cs'
    'GClass13.cs' = 'LineCollection.cs'
    'GClass14.cs' = 'CodeLine.cs'
    'GClass15.cs' = 'CodeToken.cs'
    'GClass16.cs' = 'UiThread.cs'
    'Class7.cs'   = 'FileContentInspector.cs'
}

foreach ($key in $moves.Keys) {
    $from = Join-Path $Root $key
    $to = Join-Path $Root $moves[$key]
    if (Test-Path -LiteralPath $from) {
        Move-Item -LiteralPath $from -Destination $to -Force
        Write-Host "moved $key -> $($moves[$key])"
    }
}
