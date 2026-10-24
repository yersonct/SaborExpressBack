[CmdletBinding()]
param([string]$RunCasePath)

$ErrorActionPreference = 'Stop'

# Only this temporary fixture's smoke stub runs. API replies, credentials,
# repositories and receipts below are synthetic and never contact a service.
if ($RunCasePath) {
    try {
        $case = Get-Content -LiteralPath $RunCasePath -Raw | ConvertFrom-Json
        $env:GITHUB_REPOSITORY = 'example/release-fixture'
        $env:RENDER_API_KEY = 'synthetic-render-token'
        $env:RENDER_STAGING_SERVICE_ID = 'srv-' + ('a' * 20)
        $env:RENDER_PRODUCTION_SERVICE_ID = 'srv-' + ('c' * 20)
        $env:RELEASE_SMOKE_API_URL = [string]$case.configuredUrl
        $env:FIXTURE_SMOKE_MARKER = [string]$case.markerPath
        $env:FIXTURE_SMOKE_MODE = [string]$case.smokeMode

        function Invoke-RestMethod {
            [CmdletBinding()]
            param($Method, [string]$Uri, $Headers, $TimeoutSec, [int]$MaximumRedirection)
            $property = $case.responses.PSObject.Properties[$Uri]
            if ($null -eq $property) { throw "Unexpected API request in smoke fixture: $Uri" }
            if ($case.driftAfterSmoke -and (Test-Path -LiteralPath $case.markerPath) -and
                $Uri -like '*deploys?status=live&limit=100') {
                $driftedDeployments = @([pscustomobject]@{ deploy = [pscustomobject]@{
                    id = 'dep-' + ('d' * 20); status = 'live'; commit = [pscustomobject]@{ id = $case.expectedCommit }
                }; cursor = 'synthetic-cursor' })
                Write-Output -NoEnumerate $driftedDeployments
                return
            }
            # Preserve the real cmdlet's single pipeline object for JSON arrays.
            if ($property.Value -is [Array]) {
                Write-Output -NoEnumerate $property.Value
                return
            }
            return $property.Value
        }

        $arguments = @{
            DeployId = $case.deployId; RepositoryPath = $case.repository; ReportPath = $case.reportPath
        }
        if ($case.kind -eq 'staging') { $arguments.TestedCommit = $case.expectedCommit }
        else { $arguments.ExpectedCommit = $case.expectedCommit }
        & $case.scriptPath @arguments
        exit 0
    } catch {
        [Console]::Out.WriteLine($_.Exception.Message)
        exit 1
    }
}

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$engine = (Get-Process -Id $PID).Path
$tempParent = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\', '/')
$testRoot = Join-Path $tempParent ('SaborExpress-render-smoke-tests-' + [guid]::NewGuid().ToString('N'))
$fixtureRepo = Join-Path $testRoot 'repository'
$fixtureScripts = Join-Path $testRoot 'scripts'
$script:passed = 0
$script:failures = @()
$script:caseNumber = 0
$script:mainSha = ''

function Invoke-FixtureGit {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)
    $output = & git -C $fixtureRepo @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) { throw ("Smoke fixture git failed: $Arguments" + [Environment]::NewLine + $output) }
    return $output
}

function Write-FixtureJson($Value, [string]$Path) {
    [void][IO.Directory]::CreateDirectory((Split-Path -Parent $Path))
    [IO.File]::WriteAllText($Path, ($Value | ConvertTo-Json -Depth 20), (New-Object Text.UTF8Encoding($false)))
}

function Commit-Fixture([string]$Message) {
    Invoke-FixtureGit add --all | Out-Null
    Invoke-FixtureGit commit --quiet -m $Message | Out-Null
    return (Invoke-FixtureGit rev-parse HEAD).Trim()
}

function Assert-SmokeCase {
    param(
        [string]$Name,
        [string]$Kind,
        [string]$HeadSha,
        [string]$ExpectedCommit = $HeadSha,
        [string]$MainReference = $script:mainSha,
        [bool]$ShouldPass = $true,
        [bool]$ExpectSmoke = $true,
        [ValidateSet('success', 'exit-code', 'exit', 'throw')][string]$SmokeMode = 'success',
        [bool]$DriftAfterSmoke = $false,
        [string]$ConfiguredUrl = '',
        [string]$ExpectedOutput = ''
    )
    Invoke-FixtureGit checkout --quiet --detach $HeadSha | Out-Null
    Invoke-FixtureGit update-ref refs/remotes/origin/main $MainReference | Out-Null
    $script:caseNumber++
    $reportPath = Join-Path $testRoot "receipt-$script:caseNumber.json"
    $markerPath = Join-Path $testRoot "smoke-$script:caseNumber.json"
    $serviceId = if ($Kind -eq 'staging') { 'srv-' + ('a' * 20) } else { 'srv-' + ('c' * 20) }
    $deployId = 'dep-' + ('b' * 20)
    $serviceUrl = if ($Kind -eq 'staging') { 'https://stage.example.invalid' } else { 'https://production.example.invalid' }
    if ($ConfiguredUrl -eq '') { $ConfiguredUrl = $serviceUrl }
    $renderApi = "https://api.render.com/v1/services/$serviceId"
    $deploy = @{ id = $deployId; status = 'live'; commit = @{ id = $ExpectedCommit }; finishedAt = '2026-01-01T01:00:00Z' }
    $responses = @{}
    $responses[$renderApi] = @{
        id = $serviceId; type = 'web_service'; repo = 'https://github.com/example/release-fixture'
        branch = $(if ($Kind -eq 'staging') { 'staging' } else { 'main' }); serviceDetails = @{ url = $serviceUrl }
    }
    $responses["$renderApi/deploys/$deployId"] = $deploy
    $responses["$renderApi/deploys?status=live&limit=100"] = @(@{ deploy = $deploy; cursor = 'synthetic-cursor' })
    $casePath = Join-Path $testRoot "case-$script:caseNumber.json"
    Write-FixtureJson @{
        kind = $Kind; repository = $fixtureRepo; expectedCommit = $ExpectedCommit; deployId = $deployId
        scriptPath = (Join-Path $fixtureScripts $(if ($Kind -eq 'staging') { 'smoke-render-staging.ps1' } else { 'smoke-production.ps1' }))
        reportPath = $reportPath; markerPath = $markerPath; configuredUrl = $ConfiguredUrl
        responses = $responses; smokeMode = $SmokeMode; driftAfterSmoke = $DriftAfterSmoke
    } $casePath
    $output = (& $engine -NoProfile -NonInteractive -ExecutionPolicy Bypass -File $PSCommandPath -RunCasePath $casePath 2>&1 | Out-String)
    $exitCode = $LASTEXITCODE
    $correctExit = if ($ShouldPass) { $exitCode -eq 0 } else { $exitCode -ne 0 }
    $receiptExists = Test-Path -LiteralPath $reportPath
    $correctReceipt = $receiptExists -eq $ShouldPass
    if ($ShouldPass -and $receiptExists) {
        $receipt = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
        $verifiedAt = [DateTimeOffset]::MinValue
        $correctReceipt = $receipt.schemaVersion -eq 1 -and $receipt.provider -ceq 'render-git' -and
            $receipt.serviceId -ceq $serviceId -and $receipt.deployId -ceq $deployId -and
            $receipt.testedCommit -ceq $ExpectedCommit -and $receipt.apiUrl -ceq $serviceUrl -and
            [DateTimeOffset]::TryParse([string]$receipt.verifiedAtUtc, [ref]$verifiedAt) -and
            $verifiedAt.Offset -eq [TimeSpan]::Zero
    }
    $smokeInvoked = Test-Path -LiteralPath $markerPath
    $correctSmoke = $smokeInvoked -eq $ExpectSmoke
    if ($smokeInvoked) {
        $smoke = Get-Content -LiteralPath $markerPath -Raw | ConvertFrom-Json
        $correctSmoke = $correctSmoke -and $smoke.environment -ceq $(if ($Kind -eq 'staging') { 'Staging' } else { 'Production' }) -and
            -not $smoke.startWompiSandbox -and $smoke.verifyWompiPaymentId -eq 0
    }
    if ($correctExit -and $correctReceipt -and $correctSmoke -and ($ExpectedOutput -eq '' -or $output -match $ExpectedOutput)) {
        $script:passed++
        Write-Host "PASS $Name"
    } else {
        $script:failures += $Name
        Write-Host ("FAIL $Name (exit $exitCode; receipt $receiptExists; smoke invoked $smokeInvoked)" + [Environment]::NewLine + $output)
    }
}

try {
    [void][IO.Directory]::CreateDirectory($fixtureRepo)
    [void][IO.Directory]::CreateDirectory($fixtureScripts)
    foreach ($name in @('render-deployment.ps1', 'smoke-render-staging.ps1', 'smoke-production.ps1')) {
        Copy-Item -LiteralPath (Join-Path $repositoryRoot "scripts/release/$name") -Destination (Join-Path $fixtureScripts $name)
    }
    $stub = @'
[CmdletBinding()]
param([string]$Environment, [switch]$StartWompiSandbox, [int]$VerifyWompiPaymentId = 0)
[IO.File]::WriteAllText($env:FIXTURE_SMOKE_MARKER, (@{
    environment = $Environment; startWompiSandbox = [bool]$StartWompiSandbox
    verifyWompiPaymentId = $VerifyWompiPaymentId
} | ConvertTo-Json), (New-Object Text.UTF8Encoding($false)))
switch ($env:FIXTURE_SMOKE_MODE) {
    'exit-code' { $global:LASTEXITCODE = 7; return }
    'exit' { exit 1 }
    'throw' { throw 'Synthetic API smoke failure.' }
}
'@
    [IO.File]::WriteAllText((Join-Path $fixtureScripts 'smoke-staging.ps1'), $stub, (New-Object Text.UTF8Encoding($false)))
    Invoke-FixtureGit init --quiet --initial-branch=main | Out-Null
    Invoke-FixtureGit config user.name 'Synthetic Render smoke tests' | Out-Null
    Invoke-FixtureGit config user.email 'render-smoke-tests@example.invalid' | Out-Null
    Invoke-FixtureGit config commit.gpgsign false | Out-Null
    Invoke-FixtureGit config core.autocrlf false | Out-Null
    Invoke-FixtureGit config core.safecrlf false | Out-Null
    [IO.File]::WriteAllText((Join-Path $fixtureRepo 'application.txt'), 'baseline')
    $baselineSha = Commit-Fixture 'Synthetic main baseline'
    Invoke-FixtureGit checkout --quiet -b staging | Out-Null
    [IO.File]::WriteAllText((Join-Path $fixtureRepo 'application.txt'), 'approved application code')
    $approvedSha = Commit-Fixture 'Synthetic approved staging code'
    Write-FixtureJson @{ schemaVersion = 2; deploymentProvider = 'render-git'; sourceCommit = $approvedSha } (Join-Path $fixtureRepo 'docs/release/production-release-evidence.json')
    $metadataSha = Commit-Fixture 'Synthetic release metadata'
    Invoke-FixtureGit checkout --quiet main | Out-Null
    Invoke-FixtureGit merge --quiet --no-ff $metadataSha -m 'Synthetic production merge' | Out-Null
    $script:mainSha = (Invoke-FixtureGit rev-parse HEAD).Trim()

    Assert-SmokeCase -Name 'staging smoke accepts a non-enumerated Render live array and writes its receipt' -Kind staging -HeadSha $approvedSha
    Assert-SmokeCase -Name 'staging smoke accepts metadata-only checkout changes' -Kind staging -HeadSha $metadataSha -ExpectedCommit $approvedSha
    Assert-SmokeCase -Name 'staging nonzero smoke result produces no receipt' -Kind staging -HeadSha $approvedSha -SmokeMode exit-code -ShouldPass $false -ExpectedOutput 'smoke checks failed'
    Assert-SmokeCase -Name 'staging smoke exit 1 produces no receipt' -Kind staging -HeadSha $approvedSha -SmokeMode exit -ShouldPass $false -ExpectedOutput 'smoke checks failed'
    Assert-SmokeCase -Name 'staging smoke exception produces no receipt' -Kind staging -HeadSha $approvedSha -SmokeMode throw -ShouldPass $false -ExpectedOutput 'Synthetic API smoke failure'
    Assert-SmokeCase -Name 'staging deployment replacement during smoke blocks the receipt' -Kind staging -HeadSha $approvedSha -DriftAfterSmoke $true -ShouldPass $false -ExpectedOutput 'current live deployments'
    Assert-SmokeCase -Name 'staging URL mismatch blocks smoke before credentials are used' -Kind staging -HeadSha $approvedSha -ConfiguredUrl 'https://wrong.example.invalid' -ExpectSmoke $false -ShouldPass $false -ExpectedOutput 'URL|url'
    Assert-SmokeCase -Name 'production merge SHA passes with approved application code' -Kind production -HeadSha $mainSha
    Assert-SmokeCase -Name 'production checkout must match the expected deployed SHA' -Kind production -HeadSha $mainSha -ExpectedCommit $metadataSha -ExpectSmoke $false -ShouldPass $false -ExpectedOutput 'checkout must match ExpectedCommit'
    Assert-SmokeCase -Name 'production must deploy the current main ref' -Kind production -HeadSha $mainSha -MainReference $metadataSha -ExpectSmoke $false -ShouldPass $false -ExpectedOutput 'current origin/main'
    Assert-SmokeCase -Name 'production URL mismatch blocks smoke before credentials are used' -Kind production -HeadSha $mainSha -ConfiguredUrl 'https://wrong.example.invalid' -ExpectSmoke $false -ShouldPass $false -ExpectedOutput 'URL|url'
    Assert-SmokeCase -Name 'production deployment replacement during smoke blocks the receipt' -Kind production -HeadSha $mainSha -DriftAfterSmoke $true -ShouldPass $false -ExpectedOutput 'current live deployments'
    Assert-SmokeCase -Name 'production smoke exit 1 produces no receipt' -Kind production -HeadSha $mainSha -SmokeMode exit -ShouldPass $false -ExpectedOutput 'smoke checks failed'

    Invoke-FixtureGit checkout --quiet --detach $mainSha | Out-Null
    [IO.File]::WriteAllText((Join-Path $fixtureRepo 'application.txt'), 'unapproved production code')
    $changedSha = Commit-Fixture 'Synthetic unapproved application code'
    Assert-SmokeCase -Name 'production rejects application code changed after staging approval' -Kind production -HeadSha $changedSha -MainReference $changedSha -ExpectSmoke $false -ShouldPass $false -ExpectedOutput 'changes|metadata|approved'
} finally {
    if (Test-Path -LiteralPath $testRoot) {
        $resolvedRoot = (Resolve-Path -LiteralPath $testRoot).Path
        if ([IO.Path]::GetFullPath($resolvedRoot) -ne [IO.Path]::GetFullPath($testRoot) -or
            (Split-Path -Parent $resolvedRoot) -ne $tempParent -or
            (Split-Path -Leaf $resolvedRoot) -notmatch '^SaborExpress-render-smoke-tests-[0-9a-f]{32}$') {
            throw "Refusing cleanup outside the generated smoke fixture: $resolvedRoot"
        }
        Remove-Item -LiteralPath $resolvedRoot -Recurse -Force
        if (Test-Path -LiteralPath $testRoot) { throw 'Smoke fixture cleanup failed.' }
    }
}

if ($script:failures.Count -gt 0) {
    throw "$($script:failures.Count) Render smoke regression test(s) failed; $script:passed passed."
}
Write-Host "$script:passed Render smoke regression tests passed; temporary fixtures removed."
# Native commands in expected negative cases must not determine the suite exit.
exit 0
