[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$expected = $env:EXPECTED_IMAGE_DIGEST
$deployed = $env:PRODUCTION_DEPLOYED_IMAGE_DIGEST

if ($expected -notmatch '^sha256:[0-9a-f]{64}$') {
    throw 'EXPECTED_IMAGE_DIGEST debe ser un digest OCI sha256 válido.'
}
if ([string]::IsNullOrWhiteSpace($deployed) -or $deployed -cne $expected) {
    throw 'El digest informado por el entorno de producción no coincide con el aprobado.'
}

Write-Host '[OK] El digest de producción coincide con el aprobado.'
