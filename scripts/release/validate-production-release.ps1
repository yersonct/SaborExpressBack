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

function Get-SuccessfulRun([long]$RunId, [string]$ExpectedName, [string]$Context) {
    if ($RunId -le 0) { Stop-Gate "runId de $Context no es válido." }
    $run = Invoke-GitHubGet "repos/$env:GITHUB_REPOSITORY/actions/runs/$RunId"
    if ($run.status -ne 'completed' -or $run.conclusion -ne 'success' -or $run.name -ne $ExpectedName) {
        Stop-Gate "la ejecución de $Context debe llamarse '$ExpectedName' y terminar exitosamente."
    }
    return $run
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
    Stop-Gate "no existe $EvidencePath. Complete la evidencia real de QA, staging y el artefacto antes de promover."
}

try { $evidence = Get-Content -LiteralPath $resolvedEvidence -Raw | ConvertFrom-Json }
catch { Stop-Gate 'la evidencia de producción no contiene JSON válido.' }

if ([int](Get-Required $evidence 'schemaVersion' 'raíz') -ne 1) { Stop-Gate 'schemaVersion debe ser 1.' }
$version = [string](Get-Required $evidence 'version' 'raíz')
if ($version -notmatch '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(-[0-9A-Za-z.-]+)?(\+[0-9A-Za-z.-]+)?$') {
    Stop-Gate 'version debe usar SemVer.'
}

$sourceSha = [string](Get-Required $evidence 'sourceCommit' 'raíz')
$sourceTree = Get-CommitTree $sourceSha 'sourceCommit'
Assert-Ancestor $sourceSha 'origin/staging' 'sourceCommit'
Assert-Ancestor $sourceSha $head 'sourceCommit del PR'
& git -C $RepositoryPath diff --quiet $sourceSha $head -- . `
    ':(exclude)docs/release/production-release-candidate.json' `
    ':(exclude)docs/release/production-release-evidence.json' 2>$null
if ($LASTEXITCODE -ne 0) {
    Stop-Gate 'la rama final contiene cambios adicionales al código ya aprobado en staging; desarrolle y promueva esos cambios por develop, qa y staging primero.'
}
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
$approvedAt = [DateTimeOffset]::MinValue
$approvedText = [string](Get-Required $approval 'approvedAtUtc' 'approval')
if (-not [DateTimeOffset]::TryParse($approvedText, [ref]$approvedAt) -or $approvedAt.Offset -ne [TimeSpan]::Zero) {
    Stop-Gate 'approval.approvedAtUtc debe ser una fecha ISO-8601 UTC.'
}

$artifact = Get-Required $evidence 'artifact' 'raíz'
$digest = [string](Get-Required $artifact 'digest' 'artifact')
if ($digest -notmatch '^sha256:[0-9a-f]{64}$') { Stop-Gate 'artifact.digest debe ser un digest OCI sha256.' }
$image = [string](Get-Required $artifact 'image' 'artifact')
$expectedImagePrefix = "ghcr.io/$($env:GITHUB_REPOSITORY.ToLowerInvariant())@"
if (-not $image.StartsWith($expectedImagePrefix, [StringComparison]::Ordinal) -or -not $image.EndsWith("@$digest", [StringComparison]::Ordinal)) {
    Stop-Gate 'artifact.image debe apuntar al repositorio GHCR y al mismo digest registrado.'
}
$buildRunId = [long](Get-Required $artifact 'buildRunId' 'artifact')
$buildRun = Get-SuccessfulRun $buildRunId 'Build production candidate artifact' 'artifact'
$artifactList = Invoke-GitHubGet "repos/$env:GITHUB_REPOSITORY/actions/runs/$buildRunId/artifacts"
$manifestArtifact = @($artifactList.artifacts | Where-Object { $_.name -eq "release-manifest-$sourceSha" -and -not $_.expired } | Select-Object -First 1)
if ($manifestArtifact.Count -eq 0) { Stop-Gate 'no se encontró el manifiesto inmutable del artefacto.' }

$apiBase = if ([string]::IsNullOrWhiteSpace($env:GITHUB_API_URL)) { 'https://api.github.com' } else { $env:GITHUB_API_URL.TrimEnd('/') }
$zipPath = Join-Path ([System.IO.Path]::GetTempPath()) ([guid]::NewGuid().ToString('N') + '.zip')
$extractPath = Join-Path ([System.IO.Path]::GetTempPath()) ([guid]::NewGuid().ToString('N'))
try {
    Invoke-WebRequest -Uri "$apiBase/repos/$env:GITHUB_REPOSITORY/actions/artifacts/$($manifestArtifact[0].id)/zip" `
        -Headers @{ Authorization = "Bearer $env:GITHUB_TOKEN"; Accept = 'application/vnd.github+json' } `
        -OutFile $zipPath -TimeoutSec 45
    Expand-Archive -LiteralPath $zipPath -DestinationPath $extractPath -Force
    $manifestPath = Get-ChildItem -LiteralPath $extractPath -Filter manifest.json -Recurse -File | Select-Object -First 1
    if ($null -eq $manifestPath) { Stop-Gate 'el artefacto no contiene manifest.json.' }
    $manifest = Get-Content -LiteralPath $manifestPath.FullName -Raw | ConvertFrom-Json
} finally {
    Remove-Item -LiteralPath $zipPath -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $extractPath -Recurse -Force -ErrorAction SilentlyContinue
}
if ($manifest.commit -ne $sourceSha -or $manifest.digest -ne $digest -or $manifest.image -ne $image) {
    Stop-Gate 'el manifiesto descargado no coincide con el SHA y digest de liberación.'
}

$qa = Get-Required $evidence 'qa' 'raíz'
$qaRunId = [long](Get-Required $qa 'runId' 'qa')
$qaRun = Get-SuccessfulRun $qaRunId 'QA operational regression' 'qa'
$qaSha = [string]$qaRun.head_sha
Assert-Sha $qaSha 'head_sha del run de QA'
if ((Get-CommitTree $qaSha 'head_sha del run de QA') -ne $sourceTree) {
    Stop-Gate 'el árbol de código probado por QA no coincide exactamente con el artefacto candidato.'
}
$qaTestedCommit = [string](Get-Required $qa 'testedCommit' 'qa')
if ((Get-CommitTree $qaTestedCommit 'qa.testedCommit') -ne $sourceTree) {
    Stop-Gate 'qa.testedCommit no tiene el mismo árbol que sourceCommit.'
}

$staging = Get-Required $evidence 'staging' 'raíz'
$stagingRunId = [long](Get-Required $staging 'runId' 'staging')
$stagingRun = Get-SuccessfulRun $stagingRunId 'Staging release smoke' 'staging'
$stagingTestedCommit = [string](Get-Required $staging 'testedCommit' 'staging')
if ((Get-CommitTree $stagingTestedCommit 'staging.testedCommit') -ne $sourceTree) {
    Stop-Gate 'staging.testedCommit no tiene el mismo árbol que sourceCommit.'
}
if ([string]$staging.imageDigest -cne $digest) { Stop-Gate 'staging no probó el mismo digest OCI que se propone promover.' }

Write-Host "Production release gate aprobado: versión $version; código, artefacto, QA y staging coinciden."
