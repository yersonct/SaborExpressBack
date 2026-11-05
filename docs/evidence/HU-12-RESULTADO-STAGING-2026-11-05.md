# HU-CIERRE-12 — Resultado reportado en Staging

**Fecha del registro:** 5 de noviembre de 2026
**Alcance:** migración PostgreSQL de Staging y validación funcional comunicada por el propietario.
**Estado:** **PASS reportado por el propietario; no reejecutado ni verificado independientemente por Codex en esta actualización.**

## Destino reportado

| Campo | Valor |
|---|---|
| Proyecto Neon | `odd-sound-44850362` |
| Rama de Staging | `br-ancient-violet-b45js5mp` |
| Endpoint (identificador) | `ep-raspy-frost-b49icksh` |
| Base | `neondb` |
| Respaldo de recuperación previo | `br-steep-sun-b46ajpoe` |
| Migración | `20261030124239_AuditCorrections20261030` |
| SHA-256 del SQL autorizado | `5083FBEC84B8E6367A7CC29EB36617C6F204B98257D75D5DC6E082825D18DB1A` |

No se incluyen cadenas de conexión, contraseñas, cookies ni tokens.

## Resultado comunicado por el propietario

- Preflight: **PASS**.
- Ejecución del SQL: **COMMIT**.
- Postcheck: **PASS**; historial EF Core en **36/36**.
- `employee_schedules` está presente y `customers.privacy_accepted_at_utc` existe.
- Los conteos de datos principales se informaron conservados. No se aportaron en este registro los conteos numéricos; no se inventan.
- `/health`: HTTP 200.
- `/health/ready`: HTTP 200, base conectada y migraciones aplicadas.
- Web local apuntando a Staging: login y endpoints protegidos de sedes, pedidos, pagos y productos respondieron HTTP 200.
- SignalR Web local: negociación HTTP 200 y transporte LongPolling operativo en la prueba de navegador. **La recepción comprobada de un evento real sigue pendiente.**

Estos resultados provienen de la confirmación escrita del propietario en la solicitud de publicación. No se adjuntaron aquí las salidas originales de `psql`, capturas redactadas ni exportaciones de logs; por eso no se clasifican como verificaciones independientes de Codex.

## Estado separado de Producción

La migración de Producción **no está autorizada ni se ejecutó como parte de este trabajo**. El último estado comunicado era 35/36 y queda pendiente de una intervención independiente con preflight, respaldo, autorización y validación propios. No se debe inferir que HU-12 ya esté cerrada en Producción.

## Clasificación de artefactos locales revisados

| Artefacto | Tratamiento para este PR | Razón |
|---|---|---|
| `HU-12-AUDITORIA-MIGRACION-2026-11-04.md` | Incluir como evidencia técnica histórica | Documenta la auditoría del modelo, el ensayo desechable y sus límites. Sus veredictos anteriores describen el estado previo a esta ejecución; este registro posterior los complementa. |
| `HU-12-PREFLIGHT-STAGING-2026-11-04.md` | Incluir como historial de preflight | Conserva la secuencia de preflights y bloqueos anteriores; no usar sus instrucciones antiguas como autorización vigente. Este resultado reportado es el registro posterior. |
| `HU-12-staging-quiescence-preflight.sql` | Incluir como comprobación de solo lectura | Guardia de preflight; no aplica DDL ni modifica filas. Debe ejecutarse solo con el procedimiento operativo aprobado. |
| `HU-12-staging-postcheck.sql` | Incluir como comprobación de solo lectura | Verifica el historial 36/36 y los cambios de esquema clave; no realiza escrituras. |
| `HU-12-AuditCorrections20261030-up.sql` | Excluir | SQL de migración ya ejecutado; evitar una copia ejecutable que invite a repetir el cambio. Se conserva su SHA-256 arriba. El código EF de la migración ya existe en `Migrations/`. |
| `EJECUTAR-HU12-STAGING.ps1` | Excluir | Ejecutor con capacidad de aplicar DDL; el cambio de Staging ya fue reportado como aplicado. No se publica como procedimiento para reejecución. |
| `HU-12-staging-quiescence-preflight-wait.sql` | Excluir | Guardia histórica sustituida por el guard actual; no presentarla como procedimiento vigente. |
| `HU-QA-02-wompi-sandbox-2026-10-22.md` | Excluir | Evidencia de otra HU, fuera del alcance. |

## Comprobaciones locales realizadas por Codex para preparar la publicación

- El SHA-256 calculado localmente para el archivo SQL coincide con el valor autorizado consignado arriba. El SQL no se modificó ni ejecutó.
- El ejecutor PowerShell pasó análisis sintáctico estático; referencia el guard actual, fija la rama/endpoint de Staging y dispone de `-PreflightOnly`. El ejecutor se excluye del PR y no fue ejecutado.
- La inspección estática confirma que el guard actual limita `pg_stat_activity` a `datname = current_database()`, comprueba el rol/estado/permisos de la sesión `neon_auth` permitida y no contiene sentencias de escritura. No se abrió una conexión a Neon ni se ejecutó el guard.
- El postcheck contiene comprobaciones de lectura y errores explícitos si no detecta 36/36 o el esquema esperado; no se ejecutó contra una base.
- Gitleaks no encontró hallazgos en `docs/evidence` al preparar este registro. La ausencia de hallazgos no sustituye la revisión de medios incrustados ni demuestra revocación de secretos antiguos.
- No se ejecutaron migraciones, pruebas contra Neon, cambios en Render, despliegues, commits, pushes ni merges durante esta revisión.

## Limitaciones y pasos para reevaluación

1. Adjuntar al expediente los resultados originales redactados de preflight, `COMMIT`, postcheck, health/readiness y prueba funcional, incluyendo fecha, SHA desplegado y conteos numéricos sin datos personales.
2. Demostrar recepción de un evento SignalR real antes de marcar ese criterio como PASS.
3. Mantener una fase separada para Producción; no reutilizar la autorización ni el respaldo de Staging.
4. Abrir y revisar el PR de documentación. El merge lo realizará manualmente el propietario.
