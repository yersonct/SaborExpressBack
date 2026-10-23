[CmdletBinding()]
param(
    [switch]$StartWompiSandbox,
    [int]$VerifyWompiPaymentId = 0
)

$ErrorActionPreference = 'Stop'
$baseUrl = $env:RELEASE_SMOKE_API_URL
$identifier = $env:RELEASE_SMOKE_IDENTIFIER
$password = $env:RELEASE_SMOKE_PASSWORD
$branchId = $env:RELEASE_SMOKE_BRANCH_ID
$orderId = $env:RELEASE_SMOKE_ORDER_ID
$paidOrderId = $env:RELEASE_SMOKE_PAID_ORDER_ID
$deliveryOrderId = $env:RELEASE_SMOKE_DELIVERY_ORDER_ID
$invoiceId = $env:RELEASE_SMOKE_INVOICE_ID
$failures = [System.Collections.Generic.List[string]]::new()
$login = $null
$customerToken = ''

function Invoke-SmokeStep([string]$Name, [scriptblock]$Action) {
    try {
        & $Action | Out-Null
        Write-Host "[OK] $Name" -ForegroundColor Green
    } catch {
        $script:failures.Add($Name)
        Write-Host "[FALLO] $Name" -ForegroundColor Red
    }
}

function Invoke-Api([string]$Method, [string]$Path, [string]$Token = '', $Body = $null) {
    $headers = @{}
    if (-not [string]::IsNullOrWhiteSpace($Token)) { $headers.Authorization = "Bearer $Token" }
    $parameters = @{
        Method = $Method
        Uri = $script:baseUrl.TrimEnd('/') + $Path
        Headers = $headers
        TimeoutSec = 20
    }
    if ($null -ne $Body) {
        $parameters.ContentType = 'application/json'
        $parameters.Body = $Body | ConvertTo-Json -Depth 8 -Compress
    }
    return Invoke-RestMethod @parameters
}

function Get-RequiredEnv([string]$Name) {
    $value = [Environment]::GetEnvironmentVariable($Name)
    if ([string]::IsNullOrWhiteSpace($value)) { throw "Falta la variable requerida $Name" }
    return $value
}

$apiUri = $null
if (-not [uri]::TryCreate($baseUrl, [System.UriKind]::Absolute, [ref]$apiUri) -or
    $apiUri.Scheme -ne 'https' -or -not [string]::IsNullOrEmpty($apiUri.UserInfo)) {
    Write-Error 'RELEASE_SMOKE_API_URL debe ser una URL HTTPS sin credenciales.'
    exit 1
}

foreach ($name in @('RELEASE_SMOKE_IDENTIFIER', 'RELEASE_SMOKE_PASSWORD', 'RELEASE_SMOKE_BRANCH_ID',
        'RELEASE_SMOKE_ORDER_ID', 'RELEASE_SMOKE_PAID_ORDER_ID', 'RELEASE_SMOKE_DELIVERY_ORDER_ID',
        'RELEASE_SMOKE_INVOICE_ID')) {
    try { $null = Get-RequiredEnv $name } catch { Write-Error "Falta la configuración de humo: $name"; exit 1 }
}

Invoke-SmokeStep 'API liveness GET /health' { Invoke-Api 'Get' '/health' }
Invoke-SmokeStep 'API readiness, base de datos y migraciones GET /health/ready' {
    $result = Invoke-Api 'Get' '/health/ready'
    if ($result.status -ne 'ready' -or $result.database -ne 'connected' -or $result.migrations -ne 'applied') {
        throw 'El API no está listo'
    }
}

Invoke-SmokeStep 'Inicio de sesión con usuario sintético' {
    $script:login = Invoke-Api 'Post' '/api/Auth/login' '' @{
        identifier = $identifier
        password = $password
    }
    if ([string]::IsNullOrWhiteSpace($script:login.token)) { throw 'El login no devolvió token' }
}
if ([string]::IsNullOrWhiteSpace($script:login.token)) { exit 1 }
$token = $script:login.token

Invoke-SmokeStep 'Lectura de configuración de sede' {
    Invoke-Api 'Get' "/api/branch-settings/$branchId" $token
}
Invoke-SmokeStep 'Consulta de pedido de prueba' {
    Invoke-Api 'Get' "/api/Orders/$orderId" $token
}
Invoke-SmokeStep 'Consulta de pagos del pedido pagado de prueba' {
    Invoke-Api 'Get' "/api/Payments/order/$paidOrderId" $token
}
Invoke-SmokeStep 'Consulta de entrega de prueba' {
    Invoke-Api 'Get' "/api/Deliveries/order/$deliveryOrderId" $token
}
Invoke-SmokeStep 'Consulta de factura de prueba' {
    Invoke-Api 'Get' "/api/Invoices/$invoiceId" $token
}

if ($StartWompiSandbox -or $VerifyWompiPaymentId -gt 0) {
    $wompiBaseUrl = $env:Wompi__BaseUrl
    $wompiUri = $null
    if (-not [uri]::TryCreate($wompiBaseUrl, [System.UriKind]::Absolute, [ref]$wompiUri) -or
        $wompiUri.Scheme -ne 'https' -or $wompiUri.DnsSafeHost -ne 'sandbox.wompi.co') {
        Write-Error 'Las pruebas de pago solo se ejecutan si Wompi__BaseUrl apunta a sandbox.wompi.co.'
        exit 1
    }

    $customerToken = ''
    if ($StartWompiSandbox) {
        try {
            $customerIdentifier = Get-RequiredEnv 'RELEASE_SMOKE_CUSTOMER_IDENTIFIER'
            $customerCredential = Get-RequiredEnv 'RELEASE_SMOKE_CUSTOMER_PASSWORD'
            $pendingOrderId = [int](Get-RequiredEnv 'RELEASE_SMOKE_PENDING_ORDER_ID')
            $customerLogin = Invoke-Api 'Post' '/api/Auth/login' '' @{
                identifier = $customerIdentifier
                password = $customerCredential
            }
            $customerToken = $customerLogin.token
            if ([string]::IsNullOrWhiteSpace($customerToken)) { throw 'Login de cliente inválido' }
            $pendingOrder = Invoke-Api 'Get' "/api/Orders/$pendingOrderId" $customerToken
            $wompi = Invoke-Api 'Post' '/api/Payments/wompi/init' $customerToken @{
                orderId = $pendingOrderId
                amount = $pendingOrder.total
            }
            if ($wompi.paymentId -le 0 -or [string]::IsNullOrWhiteSpace($wompi.reference) -or
                [string]::IsNullOrWhiteSpace($wompi.integritySignature)) {
                throw 'No se pudo iniciar el cobro sandbox'
            }
            Write-Host "[OK] Inicio de pago Wompi sandbox; paymentId=$($wompi.paymentId). Complete el checkout con la tarjeta de prueba oficial y luego use -VerifyWompiPaymentId $($wompi.paymentId)." -ForegroundColor Green
        } catch {
            $failures.Add('Inicio de pago Wompi sandbox')
            Write-Host '[FALLO] Inicio de pago Wompi sandbox' -ForegroundColor Red
        }
    }

    if ($VerifyWompiPaymentId -gt 0) {
        try {
            if ([string]::IsNullOrWhiteSpace($customerToken)) {
                $customerIdentifier = Get-RequiredEnv 'RELEASE_SMOKE_CUSTOMER_IDENTIFIER'
                $customerCredential = Get-RequiredEnv 'RELEASE_SMOKE_CUSTOMER_PASSWORD'
                $customerLogin = Invoke-Api 'Post' '/api/Auth/login' '' @{
                    identifier = $customerIdentifier
                    password = $customerCredential
                }
                $customerToken = $customerLogin.token
                if ([string]::IsNullOrWhiteSpace($customerToken)) { throw 'Login de cliente inválido' }
            }
            $null = Invoke-Api 'Post' "/api/Payments/$VerifyWompiPaymentId/wompi-sync" $customerToken
            $payment = Invoke-Api 'Get' "/api/Payments/$VerifyWompiPaymentId" $customerToken
            if ($payment.status -ne 'Completed') {
                throw 'El pago de prueba todavía no está aprobado y completado en Wompi sandbox'
            }
            Write-Host "[OK] Conciliación del pago sandbox paymentId=$VerifyWompiPaymentId. Revise estado aprobado en el panel." -ForegroundColor Green
        } catch {
            $failures.Add('Conciliación del pago Wompi sandbox')
            Write-Host '[FALLO] Conciliación del pago Wompi sandbox' -ForegroundColor Red
        }
    }
}

$token = $null
$script:login = $null
$customerToken = $null
if ($failures.Count -gt 0) {
    Write-Host "Smoke incompleto: $($failures.Count) paso(s) fallaron. Registre cada defecto con commit, fecha e idioma/rol cuando aplique." -ForegroundColor Red
    exit 1
}

Write-Host 'Smoke API completado. Revise el runbook para completar el pago sandbox en navegador y archivar evidencia sin credenciales.' -ForegroundColor Green
