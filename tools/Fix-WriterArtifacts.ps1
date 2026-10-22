param([string]$Root = 'src\ILXP')
$ErrorActionPreference = 'Stop'

function Patch([string]$name, [scriptblock]$fn) {
    $path = Join-Path $Root $name
    $text = Get-Content -LiteralPath $path -Raw
    $new = & $fn $text
    if ($new -ne $text) {
        Set-Content -LiteralPath $path -Value $new -NoNewline
        Write-Host "patched $name"
    }
}

Patch 'IlWriter.cs' {
    param($t)
    $t = $t.Replace('LanguageWriter.sWriteType(', 'LanguageWriter.GetShortTypeName(')
    $t = $t.Replace('LanguageWriter.sGetTypeName(', 'LanguageWriter.FormatIlLabel(')
    $t = $t.Replace('= method_2(variable2.VariableType, list)', '= CreateUniqueLocalName(variable2.VariableType, list)')
    return $t
}

Patch 'CodeEditor.cs' {
    param($t)
    $t = $t.Replace('gclass13_0[gclass13_0.Count - 1].method_1()', 'gclass13_0[gclass13_0.Count - 1].Offset')
    $t = $t.Replace('gclass13_0[gclass13_0.Count - 1].method_3()', 'gclass13_0[gclass13_0.Count - 1].Length')
    $t = $t.Replace('gclass13_0[gclass13_0.Count - 1].method_0()', 'gclass13_0[gclass13_0.Count - 1].Tokens')
    $t = $t.Replace('gclass13_0[num].method_1()', 'gclass13_0[num].Offset')
    $t = $t.Replace('gclass13_0[num].method_3()', 'gclass13_0[num].Length')
    $t = $t.Replace('gclass13_0[num].method_0()', 'gclass13_0[num].Tokens')
    $t = $t.Replace('num2 += item.Tokens.Length;', 'num2 += item.Text.Length;')
    return $t
}

Patch 'MainForm.cs' {
    param($t)
    $t = $t.Replace('navigationHistory.method_3()', 'navigationHistory.CanGoForward')
    $t = $t.Replace('navigationHistory.method_5()', 'navigationHistory.Forward()')
    $t = $t.Replace('navigationHistory.method_2()', 'navigationHistory.CanGoBack')
    $t = $t.Replace('navigationHistory.method_4()', 'navigationHistory.Back()')
    $t = $t.Replace('navigationHistory.method_1(', 'navigationHistory.Navigate(')
    $t = $t.Replace('navigationHistory.method_0()', 'navigationHistory.Clear()')
    $t = $t.Replace('gclass7_0.method_3()', 'gclass7_0.CanGoForward')
    $t = $t.Replace('gclass7_0.method_5()', 'gclass7_0.Forward()')
    $t = $t.Replace('gclass7_0.method_2()', 'gclass7_0.CanGoBack')
    $t = $t.Replace('gclass7_0.method_4()', 'gclass7_0.Back()')
    $t = $t.Replace('gclass7_0.method_1(', 'gclass7_0.Navigate(')
    $t = $t.Replace('gclass7_0.method_0()', 'gclass7_0.Clear()')
    return $t
}
