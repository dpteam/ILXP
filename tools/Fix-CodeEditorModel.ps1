param([string]$Path = 'src\ILXP\CodeEditor.cs')
$ErrorActionPreference = 'Stop'
$text = Get-Content -LiteralPath $Path -Raw
$orig = $text

# Getters: receiver.method_N() -> receiver.Property
$getters = [ordered]@{
    'gClass.method_0()'   = 'gClass.Tokens'
    'item.method_0()'     = 'item.Tokens'
    'item.method_1()'     = 'item.Offset'
    'item.method_3()'     = 'item.Length'
    'item2.method_0()'    = 'item2.Text'
    'item2.method_2()'    = 'item2.Bounds'
    'item2.method_4()'    = 'item2.Link'
    'item2.method_6()'    = 'item2.Color'
    'gclass15_.method_2()' = 'gclass15_.Bounds'
    'gclass15_.method_4()' = 'gclass15_.Link'
    'gclass15_.method_6()' = 'gclass15_.Color'
    'gclass15_0.method_2()' = 'gclass15_0.Bounds'
    'gclass15_0.method_4()' = 'gclass15_0.Link'
    'gclass15_0.method_6()' = 'gclass15_0.Color'
    'gclass13_0[num].method_1()' = 'gclass13_0[num].Offset'
}
foreach ($key in $getters.Keys) { $text = $text.Replace($key, $getters[$key]) }

# Setters: receiver.method_N(expr) -> receiver.Property = expr
$setters = @(
    @{ Pattern = 'gClass2\.method_7\(([^;]+)\);'; Replacement = 'gClass2.Color = $1;' },
    @{ Pattern = 'gClass2\.method_5\(([^;]+)\);'; Replacement = 'gClass2.Link = $1;' },
    @{ Pattern = 'item2\.method_1\(([^;]+)\);'; Replacement = 'item2.Text = $1;' },
    @{ Pattern = 'item2\.method_3\(([^;]+)\);'; Replacement = 'item2.Bounds = $1;' },
    @{ Pattern = 'item2\.method_5\(([^;]+)\);'; Replacement = 'item2.Link = $1;' },
    @{ Pattern = 'item2\.method_7\(([^;]+)\);'; Replacement = 'item2.Color = $1;' },
    @{ Pattern = 'obj\.method_3\(([^;]+)\);'; Replacement = 'obj.Bounds = $1;' },
    @{ Pattern = 'item\.method_2\(([^;]+)\);'; Replacement = 'item.Offset = $1;' },
    @{ Pattern = 'item\.method_4\(([^;]+)\);'; Replacement = 'item.Length = $1;' }
)
foreach ($rule in $setters) {
    $text = [regex]::Replace($text, $rule.Pattern, $rule.Replacement)
}

if ($text -ne $orig) {
    Set-Content -LiteralPath $Path -Value $text -NoNewline
    Write-Host 'patched CodeEditor model accessors'
}
