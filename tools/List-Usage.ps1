param([string]$Path = 'src\ILXP\CodeEditor.cs')
$text = Get-Content -LiteralPath $Path -Raw
$matches = [regex]::Matches($text, '(?<recv>[A-Za-z_][A-Za-z0-9_]*)\.(?<meth>method_\d+|smethod_\d+|vmethod_\d+)\s*\((?<args>[^()]*)')
$set = @{}
foreach ($m in $matches) {
    $key = $m.Groups['meth'].Value
    if (-not $set.ContainsKey($key)) { $set[$key] = New-Object System.Collections.Generic.HashSet[string] }
    [void]$set[$key].Add($m.Groups['recv'].Value)
}
foreach ($k in ($set.Keys | Sort-Object)) {
    Write-Host ("{0}: {1}" -f $k, (($set[$k] | Sort-Object) -join ', '))
}
