# HU-STG-02 — Preparar despliegue y recuperación en Render

## Propósito y alcance

El servicio de staging despliega desde la rama Git `staging`, y el de producción desde `main`. Se registra el SHA del código, el ID de servicio y el ID de despliegue de Render. La API de Render verifica la procedencia y el estado del despliegue real. Cada servicio construye desde Git: el contrato no garantiza imágenes con bytes idénticos ni requiere publicar en GHCR.

El backend ejecuta `Database.MigrateAsync()` al arrancar. La revisión de migraciones y el respaldo verificado deben realizarse **antes** de iniciar una versión con migraciones pendientes.

## Antes de desplegar

1. Asigne en el ticket a la persona responsable de operaciones y a quien aprueba recuperación de base de datos. Registre responsables y ventana de cambio.
2. Configure dos servicios Render Git distintos del repositorio: staging vinculado a `staging` y producción a `main`. Use bases de datos separadas. Registre el commit revisado y el despliegue anterior para recuperación.
3. Revise las migraciones y prepare el respaldo indicado abajo antes de que Render despliegue el nuevo código. Si hay auto deploy, coordine la incorporación del commit con esa ventana.
4. Guarde secretos de la aplicación en Render. Nunca los agregue al repositorio ni a la evidencia. Con las variables cargadas en el entorno de una sesión, puede validar la configuración sin imprimir sus valores:

   ```powershell
   .\scripts\release\preflight.ps1 -AllowedOrigins @('https://admin-staging.example.com','https://app-staging.example.com')
   ```

   Se requieren `ASPNETCORE_ENVIRONMENT=Staging`, `DatabaseProvider=PostgreSQL`, `ConnectionStrings__PostgresConnection`, `JwtSettings__SecretKey` (mínimo 32 caracteres), `JwtSettings__Issuer`, `JwtSettings__Audience`, las cuatro credenciales `Wompi__*` y `Wompi__BaseUrl=https://sandbox.wompi.co/v1/`. Configure `InitialManager__Email` y `InitialManager__Password` juntos si se inicializa una base vacía.
5. Configure CORS con los orígenes HTTPS exactos de los clientes de cada entorno. En staging no use localhost, IP privada ni Dev Tunnels. La producción usa sus propios orígenes.
6. Configure en GitHub la API key, los IDs de los servicios y las cuentas/datos sintéticos descritos en `HU-MAIN-01-promocion-produccion.md`. La API key es un secret, nunca una variable pública ni un campo del reporte.

**Build immutable release artifact** y **Build production candidate artifact (optional OCI)** conservan una alternativa OCI legada. Sus manifiestos no son evidencia obligatoria del flujo Render Git ni un sustituto de la verificación del despliegue real.

## Respaldo y migraciones

Antes de desplegar, el responsable de base de datos debe:

1. Revisar las migraciones nuevas desde el release actual. Buscar cambios destructivos (`DropColumn`, `DropTable`, reducción incompatible de columnas o SQL que elimine datos) y determinar compatibilidad hacia atrás.
2. Crear un snapshot administrado o un `pg_dump` cifrado de la base correspondiente. Registrar identificador, hora UTC, tamaño y retención. No guardar el dump en GitHub ni como artifact de Actions.
3. Restaurar ese respaldo en una base aislada y confirmar conexión y consulta del esquema. Registrar el resultado antes de aprobar la ventana.
4. Validar instalación limpia en una base desechable vacía. Para comprobar actualización, restaurar una copia sintética del último esquema soportado y aplicar allí las migraciones pendientes.
5. Después del arranque, exigir `GET /health/ready` con HTTP `200`, `database=connected` y `migrations=applied`. El endpoint devuelve `503` si no conecta a PostgreSQL o quedan migraciones pendientes; no revela cadenas de conexión.

No ejecutar `Down()` como rollback automático de producción. Si solo hay migraciones aditivas compatibles, volver al despliegue/commit anterior de Render puede ser suficiente. Ante cambios incompatibles o pérdida de datos, detener escrituras afectadas y evaluar una corrección hacia adelante. La restauración requiere la decisión del responsable de datos, documentando el punto de recuperación y las escrituras posteriores que se perderían.

## Smoke test de staging vinculado a Render

Use cuentas y pedidos sintéticos de staging. Reserve un usuario administrador/gerente para consultas y otro cliente para Wompi. El login puede invalidar sesiones anteriores si `Auth:SingleSession` está activo.

Configure en el entorno GitHub `staging`:

- Secrets: `RELEASE_SMOKE_API_URL`, `RELEASE_SMOKE_IDENTIFIER`, `RELEASE_SMOKE_PASSWORD`.
- Variables: `RELEASE_SMOKE_BRANCH_ID`, `RELEASE_SMOKE_ORDER_ID`, `RELEASE_SMOKE_PAID_ORDER_ID`, `RELEASE_SMOKE_DELIVERY_ORDER_ID`, `RELEASE_SMOKE_INVOICE_ID`.

Los IDs deben existir y ser accesibles por el usuario sintético. `RELEASE_SMOKE_API_URL` debe coincidir con la URL primaria de ese servicio en la API de Render, `serviceDetails.url`.

Para el primer smoke, copie `production-release-candidate.template.json` a `production-release-candidate.json`. Use el `sourceCommit` que ya contiene la evidencia de seguridad y cambie `state` a `ready-for-smoke`. Al incorporar ese JSON en `staging`, su push inicia **Staging release smoke**. El workflow obtiene el despliegue `live` desde Render y exige que conserve el código fuente: solo pueden diferir los dos JSON de producción. No necesita inventar un deploy ID, un `runId` ni crear previamente `production-release-evidence.json`.

Si Render todavía está desplegando o reemplaza la versión durante la prueba, espere a que termine y use **Re-run jobs** sobre la ejecución fallida. No agregue otro commit para reintentar.

Cuando el archivo del workflow ya exista en `main`, también puede usar **Actions → Staging release smoke → Run workflow**, seleccionar `staging` e indicar `tested_commit` (SHA completo desplegado) y `deploy_id` (ID `dep-…`). El primer trigger por push evita depender del botón manual antes de que GitHub lo muestre.

El workflow valida mediante Render el servicio configurado, repositorio, rama, commit, URL y estado `live` antes y después del smoke. Comprueba health, readiness, login, configuración, pedido, pagos, entrega y factura mediante consultas. Solo si todo pasa publica `staging-render-verification`, con `render-staging-verification.json`. Registre el ID de la ejecución exitosa en la evidencia de producción.

También puede ejecutar el smoke local con las mismas variables cargadas:

```powershell
.\scripts\release\smoke-staging.ps1
```

La ejecución local ayuda a diagnosticar; por sí sola no produce el artifact de Actions que exige el gate de producción. Una falla deja el smoke incompleto: registre endpoint lógico, defecto y SHA, sin tokens ni datos personales.

## Verificación Wompi sandbox opcional

El smoke automático anterior no crea pagos. Para comprobar un checkout sandbox de forma explícita, use un pedido pendiente descartable y defina `RELEASE_SMOKE_CUSTOMER_IDENTIFIER`, `RELEASE_SMOKE_CUSTOMER_PASSWORD` y `RELEASE_SMOKE_PENDING_ORDER_ID`. Confirme el endpoint sandbox de Wompi y ejecute:

```powershell
.\scripts\release\smoke-staging.ps1 -StartWompiSandbox
```

Complete el checkout en la web/app de pruebas con los datos oficiales de Wompi sandbox. Concilie el `paymentId` mostrado por el script:

```powershell
.\scripts\release\smoke-staging.ps1 -VerifyWompiPaymentId <paymentId>
```

Confirme que el pago quedó aprobado una sola vez, el pedido se confirmó, aparece la factura y la entrega conserva su estado esperado. No acredita cobros reales. Registre esta verificación por separado; el inicio crea un pago pendiente.

## Evidencia y recuperación

Complete `HU-STG-02-evidence-template.md` con commit, IDs Render, fechas, responsables, respaldo, migraciones y resultados. Para producción, siga además `HU-MAIN-01-promocion-produccion.md`, incluyendo las mediciones reales HU-STG-01 y la aprobación del candidato.

Ante fallo de liveness/readiness, migración o regresión crítica:

1. Operaciones pausa la promoción y registra hora, SHA y despliegue fallido.
2. Si el esquema es compatible, vuelve manualmente al despliegue anterior de Render o despliega el commit anterior. Registra el despliegue resultante y repite readiness y smoke.
3. Si hay incompatibilidad o pérdida de datos, no ejecuta `Down()` ni restore automático. El responsable de datos decide y autoriza por escrito el punto de recuperación.
4. Registre la decisión, acciones, resultado y responsable. Reanude solamente después de repetir las verificaciones.

Los scripts verifican servicios y despliegues mediante consultas a Render. No crean servicios, cambian ramas, despliegan producción ni restauran bases por sí mismos. Los workflows y pruebas locales no acreditan que se hayan ejecutado las mediciones o aprobaciones reales.
