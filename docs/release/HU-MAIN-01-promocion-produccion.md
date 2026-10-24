# HU-MAIN-01 — Promover una versión aprobada en Render

## Flujo de ramas y despliegues

```text
feature/fix → develop → qa → staging → main
                              │        │
                         Render de  Render de
                          pruebas   producción
```

Los cambios de código, scripts y workflows recorren `develop → qa → staging` mediante PR. La PR final se abre desde `staging` hacia `main`; no exige otra rama. `release/HU-MAIN-01-promocion-produccion` sigue siendo un origen compatible para una PR ya preparada, con los mismos controles. Los servicios Render continúan vinculados a Git: pruebas a `staging` y producción a `main`.

La liberación identifica el código aprobado por su SHA y el despliegue real por sus IDs de servicio y despliegue de Render. Render construye cada servicio desde Git. Este contrato comprueba la procedencia del código y el despliegue probado; no acredita que las imágenes de ambos builds tengan los mismos bytes. El gate de producción no requiere una imagen GHCR ni un digest OCI.

## Configuración inicial

En **GitHub → Settings → Secrets and variables → Actions**, configure:

| Tipo | Nombre | Valor |
| --- | --- | --- |
| Repository secret | `RENDER_API_KEY` | API key creada en Render → Account Settings. No pegarla en la evidencia. |
| Repository variable | `RENDER_STAGING_SERVICE_ID` | ID `srv-…` del servicio Git de pruebas, vinculado a `staging`. |
| Repository variable | `RENDER_PRODUCTION_SERVICE_ID` | ID `srv-…` del servicio Git productivo, vinculado a `main`. Debe ser distinto del de pruebas. |

Configure los entornos GitHub `staging` y `production` con sus propios datos:

| Tipo | Nombres |
| --- | --- |
| Environment secrets | `RELEASE_SMOKE_API_URL`, `RELEASE_SMOKE_IDENTIFIER`, `RELEASE_SMOKE_PASSWORD` |
| Environment variables | `RELEASE_SMOKE_BRANCH_ID`, `RELEASE_SMOKE_ORDER_ID`, `RELEASE_SMOKE_PAID_ORDER_ID`, `RELEASE_SMOKE_DELIVERY_ORDER_ID`, `RELEASE_SMOKE_INVOICE_ID` |

La URL debe coincidir con la URL primaria que devuelve Render en `serviceDetails.url`; este contrato no admite dominios personalizados para el smoke. Use una cuenta sintética dedicada y datos de prueba accesibles por esa cuenta. El smoke consulta los flujos operativos y hace login; no inicia pagos ni crea pedidos. Proteja el entorno `production` con revisión antes de ejecutar sus jobs.

## Preparación del candidato en staging

1. Integre el código por el flujo normal y confirme en Render que el servicio de pruebas está `live` en el SHA que quiere probar. Ejecute QA y prepare los datos sintéticos necesarios para el smoke.
2. Ejecute baseline y candidato de HU-STG-01 en staging con el mismo perfil aprobado. Guarde los reportes reales de p95 del servidor y tasa 5xx; una respuesta de health o una medición aislada no reemplaza esa evidencia. Incorpore `security-gate-evidence.json` sin modificar el código medido. El gate de seguridad admite después de medir únicamente ese reporte y los dos JSON de producción.
3. Elija como `sourceCommit` un SHA que **ya contenga el reporte de seguridad**, sea parte de `staging` y tenga los checks exitosos `security-gate` y `GitGuardian Security Checks` en ese SHA. La ejecución QA debe haber probado el mismo código. Para comparar QA se permiten exclusivamente diferencias en `security-gate-evidence.json`, `production-release-candidate.json` y `production-release-evidence.json`, con esos nombres y rutas exactos. Si los checks no existen en el SHA fuente, ejecute **Security release gate** manualmente seleccionando esa referencia y confirme GitGuardian en ese commit; no herede el resultado de sus padres. **QA operational regression** también admite ejecución manual sobre la referencia que se desea comprobar.
4. Para ejecutar el primer smoke antes de que su workflow exista en `main`, copie `production-release-candidate.template.json` a `production-release-candidate.json`, indique el `sourceCommit` aprobado del paso anterior y cambie `state` de `pending` a `ready-for-smoke`. Incorpore ese archivo a `staging` mediante PR. El push del JSON dispara **Staging release smoke**, consulta el despliegue actualmente `live` mediante la API de Render y comprueba que solo difiera del SHA fuente por los dos JSON de producción. No debe inventar un deploy ID ni un runId para iniciar este proceso. `ready-for-smoke` solicita una prueba; no declara aprobada la liberación.
5. Exija resultado exitoso y conserve el artifact `staging-render-verification`, que contiene `render-staging-verification.json` con el SHA probado, servicio y despliegue. El gate obtiene ese artifact de la ejecución cuyo `runId` se registra en la evidencia; el JSON del artifact no necesita repetir ese `runId`. Una ejecución fallida no genera una aprobación de staging.
6. Copie `production-release-evidence.template.json` a `production-release-evidence.json` y complete valores reales. Registre versión SemVer, `sourceCommit`, notas, migración, rollback, ejecución QA, ejecución smoke, SHA/servicio/despliegue de staging y aprobación real con fecha UTC. El smoke puede ejecutarse antes de crear este JSON; registre su `runId` después de que termine. No agregue contraseñas ni API keys.
7. Después de elegir `sourceCommit`, solo pueden cambiar `docs/release/production-release-candidate.json` y `docs/release/production-release-evidence.json`. Cualquier cambio de código, documentación, scripts o workflows exige preparar nuevamente el candidato y repetir las comprobaciones afectadas.

Si Render todavía está desplegando el commit de metadata o cambia el despliegue durante la prueba, el smoke falla. Espere a que Render muestre `live` y use **Re-run jobs** en esa ejecución de Actions. Reejecutar no necesita otro commit ni otra medición cuando solo cambiaron los metadatos permitidos.

Cuando el workflow ya esté disponible en `main`, también puede iniciarlo en **Actions → Staging release smoke → Run workflow**, seleccionando `staging` e indicando `tested_commit` (SHA completo realmente desplegado) y `deploy_id` (ID `dep-…`). En ambos modos se comprueban repositorio, rama, servicio, SHA, URL y estado `live` antes y después del smoke. GitHub exige el archivo en la rama predeterminada para mostrar **Run workflow**; el trigger por push permite obtener el primer smoke sin combinar una liberación incompleta a `main`.

`production-release-candidate.json` prepara ese primer smoke; no dispara la construcción de una imagen OCI. **Build production candidate artifact (optional OCI)** se conserva como workflow manual opcional para usuarios del flujo anterior; su manifiesto no es requisito del contrato Render Git.

## Qué verifica el gate de producción

`production-release-gate` exige `schemaVersion: 2` y `deploymentProvider: "render-git"`. Además de validar versión, planes y aprobación, comprueba que:

- La PR proviene de `staging` o del origen de liberación compatible, y `sourceCommit` es ancestro del candidato y de `staging`. El reporte de seguridad debe existir ya en ese commit fuente; declararlo en el JSON de producción no lo sustituye.
- Los checks de seguridad requeridos del SHA fuente terminaron correctamente.
- La ejecución **QA operational regression** fue exitosa y corresponde al código fuente, con la excepción limitada de los tres JSON de evidencia.
- La ejecución **Staging release smoke** fue exitosa, y su artifact vincula exactamente el SHA, servicio y despliegue declarados.
- La API de Render identifica el servicio de pruebas configurado en GitHub, el repositorio esperado y la rama `staging`. El despliegue probado debe corresponder al SHA declarado. Puede quedar `deactivated` después de un despliegue posterior; el artifact conserva la verificación de que estuvo `live` durante el smoke.
- El despliegue actualmente `live` de staging conserva el código aprobado. Solo puede diferir de `sourceCommit` por los dos JSON de producción, y su commit debe formar parte del candidato de la PR. Un redeploy automático causado por registrar la evidencia no autoriza otros cambios.

No se confía únicamente en IDs escritos a mano, en una variable con un SHA supuesto ni en el color verde de un despliegue. La API de Render y la evidencia de Actions deben coincidir. Las llamadas usan `RENDER_API_KEY` del almacén de secretos; los reportes no contienen esa credencial.

## Promoción y comprobación de producción

1. Abra o actualice la PR `staging → main`. Revise la evidencia, aprobación y checks requeridos antes de combinar. Configure la protección descrita en `main-branch-protection.md`.
2. Al combinar, deje que el servicio Render vinculado a `main` construya y despliegue el commit resultante. El SHA del merge de `main` puede diferir del SHA probado en staging; la PR aprobada contiene el mismo código autorizado.
3. Cuando ese despliegue esté `live`, ejecute **Production post-deployment smoke** seleccionando `main` e indicando su `deploy_id`. El workflow exige el SHA exacto de `main` seleccionado al iniciar esa ejecución, el servicio productivo configurado y la rama `main`. Comprueba el despliegue real y su URL antes y después del smoke.
4. Archive los resultados, SHAs, IDs Render, versión, fechas UTC, planes y responsable. Una comprobación fallida requiere atención; no convierte el despliegue en aprobado.

Si Render despliega automáticamente cada push a `main`, configure las aprobaciones y checks de la PR antes del merge. El smoke de producción verifica un despliegue ya realizado; no detiene por sí mismo un auto deploy de Render.

## Migraciones y recuperación

El backend aplica migraciones al arrancar. Si hay cambios de esquema, registre y verifique el respaldo antes del despliegue. Documente compatibilidad hacia atrás, responsable de datos y punto de recuperación; si no hay migraciones, indíquelo explícitamente en `migrationPlan`.

Ante una falla crítica, detenga nuevas promociones. Si el esquema es compatible, el responsable puede volver manualmente al despliegue anterior de Render o desplegar el commit previo y repetir readiness y smoke. Render puede reconstruir al desplegar otro commit: conserve tanto el SHA como el ID del despliegue recuperado. No ejecute `Down()` ni restaure una base automáticamente. Para cambios incompatibles, el responsable de datos decide entre corrección hacia adelante y restauración verificada, documentando las escrituras que se perderían.

## Diagnóstico y límites

- Si falta `security-gate-evidence.json`, todavía faltan las mediciones reales y su incorporación a Git. Tener staging funcionando no genera ese reporte.
- Si falta `production-release-evidence.json`, todavía falta registrar y aprobar la liberación. Una plantilla no es evidencia.
- Si faltan la API key o los IDs de servicio en GitHub, configure esos valores antes de reejecutar el gate. No desactive las verificaciones.
- Reejecutar un run antiguo conserva su SHA. Para comprobar un cambio integrado use la ejecución del PR actualizado.

La guía `RENDER_GIT_PASOS.md` resume los próximos pasos de configuración. Las reglas se prueban localmente con `pwsh -File tests/release/production-promotion.Tests.ps1`; `pwsh -File tests/release/render-smoke.Tests.ps1` comprueba los wrappers que verifican Render antes y después del smoke. Esas pruebas usan fixtures y API simuladas: no reemplazan mediciones, smoke ni aprobación reales. Este cambio no afirma que se hayan completado ni que la protección administrativa de `main` esté activada.

## Referencias oficiales

- [Despliegues desde Git, auto deploy y selección de commits en Render](https://render.com/docs/deploys).
- [API de Render y creación de API keys](https://render.com/docs/api).
- [Consulta de un despliegue mediante servicio y deploy ID](https://api-docs.render.com/reference/retrieve-deploy).
- [Requisitos de GitHub para ejecutar un workflow manual](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows#workflow_dispatch).
