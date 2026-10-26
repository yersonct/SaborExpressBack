# Evidencia de liberación HU-STG-02

Plantilla de registro humano. Completar con resultados reales; no incluir credenciales ni datos personales. El artifact automático del smoke se conserva en GitHub Actions.

## Identificación

- Ambiente y región:
- Fecha/hora UTC:
- Repositorio y rama Git:
- Commit probado (SHA completo):
- Servicio Render (ID `srv-…`):
- Despliegue Render probado (ID `dep-…`):
- URL primaria Render verificada:
- Ejecución Actions del smoke (runId/enlace):
- Artifact `staging-render-verification`:
- Responsable de operaciones:
- Responsable de base de datos y recuperación:
- Ventana de cambio:

## Configuración

- Preflight: aprobado / fallido (adjuntar salida sin valores de secretos)
- Secretos cargados en Render y GitHub: sí / no
- Servicios y bases separados para staging/producción: sí / no
- CORS exacto de staging verificado: sí / no
- Wompi sandbox confirmado: sí / no

## Base de datos y migraciones

- Proveedor y versión:
- Conexión verificada:
- Migraciones pendientes antes del despliegue:
- Migraciones aplicadas:
- Instalación limpia en base desechable:
- Actualización desde esquema soportado:
- Respaldo/snapshot ID y hora UTC:
- Restauración de ensayo verificada:
- Decisión de compatibilidad/rollback:

## Smoke test

| Flujo | Resultado | Evidencia o defecto |
| --- | --- | --- |
| Render: servicio, rama, SHA, URL y despliegue live antes/después | Pendiente | |
| Health y readiness | Pendiente | |
| Login de prueba | Pendiente | |
| Configuración | Pendiente | |
| Pedido | Pendiente | |
| Consulta de pago existente | Pendiente | |
| Entrega | Pendiente | |
| Consulta de factura | Pendiente | |
| Checkout sandbox y webhook/conciliación (verificación adicional) | Pendiente | |

## Mediciones y aprobación

- Baseline HU-STG-01 (commit, perfil y reporte real):
- Candidato HU-STG-01 (commit, perfil y reporte real):
- Evidencia de p95 servidor y tasa 5xx:
- Defectos abiertos:
- Decisión: aprobado / rechazado / bloqueado
- Responsable que aprueba y fecha UTC:
- Evidencia `production-release-evidence.json` y `sourceCommit` aprobado:

## Producción y recuperación

- Commit resultante en `main`:
- Servicio Render de producción (distinto de staging):
- Despliegue Render productivo:
- Ejecución **Production post-deployment smoke** y resultado:
- Fecha/hora de promoción:
- Despliegue y commit anteriores para recuperación:
- Acciones de rollback ejecutadas, si aplica:

El despliegue productivo se comprueba contra el SHA exacto de `main`. Render construye desde Git; esta evidencia no afirma que el build productivo y el de staging tengan el mismo digest OCI.
