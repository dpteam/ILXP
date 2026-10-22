param([string]$OutDir = "$env:TEMP\ilxp-math-test")
$ErrorActionPreference = 'Stop'
$root = if ($PSScriptRoot) { Split-Path -Parent $PSScriptRoot } else { (Get-Location).Path }
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$source = Join-Path $OutDir 'MathTest.cs'
@'
public static class MathTest
{
    public static int Abs() { return System.Math.Abs(-5); }
    public static double Sqrt() { return System.Math.Sqrt(16.0); }
    public static int Max() { return System.Math.Max(3, 9); }
    public static int Dynamic(int value) { return System.Math.Abs(value); }
}
'@ | Set-Content -LiteralPath $source -Encoding UTF8

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
& $csc /nologo /target:library /optimize+ "/out:$OutDir\MathTest.dll" $source | Out-Host
if ($LASTEXITCODE -ne 0) { throw "csc failed with $LASTEXITCODE" }

$exe = Join-Path $root 'src\ILXP\bin\Debug\net40\ILXP.exe'
& $exe --clean "$OutDir\MathTest.dll" --output "$OutDir\MathTest.clean.dll"
Start-Sleep -Seconds 2
& $exe --ildasm "$OutDir\MathTest.clean.dll" --output "$OutDir\MathTest.clean.il"
Start-Sleep -Seconds 2

Write-Host '--- cleaned IL (method bodies) ---'
Get-Content "$OutDir\MathTest.clean.il" | Select-String -Pattern 'Abs|Sqrt|Max|ldc|call.*Math' | ForEach-Object { $_.Line.Trim() }
