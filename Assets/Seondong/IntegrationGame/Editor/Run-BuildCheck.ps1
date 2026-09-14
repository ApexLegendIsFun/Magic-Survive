param(
    [ValidateSet('death', 'victory', 'timeout', 'layout', 'growth')][string]$Scenario = 'death',
    [ValidateSet('Fire', 'Lightning', 'Frost', 'Earth', 'Dark')][string]$Element = 'Fire',
    [int]$Width = 1280,
    [int]$Height = 720,
    [string]$RunName = '',
    [int]$DeadlineSeconds = 1050
)
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).Path
$exe = Join-Path $projectRoot 'Builds/SeondongIntegrationFinal/Magic-Survive.exe'
if (!(Test-Path -LiteralPath $exe)) { throw 'Build Windows first.' }
if (!$RunName) { $RunName = "$Scenario-${Width}x${Height}-$(Get-Date -Format yyyyMMdd-HHmmss)" }
if ($RunName -notmatch '^[a-zA-Z0-9_-]+$') { throw 'RunName must contain only letters, digits, hyphens and underscores.' }
$outDir = Join-Path $projectRoot "Logs/SeondongIntegration/$RunName"
if (Test-Path -LiteralPath $outDir) { throw 'Use a new RunName to preserve previous evidence.' }
New-Item -ItemType Directory -Path $outDir | Out-Null
$runArgs = "-screen-fullscreen 0 -screen-width $Width -screen-height $Height -integration-auto $Scenario -integration-element $Element -integration-output `"$outDir`" -logFile `"$(Join-Path $outDir 'Player.log')`""
# Keep the interactive game window visible so Unity has a swapchain to capture.
# Input stays inside Unity; this script never injects OS keyboard/mouse events.
$process = Start-Process -FilePath $exe -ArgumentList $runArgs -WorkingDirectory $projectRoot -PassThru
$started = Get-Date
$deadline = $started.AddSeconds($DeadlineSeconds)
Write-Output "Started $Scenario at ${Width}x${Height}, process $($process.Id). Evidence: $outDir"
while (!$process.WaitForExit(10000)) {
    if ((Get-Date) -gt $deadline) {
        # Only the process returned by this invocation is stopped, never another game/editor.
        Stop-Process -Id $process.Id
        @{ status = 'FAIL'; reason = 'Process deadline exceeded'; processId = $process.Id } | ConvertTo-Json | Set-Content (Join-Path $outDir 'process.json')
        throw 'Test exceeded process deadline.'
    }
}
$process.Refresh()
$reportPath = Join-Path $outDir 'report.json'
$report = if (Test-Path $reportPath) { Get-Content $reportPath -Raw | ConvertFrom-Json } else { $null }
$passed = $report -and $report.status -eq 'PASS' -and $report.quitButtonDispatched -and $process.ExitCode -eq 0
@{
    status = $(if ($passed) { 'PASS' } else { 'FAIL' })
    exitCode = $process.ExitCode
    quitButtonDispatched = [bool]$report.quitButtonDispatched
    processExited = $true
    seconds = ((Get-Date) - $started).TotalSeconds
    executableSHA256 = (Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash
} | ConvertTo-Json | Set-Content (Join-Path $outDir 'process.json')
Get-Content (Join-Path $outDir 'process.json')
if (!$passed) { exit 1 }
