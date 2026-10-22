$ErrorActionPreference = 'Stop'
$root = if ($PSScriptRoot) { Split-Path -Parent $PSScriptRoot } else { (Get-Location).Path }

$f = Join-Path $root 'src\ILXP\FileContentInspector.cs'
$t = Get-Content $f -Raw
$t = $t.Replace('public sealed class Class7', 'public static class FileContentInspector')
Set-Content $f -Value $t -NoNewline

$f = Join-Path $root 'src\ILXP\MainForm.cs'
$t = Get-Content $f -Raw
$t = $t.Replace('Class7.FileContentKind', 'FileContentKind')
$t = $t.Replace('Class7.smethod_0(', 'FileContentInspector.Detect(')
$t = $t.Replace('Class7.smethod_4(', 'FileContentInspector.FormatHexDump(')
$t = $t.Replace('Class7.smethod_3(', 'FileContentInspector.DecodeText(')
$t = $t.Replace('Class7.smethod_5(', 'FileContentInspector.TryLoadImage(')
$t = $t.Replace('Class7._003C(', 'FileContentInspector.FormatResourceTable(')
$t = $t.Replace('AssemblyResolver.smethod_0(', 'AssemblyResolver.ResolveFromTree(')
Set-Content $f -Value $t -NoNewline
Write-Host 'patched FileContentInspector usage'
