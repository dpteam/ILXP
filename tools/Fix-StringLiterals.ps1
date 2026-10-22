$ErrorActionPreference = 'Stop'
$root = if ($PSScriptRoot) { Split-Path -Parent $PSScriptRoot } else { (Get-Location).Path }

$oldFormat = '<<r>>''{0}''<<>>'
$newFormat = '<<r>>"{0}"<<>>'
$oldConstant = '<<r>>''{RuntimeHelpers.GetObjectValue(parameter.Constant)}''<<>>'
$newConstant = '<<r>>"{RuntimeHelpers.GetObjectValue(parameter.Constant)}"<<>>'

$cs = Join-Path $root 'src\ILXP\CSharpWriter.cs'
$t = Get-Content -LiteralPath $cs -Raw
$t = $t.Replace($oldFormat, $newFormat)
$t = $t.Replace($oldConstant, $newConstant)
$t = $t.Replace(@'
    		if (type.IsByReference)
    		{
    			pointerSuffix += "&";
    			type = type.GetElementType();
    		}
'@, '')
Set-Content -LiteralPath $cs -Value $t -NoNewline
Write-Host 'CSharpWriter literals fixed'

$vb = Join-Path $root 'src\ILXP\VbWriter.cs'
$t = Get-Content -LiteralPath $vb -Raw
$t = $t.Replace($oldFormat, $newFormat)
$t = $t.Replace($oldConstant, $newConstant)
Set-Content -LiteralPath $vb -Value $t -NoNewline
Write-Host 'VbWriter literals fixed'
