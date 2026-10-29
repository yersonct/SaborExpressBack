# Dot-source this file to validate Git-backed deployments using Render's API.
# The API origin is fixed so RENDER_API_KEY is never sent to an evidence URL.
function Assert-RenderGitDeployment {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][string]$ServiceId,
        [Parameter(Mandatory = $true)][string]$DeployId,
        [Parameter(Mandatory = $true)][string]$ExpectedCommit,
        [Parameter(Mandatory = $true)][string]$ExpectedBranch,
        [string]$ExpectedRepositoryFullName = $env:GITHUB_REPOSITORY,
        [switch]$AllowHistorical
    )

    $ErrorActionPreference = 'Stop'

    function Stop-RenderDeployment([string]$Message) {
        throw "Render deployment validation: $Message"
    }

    function Get-RenderProperty($Object, [string]$Name) {
        if ($null -eq $Object) { return $null }
        if ($Object -is [System.Collections.IDictionary]) { return $Object[$Name] }
        $property = $Object.PSObject.Properties[$Name]
        if ($null -eq $property) { return $null }
        return $property.Value
    }

    function Invoke-RenderGet([string]$Path) {
        try {
            return Invoke-RestMethod -Method Get -Uri "https://api.render.com/v1/$Path" `
                -Headers @{ Authorization = "Bearer $renderToken"; Accept = 'application/json' } `
                -TimeoutSec 30 -MaximumRedirection 0 -ErrorAction Stop
        } catch {
            # Do not include response bodies, headers, or the original exception:
            # these can contain credentials or private service information.
            Stop-RenderDeployment 'Render API request failed; verify API access and resource IDs.'
        }
    }

    if ($ServiceId -cnotmatch '^srv-[0-9a-z]{20}$') {
        Stop-RenderDeployment 'ServiceId must be a Render srv- ID followed by 20 lowercase letters or digits.'
    }
    if ($DeployId -cnotmatch '^dep-[0-9a-z]{20}$') {
        Stop-RenderDeployment 'DeployId must be a Render dep- ID followed by 20 lowercase letters or digits.'
    }
    if ($ExpectedCommit -notmatch '^[0-9a-fA-F]{40}$') {
        Stop-RenderDeployment 'ExpectedCommit must be a full 40-character Git SHA.'
    }
    if ([string]::IsNullOrWhiteSpace($ExpectedBranch)) {
        Stop-RenderDeployment 'ExpectedBranch is required.'
    }
    if ($ExpectedRepositoryFullName -notmatch '^[A-Za-z0-9][A-Za-z0-9-]*/[A-Za-z0-9_.-]+$') {
        Stop-RenderDeployment 'ExpectedRepositoryFullName must identify the expected GitHub owner/repository.'
    }
    $renderToken = $env:RENDER_API_KEY
    if ([string]::IsNullOrWhiteSpace($renderToken)) {
        Stop-RenderDeployment 'RENDER_API_KEY is required.'
    }
    $renderToken = $renderToken.Trim()

    $service = Invoke-RenderGet "services/$ServiceId"
    if ([string](Get-RenderProperty $service 'id') -cne $ServiceId) {
        Stop-RenderDeployment 'The service returned by Render does not match ServiceId.'
    }
    if ([string](Get-RenderProperty $service 'type') -cne 'web_service') {
        Stop-RenderDeployment 'The expected Render service must be a web_service.'
    }
    if (-not [string]::IsNullOrWhiteSpace([string](Get-RenderProperty $service 'imagePath'))) {
        Stop-RenderDeployment 'Image-backed services cannot be approved as Git-backed deployments.'
    }
    if ([string](Get-RenderProperty $service 'branch') -cne $ExpectedBranch) {
        Stop-RenderDeployment 'The Render service is configured for a different Git branch.'
    }

    $repositoryUri = $null
    $repositoryText = [string](Get-RenderProperty $service 'repo')
    if (-not [uri]::TryCreate($repositoryText, [UriKind]::Absolute, [ref]$repositoryUri) -or
        $repositoryUri.Scheme -ne 'https' -or $repositoryUri.Host -ne 'github.com' -or
        -not $repositoryUri.IsDefaultPort -or $repositoryUri.UserInfo -ne '' -or
        $repositoryUri.Query -ne '' -or $repositoryUri.Fragment -ne '') {
        Stop-RenderDeployment 'The Render service must reference the expected GitHub HTTPS repository.'
    }
    $repositoryPath = $repositoryUri.AbsolutePath.TrimEnd('/')
    if ($repositoryPath.EndsWith('.git', [StringComparison]::OrdinalIgnoreCase)) {
        $repositoryPath = $repositoryPath.Substring(0, $repositoryPath.Length - 4)
    }
    if (-not [string]::Equals($repositoryPath, "/$ExpectedRepositoryFullName", [StringComparison]::OrdinalIgnoreCase)) {
        Stop-RenderDeployment 'The Render service belongs to a different repository.'
    }

    $serviceDetails = Get-RenderProperty $service 'serviceDetails'
    $serviceUrl = [string](Get-RenderProperty $serviceDetails 'url')
    $serviceUri = $null
    if (-not [uri]::TryCreate($serviceUrl, [UriKind]::Absolute, [ref]$serviceUri) -or
        $serviceUri.Scheme -ne 'https' -or -not $serviceUri.IsDefaultPort -or
        [string]::IsNullOrWhiteSpace($serviceUri.Host) -or $serviceUri.UserInfo -ne '' -or
        $serviceUri.Query -ne '' -or $serviceUri.Fragment -ne '' -or $serviceUri.AbsolutePath -ne '/') {
        Stop-RenderDeployment 'Render serviceDetails.url must be an HTTPS origin without credentials, path, or query.'
    }

    $deploy = Invoke-RenderGet "services/$ServiceId/deploys/$DeployId"
    if ([string](Get-RenderProperty $deploy 'id') -cne $DeployId) {
        Stop-RenderDeployment 'The deploy returned by Render does not match DeployId.'
    }
    if ($null -ne (Get-RenderProperty $deploy 'image')) {
        Stop-RenderDeployment 'Image-backed deploys cannot be approved as Git-backed deployments.'
    }
    $deployStatus = [string](Get-RenderProperty $deploy 'status')
    if ($deployStatus -cne 'live' -and -not ($AllowHistorical -and $deployStatus -ceq 'deactivated')) {
        Stop-RenderDeployment 'The requested Render deployment is not live or an allowed historical deployment.'
    }
    $commit = [string](Get-RenderProperty (Get-RenderProperty $deploy 'commit') 'id')
    if ($commit -notmatch '^[0-9a-fA-F]{40}$' -or
        -not [string]::Equals($commit, $ExpectedCommit, [StringComparison]::OrdinalIgnoreCase)) {
        Stop-RenderDeployment 'The Render deployment does not contain ExpectedCommit.'
    }
    $finishedAt = [DateTimeOffset]::MinValue
    $finishedText = [string](Get-RenderProperty $deploy 'finishedAt')
    if ([string]::IsNullOrWhiteSpace($finishedText) -or
        -not [DateTimeOffset]::TryParse($finishedText, [ref]$finishedAt)) {
        Stop-RenderDeployment 'The live deployment must have a valid finishedAt timestamp.'
    }

    # Historical validation is only for records independently attested by a
    # successful smoke artifact. The default always confirms a currently live
    # deployment immediately before a smoke test can exercise it.
    if (-not $AllowHistorical) {
        # Capture first so the cmdlet's single JSON-array object is enumerated.
        $liveResponse = Invoke-RenderGet "services/$ServiceId/deploys?status=live&limit=100"
        $liveEntries = @($liveResponse)
        $matchingLiveDeploys = @($liveEntries | ForEach-Object {
            $listedDeploy = Get-RenderProperty $_ 'deploy'
            if ([string](Get-RenderProperty $listedDeploy 'id') -ceq $DeployId -and
                [string](Get-RenderProperty $listedDeploy 'status') -ceq 'live' -and
                [string]::Equals([string](Get-RenderProperty (Get-RenderProperty $listedDeploy 'commit') 'id'),
                    $ExpectedCommit, [StringComparison]::OrdinalIgnoreCase) -and
                $null -eq (Get-RenderProperty $listedDeploy 'image')) {
                $listedDeploy
            }
        })
        if ($matchingLiveDeploys.Count -ne 1) {
            Stop-RenderDeployment 'The requested deployment is absent from the current live deployments.'
        }
    }

    return [pscustomobject]@{
        ServiceId = $ServiceId
        DeployId = $DeployId
        Commit = $commit.ToLowerInvariant()
        Url = $serviceUri.GetLeftPart([UriPartial]::Authority)
        FinishedAt = $finishedAt.ToUniversalTime().ToString('o')
    }
}

function Get-CurrentRenderGitDeployment {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][string]$ServiceId,
        [Parameter(Mandatory = $true)][string]$ExpectedBranch,
        [string]$ExpectedRepositoryFullName = $env:GITHUB_REPOSITORY
    )

    $ErrorActionPreference = 'Stop'
    if ($ServiceId -cnotmatch '^srv-[0-9a-z]{20}$') {
        throw 'Render deployment validation: ServiceId must be a Render srv- ID followed by 20 lowercase letters or digits.'
    }
    if ([string]::IsNullOrWhiteSpace($ExpectedBranch) -or
        $ExpectedRepositoryFullName -notmatch '^[A-Za-z0-9][A-Za-z0-9-]*/[A-Za-z0-9_.-]+$') {
        throw 'Render deployment validation: expected GitHub repository and branch are required.'
    }
    $renderToken = $env:RENDER_API_KEY
    if ([string]::IsNullOrWhiteSpace($renderToken)) {
        throw 'Render deployment validation: RENDER_API_KEY is required.'
    }

    try {
        $response = Invoke-RestMethod -Method Get `
            -Uri "https://api.render.com/v1/services/$ServiceId/deploys?status=live&limit=100" `
            -Headers @{ Authorization = "Bearer $($renderToken.Trim())"; Accept = 'application/json' } `
            -TimeoutSec 30 -MaximumRedirection 0 -ErrorAction Stop
    } catch {
        throw 'Render deployment validation: Render API request failed; verify API access and resource IDs.'
    }
    $entries = @($response)
    if ($entries.Count -ne 1) {
        throw 'Render deployment validation: exactly one current live deployment is required.'
    }
    $currentDeploy = $entries[0].deploy
    $deployId = [string]$currentDeploy.id
    $commit = [string]$currentDeploy.commit.id
    if ($deployId -cnotmatch '^dep-[0-9a-z]{20}$' -or $commit -notmatch '^[0-9a-fA-F]{40}$' -or
        [string]$currentDeploy.status -cne 'live' -or $null -ne $currentDeploy.image) {
        throw 'Render deployment validation: the live deployment list contains an invalid Git-backed deployment.'
    }

    # Re-read the service, deployment, and live list in the shared validator so a
    # replaced deployment cannot silently pass the current-deployment lookup.
    return Assert-RenderGitDeployment -ServiceId $ServiceId -DeployId $deployId `
        -ExpectedCommit $commit -ExpectedBranch $ExpectedBranch `
        -ExpectedRepositoryFullName $ExpectedRepositoryFullName
}

function Assert-RenderMetadataOnlyChanges {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][string]$RepositoryPath,
        [Parameter(Mandatory = $true)][string]$FromCommit,
        [Parameter(Mandatory = $true)][string]$ToCommit
    )

    if ($FromCommit -notmatch '^[0-9a-fA-F]{40}$' -or $ToCommit -notmatch '^[0-9a-fA-F]{40}$') {
        throw 'Render smoke: full 40-character Git SHAs are required.'
    }
    & git -C $RepositoryPath merge-base --is-ancestor $FromCommit $ToCommit 2>$null
    if ($LASTEXITCODE -ne 0) { throw 'Render smoke: the tested source must be an ancestor of the checkout.' }
    $changes = @(& git -C $RepositoryPath diff --name-only --no-renames $FromCommit $ToCommit --)
    if ($LASTEXITCODE -ne 0) { throw 'Render smoke: failed to compare the tested source and checkout.' }
    $allowedPaths = @('docs/release/production-release-candidate.json', 'docs/release/production-release-evidence.json')
    if (@($changes | Where-Object { $_ -cnotin $allowedPaths }).Count -gt 0) {
        throw 'Render smoke: only the two production release metadata JSON files may differ from the tested source.'
    }
}

function Assert-RenderSmokeTargetUrl {
    [CmdletBinding()]
    param([Parameter(Mandatory = $true)][string]$DeploymentUrl)

    $configuredUri = $null
    if (-not [uri]::TryCreate($env:RELEASE_SMOKE_API_URL, [UriKind]::Absolute, [ref]$configuredUri) -or
        $configuredUri.Scheme -ne 'https' -or -not $configuredUri.IsDefaultPort -or
        $configuredUri.UserInfo -ne '' -or $configuredUri.Query -ne '' -or
        $configuredUri.Fragment -ne '' -or $configuredUri.AbsolutePath -ne '/' -or
        $configuredUri.GetLeftPart([UriPartial]::Authority) -cne $DeploymentUrl) {
        throw 'Render smoke: RELEASE_SMOKE_API_URL must match the HTTPS origin returned for the Render service.'
    }
}

function Write-RenderVerificationReport {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]$Deployment,
        [Parameter(Mandatory = $true)][string]$TestedCommit,
        [Parameter(Mandatory = $true)][string]$RepositoryPath,
        [Parameter(Mandatory = $true)][string]$ReportPath
    )

    $resolvedReport = if ([IO.Path]::IsPathRooted($ReportPath)) {
        [IO.Path]::GetFullPath($ReportPath)
    } else {
        [IO.Path]::GetFullPath((Join-Path $RepositoryPath $ReportPath))
    }
    [void][IO.Directory]::CreateDirectory((Split-Path -Parent $resolvedReport))
    $report = [ordered]@{
        schemaVersion = 1
        provider = 'render-git'
        serviceId = $Deployment.ServiceId
        deployId = $Deployment.DeployId
        testedCommit = $TestedCommit.ToLowerInvariant()
        apiUrl = $Deployment.Url
        verifiedAtUtc = [DateTime]::UtcNow.ToString('o')
    }
    [IO.File]::WriteAllText($resolvedReport, ($report | ConvertTo-Json -Depth 5), (New-Object Text.UTF8Encoding($false)))
    return $resolvedReport
}
