param([string]$Path, [int]$SampleRate = 32000, [int]$Channels = 2)
$bytes = [IO.File]::ReadAllBytes($Path)
$headerSize = 44
$dataBytes = $bytes.Length - $headerSize
$frames = $dataBytes / ($Channels * 2)
$max = 0
$sum = 0.0
$zero = 0
for ($i = $headerSize; $i + 1 -lt $bytes.Length; $i += 2) {
    $s = [BitConverter]::ToInt16($bytes, $i)
    $abs = [Math]::Abs($s)
    if ($abs -gt $max) { $max = $abs }
    if ($s -eq 0) { $zero++ }
    $sum += [double]$s * $s
}
$samples = $dataBytes / 2
Write-Host ("file     : {0}" -f $Path)
Write-Host ("channels : {0}" -f $Channels)
Write-Host ("frames   : {0} ({1:n1} s at {2} Hz)" -f $frames, ($frames / $SampleRate), $SampleRate)
Write-Host ("peak     : {0} / 32767" -f $max)
Write-Host ("rms      : {0}" -f [int][Math]::Sqrt($sum / $samples))
Write-Host ("silence% : {0:n1}" -f (100.0 * $zero / $samples))
