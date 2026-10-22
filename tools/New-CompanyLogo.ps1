Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent $PSScriptRoot
$icon = New-Object System.Drawing.Icon((Join-Path $root 'refs\239.ico'), 48, 48)
$bmp = New-Object System.Drawing.Bitmap(459, 128)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$g.Clear([System.Drawing.Color]::White)
$logo = $icon.ToBitmap()
$g.DrawImage($logo, 24, 24, 80, 80)
$fontBig = New-Object System.Drawing.Font('Segoe UI', 30, [System.Drawing.FontStyle]::Bold)
$fontSmall = New-Object System.Drawing.Font('Segoe UI', 11)
$g.DrawString('ILXP', $fontBig, [System.Drawing.Brushes]::Black, 120, 30)
$g.DrawString('.NET decompiler', $fontSmall, [System.Drawing.Brushes]::DimGray, 124, 78)
$g.Dispose()
$bmp.Save((Join-Path $root 'src\ILXP\Resources\CompanyLogo.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
$logo.Dispose()
$fontBig.Dispose()
$fontSmall.Dispose()
Write-Host 'CompanyLogo.png written'
