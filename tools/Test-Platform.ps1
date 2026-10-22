# Verifies that a platform-specific build really produces the expected PE header and
# CLI flags. AnyCPU and x86 share the i386 machine type, so the CorFlags
# (32BITREQUIRED / 32BITPREFERRED) are checked as well.
#
# Usage: powershell -File tools\Test-Platform.ps1 -Exe <path> -Expected AnyCPU|x86|x64
param(
    [Parameter(Mandatory = $true)][string]$Exe,
    [Parameter(Mandatory = $true)][ValidateSet('AnyCPU', 'x86', 'x64')][string]$Expected
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $Exe)) { throw "not found: $Exe" }

$bytes = [IO.File]::ReadAllBytes($Exe)
if ($bytes.Length -lt 0x100) { throw "too small: $Exe" }

$pe = [BitConverter]::ToInt32($bytes, 0x3C)
if ($pe -le 0 -or $pe + 0x100 -gt $bytes.Length) { throw "invalid PE header: $Exe" }

$machine = [BitConverter]::ToUInt16($bytes, $pe + 4)
$sectionCount = [BitConverter]::ToUInt16($bytes, $pe + 6)
$optionalSize = [BitConverter]::ToUInt16($bytes, $pe + 20)
$optional = $pe + 24
$magic = [BitConverter]::ToUInt16($bytes, $optional)
$is64 = $magic -eq 0x20B
$dataDirectories = $optional + ($(if ($is64) { 112 } else { 96 }))
$cliRva = [BitConverter]::ToUInt32($bytes, $dataDirectories + 14 * 8)

# Section table, used to translate the CLI header RVA into a file offset.
$sections = @()
$sectionTable = $optional + $optionalSize
for ($i = 0; $i -lt $sectionCount; $i++) {
    $o = $sectionTable + $i * 40
    $sections += [pscustomobject]@{
        VirtualAddress = [BitConverter]::ToUInt32($bytes, $o + 12)
        VirtualSize    = [BitConverter]::ToUInt32($bytes, $o + 8)
        RawOffset      = [BitConverter]::ToUInt32($bytes, $o + 20)
        RawSize        = [BitConverter]::ToUInt32($bytes, $o + 16)
    }
}

function Rva-To-Offset([uint32]$rva) {
    foreach ($s in $sections) {
        $size = [Math]::Max($s.VirtualSize, $s.RawSize)
        if ($rva -ge $s.VirtualAddress -and $rva -lt ($s.VirtualAddress + $size)) {
            return [int]($s.RawOffset + ($rva - $s.VirtualAddress))
        }
    }
    return -1
}

$corFlags = 0
if ($cliRva -ne 0) {
    $cliOffset = Rva-To-Offset $cliRva
    if ($cliOffset -ge 0 -and $cliOffset + 20 -le $bytes.Length) {
        $corFlags = [BitConverter]::ToUInt32($bytes, $cliOffset + 16)
    }
}

$ilOnly = ($corFlags -band 0x1) -ne 0
$requires32Bit = ($corFlags -band 0x2) -ne 0
$prefers32Bit = ($corFlags -band 0x20000) -ne 0

$machineName = switch ($machine) {
    0x014C { 'i386' }
    0x8664 { 'amd64' }
    default { ('0x{0:X4}' -f $machine) }
}

$actual = if ($machine -eq 0x8664) {
    'x64'
}
elseif ($requires32Bit) {
    'x86'
}
else {
    'AnyCPU'
}

Write-Host $Exe
Write-Host ("  machine        : 0x{0:X4} ({1})" -f $machine, $machineName)
Write-Host ("  PE magic       : 0x{0:X4}" -f $magic)
Write-Host ("  CorFlags       : 0x{0:X8} (ILOnly={1}, 32BitRequired={2}, 32BitPreferred={3})" -f $corFlags, $ilOnly, $requires32Bit, $prefers32Bit)
Write-Host ("  platform       : {0} (expected {1})" -f $actual, $Expected)

if ($actual -ne $Expected) {
    throw "unexpected platform: expected $Expected, detected $actual"
}

Write-Host '  OK' -ForegroundColor Green
