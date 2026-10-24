[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $EvidencePath,

    [Parameter(Mandatory = $true)]
    [string] $ExpectedHeadSha,

    [string] $RepositoryPath = (Get-Location).Path
)

$ErrorActionPreference = 'Stop'

function Stop-Gate([string] $Message) {
    Write-Error "Security release gate: $Message"
    exit 1
}

function Get-RequiredProperty($Object, [string] $Name, [string] $Context) {
    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property -or $null -eq $property.Value -or "$($property.Value)" -eq '') {
        Stop-Gate "falta '$Name' en $Context."
    }
    return $property.Value
}

function Assert-ValidSha([string] $Sha, [string] $Context) {
    if ($Sha -notmatch '^[0-9a-fA-F]{40}$') {
        Stop-Gate "el commit de $Context no es un SHA de 40 caracteres."
    }
}

function Assert-ValidMeasurement($Measurement, [string] $Context) {
    $method = [string](Get-RequiredProperty $Measurement 'method' $Context)
    $route = [string](Get-RequiredProperty $Measurement 'route' $Context)
    $requestCount = Get-RequiredProperty $Measurement 'requestCount' $Context
    $http5xxCount = Get-RequiredProperty $Measurement 'http5xxCount' $Context
    $serverP95Ms = Get-RequiredProperty $Measurement 'serverP95Ms' $Context

    if ($method -notmatch '^(GET|POST|PUT|PATCH|DELETE|HEAD|OPTIONS)$') {
        Stop-Gate "método HTTP inválido en $Context."
    }
    if (-not $route.StartsWith('/') -or $route.Contains('?') -or $route.Contains('://') -or $route.Length -gt 256) {
        Stop-Gate "la ruta de $Context debe ser una plantilla sin query string ni URL concreta."
    }
    if ([long]$requestCount -le 0 -or [long]$http5xxCount -lt 0 -or [long]$http5xxCount -gt [long]$requestCount) {
        Stop-Gate "los conteos de $Context no son válidos."
    }
    if ([double]::IsNaN([double]$serverP95Ms) -or [double]::IsInfinity([double]$serverP95Ms) -or [double]$serverP95Ms -le 0) {
        Stop-Gate "el p95 de servidor de $Context debe ser finito y mayor que cero."
    }

    return [pscustomobject]@{
        Key = "$method $route"
        Method = $method
        Route = $route
        RequestCount = [long]$requestCount
        Http5xxCount = [long]$http5xxCount
        ServerP95Ms = [double]$serverP95Ms
    }
}

$head = (& git -C $RepositoryPath rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) { Stop-Gate 'no se pudo leer el commit actual.' }
if ($head -ne $ExpectedHeadSha) { Stop-Gate 'el checkout no corresponde al commit del pull request.' }

$resolvedEvidence = Join-Path $RepositoryPath $EvidencePath
if (-not (Test-Path -LiteralPath $resolvedEvidence -PathType Leaf)) {
    Stop-Gate "no existe $EvidencePath. Una promoción requiere mediciones reales de HU-STG-01."
}

try {
    $evidence = Get-Content -LiteralPath $resolvedEvidence -Raw | ConvertFrom-Json
} catch {
    Stop-Gate 'el archivo de evidencia no contiene JSON válido.'
}

if ([int](Get-RequiredProperty $evidence 'schemaVersion' 'raíz') -ne 1) {
    Stop-Gate 'schemaVersion debe ser 1.'
}
if (([string](Get-RequiredProperty $evidence 'environment' 'raíz')) -ne 'staging') {
    Stop-Gate 'la evidencia debe corresponder al entorno staging.'
}

$baseline = Get-RequiredProperty $evidence 'baseline' 'raíz'
$candidate = Get-RequiredProperty $evidence 'candidate' 'raíz'
foreach ($section in @(@{ Name = 'baseline'; Value = $baseline }, @{ Name = 'candidate'; Value = $candidate })) {
    $captured = [string](Get-RequiredProperty $section.Value 'capturedAtUtc' $section.Name)
    $parsedDate = [DateTimeOffset]::MinValue
    if (-not [DateTimeOffset]::TryParse($captured, [ref]$parsedDate) -or $parsedDate.Offset -ne [TimeSpan]::Zero) {
        Stop-Gate "capturedAtUtc de $($section.Name) debe incluir zona UTC."
    }
    $commit = [string](Get-RequiredProperty $section.Value 'commit' $section.Name)
    Assert-ValidSha $commit $section.Name
    [void](Get-RequiredProperty $section.Value 'loadRunId' $section.Name)
}

$baselineDate = [DateTimeOffset]::Parse([string]$baseline.capturedAtUtc)
$candidateDate = [DateTimeOffset]::Parse([string]$candidate.capturedAtUtc)
if ($candidateDate -le $baselineDate) { Stop-Gate 'la medición candidata debe ser posterior a la línea base.' }

$candidateSha = [string]$candidate.commit
if ($candidateSha -eq $head) { Stop-Gate 'el reporte debe añadirse después del commit medido.' }
$ancestorOutput = & git -C $RepositoryPath merge-base --is-ancestor $candidateSha $head 2>$null
if ($LASTEXITCODE -ne 0) { Stop-Gate 'el commit medido no es ancestro del commit promocionado.' }

$changedAfterMeasurement = @(& git -C $RepositoryPath diff --name-only "$candidateSha..$head")
if ($LASTEXITCODE -ne 0) { Stop-Gate 'no se pudo verificar el contenido posterior a las mediciones.' }
$allowedChange = $EvidencePath.Replace('\', '/')
$allowedChanges = @(
    $allowedChange
    'docs/release/production-release-candidate.json'
    'docs/release/production-release-evidence.json'
)
$normalizedChanges = @($changedAfterMeasurement | ForEach-Object { $_.Replace('\', '/') })
if ($allowedChange -cnotin $normalizedChanges -or
    @($normalizedChanges | Where-Object { $_ -cnotin $allowedChanges }).Count -gt 0) {
    Stop-Gate 'después de medir se requiere actualizar la evidencia de seguridad; solo se permiten además los metadatos production-release-candidate.json y production-release-evidence.json, sin cambios de código.'
}

$baselineRows = @((Get-RequiredProperty $baseline 'routes' 'baseline') | ForEach-Object { Assert-ValidMeasurement $_ 'baseline.routes' })
$candidateRows = @((Get-RequiredProperty $candidate 'routes' 'candidate') | ForEach-Object { Assert-ValidMeasurement $_ 'candidate.routes' })
if ($baselineRows.Count -eq 0 -or $candidateRows.Count -eq 0) { Stop-Gate 'baseline y candidate deben incluir rutas medidas.' }
if (($baselineRows.Key | Select-Object -Unique).Count -ne $baselineRows.Count) { Stop-Gate 'hay rutas duplicadas en baseline.' }
if (($candidateRows.Key | Select-Object -Unique).Count -ne $candidateRows.Count) { Stop-Gate 'hay rutas duplicadas en candidate.' }

$baselineByKey = @{}
foreach ($row in $baselineRows) { $baselineByKey[$row.Key] = $row }
if ($candidateRows.Count -ne $baselineRows.Count) { Stop-Gate 'baseline y candidate deben cubrir el mismo conjunto de rutas.' }

$regressions = [System.Collections.Generic.List[string]]::new()
foreach ($candidateRow in $candidateRows) {
    if (-not $baselineByKey.ContainsKey($candidateRow.Key)) {
        Stop-Gate "la ruta $($candidateRow.Key) no está en baseline."
    }
    $baselineRow = $baselineByKey[$candidateRow.Key]
    $baselineErrorRate = [double]$baselineRow.Http5xxCount / $baselineRow.RequestCount
    $candidateErrorRate = [double]$candidateRow.Http5xxCount / $candidateRow.RequestCount
    if ($candidateRow.ServerP95Ms -gt $baselineRow.ServerP95Ms) {
        $regressions.Add("$($candidateRow.Key): p95 candidato $($candidateRow.ServerP95Ms) ms > baseline $($baselineRow.ServerP95Ms) ms")
    }
    if ($candidateErrorRate -gt $baselineErrorRate) {
        $regressions.Add("$($candidateRow.Key): tasa 5xx candidata $candidateErrorRate > baseline $baselineErrorRate")
    }
}

if ($regressions.Count -gt 0) {
    Stop-Gate ($regressions -join '; ')
}

Write-Host "Security release gate: evidencia válida; $($candidateRows.Count) rutas dentro de la línea base medida."
