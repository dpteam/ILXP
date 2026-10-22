param([string]$Path = 'src\Cecil\Properties\AssemblyInfo.cs')
$ErrorActionPreference = 'Stop'
$text = Get-Content -LiteralPath $Path -Raw
$text = [regex]::Replace($text, 'InternalsVisibleTo\("([^",]+), PublicKey=[0-9a-fA-F]+"\)', 'InternalsVisibleTo("$1")')
Set-Content -LiteralPath $Path -Value $text -NoNewline
Write-Host 'AssemblyInfo patched'
