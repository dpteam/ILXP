param([string]$Path, [int]$X0 = 0, [int]$Y0 = 0, [int]$W = 540, [int]$H = 150)
Add-Type -AssemblyName System.Drawing
$bmp = New-Object System.Drawing.Bitmap($Path)
$colors = @{}
for ($y = $Y0; $y -lt [Math]::Min($Y0 + $H, $bmp.Height); $y += 2) {
    for ($x = $X0; $x -lt [Math]::Min($X0 + $W, $bmp.Width); $x += 2) {
        $c = $bmp.GetPixel($x, $y)
        $key = "{0},{1},{2}" -f $c.R, $c.G, $c.B
        if ($colors.ContainsKey($key)) { $colors[$key]++ } else { $colors[$key] = 1 }
    }
}
$bmp.Dispose()
$colors.GetEnumerator() | Sort-Object -Property Value -Descending | Select-Object -First 15 | ForEach-Object { "{0,-14} {1}" -f $_.Key, $_.Value }
