[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$TestedCommit,
    [Parameter(Mandatory = $true)][string]$DeployId,
    [string]$RepositoryPath = '.',
    [string]$ReportPath = 'release-results/render-staging-verification.json'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'render-deployment.ps1')
$RepositoryPath = (Resolve-Path -LiteralPath $RepositoryPath).Path
$head = ([string](& git -C $RepositoryPath rev-parse HEAD)).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Render staging smoke: failed to resolve the checkout HEAD.' }
Assert-RenderMetadataOnlyChanges -RepositoryPath $RepositoryPath -FromCommit $TestedCommit -ToCommit $head

$serviceId = $env:RENDER_STAGING_SERVICE_ID
if ([string]::IsNullOrWhiteSpace($serviceId)) { throw 'RENDER_STAGING_SERVICE_ID is required.' }
if (-not [string]::IsNullOrWhiteSpace($env:RENDER_PRODUCTION_SERVICE_ID) -and
    $serviceId -ceq $env:RENDER_PRODUCTION_SERVICE_ID) {
    throw 'Render staging smoke: staging and production must use different services.'
}

$before = Assert-RenderGitDeployment -ServiceId $serviceId -DeployId $DeployId `
    -ExpectedCommit $TestedCommit -ExpectedBranch 'staging'
Assert-RenderSmokeTargetUrl -DeploymentUrl $before.Url

$global:LASTEXITCODE = 0
& (Join-Path $PSScriptRoot 'smoke-staging.ps1') -Environment Staging
$smokeSucceeded = $?
$smokeExitCode = $LASTEXITCODE
if (-not $smokeSucceeded -or $smokeExitCode -ne 0) { throw 'Render staging smoke: API smoke checks failed.' }

$after = Assert-RenderGitDeployment -ServiceId $serviceId -DeployId $DeployId `
    -ExpectedCommit $TestedCommit -ExpectedBranch 'staging'
if ($before.Url -cne $after.Url) { throw 'Render staging smoke: the service URL changed during verification.' }
Assert-RenderSmokeTargetUrl -DeploymentUrl $after.Url

$receipt = Write-RenderVerificationReport -Deployment $after -TestedCommit $TestedCommit `
    -RepositoryPath $RepositoryPath -ReportPath $ReportPath
Write-Host '[OK] The same Render staging deployment stayed live throughout the API smoke checks.'
$receipt
