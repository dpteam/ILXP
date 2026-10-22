$path = 'src\ILXP\MainForm.cs'
$lines = Get-Content $path
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match 'internal void (vmethod_\d+|_003C|_0024)\(([^)]+)\)') {
        $sig = $matches[1] + '(' + $matches[2] + ')'
        $handlers = @()
        for ($j = $i; $j -lt [Math]::Min($i + 25, $lines.Count); $j++) {
            if ($lines[$j] -match '= (method_\d+|_003C|_0024);') { $handlers += $matches[1] }
            if ($lines[$j] -match '^\s*\}\s*$' -and $j -gt $i + 2) { break }
        }
        Write-Host ("{0} -> {1}" -f $sig, ($handlers -join ', '))
    }
}
