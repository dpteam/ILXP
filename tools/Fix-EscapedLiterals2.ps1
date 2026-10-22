$ErrorActionPreference = 'Stop'
$root = if ($PSScriptRoot) { Split-Path -Parent $PSScriptRoot } else { (Get-Location).Path }

$old = '<<r>>"{RuntimeHelpers.GetObjectValue(parameter.Constant)}"<<>>'
$new = '<<r>>\"{RuntimeHelpers.GetObjectValue(parameter.Constant)}\"<<>>'

foreach ($name in 'CSharpWriter.cs', 'VbWriter.cs') {
    $path = Join-Path $root "src\ILXP\$name"
    $t = Get-Content -LiteralPath $path -Raw
    if ($t.Contains($old)) {
        $t = $t.Replace($old, $new)
        Set-Content -LiteralPath $path -Value $t -NoNewline
        Write-Host "escaped interpolated literal in $name"
    }
}
