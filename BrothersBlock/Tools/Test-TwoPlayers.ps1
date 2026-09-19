param()
$ErrorActionPreference = 'Stop'
$gameRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$gameExe = Join-Path $gameRoot 'Builds/Windows/ElementalJourney.exe'
if (-not (Test-Path -LiteralPath $gameExe)) { throw 'Run Run-Unity.ps1 -Action BuildDesktopTest first.' }
$resultDirectory = Join-Path $gameRoot ('Logs/Lan-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $resultDirectory | Out-Null
foreach ($role in @('host', 'client')) {
    $gameArguments = @('-screen-fullscreen', '0', '-screen-width', '1600', '-screen-height', '900', '-force-d3d11',
        '--lan-probe', $role, '--probe-dir', ('"' + $resultDirectory + '"'),
        '-logFile', ('"' + (Join-Path $resultDirectory ($role + '.log')) + '"'))
    $gameProcess = Start-Process -FilePath $gameExe -ArgumentList $gameArguments -WorkingDirectory $gameRoot -WindowStyle Hidden -PassThru
    Write-Output ($role + ' process: ' + $gameProcess.Id)
}
Write-Output ('Results: ' + $resultDirectory)
Write-Output 'Both processes exit automatically after recording their result. Inspect both JSON files for passed=true.'
