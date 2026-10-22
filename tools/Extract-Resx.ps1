# Extracts binary (base64) resources from a .resx file into a folder.
# Handles BinaryFormatter-serialized Bitmap and ImageListStreamer payloads.
# Usage: pwsh tools/Extract-Resx.ps1 -Resx GForm1.resx -Out extracted
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Resx,
    [Parameter(Mandatory = $true)][string]$Out
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

[xml]$doc = Get-Content -LiteralPath $Resx -Raw
New-Item -ItemType Directory -Force -Path $Out | Out-Null

$formatter = New-Object System.Runtime.Serialization.Formatters.Binary.BinaryFormatter

foreach ($data in $doc.root.data) {
    if (-not $data.value) { continue }
    if ($data.mimetype -notmatch 'base64') { continue }

    $name = [string]$data.name
    $safe = ($name -replace '[^\w\.\-]', '_')
    $bytes = [Convert]::FromBase64String(($data.value -replace '\s', ''))
    $ms = New-Object IO.MemoryStream(, $bytes)
    $obj = $null
    try { $obj = $formatter.Deserialize($ms) } catch { $obj = $null }
    $ms.Dispose()

    if ($obj -is [System.Drawing.Image]) {
        $path = Join-Path $Out ($safe + '.png')
        $obj.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
        Write-Host ("{0,-45} {1,8} bytes  image {2}x{3} -> {4}" -f $name, $bytes.Length, $obj.Width, $obj.Height, $path)
        $obj.Dispose()
    }
    elseif ($obj -is [System.Windows.Forms.ImageListStreamer]) {
        $rawPath = Join-Path $Out ($safe + '.bin')
        [IO.File]::WriteAllBytes($rawPath, $bytes)
        $il = New-Object System.Windows.Forms.ImageList
        $il.ImageSize = New-Object System.Drawing.Size(16, 16)
        $il.ColorDepth = [System.Windows.Forms.ColorDepth]::Depth32Bit
        $il.ImageStream = $obj
        Write-Host ("{0,-45} {1,8} bytes  ImageList with {2} images" -f $name, $bytes.Length, $il.Images.Count)
        for ($i = 0; $i -lt $il.Images.Count; $i++) {
            $img = $il.Images[$i]
            $idx = '{0:D2}' -f $i
            $path = Join-Path $Out ("{0}_{1}.png" -f $safe, $idx)
            $img.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        $il.Dispose()
    }
    else {
        $path = Join-Path $Out ($safe + '.bin')
        [IO.File]::WriteAllBytes($path, $bytes)
        Write-Host ("{0,-45} {1,8} bytes  raw ({2}) -> {3}" -f $name, $bytes.Length, $obj.GetType().FullName, $path)
    }
}
