param(
    [string]$Root = $(if ($PSScriptRoot) { Join-Path (Split-Path -Parent $PSScriptRoot) 'src\ILXP' } else { Join-Path (Get-Location) 'src\ILXP' })
)
$ErrorActionPreference = 'Stop'

function Rename-InFile([string]$name, [System.Collections.Specialized.OrderedDictionary]$map) {
    $path = Join-Path $Root $name
    $text = Get-Content -LiteralPath $path -Raw
    $orig = $text
    foreach ($key in $map.Keys) {
        $text = [regex]::Replace($text, $key, $map[$key])
    }
    if ($text -ne $orig) {
        Set-Content -LiteralPath $path -Value $text -NoNewline
        Write-Host "renamed members in $name"
    }
}

$writer = [ordered]@{
    'vmethod_0' = 'WriteAssembly'
    'vmethod_1' = 'WriteNamespace'
    'vmethod_2' = 'WriteType'
    'vmethod_3' = 'WriteModule'
    'vmethod_4' = 'WriteMethod'
    'vmethod_5' = 'WriteProperty'
    'vmethod_6' = 'WriteField'
    'vmethod_7' = 'WriteEvent'
    'method_3' = 'WriteType'
    'method_4' = 'WriteMethod'
    'method_5' = 'WriteProperty'
    'method_6' = 'WriteCustomAttributes'
    'method_7' = 'GetQualifiedTypeName'
    'method_8' = 'GetTypeName'
    '_003C' = 'GetTypeNameShort'
}

Rename-InFile 'CSharpWriter.cs' $writer

$il = [ordered]@{
    'vmethod_0' = 'WriteAssembly'
    'vmethod_1' = 'WriteNamespace'
    'vmethod_2' = 'WriteType'
    'vmethod_3' = 'WriteModule'
    'vmethod_4' = 'WriteMethod'
    'vmethod_5' = 'WriteProperty'
    'vmethod_6' = 'WriteField'
    'vmethod_7' = 'WriteEvent'
    'method_3' = 'WriteType'
    'method_4' = 'WriteMethod'
    'method_5' = 'WriteCustomAttributes'
    'method_6' = 'GetTypeName'
}

Rename-InFile 'IlWriter.cs' $il

$vb = [ordered]@{
    'vmethod_0' = 'WriteAssembly'
    'vmethod_1' = 'WriteNamespace'
    'vmethod_2' = 'WriteType'
    'vmethod_3' = 'WriteModule'
    'vmethod_4' = 'WriteMethod'
    'vmethod_5' = 'WriteProperty'
    'vmethod_6' = 'WriteField'
    'vmethod_7' = 'WriteEvent'
    'method_3' = 'WriteType'
    'method_4' = 'WriteMethod'
    'method_5' = 'WriteProperty'
    'method_6' = 'WriteCustomAttributes'
    'method_7' = 'GetQualifiedTypeName'
    'method_8' = 'GetTypeName'
}

Rename-InFile 'VbWriter.cs' $vb

# LanguageWriter static helpers are called from all writers and from the UI.
foreach ($file in Get-ChildItem -LiteralPath $Root -Filter *.cs -File) {
    $text = Get-Content -LiteralPath $file.FullName -Raw
    $orig = $text
    $text = $text.Replace('LanguageWriter.smethod_0', 'LanguageWriter.GetReferenceKey')
    $text = $text.Replace('LanguageWriter.smethod_1', 'LanguageWriter.GetAssemblyName')
    $text = $text.Replace('LanguageWriter.smethod_2', 'LanguageWriter.GetFullTypeName')
    $text = $text.Replace('LanguageWriter.smethod_3', 'LanguageWriter.GetShortTypeName')
    $text = $text.Replace('LanguageWriter.smethod_4', 'LanguageWriter.GetMethodSignature')
    $text = $text.Replace('LanguageWriter.smethod_5', 'LanguageWriter.GetMethodSignatureWithReturn')
    $text = $text.Replace('LanguageWriter.smethod_6', 'LanguageWriter.FormatIlLabel')
    if ($text -ne $orig) {
        Set-Content -LiteralPath $file.FullName -Value $text -NoNewline
        Write-Host "updated LanguageWriter calls in $($file.Name)"
    }
}
