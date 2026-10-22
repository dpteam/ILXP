try {
    & "$PSScriptRoot\Decrypt-Strings.ps1"
} catch {
    $_.Exception.ToString()
    $_.ScriptStackTrace
    $_.InvocationInfo.PositionMessage
}
