[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ExpectedCommit,
    [Parameter(Mandatory = $true)][string]$DeployId
)

$ErrorActionPreference = 'Stop'
$serviceId = $env:RENDER_PRODUCTION_SERVICE_ID
if ([string]::IsNullOrWhiteSpace($serviceId)) {
    throw 'RENDER_PRODUCTION_SERVICE_ID is required to verify the production deployment.'
}

. (Join-Path $PSScriptRoot 'render-deployment.ps1')
$deployment = Assert-RenderGitDeployment -ServiceId $serviceId -DeployId $DeployId `
    -ExpectedCommit $ExpectedCommit -ExpectedBranch 'main'

Write-Host "[OK] Render production deployment $($deployment.DeployId) is live at commit $($deployment.Commit)."
$deployment
