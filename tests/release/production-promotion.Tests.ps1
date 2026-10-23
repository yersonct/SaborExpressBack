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

        function Invoke-RestMethod {
            param($Method, [string]$Uri, $Headers, $TimeoutSec)
            $property = $case.responses.PSObject.Properties[$Uri]
            if ($null -eq $property) { throw "Unexpected API request in fixture: $Uri" }
            return $property.Value
        }

        function Invoke-WebRequest {
            param([string]$Uri, $Headers, [string]$OutFile, $TimeoutSec)
            if ($Uri -ne 'https://release-gate.invalid/repos/example/release-fixture/actions/artifacts/41/zip') {
                throw "Unexpected artifact request in fixture: $Uri"
            }
            Copy-Item -LiteralPath $case.manifestZip -Destination $OutFile
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
        $Responses = @{}
    )
    Invoke-FixtureGit checkout --quiet --detach $HeadSha | Out-Null
    $script:caseNumber++
    $casePath = Join-Path $testRoot ("case-$script:caseNumber.json")
    Write-FixtureJson @{
        gate = $Gate
        scriptPath = $(if ($Gate -eq 'production') { $productionScript } else { $securityScript })
        evidencePath = $(if ($Gate -eq 'production') { $productionPath } else { $securityPath })
        headSha = $HeadSha
        headRef = $HeadRef
        repository = $fixtureRepo
        responses = $Responses
        manifestZip = (Join-Path $testRoot 'manifest.zip')
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

    $digest = 'sha256:' + ('a' * 64)
    $image = "ghcr.io/example/release-fixture@$digest"
    $productionEvidence = @{
        schemaVersion = 1; version = '1.2.3'; sourceCommit = $approvedSha
        releaseNotes = @('Synthetic fixture release')
        migrationPlan = 'Synthetic fixture: this release has no database migrations.'
        rollbackPlan = 'Synthetic fixture: restore the previous approved image digest.'
        artifact = @{ image = $image; digest = $digest; buildRunId = 11 }
        qa = @{ runId = 12; testedCommit = $approvedSha }
        staging = @{ runId = 13; testedCommit = $approvedSha; imageDigest = $digest }
        approval = @{ approvedBy = 'synthetic-fixture'; approvedAtUtc = '2026-01-01T02:00:00Z' }
    }
    Write-FixtureJson @{ schemaVersion = 1; state = 'approved'; sourceCommit = $approvedSha } (Join-Path $fixtureRepo $candidatePath)
    Write-FixtureJson $productionEvidence (Join-Path $fixtureRepo $productionPath)
    $metadataSha = Commit-Fixture 'Synthetic production metadata'
    Invoke-FixtureGit update-ref refs/remotes/origin/staging $metadataSha | Out-Null
    Assert-Gate -Name 'security accepts production metadata after measurement' -Gate security -HeadSha $metadataSha -ExpectedOutput 'Security release gate: evidencia'

    # Two explicit moves make the case-only rename observable on Windows, too.
    Invoke-FixtureGit mv -- $candidatePath 'docs/release/candidate-temporary.json' | Out-Null
    Invoke-FixtureGit mv -- 'docs/release/candidate-temporary.json' 'docs/release/Production-release-candidate.json' | Out-Null
    $caseVariantSha = Commit-Fixture 'Synthetic metadata with unauthorized filename case'
    Assert-Gate -Name 'security metadata allowlist requires the exact filename case' -Gate security -HeadSha $caseVariantSha -ShouldPass $false -ExpectedOutput 'solo se permite'
    Assert-Gate -Name 'production metadata allowlist requires the exact filename case' -Gate production -HeadSha $caseVariantSha -ShouldPass $false -ExpectedOutput 'contiene cambios adicionales'

    $manifestDirectory = Join-Path $testRoot 'artifact'
    Write-FixtureJson @{ commit = $approvedSha; image = $image; digest = $digest } (Join-Path $manifestDirectory 'manifest.json')
    Compress-Archive -LiteralPath (Join-Path $manifestDirectory 'manifest.json') -DestinationPath (Join-Path $testRoot 'manifest.zip')
    $api = 'https://release-gate.invalid/repos/example/release-fixture'
    $responses = @{}
    $responses["$api/commits/$approvedSha/check-runs"] = @{
        check_runs = @(
            @{ name = 'security-gate'; status = 'completed'; conclusion = 'success'; started_at = '2026-01-01T02:00:00Z' },
            @{ name = 'GitGuardian Security Checks'; status = 'completed'; conclusion = 'success'; started_at = '2026-01-01T02:00:00Z' }
        )
    }
    $responses["$api/actions/runs/11"] = @{ status = 'completed'; conclusion = 'success'; name = 'Build production candidate artifact' }
    $responses["$api/actions/runs/11/artifacts"] = @{ artifacts = @(@{ id = 41; name = "release-manifest-$approvedSha"; expired = $false }) }
    $responses["$api/actions/runs/12"] = @{ status = 'completed'; conclusion = 'success'; name = 'QA operational regression'; head_sha = $approvedSha }
    $responses["$api/actions/runs/13"] = @{ status = 'completed'; conclusion = 'success'; name = 'Staging release smoke' }

    Assert-Gate -Name 'complete staging promotion passes with synthetic API and manifest' -Gate production -HeadSha $metadataSha -Responses $responses -ExpectedOutput 'Production release gate aprobado'
    Assert-Gate -Name 'complete legacy release promotion remains compatible' -Gate production -HeadSha $metadataSha -HeadRef 'release/HU-MAIN-01-promocion-produccion' -Responses $responses -ExpectedOutput 'Production release gate aprobado'

    $responses["$api/commits/$approvedSha/check-runs"].check_runs[0].conclusion = 'failure'
    Assert-Gate -Name 'failed candidate security check still blocks production' -Gate production -HeadSha $metadataSha -Responses $responses -ShouldPass $false -ExpectedOutput "el check 'security-gate'"
    $responses["$api/commits/$approvedSha/check-runs"].check_runs[0].conclusion = 'success'

    $responses["$api/actions/runs/12"].head_sha = $baselineSha
    Assert-Gate -Name 'QA source mismatch still blocks production' -Gate production -HeadSha $metadataSha -Responses $responses -ShouldPass $false -ExpectedOutput 'probado por QA no coincide'
    $responses["$api/actions/runs/12"].head_sha = $approvedSha

    [IO.File]::WriteAllText((Join-Path $fixtureRepo 'application.txt'), 'unmeasured code change')
    $changedCodeSha = Commit-Fixture 'Synthetic unmeasured code change'
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
