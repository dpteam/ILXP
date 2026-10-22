$ErrorActionPreference = 'Stop'
$root = if ($PSScriptRoot) { Split-Path -Parent $PSScriptRoot } else { (Get-Location).Path }

foreach ($name in 'CSharpWriter.cs', 'VbWriter.cs') {
    $path = Join-Path $root "src\ILXP\$name"
    $t = Get-Content -LiteralPath $path -Raw
    $t = $t.Replace('"<<r>>"{0}"<<>>"', '"<<r>>\"{0}\"<<>>"')
    $t = $t.Replace('"<<r>>"{RuntimeHelpers.GetObjectValue(parameter.Constant)}"<<>>"', '"<<r>>\"{RuntimeHelpers.GetObjectValue(parameter.Constant)}\"<<>>"')
    Set-Content -LiteralPath $path -Value $t -NoNewline
    Write-Host "fixed $name"
}
