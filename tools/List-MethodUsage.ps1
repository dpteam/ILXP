param([string]$Root = 'src\ILXP')
$files = Get-ChildItem -LiteralPath $Root -Filter *.cs -File
foreach ($f in $files) {
    $matches = Select-String -Path $f.FullName -Pattern '([A-Za-z_][A-Za-z0-9_]*(?:\[[^\]]*\])?(?:\))?)\.(method_\d+|smethod_\d+|vmethod_\d+|_003C|_0024)\s*\(' -AllMatches
    $set = @{}
    foreach ($m in $matches) {
        foreach ($mm in $m.Matches) {
            $key = $mm.Groups[2].Value
            if (-not $set.ContainsKey($key)) { $set[$key] = @{} }
            $recv = $mm.Groups[1].Value
            $set[$key][$recv] = 1
        }
    }
    if ($set.Count -eq 0) { continue }
    Write-Host "=== $($f.Name)"
    foreach ($k in ($set.Keys | Sort-Object)) {
        Write-Host ("  {0}: {1}" -f $k, (($set[$k].Keys | Sort-Object) -join ', '))
    }
}
