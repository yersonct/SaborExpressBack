[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ExpectedCommit,
    [Parameter(Mandatory = $true)][string]$DeployId,
    [string]$RepositoryPath = '.',
    [string]$ReportPath = 'release-results/render-production-verification.json'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'render-deployment.ps1')
$RepositoryPath = (Resolve-Path -LiteralPath $RepositoryPath).Path
if ($ExpectedCommit -notmatch '^[0-9a-fA-F]{40}$') { throw 'Render production smoke: ExpectedCommit must be a full Git SHA.' }
$head = ([string](& git -C $RepositoryPath rev-parse HEAD)).Trim()
if ($LASTEXITCODE -ne 0 -or $head -cne $ExpectedCommit.ToLowerInvariant()) {
    throw 'Render production smoke: the checkout must match ExpectedCommit.'
}
$main = ([string](& git -C $RepositoryPath rev-parse --verify origin/main 2>$null)).Trim()
if ($LASTEXITCODE -ne 0 -or $main -cne $head) {
    throw 'Render production smoke: ExpectedCommit must be the current origin/main commit.'
}

$evidencePath = Join-Path $RepositoryPath 'docs/release/production-release-evidence.json'
if (-not (Test-Path -LiteralPath $evidencePath -PathType Leaf)) { throw 'Render production smoke: production release evidence is required.' }
try { $evidence = Get-Content -LiteralPath $evidencePath -Raw | ConvertFrom-Json }
catch { throw 'Render production smoke: production release evidence must contain valid JSON.' }
if ([string]$evidence.schemaVersion -cne '2' -or [string]$evidence.deploymentProvider -cne 'render-git') {
    throw 'Render production smoke: schemaVersion 2 and deploymentProvider render-git are required.'
}
Assert-RenderMetadataOnlyChanges -RepositoryPath $RepositoryPath -FromCommit ([string]$evidence.sourceCommit) -ToCommit $head

$serviceId = $env:RENDER_PRODUCTION_SERVICE_ID
if ([string]::IsNullOrWhiteSpace($serviceId)) { throw 'RENDER_PRODUCTION_SERVICE_ID is required.' }
if (-not [string]::IsNullOrWhiteSpace($env:RENDER_STAGING_SERVICE_ID) -and
    $serviceId -ceq $env:RENDER_STAGING_SERVICE_ID) {
    throw 'Render production smoke: staging and production must use different services.'
}

$before = Assert-RenderGitDeployment -ServiceId $serviceId -DeployId $DeployId `
    -ExpectedCommit $ExpectedCommit -ExpectedBranch 'main'
Assert-RenderSmokeTargetUrl -DeploymentUrl $before.Url

$global:LASTEXITCODE = 0
& (Join-Path $PSScriptRoot 'smoke-staging.ps1') -Environment Production
$smokeSucceeded = $?
$smokeExitCode = $LASTEXITCODE
if (-not $smokeSucceeded -or $smokeExitCode -ne 0) { throw 'Render production smoke: API smoke checks failed.' }

$after = Assert-RenderGitDeployment -ServiceId $serviceId -DeployId $DeployId `
    -ExpectedCommit $ExpectedCommit -ExpectedBranch 'main'
if ($before.Url -cne $after.Url) { throw 'Render production smoke: the service URL changed during verification.' }
Assert-RenderSmokeTargetUrl -DeploymentUrl $after.Url

$receipt = Write-RenderVerificationReport -Deployment $after -TestedCommit $ExpectedCommit `
    -RepositoryPath $RepositoryPath -ReportPath $ReportPath
Write-Host '[OK] The current main deployment stayed live throughout the production API smoke checks.'
$receipt
