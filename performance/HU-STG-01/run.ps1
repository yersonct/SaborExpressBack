[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][ValidateSet('staging')][string]$Environment,
    [Parameter(Mandatory = $true)][uri]$ApiUrl,
    [uri]$WebUrl,
    [Parameter(Mandatory = $true)][string]$Region,
    [Parameter(Mandatory = $true)][string]$DatasetSize,
    [Parameter(Mandatory = $true)][string]$UserProfile,
    [ValidateRange(1, 500)][int]$Vus = 15,
    [ValidateRange(30, 3600)][int]$SteadySeconds = 600,
    [ValidateRange(30, 600)][int]$WarmupSeconds = 120,
    [ValidateRange(10, 600)][int]$RampSeconds = 120,
    [ValidateRange(1, 30)][int]$ThinkTimeSeconds = 1,
    [Parameter(Mandatory = $true)][switch]$ConfirmStagingTarget,
    [Parameter(Mandatory = $true)][switch]$ConfirmLoadProfile,
    [string]$OutputDirectory = (Join-Path $env:TEMP 'SaborExpress-load-reports')
)

$ErrorActionPreference = 'Stop'
if (-not $ConfirmStagingTarget) { throw 'Confirma que ApiUrl/WebUrl apuntan al staging aislado antes de generar carga.' }
if (-not $ConfirmLoadProfile) { throw 'Confirma que concurrencia, duración y perfil de usuarios fueron acordados antes de ejecutar.' }
if (-not $ApiUrl.IsAbsoluteUri -or $ApiUrl.Scheme -ne 'https') { throw 'ApiUrl debe ser una URL HTTPS absoluta del staging.' }
if ($WebUrl -and (-not $WebUrl.IsAbsoluteUri -or $WebUrl.Scheme -ne 'https')) { throw 'WebUrl debe ser una URL HTTPS absoluta del staging.' }

$knownProductionHosts = @('saborexpressback.onrender.com', 'sabor-express-web.vercel.app')
if ($ApiUrl.Host -in $knownProductionHosts -or ($WebUrl -and $WebUrl.Host -in $knownProductionHosts)) {
    throw 'El host corresponde al despliegue productivo conocido; se bloquea el generador de carga.'
}

$k6 = Get-Command k6 -ErrorAction SilentlyContinue
if (-not $k6) { throw 'No se encontró k6. Instálalo siguiendo la documentación oficial antes de ejecutar.' }

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$apiScript = Join-Path $scriptDirectory 'api.js'
$webScript = Join-Path $scriptDirectory 'web.js'
$runId = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ')
$runDirectory = Join-Path $OutputDirectory $runId
New-Item -ItemType Directory -Force -Path $runDirectory | Out-Null

$commit = (& git -C $scriptDirectory rev-parse HEAD).Trim()
$k6Version = (& $k6.Source version 2>&1 | Out-String).Trim()
$webHost = $null
if ($WebUrl) { $webHost = $WebUrl.Host }
$metadata = [ordered]@{
    story = 'HU-STG-01'
    environment = $Environment
    region = $Region
    apiHost = $ApiUrl.Host
    webHost = $webHost
    datasetSize = $DatasetSize
    profile = $UserProfile
    vus = $Vus
    warmupSeconds = $WarmupSeconds
    rampSeconds = $RampSeconds
    steadySeconds = $SteadySeconds
    rampDownSeconds = 30
    thinkTimeSeconds = $ThinkTimeSeconds
    serverP95TargetMs = 300
    commit = $commit
    k6Version = $k6Version
    startedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
    note = 'No se guardan tokens ni URLs completas. API client duration incluye red; server duration se extrae de Server-Timing.'
}
$metadata | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $runDirectory 'metadata.json') -Encoding utf8

$env:BASE_URL = $ApiUrl.ToString().TrimEnd('/')
$env:LOAD_VUS = [string]$Vus
$env:WARMUP_SECONDS = [string]$WarmupSeconds
$env:RAMP_SECONDS = [string]$RampSeconds
$env:STEADY_SECONDS = [string]$SteadySeconds
$env:THINK_TIME_SECONDS = [string]$ThinkTimeSeconds
$env:MAX_SERVER_P95_MS = '300'
$apiSummary = Join-Path $runDirectory 'api-summary.json'
& $k6.Source run --summary-export $apiSummary $apiScript
if ($LASTEXITCODE -ne 0) { throw "La medición API terminó con error. Revisa $apiSummary y los logs de k6." }

if ($WebUrl) {
    $env:WEB_BASE_URL = $WebUrl.ToString().TrimEnd('/')
    $webSummary = Join-Path $runDirectory 'web-summary.json'
    & $k6.Source run --summary-export $webSummary $webScript
    if ($LASTEXITCODE -ne 0) { throw "La medición web terminó con error. Revisa $webSummary y los logs de k6." }
}

Write-Output "Ejecuciones guardadas en: $runDirectory"
Write-Output "Commit medido: $commit"
Write-Output 'Estos datos describen el staging seleccionado; no certifican dispositivos móviles físicos.'
