[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($env:PRODUCTION_DEPLOYED_IMAGE_DIGEST)) {
    throw 'Falta PRODUCTION_DEPLOYED_IMAGE_DIGEST.'
}
if ($env:EXPECTED_IMAGE_DIGEST -cne $env:PRODUCTION_DEPLOYED_IMAGE_DIGEST) {
    throw 'El digest desplegado cambió antes de ejecutar el smoke test.'
}

# El smoke compartido consulta health, readiness y datos sintéticos. Al no
# activar los parámetros de Wompi, no inicia ni modifica transacciones.
& (Join-Path $PSScriptRoot 'smoke-staging.ps1') -Environment Production
