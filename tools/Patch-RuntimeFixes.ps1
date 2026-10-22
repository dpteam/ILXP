param(
    [string]$Root = $(if ($PSScriptRoot) { Join-Path (Split-Path -Parent $PSScriptRoot) 'src\ILXP' } else { Join-Path (Get-Location) 'src\ILXP' })
)
$ErrorActionPreference = 'Stop'

function Update-File([string]$name, [scriptblock]$patch) {
    $path = Join-Path $Root $name
    $text = Get-Content -LiteralPath $path -Raw
    $new = & $patch $text
    if ($new -ne $text) {
        Set-Content -LiteralPath $path -Value $new -NoNewline
        Write-Host "patched $name"
    }
    else {
        Write-Warning "no change in $name"
    }
}

# --- MainForm: harden the default font override -----------------------------
Update-File 'MainForm.cs' {
    param($text)
    $pattern = '(?s)Type typeFromHandle = typeof\(Control\);\s*FieldInfo field = typeFromHandle\.GetField\("defaultFont", BindingFlags\.Static \| BindingFlags\.NonPublic\);\s*Font font = Control\.DefaultFont;\s*Font value = new Font\(SystemFonts\.DialogFont\.Name, font\.SizeInPoints\);\s*field\.SetValue\(null, value\);'
    $replacement = @'
FieldInfo defaultFontField = typeof(Control).GetField("defaultFont", BindingFlags.Static | BindingFlags.NonPublic);
    		if (defaultFontField != null)
    		{
    			using (Font dialogFont = new Font(SystemFonts.DialogFont.Name, Control.DefaultFont.SizeInPoints))
    			{
    				defaultFontField.SetValue(null, dialogFont);
    			}
    		}
'@
    return [regex]::Replace($text, $pattern, $replacement)
}

# --- AboutForm: restore the real designer strings ---------------------------
Update-File 'AboutForm.cs' {
    param($text)
    $text = $text.Replace('vmethod_0().Text = "Label1";', 'vmethod_0().Text = "Close";')
    $text = $text.Replace('vmethod_4().Name = "Label1.Text";', 'vmethod_4().Name = "LabelInfo";')
    $text = $text.Replace('vmethod_6().Name = "<ProductName> (<Version>)";', 'vmethod_6().Name = "Label1";')
    $text = $text.Replace('vmethod_6().Text = "<ProductName>";', 'vmethod_6().Text = "<ProductName> (<Version>)";')
    $text = $text.Replace('vmethod_2().Name = "FlowLayoutPanel1";', 'vmethod_2().Name = "PictureBox1";')
    $text = $text.Replace('_003C().Name = "LabelCopyright";', '_003C().Name = "FlowLayoutPanel1";')
    $text = $text.Replace('vmethod_9().Name = "<CopyRight>";', 'vmethod_9().Name = "LabelCopyright";')
    $text = $text.Replace('vmethod_9().Text = "AboutDialog";', 'vmethod_9().Text = "<CopyRight>";')
    $text = $text.Replace('Text = "ButtonClose";', 'Text = "About ILXP";')
    return $text
}
