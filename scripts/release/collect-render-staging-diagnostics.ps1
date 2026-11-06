[CmdletBinding()]
param(
    [string]$RepositoryPath = '.',
    [string]$ReportPath = 'release-results/render-staging-readonly-metadata.json'
)

$ErrorActionPreference = 'Stop'
$stagingId = 'srv-dbdonlt9fdbs73cir000'
$stagingUrl = 'https://saborexpressback-staging.onrender.com'
if ($env:RENDER_STAGING_SERVICE_ID -cne $stagingId -or
    $env:RENDER_PRODUCTION_SERVICE_ID -ceq $stagingId) {
    throw 'Diagnostics: the configured service is not the expected isolated staging service.'
}
if ([string]::IsNullOrWhiteSpace($env:RENDER_API_KEY)) { throw 'Diagnostics: Render API access is unavailable.' }

. (Join-Path $PSScriptRoot 'render-deployment.ps1')

function Invoke-DiagnosticRead([string]$Path) {
    if (-not ($Path.StartsWith("services/$stagingId", [StringComparison]::Ordinal) -or $Path.StartsWith('logs?', [StringComparison]::Ordinal))) {
        throw 'Diagnostics: unexpected API path.'
    }
    try {
        $data = Invoke-RestMethod -Method Get -Uri "https://api.render.com/v1/$Path" -Headers @{ Authorization = "Bearer $($env:RENDER_API_KEY.Trim())"; Accept = 'application/json' } -TimeoutSec 30 -MaximumRedirection 0 -ErrorAction Stop
        return [pscustomobject]@{ Ok = $true; HttpStatus = 200; Data = $data }
    } catch {
        $status = 0
        if ($null -ne $_.Exception.Response) { $status = [int]$_.Exception.Response.StatusCode }
        # Never return original exceptions, response bodies, headers or log text.
        return [pscustomobject]@{ Ok = $false; HttpStatus = $status; Data = $null }
    }
}

function Get-SafeUtc($Value) {
    if ($null -eq $Value) { return $null }
    if ($Value -is [DateTimeOffset]) { return $Value.ToUniversalTime().ToString('o') }
    if ($Value -is [DateTime] -and $Value.Kind -ne [DateTimeKind]::Unspecified) { return ([DateTimeOffset]$Value).ToUniversalTime().ToString('o') }
    $text = [string]$Value
    $parsed = [DateTimeOffset]::MinValue
    if ($text -match '(?i)(?:Z|[+-][0-9]{2}:[0-9]{2})$' -and [DateTimeOffset]::TryParse($text, [ref]$parsed)) { return $parsed.ToUniversalTime().ToString('o') }
    return $null
}

function Read-DeployHistory {
    $rows = [Collections.Generic.List[object]]::new()
    $cursor = ''
    $complete = $false
    $status = 200
    for ($page = 0; $page -lt 10; $page++) {
        $path = "services/$stagingId/deploys?limit=100"
        if ($cursor) { $path += '&cursor=' + [uri]::EscapeDataString($cursor) }
        $response = Invoke-DiagnosticRead $path
        $status = $response.HttpStatus
        if (-not $response.Ok) { break }
        $entries = @($response.Data | Where-Object { $null -ne $_ })
        foreach ($entry in $entries) {
            $deploy = $entry.deploy
            if ([string]$deploy.id -cnotmatch '^dep-[0-9a-z]{20}$') { throw 'Diagnostics: invalid deployment metadata.' }
            $commit = if ([string]$deploy.commit.id -match '^[0-9a-fA-F]{40}$') { [string]$deploy.commit.id } else { $null }
            $state = if ([string]$deploy.status -cmatch '^[a-z_]{1,50}$') { [string]$deploy.status } else { 'unknown' }
            $rows.Add([ordered]@{ deployId = $deploy.id; status = $state; commit = $commit; createdAt = Get-SafeUtc $deploy.createdAt; startedAt = Get-SafeUtc $deploy.startedAt; finishedAt = Get-SafeUtc $deploy.finishedAt; updatedAt = Get-SafeUtc $deploy.updatedAt })
        }
        if ($entries.Count -lt 100 -or -not $entries[-1].cursor) { $complete = $true; break }
        $next = [string]$entries[-1].cursor
        if ($next -ceq $cursor) { break }
        $cursor = $next
    }
    return [ordered]@{ httpStatus = $status; complete = $complete; deployments = @($rows.ToArray()) }
}

function Read-Events([string]$Start, [string]$End) {
    $rows = [Collections.Generic.List[object]]::new()
    $cursor = ''
    $complete = $false
    $status = 200
    for ($page = 0; $page -lt 10; $page++) {
        $path = "services/$stagingId/events?limit=100&startTime=$([uri]::EscapeDataString($Start))&endTime=$([uri]::EscapeDataString($End))"
        if ($cursor) { $path += '&cursor=' + [uri]::EscapeDataString($cursor) }
        $response = Invoke-DiagnosticRead $path
        $status = $response.HttpStatus
        if (-not $response.Ok) { break }
        $entries = @($response.Data | Where-Object { $null -ne $_ })
        foreach ($entry in $entries) {
            $event = $entry.event
            $kind = if ([string]$event.type -cmatch '^[A-Za-z0-9_-]{1,80}$') { [string]$event.type } else { 'unknown' }
            $timestamp = Get-SafeUtc $event.timestamp
            if (-not $timestamp) { $timestamp = Get-SafeUtc $event.createdAt }
            $deployId = [string]$event.details.deployId
            if ($deployId -cnotmatch '^dep-[0-9a-z]{20}$') { $deployId = $null }
            $rows.Add([ordered]@{ type = $kind; timestamp = $timestamp; deployId = $deployId })
        }
        if ($entries.Count -lt 100 -or -not $entries[-1].cursor) { $complete = $true; break }
        $next = [string]$entries[-1].cursor
        if ($next -ceq $cursor) { break }
        $cursor = $next
    }
    return [ordered]@{ startUtc = $Start; endUtc = $End; httpStatus = $status; complete = $complete; events = @($rows.ToArray()) }
}

function Read-LogSummary([string]$OwnerId, [string]$Type) {
    $start = '2026-11-06T15:15:00Z'
    $end = '2026-11-06T15:40:00Z'
    $counts = @{}
    $classes = @{}
    $complete = $false
    $status = 200
    $samples = 0
    $publicRoutes = @('/health', '/api/Branches/public', '/api/Categories', '/api/Products')
    for ($page = 0; $page -lt 20; $page++) {
        $path = "logs?ownerId=$([uri]::EscapeDataString($OwnerId))&resource=$stagingId&type=$Type&direction=forward&limit=100&startTime=$([uri]::EscapeDataString($start))&endTime=$([uri]::EscapeDataString($end))"
        if ($Type -ceq 'request') { $path += '&statusCode=4%2A&statusCode=5%2A' }
        $response = Invoke-DiagnosticRead $path
        $status = $response.HttpStatus
        if (-not $response.Ok) { break }
        foreach ($log in @($response.Data.logs | Where-Object { $null -ne $_ })) {
            $samples++
            $labels = @{}
            foreach ($label in @($log.labels)) { if ($label.name -in @('statusCode', 'method', 'path')) { $labels[[string]$label.name] = [string]$label.value } }
            $code = if ($labels.statusCode -match '^[0-9]{3}$') { $labels.statusCode } else { 'unknown-status' }
            $method = if ($labels.method -cmatch '^(GET|POST|PUT|PATCH|DELETE|HEAD|OPTIONS)$') { $labels.method } else { 'unknown-method' }
            $route = ([string]$labels.path).Split('?')[0]
            if ($route -cnotin $publicRoutes) { $route = 'other-route' }
            $key = "$method $route $code"
            if (-not $counts.ContainsKey($key)) { $counts[$key] = 0 }
            $counts[$key]++
            # Classify in memory. Never save messages, SQL, headers, IDs or stack traces.
            foreach ($pattern in @('42P01', '42703', 'OutOfMemoryException', 'TimeoutException', 'TaskCanceledException', 'NpgsqlException', 'connection reset', 'timed out')) {
                if ([string]$log.message -match [regex]::Escape($pattern)) {
                    if (-not $classes.ContainsKey($pattern)) { $classes[$pattern] = 0 }
                    $classes[$pattern]++
                }
            }
        }
        if (-not $response.Data.hasMore) { $complete = $true; break }
        $nextStart = Get-SafeUtc $response.Data.nextStartTime
        $nextEnd = Get-SafeUtc $response.Data.nextEndTime
        if (-not $nextStart -or -not $nextEnd -or ($nextStart -ceq $start -and $nextEnd -ceq $end)) { break }
        $start = $nextStart
        $end = $nextEnd
    }
    return [ordered]@{ type = $Type; startUtc = '2026-11-06T15:15:00Z'; endUtc = '2026-11-06T15:40:00Z'; httpStatus = $status; complete = $complete; logSamples = $samples; requestCounts = $counts; errorClasses = $classes; note = 'Window diagnostics only; these are not exact k6 request counts or release evidence.' }
}

$before = Get-CurrentRenderGitDeployment -ServiceId $stagingId -ExpectedBranch staging -ExpectedRepositoryFullName 'yersonct/SaborExpressBack'
if ($before.Url -cne $stagingUrl) { throw 'Diagnostics: unexpected staging origin.' }
$serviceResponse = Invoke-DiagnosticRead "services/$stagingId"
if (-not $serviceResponse.Ok) { throw "Diagnostics: service metadata request failed (HTTP $($serviceResponse.HttpStatus))." }
$service = $serviceResponse.Data
if ($service.id -cne $stagingId -or $service.branch -cne 'staging' -or $service.repo.TrimEnd('/').Replace('.git', '') -cne 'https://github.com/yersonct/SaborExpressBack') { throw 'Diagnostics: staging identity changed.' }

$history = Read-DeployHistory
$baselineEvents = Read-Events '2026-11-05T22:30:00Z' '2026-11-05T22:55:00Z'
$candidateEvents = Read-Events '2026-11-06T15:15:00Z' '2026-11-06T15:40:00Z'
$logSummaries = @()
if ([string]$service.ownerId -cmatch '^(tea|usr)-[0-9a-z]+$') {
    $logSummaries = @((Read-LogSummary $service.ownerId 'request'), (Read-LogSummary $service.ownerId 'app'))
}
$after = Get-CurrentRenderGitDeployment -ServiceId $stagingId -ExpectedBranch staging -ExpectedRepositoryFullName 'yersonct/SaborExpressBack'
if ($before.DeployId -cne $after.DeployId -or $before.Commit -cne $after.Commit) { throw 'Diagnostics: live deployment changed while reading; repeat only the read-only collection.' }

$report = [ordered]@{
    kind = 'render-staging-readonly-diagnostic'
    notReleaseEvidence = $true
    collectedAtUtc = [DateTime]::UtcNow.ToString('o')
    serviceId = $stagingId
    current = [ordered]@{ deployId = $after.DeployId; commit = $after.Commit; apiUrl = $after.Url; finishedAt = $after.FinishedAt }
    history = $history
    baselineEvents = $baselineEvents
    candidateEvents = $candidateEvents
    logSummaries = $logSummaries
}
$resolvedReport = [IO.Path]::GetFullPath((Join-Path $RepositoryPath $ReportPath))
[void][IO.Directory]::CreateDirectory((Split-Path -Parent $resolvedReport))
[IO.File]::WriteAllText($resolvedReport, ($report | ConvertTo-Json -Depth 12), [Text.UTF8Encoding]::new($false))
Write-Host "Staging Live SHA: $($after.Commit)"
Write-Host "Staging Live deploy: $($after.DeployId)"
Write-Host 'Read-only metadata collected. No smoke, performance approval or deployment was executed.'
