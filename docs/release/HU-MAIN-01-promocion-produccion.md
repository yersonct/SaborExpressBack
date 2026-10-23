# HU-MAIN-01 — Promover una versión aprobada

## Flujo de ramas

La implementación de esta HU vive en `feature/HU-MAIN-01-promocion-produccion`, creada desde `develop`. Su primer PR apunta a `develop`. Los cambios siguen el recorrido normal del producto, mediante PR y controles en cada etapa:

```text
feature/HU-MAIN-01-promocion-produccion → develop → qa → staging → main
```

No se crea ahora una rama desde `staging` para desarrollar esta HU ni se envía el trabajo directamente a `qa`, `staging` o `main`. Cuando los cambios hayan pasado por las tres ramas previas y exista un candidato aprobado en `staging`, se podrá cortar `release/HU-MAIN-01-promocion-produccion` desde ese candidato como paso final de liberación. Esa rama final solo prepara y verifica la promoción a `main`; no sustituye el flujo anterior.

## Artefacto inmutable

La imagen que se probó y desplegó en staging debe ser la misma que se promueve a producción. Se identifica por digest OCI; no se reconstruye desde Git para producción ni se despliega usando una etiqueta mutable.

Al llegar a `staging` y antes de crear la rama final:

1. Confirme que el SHA fuente está en `staging`, que su árbol de código coincide con el candidato aprobado por QA y que los checks `security-gate` y `GitGuardian Security Checks` terminaron exitosamente.
2. Cree `release/HU-MAIN-01-promocion-produccion` desde el SHA aprobado de `staging`.
3. Copie `production-release-candidate.template.json` a `production-release-candidate.json`; registre el SHA completo y marque `state` como `approved` solo si la aprobación es real. El push ejecuta **Build production candidate artifact**, que valida las referencias y checks, compila exactamente el SHA fuente y publica `ghcr.io/yersonct/saborexpressback:<sha>`.
4. Descargue `release-manifest-<sha>` y registre `commit`, `image`, `digest` y `workflow_run`. No reconstruya ni use la etiqueta mutable en los siguientes despliegues.
5. Despliegue ese digest en staging. Registre el digest como `STAGING_DEPLOYED_IMAGE_DIGEST` en el entorno GitHub `staging` y ejecute **Staging release smoke**. El job verifica el digest y consulta health, readiness, login sintético, configuración, pedido, pago, entrega y factura con datos sintéticos.
6. Registre las ejecuciones exitosas de QA y staging. Ambas deben probar un árbol de código idéntico al SHA fuente; staging también debe registrar el digest OCI desplegado.

## Evidencia de liberación

Copie `production-release-evidence.template.json` a `production-release-evidence.json` en la rama final y complete cada campo con evidencia real. No incluya credenciales, tokens, datos personales ni secretos. `runId` es el identificador numérico de GitHub Actions.

El check `production-release-gate` rechaza una PR a `main` cuando falta evidencia, hay marcadores de plantilla, el SHA no pertenece a staging, falla un check de seguridad, el código probado en QA o staging no coincide, el manifiesto no corresponde al artefacto, staging no probó el mismo digest o faltan versión SemVer, notas, plan de migración, rollback o aprobación.

Si no hay migraciones, indíquelo explícitamente en `migrationPlan`. Si cambia el esquema o los datos, registre antes de aprobar el respaldo verificado, compatibilidad hacia atrás, responsable y recuperación.

## Promoción y verificación en producción

1. Abra una PR desde `release/HU-MAIN-01-promocion-produccion` hacia `main`. No combine sin aprobación y checks requeridos verdes.
2. Despliegue exactamente la imagen por digest del manifiesto aprobado.
3. Registre en el entorno GitHub `production` la variable `PRODUCTION_DEPLOYED_IMAGE_DIGEST` con el digest reportado por la plataforma.
4. Ejecute **Production post-deployment smoke** con el digest aprobado. El job confirma el digest y consulta health, readiness y flujos sintéticos. No inicia pagos Wompi ni crea pedidos.
5. Archive resultado, SHA, digest, versión, fecha UTC, notas, migraciones y responsable junto al release.

El usuario de smoke y sus IDs deben ser sintéticos y de solo lectura. Guarde usuario y contraseña en secretos del entorno `production`, e IDs en variables. No use cuentas personales.

## Recuperación

Ante un health check, readiness o smoke crítico fallido, detenga la promoción. Si el esquema es compatible, operaciones puede volver manualmente al digest anterior y repetir health, readiness y smoke. No ejecute `Down()` ni restaure una base automáticamente. Para cambios incompatibles, el responsable de datos decide entre corrección hacia adelante y restauración del respaldo, documentando el punto de recuperación y las escrituras que se perderían.

## Estado conocido

Al preparar esta HU, el candidato de staging revisado tenía GitGuardian exitoso, pero `security-gate` fallaba por falta de evidencia real de carga HU-STG-01 en `docs/release/security-gate-evidence.json`. Ese SHA no está aprobado: no se debe activar el workflow de artefacto ni promoverlo hasta obtener la evidencia y los checks requeridos. El archivo activo de candidato se crea únicamente en la rama final, a partir de un SHA que haya completado `develop → qa → staging`.

La protección de `main` requiere configuración administrativa en GitHub. `main-branch-protection.md` documenta esos pasos; este código no afirma que la regla ya esté activada.
