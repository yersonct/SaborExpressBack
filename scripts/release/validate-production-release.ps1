[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$EvidencePath,

    [Parameter(Mandatory = $true)]
    [string]$ExpectedHeadSha,

    [Parameter(Mandatory = $true)]
    [string]$ExpectedHeadRef,

    [string]$RepositoryPath = (Get-Location).Path
)

$ErrorActionPreference = 'Stop'

function Stop-Gate([string]$Message) {
    throw "Production release gate: $Message"
}

function Get-Required($Object, [string]$Name, [string]$Context) {
    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property -or $null -eq $property.Value -or [string]::IsNullOrWhiteSpace([string]$property.Value)) {
        Stop-Gate "falta '$Name' en $Context."
    }
    return $property.Value
}

function Assert-Sha([string]$Value, [string]$Context) {
    if ($Value -notmatch '^[0-9a-fA-F]{40}$') { Stop-Gate "$Context debe ser un SHA de 40 caracteres." }
}

function Get-CommitTree([string]$Sha, [string]$Context) {
    Assert-Sha $Sha $Context
    & git -C $RepositoryPath cat-file -e "${Sha}^{commit}" 2>$null
    if ($LASTEXITCODE -ne 0) { Stop-Gate "no se encuentra el commit de $Context en el historial descargado." }
    $tree = (& git -C $RepositoryPath rev-parse "${Sha}^{tree}").Trim()
    if ($LASTEXITCODE -ne 0) { Stop-Gate "no se pudo resolver el árbol de $Context." }
    return $tree
}

function Assert-Ancestor([string]$Sha, [string]$Ref, [string]$Context) {
    & git -C $RepositoryPath merge-base --is-ancestor $Sha $Ref 2>$null
    if ($LASTEXITCODE -ne 0) { Stop-Gate "$Context no pertenece a $Ref." }
}

function Assert-SameCode([string]$From, [string]$To, [string[]]$AllowedPaths, [string]$Message) {
    [void](Get-CommitTree $From 'commit comparado')
    [void](Get-CommitTree $To 'commit comparado')
    $changes = @(& git -C $RepositoryPath diff --name-only --no-renames $From $To)
    if ($LASTEXITCODE -ne 0) { Stop-Gate 'no se pudo comparar el contenido de los commits.' }
    if (@($changes | Where-Object { $_ -cnotin $AllowedPaths }).Count -gt 0) { Stop-Gate $Message }
}

function Get-UtcDate($Value, [string]$Context) {
    $parsed = [DateTimeOffset]::MinValue

    if ($Value -is [DateTime]) {
        if ($Value.Kind -ne [DateTimeKind]::Utc) {
            Stop-Gate "$Context debe ser una fecha ISO-8601 UTC real y no futura."
        }
        $parsed = [DateTimeOffset]::new($Value)
    }
    elseif ($Value -is [DateTimeOffset]) {
        $parsed = $Value
    }
    else {
        $raw = [string]$Value
        if ($raw -notmatch '(?:Z|\+00:00)$' -or
            -not [DateTimeOffset]::TryParse(
                $raw,
                [Globalization.CultureInfo]::InvariantCulture,
                [Globalization.DateTimeStyles]::None,
                [ref]$parsed
            )) {
            Stop-Gate "$Context debe ser una fecha ISO-8601 UTC real y no futura."
        }
    }

    if ($parsed.Offset -ne [TimeSpan]::Zero -or
        $parsed -gt [DateTimeOffset]::UtcNow) {
        Stop-Gate "$Context debe ser una fecha ISO-8601 UTC real y no futura."
    }

    return $parsed
}

function Invoke-GitHubGet([string]$Path) {
    $token = $env:GITHUB_TOKEN
    if ([string]::IsNullOrWhiteSpace($token)) { Stop-Gate 'GITHUB_TOKEN no está disponible para validar la evidencia.' }
    $apiBase = if ([string]::IsNullOrWhiteSpace($env:GITHUB_API_URL)) { 'https://api.github.com' } else { $env:GITHUB_API_URL.TrimEnd('/') }
    $headers = @{
        Authorization = "Bearer $token"
        Accept = 'application/vnd.github+json'
        'X-GitHub-Api-Version' = '2022-11-28'
    }
    return Invoke-RestMethod -Method Get -Uri "$apiBase/$Path" -Headers $headers -TimeoutSec 30
}

function Get-SuccessfulRun([long]$RunId, [string]$ExpectedName, [string]$ExpectedPath, [string]$Context) {
    if ($RunId -le 0) { Stop-Gate "runId de $Context no es válido." }
    $run = Invoke-GitHubGet "repos/$env:GITHUB_REPOSITORY/actions/runs/$RunId"
    if ($run.status -ne 'completed' -or $run.conclusion -ne 'success' -or $run.name -ne $ExpectedName -or
        $run.path -cne $ExpectedPath) {
        Stop-Gate "la ejecución de $Context debe llamarse '$ExpectedName' y terminar exitosamente."
    }
    return $run
}

function Get-StagingVerification([long]$RunId) {
    $artifacts = Invoke-GitHubGet "repos/$env:GITHUB_REPOSITORY/actions/runs/$RunId/artifacts"
    $reports = @($artifacts.artifacts | Where-Object { $_.name -ceq 'staging-render-verification' -and -not $_.expired })
    if ($reports.Count -ne 1 -or [long]$reports[0].id -le 0) {
        Stop-Gate 'falta el reporte staging-render-verification vigente del smoke de staging.'
    }
    $apiBase = if ([string]::IsNullOrWhiteSpace($env:GITHUB_API_URL)) { 'https://api.github.com' } else { $env:GITHUB_API_URL.TrimEnd('/') }
    $zipPath = Join-Path ([IO.Path]::GetTempPath()) ('render-smoke-' + [guid]::NewGuid().ToString('N') + '.zip')
    $archive = $null
    $reader = $null
    try {
        Invoke-WebRequest -Uri "$apiBase/repos/$env:GITHUB_REPOSITORY/actions/artifacts/$($reports[0].id)/zip" `
            -Headers @{ Authorization = "Bearer $env:GITHUB_TOKEN"; Accept = 'application/vnd.github+json' } `
            -OutFile $zipPath -TimeoutSec 45 | Out-Null
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $archive = [IO.Compression.ZipFile]::OpenRead($zipPath)
        $entries = @($archive.Entries | Where-Object { $_.FullName -ceq 'render-staging-verification.json' })
        if ($entries.Count -ne 1 -or $entries[0].Length -gt 65536) { Stop-Gate 'el artifact de smoke no contiene un reporte válido.' }
        $reader = New-Object IO.StreamReader($entries[0].Open())
        return ($reader.ReadToEnd() | ConvertFrom-Json)
    } finally {
        if ($null -ne $reader) { $reader.Dispose() }
        if ($null -ne $archive) { $archive.Dispose() }
        Remove-Item -LiteralPath $zipPath -Force -ErrorAction SilentlyContinue
    }
}

function Assert-RequiredCommitChecks([string]$Sha) {
    $checks = Invoke-GitHubGet "repos/$env:GITHUB_REPOSITORY/commits/$Sha/check-runs"
    foreach ($name in @('security-gate', 'GitGuardian Security Checks')) {
        $latest = @($checks.check_runs | Where-Object { $_.name -eq $name } | Sort-Object started_at | Select-Object -Last 1)
        if ($latest.Count -eq 0 -or $latest[0].status -ne 'completed' -or $latest[0].conclusion -ne 'success') {
            Stop-Gate "el check '$name' del commit candidato no está exitoso."
        }
    }
}

$head = (& git -C $RepositoryPath rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $head -ne $ExpectedHeadSha) { Stop-Gate 'el checkout no corresponde al HEAD del pull request.' }
if ($ExpectedHeadRef -cnotin @('staging', 'release/HU-MAIN-01-promocion-produccion')) {
    Stop-Gate 'la PR a main debe provenir de staging o de release/HU-MAIN-01-promocion-produccion, con evidencia del candidato aprobado de staging.'
}

$resolvedEvidence = Join-Path $RepositoryPath $EvidencePath
if (-not (Test-Path -LiteralPath $resolvedEvidence -PathType Leaf)) {
    Stop-Gate "no existe $EvidencePath. Complete la evidencia real de QA y del despliegue Render de staging antes de promover."
}

try { $evidence = Get-Content -LiteralPath $resolvedEvidence -Raw | ConvertFrom-Json }
catch { Stop-Gate 'la evidencia de producción no contiene JSON válido.' }

if ([int](Get-Required $evidence 'schemaVersion' 'raíz') -ne 2) { Stop-Gate 'schemaVersion debe ser 2 para el despliegue Render desde Git.' }
if ([string](Get-Required $evidence 'deploymentProvider' 'raíz') -cne 'render-git') { Stop-Gate 'deploymentProvider debe ser render-git.' }
$version = [string](Get-Required $evidence 'version' 'raíz')
if ($version -notmatch '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(-[0-9A-Za-z.-]+)?(\+[0-9A-Za-z.-]+)?$') {
    Stop-Gate 'version debe usar SemVer.'
}

$sourceSha = [string](Get-Required $evidence 'sourceCommit' 'raíz')
[void](Get-CommitTree $sourceSha 'sourceCommit')
Assert-Ancestor $sourceSha 'origin/staging' 'sourceCommit'
Assert-Ancestor $sourceSha $head 'sourceCommit del PR'
& git -C $RepositoryPath cat-file -e "${sourceSha}:docs/release/security-gate-evidence.json" 2>$null
if ($LASTEXITCODE -ne 0) { Stop-Gate 'sourceCommit debe incluir la evidencia de seguridad guardada en Git.' }
$productionMetadata = @('docs/release/production-release-candidate.json', 'docs/release/production-release-evidence.json')
Assert-SameCode $sourceSha $head $productionMetadata 'la rama final contiene cambios adicionales al código ya aprobado en staging; desarrolle y promueva esos cambios por develop, qa y staging primero.'
Assert-RequiredCommitChecks $sourceSha

$releaseNotes = @(Get-Required $evidence 'releaseNotes' 'raíz')
if ($releaseNotes.Count -eq 0) { Stop-Gate 'releaseNotes debe incluir al menos un cambio.' }
foreach ($note in $releaseNotes) {
    if ([string]::IsNullOrWhiteSpace([string]$note) -or [string]$note -match '^(<|TODO|TBD|PENDIENTE)') {
        Stop-Gate 'releaseNotes contiene marcadores sin completar.'
    }
}
foreach ($name in @('migrationPlan', 'rollbackPlan')) {
    $plan = [string](Get-Required $evidence $name 'raíz')
    if ($plan.Length -lt 20 -or $plan -match 'TODO|TBD|PENDIENTE|<') { Stop-Gate "$name debe estar documentado y sin marcadores." }
}

$approval = Get-Required $evidence 'approval' 'raíz'
$approver = [string](Get-Required $approval 'approvedBy' 'approval')
if ($approver -match '^(<|TODO|TBD|PENDIENTE)') { Stop-Gate 'approval.approvedBy contiene un marcador sin completar.' }
$approvedAt = Get-UtcDate (Get-Required $approval 'approvedAtUtc' 'approval') 'approval.approvedAtUtc'

$qa = Get-Required $evidence 'qa' 'raíz'
$qaRunId = [long](Get-Required $qa 'runId' 'qa')
$qaRun = Get-SuccessfulRun $qaRunId 'QA operational regression' '.github/workflows/qa-operational-regression.yml' 'qa'
$qaSha = [string]$qaRun.head_sha
Assert-Sha $qaSha 'head_sha del run de QA'
$qaTestedCommit = [string](Get-Required $qa 'testedCommit' 'qa')
if ($qaTestedCommit -ne $qaSha) { Stop-Gate 'qa.testedCommit debe identificar el commit de la ejecución de QA.' }
# QA can run before measured reports are committed. Only the three exact
# evidence filenames may differ; all application, scripts and workflows match.
$qaMetadata = @('docs/release/security-gate-evidence.json') + $productionMetadata
Assert-SameCode $qaSha $sourceSha $qaMetadata 'el código probado por QA no coincide con sourceCommit.'

$staging = Get-Required $evidence 'staging' 'raíz'
$stagingRunId = [long](Get-Required $staging 'runId' 'staging')
$stagingRun = Get-SuccessfulRun $stagingRunId 'Staging release smoke' '.github/workflows/staging-release-smoke.yml' 'staging'
if ($stagingRun.head_branch -cnotin @('staging', 'release/HU-MAIN-01-promocion-produccion')) {
    Stop-Gate 'el smoke debe ejecutarse desde la rama staging aprobada.'
}
$stagingTestedCommit = [string](Get-Required $staging 'testedCommit' 'staging')
Assert-Sha $stagingTestedCommit 'staging.testedCommit'
Assert-Sha ([string]$stagingRun.head_sha) 'head_sha de la ejecución del smoke'
Assert-Ancestor $sourceSha $stagingTestedCommit 'sourceCommit del smoke'
Assert-Ancestor $stagingTestedCommit $head 'staging.testedCommit'
Assert-SameCode $sourceSha $stagingTestedCommit $productionMetadata 'staging.testedCommit contiene cambios adicionales al candidato.'
Assert-Ancestor $sourceSha ([string]$stagingRun.head_sha) 'sourceCommit de la ejecución del smoke'
Assert-SameCode $sourceSha ([string]$stagingRun.head_sha) $productionMetadata 'el workflow del smoke contiene cambios adicionales al candidato.'

$serviceId = [string](Get-Required $staging 'serviceId' 'staging')
$deployId = [string](Get-Required $staging 'deployId' 'staging')
if ([string]::IsNullOrWhiteSpace($env:RENDER_STAGING_SERVICE_ID) -or $serviceId -cne $env:RENDER_STAGING_SERVICE_ID) {
    Stop-Gate 'staging.serviceId debe coincidir con RENDER_STAGING_SERVICE_ID configurado en GitHub.'
}
if (-not [string]::IsNullOrWhiteSpace($env:RENDER_PRODUCTION_SERVICE_ID) -and $serviceId -ceq $env:RENDER_PRODUCTION_SERVICE_ID) {
    Stop-Gate 'los servicios Render de staging y producción deben ser distintos.'
}

$report = Get-StagingVerification $stagingRunId
if ([int]$report.schemaVersion -ne 1 -or [string]$report.provider -cne 'render-git' -or
    [string]$report.serviceId -cne $serviceId -or [string]$report.deployId -cne $deployId -or
    [string]$report.testedCommit -ne $stagingTestedCommit) {
    Stop-Gate 'el reporte del smoke no coincide con el servicio, despliegue y commit de staging.'
}
$verifiedAt = Get-UtcDate (Get-Required $report 'verifiedAtUtc' 'reporte del smoke') 'verifiedAtUtc del smoke'
if ($approvedAt -lt $verifiedAt) { Stop-Gate 'la aprobación debe ser posterior al smoke de staging.' }

. (Join-Path $PSScriptRoot 'render-deployment.ps1')
# Recording the smoke run creates a new metadata commit and Render deployment.
# Validate the tested historical deployment, then validate the current live one.
$testedDeploy = Assert-RenderGitDeployment -ServiceId $serviceId -DeployId $deployId -ExpectedCommit $stagingTestedCommit -ExpectedBranch staging -AllowHistorical
if ([string]$report.apiUrl -cne $testedDeploy.Url) { Stop-Gate 'el smoke no consultó la URL del servicio Render validado.' }
if ($verifiedAt -lt [DateTimeOffset]::Parse($testedDeploy.FinishedAt)) { Stop-Gate 'el smoke debe ser posterior al despliegue de staging.' }
$currentDeploy = Get-CurrentRenderGitDeployment -ServiceId $serviceId -ExpectedBranch staging
Assert-Ancestor $sourceSha $currentDeploy.Commit 'commit actualmente desplegado en Render'
Assert-Ancestor $currentDeploy.Commit $head 'commit actualmente desplegado en Render'
Assert-SameCode $sourceSha $currentDeploy.Commit $productionMetadata 'Render live contiene cambios adicionales al candidato aprobado.'

Write-Host "Production release gate aprobado: versión $version; código, QA y despliegue Render de staging verificados."
