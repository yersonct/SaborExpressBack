# Próximos pasos para promover staging a main

El flujo sigue siendo **develop → qa → staging → main**. El backend de pruebas ya responde en Render. Ese arranque confirma conexión y migraciones, pero no crea los dos reportes que están bloqueando la PR a `main`.

## 1. Incorporar esta corrección

Integre la corrección del contrato Render Git en `develop` y promuévala por `qa` hasta `staging`. Espere a que Render despliegue ese nuevo commit. Los cambios de scripts/workflows, incluida la corrección del checkout de QA, forman parte del candidato que debe medirse: obtenga las mediciones finales después de incorporarlos.

Mantenga abierta la PR `staging → main`; se actualizará al avanzar `staging`. No combine mientras falle un control requerido.

## 2. Dar acceso de verificación a GitHub

En Render, abra **Account Settings → API Keys** y cree una API key. Guárdela directamente en **GitHub → Settings → Secrets and variables → Actions → New repository secret**, con el nombre `RENDER_API_KEY`. No la pegue en el chat, en Git ni en los JSON de evidencia.

En la pestaña **Variables** de ese mismo apartado de GitHub agregue:

| Nombre | Valor que debe copiar |
| --- | --- |
| `RENDER_STAGING_SERVICE_ID` | ID `srv-…` del servicio `saborexpressback-staging`. |
| `RENDER_PRODUCTION_SERVICE_ID` | ID `srv-…` del servicio productivo `saborexpressback`. |

El ID aparece en la URL del panel de cada servicio Render, después de `/web/`. Los dos IDs deben ser distintos. Confirme en **Settings** de Render las ramas `staging` y `main`, respectivamente. La API de Render verificará estos datos; escribir los IDs no aprueba la versión.

## 3. Preparar datos de prueba y el smoke

La base nueva de staging se encontró sin sedes, productos, pedidos, facturas, pagos ni entregas. Prepare una cuenta sintética dedicada y datos sintéticos para esos flujos mediante los mecanismos de la aplicación. Tener solo al gerente inicial no alcanza para probarlos.

En **GitHub → Settings → Environments**, cree/configure `staging` con:

- Secrets `RELEASE_SMOKE_API_URL` (URL primaria HTTPS de Render), `RELEASE_SMOKE_IDENTIFIER` y `RELEASE_SMOKE_PASSWORD` (cuenta sintética).
- Variables `RELEASE_SMOKE_BRANCH_ID`, `RELEASE_SMOKE_ORDER_ID`, `RELEASE_SMOKE_PAID_ORDER_ID`, `RELEASE_SMOKE_DELIVERY_ORDER_ID` y `RELEASE_SMOKE_INVOICE_ID` (IDs reales de los datos sintéticos).

Configure después `production` con su propia URL, cuenta e IDs, y revisores autorizados. No use contraseñas personales.

## 4. Medir y comprobar staging

1. Defina el perfil de carga y obtenga baseline y candidato reales de HU-STG-01. Conserve las mediciones de p95 del servidor y tasa 5xx. No complete valores inventados ni copie un ejemplo como resultado.
2. Incorpore `docs/release/security-gate-evidence.json` con esos resultados, sin cambiar el código medido. Elija el SHA fuente que ya contenga el reporte. Confirme sus checks de seguridad y QA; puede ejecutar manualmente **Security release gate** y **QA operational regression** sobre la referencia que necesita comprobar.
3. Copie `production-release-candidate.template.json` a `production-release-candidate.json`. Complete `sourceCommit` con el SHA que ya contiene el reporte de seguridad y cambie `state` de `pending` a `ready-for-smoke`.
4. Incorpore ese JSON a `staging` mediante PR. El push inicia **Staging release smoke** y consulta en Render el despliegue `live` real. No necesita el botón **Run workflow**, que GitHub puede ocultar hasta que el workflow exista en `main`.
5. Si falla porque Render todavía está desplegando, espere a que muestre `live` y use **Re-run jobs** en la ejecución fallida de Actions. Reintente esa ejecución sin agregar otro commit.
6. Espere el resultado exitoso. En el resumen de esa ejecución encontrará el artifact `staging-render-verification`. El runId es el número que aparece en su URL después de `/actions/runs/`.

Cuando el workflow ya exista en `main`, podrá usar además **Run workflow** seleccionando `staging`. Ese modo pide `tested_commit` y `deploy_id`: son el SHA completo y el ID `dep-…` del despliegue `live` de pruebas. El ID aparece en la URL del despliegue Render después de `/deploys/`.

## 5. Registrar la aprobación y promover

Copie `production-release-evidence.template.json` a `production-release-evidence.json`. Complete versión, SHA fuente, notas, planes, QA, smoke, IDs Render y aprobación real. El formato detallado está en `HU-MAIN-01-promocion-produccion.md`.

Después del SHA fuente solo pueden cambiar los dos JSON de producción. Incorpore esos metadatos en `staging`; si Render los vuelve a desplegar, el gate permite ese cambio de metadata y exige el mismo código aprobado.

Cuando los checks de la PR `staging → main` estén verdes y esté aprobada, podrá combinarla. Render desplegará desde `main`. Después ejecute **Production post-deployment smoke** sobre `main` con el nuevo `deploy_id` productivo.

## Lo que todavía falta

La existencia de staging no demuestra que se hayan completado HU-STG-01, los datos sintéticos, el smoke operativo ni la aprobación. Esta guía no genera `security-gate-evidence.json` ni `production-release-evidence.json` con valores ficticios. Los resultados reales siguen siendo necesarios para liberar.
