# HU-MAIN-01 — Promover una versión aprobada

## Flujo de ramas

La implementación de esta HU vive en `feature/HU-MAIN-01-promocion-produccion`, creada desde `develop`. Su primer PR apunta a `develop`. Los cambios siguen el recorrido normal del producto, mediante PR y controles en cada etapa:

```text
feature/HU-MAIN-01-promocion-produccion → develop → qa → staging → main
```

Los cambios de código y de los controles de liberación deben recorrer `develop → qa → staging`. La promoción final se abre directamente desde `staging` hacia `main`: no requiere crear otra rama. `release/HU-MAIN-01-promocion-produccion` se conserva como origen compatible para liberaciones que ya la utilicen, con las mismas validaciones de evidencia y artefacto.

## Artefacto inmutable

La imagen que se probó y desplegó en staging debe ser la misma que se promueve a producción. Se identifica por digest OCI; no se reconstruye desde Git para producción ni se despliega usando una etiqueta mutable.

Al llegar a `staging`, prepare la liberación en este orden:

1. Ejecute las mediciones reales HU-STG-01 sobre el código desplegado en staging. Incorpore `security-gate-evidence.json` por el flujo normal, sin cambiar el código medido. El SHA medido será un ancestro del candidato de producción; no se vuelve a medir solo por agregar los metadatos de liberación.
2. Elija como `sourceCommit` un SHA que ya incluya esa evidencia de seguridad, pertenezca a `staging` y tenga exactamente el mismo árbol que el candidato probado en QA. Confirme los checks exitosos `security-gate` y `GitGuardian Security Checks` en **ese SHA**. No dé por aprobados los checks de un merge solo porque sus padres los pasaron. **QA operational regression** y **Security release gate** admiten ejecución manual sobre la referencia que se desea validar.
3. Incorpore en `staging` `production-release-candidate.json`, a partir de su plantilla, mediante un PR que solo prepare metadatos. Registre `sourceCommit` y marque `state` como `approved` solo si la aprobación es real. El push a `staging` ejecuta **Build production candidate artifact**; también puede ejecutarlo manualmente seleccionando `staging`. Valida las referencias y checks, compila exactamente el SHA fuente y publica `ghcr.io/yersonct/saborexpressback:<sha>`.
4. Descargue `release-manifest-<sha>` y registre `commit`, `image`, `digest` y `workflow_run`. Prepare `production-release-evidence.json` con ese SHA, digest y ejecución de build. Despliegue ese digest en staging; no reconstruya ni use una etiqueta mutable.
5. Incorpore la evidencia en `staging` y registre el digest como `STAGING_DEPLOYED_IMAGE_DIGEST` en el entorno GitHub `staging`. El push de la evidencia ejecuta **Staging release smoke**; también puede ejecutarlo manualmente seleccionando `staging`. El job verifica el digest y consulta health, readiness, login sintético, configuración, pedido, pago, entrega y factura con datos sintéticos.
6. Actualice la evidencia con los IDs de las ejecuciones exitosas de QA y staging y la aprobación real. **Staging release smoke** se ejecuta sobre `sourceCommit`; actualizar la evidencia para registrar su resultado no cambia el código ni el digest probados. Después de elegir `sourceCommit`, solo pueden cambiar los dos JSON de producción. Cualquier cambio adicional exige preparar y aprobar un nuevo candidato.

## Evidencia de liberación

Copie `production-release-evidence.template.json` a `production-release-evidence.json` durante la preparación en `staging` y complete cada campo con evidencia real. No incluya credenciales, tokens, datos personales ni secretos. `runId` es el identificador numérico de GitHub Actions. El JSON debe incluir el resultado del smoke antes de aprobar la PR a `main`; no es necesario inventar un `runId` para iniciar el smoke, que solo lee `sourceCommit` y el digest.

El check `production-release-gate` rechaza una PR a `main` cuando falta evidencia, hay marcadores de plantilla, el SHA no pertenece a staging, falla un check de seguridad, el código probado en QA o staging no coincide, el manifiesto no corresponde al artefacto, staging no probó el mismo digest o faltan versión SemVer, notas, plan de migración, rollback o aprobación.

Si no hay migraciones, indíquelo explícitamente en `migrationPlan`. Si cambia el esquema o los datos, registre antes de aprobar el respaldo verificado, compatibilidad hacia atrás, responsable y recuperación.

## Promoción y verificación en producción

1. Abra o actualice la PR desde `staging` hacia `main`. No combine sin aprobación y checks requeridos verdes. El gate acepta este origen directamente y exige la misma evidencia que una rama de liberación.
2. Despliegue exactamente la imagen por digest del manifiesto aprobado.
3. Registre en el entorno GitHub `production` la variable `PRODUCTION_DEPLOYED_IMAGE_DIGEST` con el digest reportado por la plataforma.
4. Ejecute **Production post-deployment smoke** con el digest aprobado. El job confirma el digest y consulta health, readiness y flujos sintéticos. No inicia pagos Wompi ni crea pedidos.
5. Archive resultado, SHA, digest, versión, fecha UTC, notas, migraciones y responsable junto al release.

El usuario de smoke y sus IDs deben ser sintéticos y de solo lectura. Guarde usuario y contraseña en secretos del entorno `production`, e IDs en variables. No use cuentas personales.

## Recuperación

Ante un health check, readiness o smoke crítico fallido, detenga la promoción. Si el esquema es compatible, operaciones puede volver manualmente al digest anterior y repetir health, readiness y smoke. No ejecute `Down()` ni restaure una base automáticamente. Para cambios incompatibles, el responsable de datos decide entre corrección hacia adelante y restauración del respaldo, documentando el punto de recuperación y las escrituras que se perderían.

## Estado conocido

Las ejecuciones compartidas para `qa → staging` fallaban porque el workflow pedía `docs/release/security-gate-evidence.json` antes de que pudiera ejecutarse la medición HU-STG-01 en staging. Esta rama corrige el orden: la evidencia de latencia se requiere al promover a `main`; al promover a `staging` siguen ejecutándose las pruebas de seguridad y el escaneo de secretos. El candidato no queda aprobado por este cambio: después del despliegue en staging todavía se deben ejecutar HU-STG-01, registrar resultados reales y completar los checks antes de la promoción a `main`.

La protección de `main` requiere configuración administrativa en GitHub. `main-branch-protection.md` documenta esos pasos; este código no afirma que la regla ya esté activada.

## Diagnóstico de una PR bloqueada

- Si el error exige `release/HU-MAIN-01-promocion-produccion`, la PR todavía usa la versión anterior del gate. Promueva esta corrección por `develop → qa → staging` y actualice la PR `staging → main`.
- Si falta `production-release-evidence.json` o `security-gate-evidence.json`, todavía faltan resultados reales, aunque el código haya llegado a staging. Solo las plantillas no aprueban la promoción.
- Reejecutar una ejecución antigua conserva su SHA. Para probar la corrección, use la nueva ejecución del PR actualizado.

Las reglas de ramas, evidencia, checks y archivos permitidos se verifican sin red con `pwsh -File tests/release/production-promotion.Tests.ps1`. El workflow de seguridad ejecuta estas pruebas antes de las pruebas .NET.
