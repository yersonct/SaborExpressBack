# HU-STG-02 — Preparar despliegue y recuperación

## Propósito y alcance

Este procedimiento prepara una liberación verificable del backend. El workflow manual construye y publica una sola imagen OCI para el commit seleccionado y conserva un manifiesto con su digest. Staging y producción deben desplegar la misma referencia `ghcr.io/...@sha256:...`; no se debe reconstruir la imagen al promoverla.

El servicio ejecuta `Database.MigrateAsync()` al arrancar. Por eso, la copia de seguridad y su verificación son una puerta obligatoria antes de iniciar una versión que tenga migraciones pendientes.

## Antes de desplegar

1. Asigne en el ticket de liberación a la persona responsable de operaciones y a quien aprueba recuperación de base de datos. Registre nombre, contacto y ventana de cambio.
2. Cada push a `release/HU-STG-02-smoke-migraciones` construye y publica la imagen. Revise **Actions → Build immutable release artifact**, descargue el manifiesto y guarde el digest junto al ticket. Verifique que el SHA corresponda al commit revisado.
3. Configure el servicio de staging para usar la imagen por digest del manifiesto. Después de aprobar staging, configure producción con exactamente el mismo digest. Si el proveedor actual solo reconstruye desde Git, detenga la promoción hasta habilitar despliegue desde imagen inmutable.
4. Configure secretos en el almacén del entorno de staging. Nunca los agregue al repositorio, al manifiesto, a comandos con valores literales ni a la evidencia.
5. Ejecute la validación de configuración en una sesión con las variables ya cargadas en el entorno del proceso:

   ```powershell
   .\scripts\release\preflight.ps1 -AllowedOrigins @('https://admin-staging.example.com','https://app-staging.example.com')
   ```

   Deben estar configurados `ASPNETCORE_ENVIRONMENT=Staging`, `DatabaseProvider=PostgreSQL`, `ConnectionStrings__PostgresConnection`, `JwtSettings__SecretKey` (mínimo 32 caracteres), `JwtSettings__Issuer`, `JwtSettings__Audience`, los cuatro secretos/llaves `Wompi__*` y `Wompi__BaseUrl=https://sandbox.wompi.co/v1/`. Configure `InitialManager__Email` y `InitialManager__Password` juntos únicamente si se inicializa una base vacía. El script solo informa nombres de variables y resultado; no imprime sus valores.
6. Los orígenes CORS de staging deben ser HTTPS, exactos y de staging; no use localhost, IP privada ni Dev Tunnels. La producción debe usar solamente sus orígenes productivos.

## Respaldo y migraciones

Antes de desplegar, el responsable de base de datos debe:

1. Revisar las migraciones nuevas desde el release actual. Buscar cambios destructivos (`DropColumn`, `DropTable`, reducción incompatible de columnas o SQL que elimine datos) y determinar compatibilidad hacia atrás.
2. Crear un snapshot administrado o un `pg_dump` cifrado de la base de staging y registrar identificador, hora UTC, tamaño y retención. No guardar el dump en GitHub ni en el artefacto.
3. Restaurar ese respaldo en una base aislada y confirmar que la restauración puede conectarse y consultar el esquema. Registrar el resultado antes de aprobar la ventana.
4. Para validar una instalación limpia, aplicar todas las migraciones en una base desechable vacía. Para validar actualización, restaurar una copia sintética del último esquema soportado y aplicar las migraciones pendientes en esa copia.
5. Después del arranque, exigir `GET /health/ready` con `200`, `database=connected` y `migrations=applied`. Este endpoint devuelve `503` si no conecta a PostgreSQL o quedan migraciones pendientes; no revela cadenas de conexión ni detalles del proveedor.

No ejecutar `Down()` automáticamente como rollback de producción. Si la nueva versión solo contiene migraciones aditivas compatibles, volver al digest anterior puede ser suficiente. Si cambió o eliminó datos, detener escrituras afectadas, evaluar primero una corrección hacia adelante y restaurar el respaldo solo con aprobación del responsable de datos, aceptando y documentando el punto de recuperación y la pérdida potencial de escrituras posteriores.

## Smoke test en staging

Use exclusivamente cuentas y pedidos sintéticos de staging. Reserve un usuario administrador/gerente para consultas y un usuario cliente separado para Wompi. El login puede invalidar sesiones anteriores si `Auth:SingleSession` está activo.

Defina las variables `RELEASE_SMOKE_API_URL`, `RELEASE_SMOKE_IDENTIFIER`, `RELEASE_SMOKE_PASSWORD`, `RELEASE_SMOKE_BRANCH_ID`, `RELEASE_SMOKE_ORDER_ID`, `RELEASE_SMOKE_PAID_ORDER_ID`, `RELEASE_SMOKE_DELIVERY_ORDER_ID` y `RELEASE_SMOKE_INVOICE_ID`. Los IDs deben pertenecer a datos sintéticos accesibles por el usuario de prueba. Ejecute:

```powershell
.\scripts\release\smoke-staging.ps1
```

El script comprueba liveness, readiness, login, lectura de configuración, pedido, pagos, entrega y factura. No escribe credenciales, JWT, referencias ni firmas en la consola. Una falla marca el smoke como incompleto; regístrela como defecto con endpoint lógico y commit.

Para iniciar una transacción sandbox con un pedido pendiente y descartable, agregue `RELEASE_SMOKE_CUSTOMER_IDENTIFIER`, `RELEASE_SMOKE_CUSTOMER_PASSWORD` y `RELEASE_SMOKE_PENDING_ORDER_ID`, confirme `Wompi__BaseUrl` en sandbox y ejecute:

```powershell
.\scripts\release\smoke-staging.ps1 -StartWompiSandbox
```

Complete el checkout en la web/app contra el entorno sandbox de Wompi usando sus datos oficiales de prueba; luego concilie el `paymentId` que mostró el script:

```powershell
.\scripts\release\smoke-staging.ps1 -VerifyWompiPaymentId <paymentId>
```

Confirme que el pago sandbox quedó aprobado una sola vez, el pedido se confirmó, la factura aparece y la entrega conserva su estado esperado. Esta evidencia no acredita cobros reales. Use un pedido descartable porque el paso de inicio crea un pago pendiente.

## Evidencia y rollback

Complete `docs/release/HU-STG-02-evidence-template.md` con versión, commit, digest, fecha UTC, región, responsables, respaldo, migraciones, resultados de smoke y defectos. No adjunte tokens, secretos, dumps ni datos personales.

Ante fallo de liveness/readiness, error de migración o regresión crítica:

1. El responsable de operaciones pausa la promoción y registra hora y commit.
2. Si el esquema es compatible hacia atrás, vuelve manualmente al digest anterior y verifica readiness y smoke mínimo.
3. Si hay incompatibilidad o pérdida de datos, no inicie `Down()` ni restore automático. El responsable de datos evalúa el respaldo aislado y autoriza por escrito el punto de recuperación.
4. Registre la decisión, los comandos ejecutados, el resultado y el responsable. Reanude promoción solamente después de repetir readiness y smoke.

## Límite de esta implementación

El workflow publica la imagen y el manifiesto; no tiene credenciales para modificar servicios externos ni base de datos. El despliegue real, la restauración, el checkout Wompi y la aprobación de smoke deben ejecutarse en el entorno de staging por los responsables nombrados en el ticket. No se reportan como ejecutados desde este cambio.
