param([string]$Ico = 'refs\239.ico', [string]$OutDir = 'extracted')
$ErrorActionPreference = 'Stop'
$bytes = [IO.File]::ReadAllBytes((Resolve-Path $Ico))
$count = [BitConverter]::ToUInt16($bytes, 4)
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
for ($i = 0; $i -lt $count; $i++) {
    $o = 6 + $i * 16
    $w = $bytes[$o]; if ($w -eq 0) { $w = 256 }
    $h = $bytes[$o + 1]; if ($h -eq 0) { $h = 256 }
    $size = [BitConverter]::ToInt32($bytes, $o + 8)
    $off = [BitConverter]::ToInt32($bytes, $o + 12)
    $isPng = ($bytes[$off] -eq 0x89 -and $bytes[$off + 1] -eq 0x50)
    Write-Host ("frame {0}: {1}x{2} size={3} offset={4} png={5}" -f $i, $w, $h, $size, $off, $isPng)
    if ($isPng) {
        $frame = New-Object byte[] $size
        [Array]::Copy($bytes, $off, $frame, 0, $size)
        [IO.File]::WriteAllBytes((Join-Path $OutDir ("icon_{0}_{1}x{2}.png" -f $i, $w, $h)), $frame)
    }
}
