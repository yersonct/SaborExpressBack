[CmdletBinding()]
param([string]$RunCasePath)

$ErrorActionPreference = 'Stop'

# Run every gate in its own process: exit statements and test-only API mocks must
# never affect the caller. These fixtures are not release evidence.
if ($RunCasePath) {
    try {
        $case = Get-Content -LiteralPath $RunCasePath -Raw | ConvertFrom-Json
        $env:GITHUB_TOKEN = 'synthetic-test-token'
        $env:GITHUB_REPOSITORY = 'example/release-fixture'
        $env:GITHUB_API_URL = 'https://release-gate.invalid'
        $env:RENDER_API_KEY = [string]$case.renderApiKey
        $env:RENDER_STAGING_SERVICE_ID = [string]$case.renderServiceId

        function Invoke-RestMethod {
            [CmdletBinding()]
            param($Method, [string]$Uri, $Headers, $TimeoutSec, [int]$MaximumRedirection)
            $property = $case.responses.PSObject.Properties[$Uri]
            if ($null -eq $property) { throw "Unexpected API request in fixture: $Uri" }
            if ($property.Value.PSObject.Properties['fixtureError']) { throw [string]$property.Value.fixtureError }
            # Invoke-RestMethod emits JSON arrays as a single pipeline object.
            # Returning the array normally here would hide nested-array bugs.
            if ($property.Value -is [Array]) {
                Write-Output -NoEnumerate $property.Value
                return
            }
            return $property.Value
        }

        function Invoke-WebRequest {
            [CmdletBinding()]
            param($Method, [string]$Uri, $Headers, [string]$OutFile, $TimeoutSec, [int]$MaximumRedirection)
            if ($Uri -ne 'https://release-gate.invalid/repos/example/release-fixture/actions/artifacts/41/zip') {
                throw "Unexpected artifact request in fixture: $Uri"
            }
            Copy-Item -LiteralPath $case.reportZip -Destination $OutFile
        }

        $arguments = @{
            EvidencePath = $case.evidencePath
            ExpectedHeadSha = $case.headSha
            RepositoryPath = $case.repository
        }
        if ($case.gate -eq 'production') { $arguments.ExpectedHeadRef = $case.headRef }
        & $case.scriptPath @arguments
        exit 0
    } catch {
        [Console]::Out.WriteLine($_.Exception.Message)
        exit 1
    }
}

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$securityScript = Join-Path $repositoryRoot 'scripts/release/security-gate.ps1'
$productionScript = Join-Path $repositoryRoot 'scripts/release/validate-production-release.ps1'
$engine = (Get-Process -Id $PID).Path
$tempParent = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\', '/')
$testRoot = Join-Path $tempParent ('SaborExpress-promotion-tests-' + [guid]::NewGuid().ToString('N'))
$fixtureRepo = Join-Path $testRoot 'repository'
$securityPath = 'docs/release/security-gate-evidence.json'
$productionPath = 'docs/release/production-release-evidence.json'
$candidatePath = 'docs/release/production-release-candidate.json'
$script:caseNumber = 0
$script:passed = 0
$script:failures = @()
$script:serviceId = 'srv-' + ('a' * 20)
$script:deployId = 'dep-' + ('b' * 20)
$script:currentDeployId = 'dep-' + ('c' * 20)
$script:stagingReport = $null

function Invoke-FixtureGit {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)
    $output = & git -C $fixtureRepo @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Fixture git failed: $Arguments`n$output" }
    return $output
}

function Write-FixtureJson($Value, [string]$Path) {
    $parent = Split-Path -Parent $Path
    [void][IO.Directory]::CreateDirectory($parent)
    $json = $Value | ConvertTo-Json -Depth 20
    [IO.File]::WriteAllText($Path, $json, (New-Object Text.UTF8Encoding($false)))
}

function Commit-Fixture([string]$Message) {
    Invoke-FixtureGit add --all | Out-Null
    Invoke-FixtureGit commit --quiet -m $Message | Out-Null
    return (Invoke-FixtureGit rev-parse HEAD).Trim()
}

function Assert-Gate {
    param(
        [string]$Name,
        [string]$Gate,
        [string]$HeadSha,
        [string]$HeadRef = 'staging',
        [bool]$ShouldPass = $true,
        [string]$ExpectedOutput = '',
        $Responses = @{},
        $Report = $script:stagingReport,
        [string]$RenderApiKey = 'synthetic-render-token',
        [string]$RenderServiceId = $script:serviceId
    )
    Invoke-FixtureGit checkout --quiet --detach $HeadSha | Out-Null
    $script:caseNumber++
    $casePath = Join-Path $testRoot ("case-$script:caseNumber.json")
    $reportZip = Join-Path $testRoot "report-$script:caseNumber.zip"
    if ($null -ne $Report) {
        $reportDirectory = Join-Path $testRoot "report-$script:caseNumber"
        Write-FixtureJson $Report (Join-Path $reportDirectory 'render-staging-verification.json')
        Compress-Archive -LiteralPath (Join-Path $reportDirectory 'render-staging-verification.json') -DestinationPath $reportZip
    }
    Write-FixtureJson @{
        gate = $Gate
        scriptPath = $(if ($Gate -eq 'production') { $productionScript } else { $securityScript })
        evidencePath = $(if ($Gate -eq 'production') { $productionPath } else { $securityPath })
        headSha = $HeadSha
        headRef = $HeadRef
        repository = $fixtureRepo
        responses = $Responses
        reportZip = $reportZip
        renderApiKey = $RenderApiKey
        renderServiceId = $RenderServiceId
    } $casePath
    $output = (& $engine -NoProfile -NonInteractive -ExecutionPolicy Bypass -File $PSCommandPath -RunCasePath $casePath 2>&1 | Out-String)
    $exitCode = $LASTEXITCODE
    $correctExit = if ($ShouldPass) { $exitCode -eq 0 } else { $exitCode -ne 0 }
    if ($correctExit -and ($ExpectedOutput -eq '' -or $output -match $ExpectedOutput)) {
        $script:passed++
        Write-Host "PASS $Name"
    } else {
        $script:failures += $Name
        Write-Host "FAIL $Name (exit $exitCode)`n$output"
    }
}

function Copy-Fixture($Value) {
    return ($Value | ConvertTo-Json -Depth 20 | ConvertFrom-Json)
}

function Commit-EvidenceVariant($Evidence, [string]$Message) {
    Invoke-FixtureGit checkout --quiet --detach $script:metadataSha | Out-Null
    Write-FixtureJson $Evidence (Join-Path $fixtureRepo $productionPath)
    return Commit-Fixture $Message
}

try {
    [void][IO.Directory]::CreateDirectory($fixtureRepo)
    Invoke-FixtureGit init --quiet | Out-Null
    Invoke-FixtureGit config user.name 'Synthetic release tests' | Out-Null
    Invoke-FixtureGit config user.email 'release-tests@example.invalid' | Out-Null
    Invoke-FixtureGit config commit.gpgsign false | Out-Null
    Invoke-FixtureGit config core.autocrlf false | Out-Null
    Invoke-FixtureGit config core.safecrlf false | Out-Null

    [IO.File]::WriteAllText((Join-Path $fixtureRepo 'application.txt'), 'baseline')
    $baselineSha = Commit-Fixture 'Synthetic baseline'
    [IO.File]::WriteAllText((Join-Path $fixtureRepo 'application.txt'), 'measured candidate')
    $measuredSha = Commit-Fixture 'Synthetic measured candidate'

    Assert-Gate -Name 'staging reaches missing production evidence validation' -Gate production -HeadSha $measuredSha -ShouldPass $false -ExpectedOutput 'no existe docs/release/production-release-evidence.json'
    Assert-Gate -Name 'legacy release branch reaches evidence validation' -Gate production -HeadSha $measuredSha -HeadRef 'release/HU-MAIN-01-promocion-produccion' -ShouldPass $false -ExpectedOutput 'no existe docs/release/production-release-evidence.json'
    Assert-Gate -Name 'develop is rejected as production source' -Gate production -HeadSha $measuredSha -HeadRef develop -ShouldPass $false -ExpectedOutput 'PR a main debe provenir'
    Assert-Gate -Name 'security evidence remains mandatory' -Gate security -HeadSha $measuredSha -ShouldPass $false -ExpectedOutput 'no existe docs/release/security-gate-evidence.json'

    $securityEvidence = @{
        schemaVersion = 1
        environment = 'staging'
        baseline = @{
            commit = $baselineSha; capturedAtUtc = '2026-01-01T00:00:00Z'; loadRunId = 'synthetic-baseline'
            routes = @(@{ method = 'GET'; route = '/health'; requestCount = 100; http5xxCount = 0; serverP95Ms = 100 })
        }
        candidate = @{
            commit = $measuredSha; capturedAtUtc = '2026-01-01T01:00:00Z'; loadRunId = 'synthetic-candidate'
            routes = @(@{ method = 'GET'; route = '/health'; requestCount = 100; http5xxCount = 0; serverP95Ms = 90 })
        }
    }
    Write-FixtureJson $securityEvidence (Join-Path $fixtureRepo $securityPath)
    # A file in the working tree must not substitute for committed evidence:
    # this commit deliberately contains only an otherwise permitted metadata path.
    Write-FixtureJson @{ schemaVersion = 1; state = 'pending'; sourceCommit = $measuredSha } (Join-Path $fixtureRepo $candidatePath)
    Invoke-FixtureGit add -- $candidatePath | Out-Null
    Invoke-FixtureGit commit --quiet -m 'Synthetic metadata without committed security evidence' | Out-Null
    $metadataOnlySha = (Invoke-FixtureGit rev-parse HEAD).Trim()
    Assert-Gate -Name 'metadata alone cannot replace committed security evidence' -Gate security -HeadSha $metadataOnlySha -ShouldPass $false -ExpectedOutput 'solo se permite'
    [IO.File]::Delete((Join-Path $fixtureRepo $securityPath))
    Invoke-FixtureGit checkout --quiet --detach $measuredSha | Out-Null
    Write-FixtureJson $securityEvidence (Join-Path $fixtureRepo $securityPath)
    $approvedSha = Commit-Fixture 'Synthetic security evidence'
    Assert-Gate -Name 'security evidence by itself remains accepted' -Gate security -HeadSha $approvedSha -ExpectedOutput 'Security release gate: evidencia'

    $productionEvidence = @{
        schemaVersion = 2; deploymentProvider = 'render-git'; version = '1.2.3'; sourceCommit = $approvedSha
        releaseNotes = @('Synthetic fixture release')
        migrationPlan = 'Synthetic fixture: this release has no database migrations.'
        rollbackPlan = 'Synthetic fixture: deploy the previously approved Git commit in Render.'
        qa = @{ runId = 12; testedCommit = $approvedSha }
        staging = @{ runId = 13; testedCommit = $approvedSha; serviceId = $script:serviceId; deployId = $script:deployId }
        approval = @{ approvedBy = 'synthetic-fixture'; approvedAtUtc = '2026-01-01T02:00:00Z' }
    }
    Write-FixtureJson @{ schemaVersion = 2; state = 'approved'; sourceCommit = $approvedSha } (Join-Path $fixtureRepo $candidatePath)
    Write-FixtureJson $productionEvidence (Join-Path $fixtureRepo $productionPath)
    $script:metadataSha = Commit-Fixture 'Synthetic production metadata'
    Invoke-FixtureGit update-ref refs/remotes/origin/staging $metadataSha | Out-Null
    Assert-Gate -Name 'security accepts production metadata after measurement' -Gate security -HeadSha $metadataSha -ExpectedOutput 'Security release gate: evidencia'

    # Two explicit moves make the case-only rename observable on Windows, too.
    Invoke-FixtureGit mv -- $candidatePath 'docs/release/candidate-temporary.json' | Out-Null
    Invoke-FixtureGit mv -- 'docs/release/candidate-temporary.json' 'docs/release/Production-release-candidate.json' | Out-Null
    $caseVariantSha = Commit-Fixture 'Synthetic metadata with unauthorized filename case'
    Assert-Gate -Name 'security metadata allowlist requires the exact filename case' -Gate security -HeadSha $caseVariantSha -ShouldPass $false -ExpectedOutput 'solo se permite'
    Assert-Gate -Name 'production metadata allowlist requires the exact filename case' -Gate production -HeadSha $caseVariantSha -ShouldPass $false -ExpectedOutput 'contiene cambios adicionales'

    $script:stagingReport = @{
        schemaVersion = 1; provider = 'render-git'; serviceId = $script:serviceId; deployId = $script:deployId
        testedCommit = $approvedSha; apiUrl = 'https://stage.example.invalid'; verifiedAtUtc = '2026-01-01T01:30:00Z'
    }
    $api = 'https://release-gate.invalid/repos/example/release-fixture'
    $renderServiceApi = "https://api.render.com/v1/services/$script:serviceId"
    $renderDeployApi = "$renderServiceApi/deploys/$script:deployId"
    $renderLiveApi = "$renderServiceApi/deploys?status=live&limit=100"
    $responses = @{}
    $responses["$api/commits/$approvedSha/check-runs"] = @{
        check_runs = @(
            @{ name = 'security-gate'; status = 'completed'; conclusion = 'success'; started_at = '2026-01-01T02:00:00Z' },
            @{ name = 'GitGuardian Security Checks'; status = 'completed'; conclusion = 'success'; started_at = '2026-01-01T02:00:00Z' }
        )
    }
    $responses["$api/actions/runs/12"] = @{
        status = 'completed'; conclusion = 'success'; name = 'QA operational regression'
        path = '.github/workflows/qa-operational-regression.yml'; head_sha = $approvedSha; head_branch = 'qa'
        created_at = '2026-01-01T00:45:00Z'; updated_at = '2026-01-01T00:50:00Z'
    }
    $responses["$api/actions/runs/13"] = @{
        status = 'completed'; conclusion = 'success'; name = 'Staging release smoke'
        path = '.github/workflows/staging-release-smoke.yml'; head_sha = $approvedSha; head_branch = 'staging'
        created_at = '2026-01-01T01:10:00Z'; updated_at = '2026-01-01T01:31:00Z'
    }
    $responses["$api/actions/runs/13/artifacts"] = @{ artifacts = @(@{ id = 41; name = 'staging-render-verification'; expired = $false }) }
    $responses[$renderServiceApi] = @{
        id = $script:serviceId; type = 'web_service'; repo = 'https://github.com/example/release-fixture'; branch = 'staging'
        serviceDetails = @{ url = 'https://stage.example.invalid' }
    }
    $historicalDeploy = @{
        id = $script:deployId; status = 'live'; commit = @{ id = $approvedSha }; finishedAt = '2026-01-01T01:00:00Z'
    }
    $responses[$renderDeployApi] = $historicalDeploy
    $responses[$renderLiveApi] = @(@{ deploy = $historicalDeploy; cursor = 'synthetic-cursor' })

    Assert-Gate -Name 'complete staging promotion accepts a non-enumerated Render live array' -Gate production -HeadSha $metadataSha -Responses $responses -ExpectedOutput 'Production release gate aprobado'
    Assert-Gate -Name 'complete legacy release promotion remains compatible' -Gate production -HeadSha $metadataSha -HeadRef 'release/HU-MAIN-01-promocion-produccion' -Responses $responses -ExpectedOutput 'Production release gate aprobado'

    $responses[$renderLiveApi] = @()
    Assert-Gate -Name 'empty Render live array blocks production' -Gate production -HeadSha $metadataSha -Responses $responses -ShouldPass $false -ExpectedOutput 'exactly one current live deployment'
    $responses[$renderLiveApi] = @(
        @{ deploy = $historicalDeploy; cursor = 'synthetic-cursor' },
        @{ deploy = @{ id = $script:currentDeployId; status = 'live'; commit = @{ id = $approvedSha } }; cursor = 'synthetic-other-cursor' }
    )
    Assert-Gate -Name 'multiple current Render deployments block production' -Gate production -HeadSha $metadataSha -Responses $responses -ShouldPass $false -ExpectedOutput 'exactly one current live deployment'
    $responses[$renderLiveApi] = @(@{ deploy = @{
        id = $script:deployId; status = 'live'; commit = @{ id = $approvedSha }
        image = @{ ref = 'ghcr.io/example/release-fixture:synthetic' }
    }; cursor = 'synthetic-cursor' })
    Assert-Gate -Name 'image-backed current Render deployment blocks production' -Gate production -HeadSha $metadataSha -Responses $responses -ShouldPass $false -ExpectedOutput 'invalid Git-backed deployment'
    $responses[$renderLiveApi] = @(@{ deploy = $historicalDeploy; cursor = 'synthetic-cursor' })

    $responses["$api/commits/$approvedSha/check-runs"].check_runs[0].conclusion = 'failure'
    Assert-Gate -Name 'failed candidate security check still blocks production' -Gate production -HeadSha $metadataSha -Responses $responses -ShouldPass $false -ExpectedOutput "el check 'security-gate'"
    $responses["$api/commits/$approvedSha/check-runs"].check_runs[0].conclusion = 'success'

    $responses["$api/actions/runs/12"].head_sha = $baselineSha
    Assert-Gate -Name 'QA evidence must identify the actual workflow commit' -Gate production -HeadSha $metadataSha -Responses $responses -ShouldPass $false -ExpectedOutput 'qa.testedCommit'
    $qaCodeMismatch = Copy-Fixture $productionEvidence
    $qaCodeMismatch.qa.testedCommit = $baselineSha
    $qaCodeMismatchSha = Commit-EvidenceVariant $qaCodeMismatch 'Synthetic QA of different application code'
    Assert-Gate -Name 'QA source mismatch still blocks production' -Gate production -HeadSha $qaCodeMismatchSha -Responses $responses -ShouldPass $false -ExpectedOutput 'probado por QA no coincide'
    $responses["$api/actions/runs/12"].head_sha = $approvedSha

    # QA may run before measured security evidence is committed; application code
    # must match and only the three exact evidence paths may differ.
    $responses["$api/actions/runs/12"].head_sha = $measuredSha
    $qaBeforeEvidence = Copy-Fixture $productionEvidence
    $qaBeforeEvidence.qa.testedCommit = $measuredSha
    $qaBeforeEvidenceSha = Commit-EvidenceVariant $qaBeforeEvidence 'Synthetic QA before security metadata'
    Assert-Gate -Name 'QA before security metadata remains compatible with identical application code' -Gate production -HeadSha $qaBeforeEvidenceSha -Responses $responses -ExpectedOutput 'Production release gate aprobado'
    $responses["$api/actions/runs/12"].head_sha = $approvedSha

    $responses[$renderServiceApi].repo = 'https://github.com/example/wrong-repository'
    Assert-Gate -Name 'Render repository mismatch blocks production' -Gate production -HeadSha $metadataSha -Responses $responses -ShouldPass $false -ExpectedOutput 'Render'
    $responses[$renderServiceApi].repo = 'https://github.com/example/release-fixture'
    $responses[$renderServiceApi].branch = 'develop'
    Assert-Gate -Name 'Render branch mismatch blocks production' -Gate production -HeadSha $metadataSha -Responses $responses -ShouldPass $false -ExpectedOutput 'Render'
    $responses[$renderServiceApi].branch = 'staging'
    $historicalDeploy.commit.id = $baselineSha
    Assert-Gate -Name 'Render deployed commit mismatch blocks production' -Gate production -HeadSha $metadataSha -Responses $responses -ShouldPass $false -ExpectedOutput 'Render'
    $historicalDeploy.commit.id = $approvedSha
    $historicalDeploy.status = 'build_failed'
    Assert-Gate -Name 'Render failed deployment blocks production' -Gate production -HeadSha $metadataSha -Responses $responses -ShouldPass $false -ExpectedOutput 'Render'
    $historicalDeploy.status = 'live'

    $newLiveDeploy = @{
        id = $script:currentDeployId; status = 'live'; commit = @{ id = $metadataSha }; finishedAt = '2026-01-01T02:15:00Z'
    }
    $responses["$renderServiceApi/deploys/$script:currentDeployId"] = $newLiveDeploy
    $responses[$renderLiveApi] = @(@{ deploy = $newLiveDeploy; cursor = 'synthetic-cursor' })
    $historicalDeploy.status = 'deactivated'
    Assert-Gate -Name 'historical smoke remains valid with a newer metadata-only live deployment' -Gate production -HeadSha $metadataSha -Responses $responses -ExpectedOutput 'Production release gate aprobado'
    $responses[$renderLiveApi] = @(@{ deploy = $historicalDeploy; cursor = 'synthetic-cursor' })
    $historicalDeploy.status = 'live'

    $staleReport = Copy-Fixture $script:stagingReport
    $staleReport.deployId = 'dep-' + ('d' * 20)
    Assert-Gate -Name 'smoke artifact from another deployment blocks production' -Gate production -HeadSha $metadataSha -Responses $responses -Report $staleReport -ShouldPass $false -ExpectedOutput 'smoke|Render|despliegue|reporte|informe'
    $staleReport = Copy-Fixture $script:stagingReport
    $staleReport.testedCommit = $baselineSha
    Assert-Gate -Name 'smoke artifact from another commit blocks production' -Gate production -HeadSha $metadataSha -Responses $responses -Report $staleReport -ShouldPass $false -ExpectedOutput 'smoke|Render|commit|reporte|informe'

    Assert-Gate -Name 'evidence cannot override the trusted Render service' -Gate production -HeadSha $metadataSha -Responses $responses -RenderServiceId ('srv-' + ('e' * 20)) -ShouldPass $false -ExpectedOutput 'Render|RENDER_STAGING_SERVICE_ID|servicio'
    $savedServiceResponse = $responses[$renderServiceApi]
    $responses[$renderServiceApi] = @{ fixtureError = 'Synthetic Render transport failure' }
    Assert-Gate -Name 'Render API failure blocks production' -Gate production -HeadSha $metadataSha -Responses $responses -ShouldPass $false -ExpectedOutput 'Render'
    $responses[$renderServiceApi] = $savedServiceResponse
    Assert-Gate -Name 'missing Render API key blocks production' -Gate production -HeadSha $metadataSha -Responses $responses -RenderApiKey '' -ShouldPass $false -ExpectedOutput 'RENDER_API_KEY'

    $earlyApproval = Copy-Fixture $productionEvidence
    $earlyApproval.approval.approvedAtUtc = '2026-01-01T01:15:00Z'
    $earlyApprovalSha = Commit-EvidenceVariant $earlyApproval 'Synthetic approval before smoke'
    Assert-Gate -Name 'approval before verified smoke blocks production' -Gate production -HeadSha $earlyApprovalSha -Responses $responses -ShouldPass $false -ExpectedOutput 'approval|aprobaci|smoke'
    $futureApproval = Copy-Fixture $productionEvidence
    $futureApproval.approval.approvedAtUtc = '2099-01-01T02:00:00Z'
    $futureApprovalSha = Commit-EvidenceVariant $futureApproval 'Synthetic future approval'
    Assert-Gate -Name 'future approval blocks production' -Gate production -HeadSha $futureApprovalSha -Responses $responses -ShouldPass $false -ExpectedOutput 'approval|aprobaci|futur'

    Invoke-FixtureGit checkout --quiet --detach $metadataSha | Out-Null
    [IO.File]::WriteAllText((Join-Path $fixtureRepo 'application.txt'), 'unmeasured code change')
    $changedCodeSha = Commit-Fixture 'Synthetic unmeasured code change'
    $newLiveDeploy.commit.id = $changedCodeSha
    $responses[$renderLiveApi] = @(@{ deploy = $newLiveDeploy; cursor = 'synthetic-cursor' })
    Assert-Gate -Name 'live Render code drift blocks production' -Gate production -HeadSha $metadataSha -Responses $responses -ShouldPass $false -ExpectedOutput 'Render|cambios|commit|c.digo'
    $newLiveDeploy.commit.id = $metadataSha
    $responses[$renderLiveApi] = @(@{ deploy = $historicalDeploy; cursor = 'synthetic-cursor' })
    Assert-Gate -Name 'security rejects code changed after measurement' -Gate security -HeadSha $changedCodeSha -ShouldPass $false -ExpectedOutput 'solo se permite'
    Assert-Gate -Name 'production rejects code changed after approval' -Gate production -HeadSha $changedCodeSha -Responses $responses -ShouldPass $false -ExpectedOutput 'contiene cambios adicionales'

    Invoke-FixtureGit checkout --quiet --detach $metadataSha | Out-Null
    $securityEvidence.candidate.routes[0].serverP95Ms = 110
    Write-FixtureJson $securityEvidence (Join-Path $fixtureRepo $securityPath)
    $regressionSha = Commit-Fixture 'Synthetic latency regression'
    Assert-Gate -Name 'latency regression still blocks security gate' -Gate security -HeadSha $regressionSha -ShouldPass $false -ExpectedOutput 'p95 candidato'
} finally {
    if (Test-Path -LiteralPath $testRoot) {
        # Delete only the exact generated fixture under the system temp parent.
        $resolvedRoot = (Resolve-Path -LiteralPath $testRoot).Path
        if ([IO.Path]::GetFullPath($resolvedRoot) -ne [IO.Path]::GetFullPath($testRoot) -or
            (Split-Path -Parent $resolvedRoot) -ne $tempParent -or
            (Split-Path -Leaf $resolvedRoot) -notmatch '^SaborExpress-promotion-tests-[0-9a-f]{32}$') {
            throw "Refusing cleanup outside the generated fixture: $resolvedRoot"
        }
        Remove-Item -LiteralPath $resolvedRoot -Recurse -Force
        if (Test-Path -LiteralPath $testRoot) { throw 'Fixture cleanup failed.' }
    }
}

if ($script:failures.Count -gt 0) {
    throw "$($script:failures.Count) gate regression test(s) failed; $script:passed passed."
}
Write-Host "$script:passed gate regression tests passed; temporary fixtures removed."
# Expected rejection cases leave LASTEXITCODE = 1. Report the suite result
# explicitly so GitHub Actions' PowerShell wrapper does not reuse that value.
exit 0
