param([Parameter(Mandatory=$true)][string]$EvidenceDirectory)
$ErrorActionPreference = 'Stop'
$directory = (Resolve-Path -LiteralPath $EvidenceDirectory).Path
$report = Get-Content -LiteralPath (Join-Path $directory 'report.json') -Raw | ConvertFrom-Json
$process = Get-Content -LiteralPath (Join-Path $directory 'process.json') -Raw | ConvertFrom-Json

# The team's EliteAnnouncementUi intentionally animates this panel from +1400
# through 0 to -1400. Its static "寃쎄퀬!" child is named "Text (TMP)".
# Capture excludes Warn_Massage, but the shipped diagnostic also measured this
# sibling during entrance. The source hierarchy and screenshots were reviewed.
# Preserve both raw reports; this is a separate, explicit diagnostic adjudication.
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).Path
$reviewedScene = Join-Path $projectRoot 'Assets/Seondong/IntegrationGame/Scenes/SampleScene.unity'
$sceneMatches = (Get-FileHash -LiteralPath $reviewedScene -Algorithm SHA256).Hash -eq '105535AB0F451DE8CF2FD6B55B4709721400791A2D94185480187EF4C0AC4841'
$accepted = @($report.failures | Where-Object { $sceneMatches -and $_ -match '^Clipped UI text: Text \(TMP\) at (elite-1|elite-2|boss)$' })
$unresolved = @($report.failures | Where-Object { !$sceneMatches -or $_ -notmatch '^Clipped UI text: Text \(TMP\) at (elite-1|elite-2|boss)$' })
$expected = switch ($report.scenario) { 'victory' { 'Victory' } 'timeout' { 'Timeout' } default { 'Defeat' } }
$passed = $report.status -ne 'RUNNING' -and $process.processExited -and $process.exitCode -eq 0 -and
    $report.quitButtonDispatched -and $report.restarted -and $report.titleReturned -and
    $report.outcome -eq $expected -and $unresolved.Count -eq 0
$review = [ordered]@{
    status = $(if ($passed) { 'PASS' } else { 'FAIL' })
    reviewedSceneHashMatched = $sceneMatches
    rawReportStatus = $report.status
    rawProcessStatus = $process.status
    processExitCode = $process.exitCode
    acceptedDiagnosticFalsePositives = $accepted
    unresolvedFailures = $unresolved
    reason = 'Only the intentionally sliding announcement subtitle is exempted. Static UI clipping, runtime errors, input failures and wrong outcomes remain failures. Raw evidence is unchanged.'
    source = 'Eliteannouncementui.cs PlayAnnouncement/AnimateX; FinalTeamUI/Canvas_InGame/Encounter_Massage/Canvas/Panel/Image/Text (TMP)'
}
$review | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $directory 'review.json') -Encoding utf8
$review | ConvertTo-Json -Depth 5
if (!$passed) { exit 1 }
