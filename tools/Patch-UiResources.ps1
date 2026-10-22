param(
    [string]$Root = $(if ($PSScriptRoot) { Join-Path (Split-Path -Parent $PSScriptRoot) 'src\ILXP' } else { Join-Path (Get-Location) 'src\ILXP' })
)
$ErrorActionPreference = 'Stop'

function Update-File([string]$name, [hashtable]$replacements) {
    $path = Join-Path $Root $name
    $text = Get-Content -LiteralPath $path -Raw
    foreach ($key in $replacements.Keys) {
        $text = $text.Replace($key, $replacements[$key])
    }
    Set-Content -LiteralPath $path -Value $text -NoNewline
    Write-Host "patched $name"
}

$main = @{
    'Class6.smethod_0()' = 'AppSettings.Default'
    'Class1.smethod_1().Info.Version.ToString(3)' = 'AppInfo.VersionText'
    'Class1.smethod_0().Clipboard.SetText' = 'Clipboard.SetText'
    '(Image)componentResourceManager.GetObject("MenuItemFileOpen.Image")' = 'AppResources.FileOpen'
    '(Image)componentResourceManager.GetObject("ToolStripButtonLoadFile.Image")' = 'AppResources.FileOpen'
    '(Image)componentResourceManager.GetObject("MenuItemViewRefresh.Image")' = 'AppResources.Refresh'
    '(Image)componentResourceManager.GetObject("ToolStripButtonRefresh.Image")' = 'AppResources.Refresh'
    '(Image)componentResourceManager.GetObject("MenuItemViewBack.Image")' = 'AppResources.NavBack'
    '(Image)componentResourceManager.GetObject("ToolStripButtonBack.Image")' = 'AppResources.NavBack'
    '(Image)componentResourceManager.GetObject("MenuItemContextBack.Image")' = 'AppResources.NavBack'
    '(Image)componentResourceManager.GetObject("MenuItemViewForward.Image")' = 'AppResources.NavForward'
    '(Image)componentResourceManager.GetObject("ToolStripButtonForward.Image")' = 'AppResources.NavForward'
    '(Image)componentResourceManager.GetObject("MenuItemContextForward.Image")' = 'AppResources.NavForward'
    '(ImageListStreamer)componentResourceManager.GetObject("ImageList1.ImageStream")' = 'AppResources.CreateTreeIconStreamer()'
    '(Icon)componentResourceManager.GetObject("$this.Icon")' = 'AppResources.AppIcon'
    'Text = "CodeReflect <Version>";' = 'Text = "ILXP <Version>";'
    "`t`tComponentResourceManager componentResourceManager = new ComponentResourceManager(typeof(GForm1));`r`n" = ''
}

$about = @{
    'Class5.smethod_6()' = 'AppResources.CompanyLogo'
    'Class1.smethod_1().Info.ProductName' = 'AppInfo.ProductName'
    'Class1.smethod_1().Info.Version.ToString(3)' = 'AppInfo.VersionText'
    'Class1.smethod_1().Info.Copyright' = 'AppInfo.Copyright'
    'vmethod_4().Text = componentResourceManager.GetString("Label1.Text");' = 'vmethod_4().Text = "Warning: This computer program is protected by copyright law and international treaties.";'
    "`t`tComponentResourceManager componentResourceManager = new ComponentResourceManager(typeof(GForm0));`r`n" = ''
}

Update-File 'MainForm.cs' $main
Update-File 'AboutForm.cs' $about
