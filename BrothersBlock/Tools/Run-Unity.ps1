param(
    [ValidateSet('Import', 'EditTests', 'PlayTests', 'BuildAndroid', 'BuildDesktopTest')]
    [string]$Action = 'Import',
    [string]$EditorPath = 'C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Unity.exe'
)
$ErrorActionPreference = 'Stop'
$gameRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (-not (Test-Path -LiteralPath $EditorPath)) { throw 'Unity Editor was not found. Pass -EditorPath with the installed Unity.exe path.' }
$logDirectory = Join-Path $gameRoot 'Logs'
New-Item -ItemType Directory -Force -Path $logDirectory | Out-Null
$logPath = Join-Path $logDirectory ($Action.ToLowerInvariant() + '.log')
$gameArguments = @('-batchmode', '-nographics', '-projectPath', ('"' + $gameRoot + '"'), '-logFile', ('"' + $logPath + '"'))
switch ($Action) {
    'Import' { $gameArguments += @('-quit', '-executeMethod', 'BrothersBlockEditor.StarterProject.CreateStarter') }
    'BuildAndroid' { $gameArguments += @('-quit', '-buildTarget', 'Android', '-executeMethod', 'BrothersBlockEditor.StarterProject.BuildAndroid') }
    'BuildDesktopTest' { $gameArguments += @('-quit', '-buildTarget', 'Win64', '-executeMethod', 'BrothersBlockEditor.StarterProject.BuildDesktopTest') }
    'EditTests' { $gameArguments += @('-runTests', '-testPlatform', 'EditMode', '-assemblyNames', 'BrothersBlock.Tests', '-testResults', ('"' + (Join-Path $logDirectory 'edit-results.xml') + '"')) }
    'PlayTests' { $gameArguments += @('-runTests', '-testPlatform', 'PlayMode', '-assemblyNames', 'BrothersBlock.PlayModeTests', '-testResults', ('"' + (Join-Path $logDirectory 'play-results.xml') + '"')) }
}
# Optional, machine-specific CA configuration; never disables certificate checks.
$upmConfig = Join-Path $gameRoot '.validation/upmconfig.toml'
if (Test-Path -LiteralPath $upmConfig) { $env:UPM_GLOBAL_CONFIG_FILE = $upmConfig }
$caFile = Join-Path $gameRoot '.validation/system-ca.pem'
if (Test-Path -LiteralPath $caFile) { $env:NODE_EXTRA_CA_CERTS = $caFile }
$androidTrustStore = Join-Path $gameRoot '.validation/android-cacerts.p12'
if ($Action -eq 'BuildAndroid' -and (Test-Path -LiteralPath $androidTrustStore)) {
    # Contains public certificates already trusted by this machine, no private keys.
    $env:JAVA_TOOL_OPTIONS = '-Djavax.net.ssl.trustStore="' + $androidTrustStore + '" -Djavax.net.ssl.trustStorePassword=changeit'
}
$unityProcess = Start-Process -FilePath $EditorPath -ArgumentList $gameArguments -WindowStyle Hidden -PassThru
Write-Output ('Unity process: ' + $unityProcess.Id)
Write-Output ('Log: ' + $logPath)
