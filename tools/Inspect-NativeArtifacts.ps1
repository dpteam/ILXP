param(
    [string]$Dir = 'refs\_MayBeNeedReverse'
)
$ErrorActionPreference = 'Stop'
$root = if ($PSScriptRoot) { Split-Path -Parent $PSScriptRoot } else { (Get-Location).Path }
$dir = Join-Path $root $Dir

function Read-U16([byte[]]$b, [int]$o) { [BitConverter]::ToUInt16($b, $o) }
function Read-U32([byte[]]$b, [int]$o) { [BitConverter]::ToUInt32($b, $o) }
function Read-Str([byte[]]$b, [int]$o) {
    $end = $o
    while ($end -lt $b.Length -and $b[$end] -ne 0) { $end++ }
    [Text.Encoding]::ASCII.GetString($b, $o, $end - $o)
}

foreach ($file in Get-ChildItem -LiteralPath $dir -File) {
    $path = $file.FullName
    $bytes = [IO.File]::ReadAllBytes($path)
    $pe = [BitConverter]::ToInt32($bytes, 0x3C)
    $machine = Read-U16 $bytes ($pe + 4)
    $opt = $pe + 24
    $magic = Read-U16 $bytes $opt
    $is64 = $magic -eq 0x20B
    $dataDir = $opt + ($(if ($is64) { 112 } else { 96 }))
    $exportRva = Read-U32 $bytes $dataDir
    $exportSize = Read-U32 $bytes ($dataDir + 4)

    # Section table.
    $numSections = Read-U16 $bytes ($pe + 6)
    $optSize = Read-U16 $bytes ($pe + 20)
    $secTable = $opt + $optSize
    $sections = @()
    for ($i = 0; $i -lt $numSections; $i++) {
        $o = $secTable + $i * 40
        $sections += [pscustomobject]@{
            Name = (Read-Str $bytes $o)
            VirtualAddress = (Read-U32 $bytes ($o + 12))
            VirtualSize = (Read-U32 $bytes ($o + 8))
            RawOffset = (Read-U32 $bytes ($o + 20))
            RawSize = (Read-U32 $bytes ($o + 16))
        }
    }

    function Rva-To-Offset([uint32]$rva) {
        foreach ($s in $sections) {
            if ($rva -ge $s.VirtualAddress -and $rva -lt ($s.VirtualAddress + [Math]::Max($s.VirtualSize, $s.RawSize))) {
                return [int]($s.RawOffset + ($rva - $s.VirtualAddress))
            }
        }
        return -1
    }

    Write-Host ("=== {0}  ({1} bytes)" -f $file.Name, $file.Length)
    $vi = [Diagnostics.FileVersionInfo]::GetVersionInfo($path)
    Write-Host ("  file version : {0}" -f $vi.FileVersion)
    Write-Host ("  product      : {0} / {1}" -f $vi.ProductName, $vi.ProductVersion)
    Write-Host ("  company      : {0}" -f $vi.CompanyName)
    Write-Host ("  description  : {0}" -f $vi.FileDescription)
    Write-Host ("  machine      : 0x{0:X4}  magic: 0x{1:X4}" -f $machine, $magic)

    if ($exportRva -ne 0) {
        $eo = Rva-To-Offset $exportRva
        if ($eo -ge 0) {
            $nameCount = Read-U32 $bytes ($eo + 24)
            $namesRva = Read-U32 $bytes ($eo + 32)
            $namesOff = Rva-To-Offset $namesRva
            $names = @()
            for ($i = 0; $i -lt $nameCount; $i++) {
                $nameRva = Read-U32 $bytes ($namesOff + $i * 4)
                $nameOff = Rva-To-Offset $nameRva
                if ($nameOff -ge 0) { $names += (Read-Str $bytes $nameOff) }
            }
            Write-Host ("  exports      : {0}" -f ($names -join ', '))
        }
    }

    # Interesting ASCII strings.
    $interesting = @()
    $sb = New-Object Text.StringBuilder
    foreach ($b in $bytes) {
        if ($b -ge 32 -and $b -lt 127) { [void]$sb.Append([char]$b) }
        else {
            if ($sb.Length -ge 6) {
                $s = $sb.ToString()
                if ($s -match 'BeaEngine|beaengine|version|Version|ILASM|ILDASM|ilasm|ildasm|fusion|Fusion|mscoree|disasm|Disasm|Copyright') {
                    $interesting += $s
                }
            }
            [void]$sb.Clear()
        }
    }
    Write-Host ("  strings      : {0}" -f (($interesting | Select-Object -Unique -First 25) -join ' | '))
    Write-Host ''
}
