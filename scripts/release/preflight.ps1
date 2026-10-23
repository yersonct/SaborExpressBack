[CmdletBinding()]
param(
    [string[]]$AllowedOrigins = @(),
    [string]$ApiBaseUrl = ''
)

$ErrorActionPreference = 'Stop'
$failures = [System.Collections.Generic.List[string]]::new()
$checks = [System.Collections.Generic.List[string]]::new()

function Get-EnvValue([string]$Name) {
    return [Environment]::GetEnvironmentVariable($Name)
}

function Test-PrivateOrDevelopmentOrigin([uri]$Uri) {
    $hostName = $Uri.DnsSafeHost.ToLowerInvariant()
    if ($hostName -eq 'localhost' -or $hostName.EndsWith('.localhost') -or
        $hostName.EndsWith('.local') -or $hostName.EndsWith('.devtunnels.ms')) {
        return $true
    }

    $address = $null
    if ([System.Net.IPAddress]::TryParse($hostName, [ref]$address)) {
        if ([System.Net.IPAddress]::IsLoopback($address) -or $address.IsIPv6LinkLocal) {
            return $true
        }

        $bytes = $address.GetAddressBytes()
        if ($bytes.Length -eq 4) {
            return $bytes[0] -eq 10 -or $bytes[0] -eq 127 -or
                ($bytes[0] -eq 169 -and $bytes[1] -eq 254) -or
                ($bytes[0] -eq 172 -and $bytes[1] -ge 16 -and $bytes[1] -le 31) -or
                ($bytes[0] -eq 192 -and $bytes[1] -eq 168) -or
                ($bytes[0] -eq 100 -and $bytes[1] -ge 64 -and $bytes[1] -le 127)
        }

        return ($bytes[0] -band 0xfe) -eq 0xfc
    }

    return $false
}

function Add-Check([string]$Name, [bool]$Passed) {
    if ($Passed) { $checks.Add($Name) } else { $failures.Add($Name) }
}

$environment = Get-EnvValue 'ASPNETCORE_ENVIRONMENT'
Add-Check 'ASPNETCORE_ENVIRONMENT=Staging' ($environment -eq 'Staging')
Add-Check 'DatabaseProvider=PostgreSQL' ((Get-EnvValue 'DatabaseProvider') -eq 'PostgreSQL')
Add-Check 'ConnectionStrings__PostgresConnection configurada' (-not [string]::IsNullOrWhiteSpace((Get-EnvValue 'ConnectionStrings__PostgresConnection')))

$jwtSecret = Get-EnvValue 'JwtSettings__SecretKey'
Add-Check 'JwtSettings__SecretKey presente y de al menos 32 caracteres' ($null -ne $jwtSecret -and $jwtSecret.Length -ge 32)
Add-Check 'JwtSettings__Issuer configurado' (-not [string]::IsNullOrWhiteSpace((Get-EnvValue 'JwtSettings__Issuer')))
Add-Check 'JwtSettings__Audience configurado' (-not [string]::IsNullOrWhiteSpace((Get-EnvValue 'JwtSettings__Audience')))

$wompiBaseUrl = Get-EnvValue 'Wompi__BaseUrl'
$wompiUri = $null
$isWompiSandbox = [uri]::TryCreate($wompiBaseUrl, [System.UriKind]::Absolute, [ref]$wompiUri) -and
    $wompiUri.Scheme -eq 'https' -and $wompiUri.DnsSafeHost -eq 'sandbox.wompi.co'
Add-Check 'Wompi__BaseUrl apunta a sandbox.wompi.co' $isWompiSandbox
foreach ($name in @('Wompi__PublicKey', 'Wompi__PrivateKey', 'Wompi__IntegritySecret', 'Wompi__EventsSecret')) {
    Add-Check "$name presente" (-not [string]::IsNullOrWhiteSpace((Get-EnvValue $name)))
}

$managerEmail = Get-EnvValue 'InitialManager__Email'
$managerCredential = Get-EnvValue 'InitialManager__Password'
if ([string]::IsNullOrWhiteSpace($managerEmail) -xor [string]::IsNullOrWhiteSpace($managerCredential)) {
    $failures.Add('InitialManager__Email e InitialManager__Password deben configurarse juntos o ambos omitirse')
} else {
    $checks.Add('Credenciales del gerente inicial completas u omitidas')
}

if ($AllowedOrigins.Count -eq 0) {
    $AllowedOrigins = @(Get-ChildItem Env: | Where-Object { $_.Name -match '^AllowedOrigins__\d+$' } |
        Sort-Object { [int]($_.Name -replace '\D', '') } | ForEach-Object { $_.Value })
}

$originValidationPassed = $AllowedOrigins.Count -gt 0
foreach ($origin in $AllowedOrigins) {
    $parsed = $null
    $isExactHttpsOrigin = [uri]::TryCreate($origin, [System.UriKind]::Absolute, [ref]$parsed) -and
        $parsed.Scheme -eq 'https' -and
        $parsed.GetLeftPart([System.UriPartial]::Authority).TrimEnd('/') -eq $origin.TrimEnd('/') -and
        -not (Test-PrivateOrDevelopmentOrigin $parsed)
    if (-not $isExactHttpsOrigin) { $originValidationPassed = $false }
}
Add-Check 'AllowedOrigins contiene solo orígenes HTTPS exactos y no locales' $originValidationPassed

if (-not [string]::IsNullOrWhiteSpace($ApiBaseUrl)) {
    $apiUri = $null
    $validApiUrl = [uri]::TryCreate($ApiBaseUrl, [System.UriKind]::Absolute, [ref]$apiUri) -and
        $apiUri.Scheme -eq 'https' -and [string]::IsNullOrEmpty($apiUri.UserInfo)
    if (-not $validApiUrl) {
        $failures.Add('ApiBaseUrl debe ser HTTPS y no incluir credenciales')
    } else {
        try {
            $health = Invoke-RestMethod -Method Get -Uri ($ApiBaseUrl.TrimEnd('/') + '/health/ready') -TimeoutSec 15
            Add-Check 'GET /health/ready confirma DB conectada y migraciones aplicadas' (
                $health.status -eq 'ready' -and $health.database -eq 'connected' -and $health.migrations -eq 'applied')
        } catch {
            $failures.Add('GET /health/ready respondió con error o no fue accesible')
        }
    }
}

foreach ($check in $checks) { Write-Host "[OK] $check" -ForegroundColor Green }
foreach ($failure in $failures) { Write-Host "[FALLO] $failure" -ForegroundColor Red }

if ($failures.Count -gt 0) {
    Write-Host "Preflight falló: $($failures.Count) comprobación(es). No continuar con el despliegue." -ForegroundColor Red
    exit 1
}

Write-Host 'Preflight completado. No se imprimieron valores de secretos.' -ForegroundColor Green
