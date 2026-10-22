param([string]$Path)
$bytes = [IO.File]::ReadAllBytes($Path)
$count = [int](($bytes.Length - 44) / 2)
$max = 0
$sum = 0.0
$zero = 0
for ($i = 44; $i -lt $bytes.Length; $i += 2) {
    $s = [BitConverter]::ToInt16($bytes, $i)
    $abs = [Math]::Abs($s)
    if ($abs -gt $max) { $max = $abs }
    if ($s -eq 0) { $zero++ }
    $sum += [double]$s * $s
}
Write-Host ("file     : {0}" -f $Path)
Write-Host ("samples  : {0} ({1:n1} s)" -f $count, ($count / 22050))
Write-Host ("peak     : {0} / 32767" -f $max)
Write-Host ("rms      : {0}" -f [int][Math]::Sqrt($sum / $count))
Write-Host ("silence% : {0:n1}" -f (100.0 * $zero / $count))
