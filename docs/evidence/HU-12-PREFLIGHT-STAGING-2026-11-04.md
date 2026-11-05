# HU-CIERRE-12 — Preflight final para migración de staging

- **Corte:** 4 de noviembre de 2026, 19:36:38 UTC.
- **Repositorio:** SaborExpressBack. Rama local de revisión: audit/hu-12-pending-migration.
- **Modo:** metadatos Neon y SELECT de solo lectura, además de revisión local de artefactos. No se ejecutó DDL/DML en la base original, no se cambió Render ni se tocó producción. No se repitió el ensayo de migración.

## Veredicto

**NO-GO para aplicar ahora.** El target y el artefacto están identificados, el SHA informado para Render coincide con la referencia local de staging y la migración ensayada conserva los datos de la copia. Sin embargo, la rama de recuperación br-wandering-darkness-b4fyyyao no está en el WAL actual de staging: se creó desde 0/2129D28, y staging ahora está en 0/2151788. Los conteos consultados siguen iguales, pero eso no prueba que la rama contenga cada escritura posterior. Se requiere un punto de recuperación tomado después de congelar las escrituras y una restauración verificada de ese punto.

También deben quedar comprobados en Render: que ApplyMigrationsOnStartup está ausente o false, que no hay workers/jobs adicionales escribiendo en la misma base, y que el mantenimiento de webhooks Wompi tiene una reconciliación preparada. No solicitar autorización de DDL hasta cerrar estos controles.

## 1. Identidad y estado actual — consultas de solo lectura

Neon confirmó, consultando identificadores explícitos:

| Recurso | Observación |
|---|---|
| Proyecto staging | SaborExpress-Staging, odd-sound-44850362. |
| Staging original | br-ancient-violet-b45js5mp, nombre Neon production, endpoint ep-raspy-frost-b49icksh, base neondb. Es staging de SaborExpress; el nombre de rama no identifica el proyecto real de producción. |
| Producción real | Es otro proyecto Neon, fancy-night-67370126. No se consultó por SQL ni se modificó. |
| Render Staging | El propietario reporta el commit live d2dc0199ced9374c108090bbfcae5ee01dd676c3 y conexión al branch staging anterior. No tuve acceso al panel de Render para verificar esos ajustes independientemente. La referencia local origin/staging apunta a ese mismo SHA, y contiene el mismo blob de migración que el archivo local: 733c95d09bcc6ac6657798ae24ebec56d3c609af. |

SELECT ejecutado contra staging original y recovery branch, especificando proyecto, branch, endpoint y base en cada llamada. Ambos devolvieron base neondb y rol neondb_owner.

| Lectura | Staging original | Recovery branch |
|---|---:|---:|
| Migraciones EF | 35/36; última 20261022110000_EnforceNotificationIdempotency | 35/36; misma última migración |
| users / customers | 4 / 1 | 4 / 1 |
| orders / order_details | 1 / 1 | 1 / 1 |
| payments / Invoices | 1 / 1 | 1 / 1 |
| RefreshTokens / Notifications | 12 / 0 | 12 / 0 |
| EmployeeSchedules / EmailConfirmationCodes | 0 / 0 | 0 / 0 |
| public.employee_schedules | No existe | No existe |
| public."EmployeeSchedules" | Existe | Existe |
| customers.privacy_accepted_at_utc | No existe | No existe |

La rama recovery está lista y cuelga directamente de br-ancient-violet-b45js5mp. Neon informa que se creó el 2026-11-04 19:20:27 UTC desde parent_timestamp 19:16:17 UTC y parent_lsn 0/2129D28. A las 19:36:38 UTC, la lectura de staging original devolvió pg_current_wal_lsn() = 0/2151788. Por ello la rama sirve como punto histórico anterior a la migración, pero **no se acepta como recuperación final del estado actual**. Conteos iguales no descartan modificaciones de valores dentro de filas existentes. No se alteró ni eliminó esa rama.

El snapshot anterior snap-round-sun-b4vxzpi8, creado a las 15:40:33 UTC, también se conserva, pero es más antiguo y no reemplaza un punto fresco. No se creó ni restauró un snapshot en esta revisión.

## 2. SHA y SQL exacto

- Commit informado por el propietario como live en Render Staging: d2dc0199ced9374c108090bbfcae5ee01dd676c3. Coincide con la referencia local origin/staging observada; la pertenencia a Render es una confirmación del propietario, no una lectura independiente del Dashboard.
- El blob de Migrations/20261030124239_AuditCorrections20261030.cs en origin/staging coincide con el archivo local: 733c95d09bcc6ac6657798ae24ebec56d3c609af.
- GitHub check-runs consultados en modo lectura para d2dc0199ced9374c108090bbfcae5ee01dd676c3: de 8 entradas devueltas, la ejecución PR más reciente muestra GitGuardian Security Checks = failure (4 hallazgos de contraseña genérica en PasswordResetPostgresIntegrationTests.cs), security-gate = failure y production-release-gate = failure; verify y scan aparecen successful. No se copiaron valores sensibles. Las ejecuciones successful anteriores/duplicadas no sustituyen los checks fallidos de la ejecución reciente. Resolver/clasificar estos findings y obtener el estado de checks requerido antes de promoción/release.
- Artefacto a ejecutar: docs/evidence/sql/HU-12-AuditCorrections20261030-up.sql.
- SHA-256: 5083FBEC84B8E6367A7CC29EB36617C6F204B98257D75D5DC6E082825D18DB1A; 9.662 bytes.
- Revisión del archivo: un BEGIN y un COMMIT explícitos; sin ROLLBACK escrito en el archivo; 117 sentencias Up más los dos límites transaccionales. No hay DROP TABLE, TRUNCATE ni DELETE FROM. El archivo no establece lock_timeout, statement_timeout ni advisory lock.
- Efectos revisados previamente: renombra 5 tablas y 49 columnas, amplía 11 columnas numéricas a numeric(14,2), agrega 2 columnas de privacidad, crea/valida 7 CHECK, recrea 10 FK y 5 PK, y crea el índice único parcial de idempotencia.

El ensayo aislado previo pasó de 35 a 36 migraciones, conservó los conteos sintéticos y validó checks, claves e índice. No se repitió. El ensayo no mide duración ni contención de locks con el tráfico o volumen reales de staging.

La migración requiere locks DDL y no es compatible con instancias antiguas que esperan los nombres PascalCase mientras la nueva versión espera snake_case. El SQL es transaccional; un error antes del COMMIT debe revertir la transacción si la conexión se cierra. Una respuesta ambigua después del COMMIT no autoriza reenviarlo: primero se consulta el historial de la misma rama. No usar el método Down como recuperación; puede perder datos o fallar después de nuevas escrituras.

## 3. Mantenimiento y exclusión de escrituras

El backend registra estos workers dentro del proceso ASP.NET: ShiftReminderBackgroundService, KitchenLinkNotifierBackgroundService, RoleExpirationBackgroundService, WompiPendingPaymentsSyncService, ExpoPushNotificationQueue y EmailQueueProcessor. Activar Render Maintenance Mode bloquea tráfico público pero **no detiene el proceso ni esos workers**. Deben detenerse además los procesos que escriben.

Procedimiento staging-only, aún no ejecutado:

1. Acordar y anunciar la ventana. Pausar los clientes Web/Móvil de staging, bloquear nuevos despliegues/auto-deploy y confirmar que no hay otra instancia, preview, worker ni cron conectado a este branch.
2. Revisar Wompi: confirmar que el endpoint de staging usa Sandbox y que no se mezclan notificaciones de producción. Guardar en un registro privado las referencias de eventos/pagos pendientes para conciliarlos después, sin guardar secretos.
3. Habilitar Maintenance Mode solo en el Web Service de Backend Staging. Render responde HTTP 503 a todas las peticiones públicas; esto incluye POST /api/Payments/wompi/webhook. Esperar a que terminen las peticiones en curso. La documentación actual de Render dice que el servicio permanece activo en Maintenance Mode; por eso no basta para parar los workers. Como Render mantiene accesibilidad privada/SSH, confirmar y detener cualquier caller interno que pueda seguir escribiendo.
4. Después del drenaje, suspender temporalmente el servicio Backend Staging desde Render y comprobar que todas sus instancias se detuvieron. Pausar aparte cualquier servicio worker/cron que use la misma base. No suspender otro entorno ni ninguna base. La suspensión es reversible, pero es una acción de Render que no se realizó en esta preparación.
5. Con escritores detenidos, capturar baseline sin datos personales: 35 migraciones, conteos de tablas relevantes, checks, huérfanos, duplicados de idempotencia, y WAL. Crear un nuevo punto de recuperación desde el branch de staging y restaurarlo como una rama aislada. Verificar IDs de proyecto/branch, migraciones, conteos y restore antes de seguir. Conservar tanto el nuevo punto como br-wandering-darkness-b4fyyyao.
6. En Render Environment confirmar en privado que Database__ApplyMigrationsOnStartup esté ausente o false. El código tiene default false y .env.example también lo marca false, pero el valor activo de Render no está verificado. Si está true, detener el procedimiento; no activar ni cambiar esa variable en este paso.
7. Asegurar un único operador, usar conexión directa al endpoint Neon de staging (no pooler), y verificar target y preflight 35/36 justo antes de DDL. Aplicar solo el archivo con el hash fijado abajo. Ninguna instancia antigua debe conectarse mientras se renombra el esquema.
8. Si el preflight de datos, restore, SHA, rama o destino difiere, abortar antes de DDL. Si DDL falla antes del COMMIT, confirmar 35/36 y esquema anterior antes de decidir cualquier intento. Si hay timeout/respuesta ambigua, no reenviar: comprobar primero si el commit quedó en 35 o 36.

Wompi reintenta una notificación hasta 3 veces adicionales si no obtiene 2xx. El 503 de mantenimiento evita confirmar falsamente la recepción, pero la ventana debe ser breve: tras reabrir, revisar el debugger/eventos de Wompi y conciliar explícitamente pendientes, porque no se debe asumir que todos los eventos se reenviarán indefinidamente. Si staging no está aislado de producción o no hay forma de reconciliar los eventos, es NO-GO operativo.

## 4. Procedimiento de ejecución previsto — solo tras autorización

Conexión directa correspondiente al endpoint verificado de Neon: host ep-raspy-frost-b49icksh.c-6.us-east-2.aws.neon.tech, puerto 5432, base neondb, rol neondb_owner, SSL requerido. En PowerShell, almacenar esos parámetros en el entorno del proceso y dejar que psql solicite la contraseña de forma interactiva con -W; no poner una URL/contraseña en la línea de comando ni en el historial. No usar URL de Render, endpoint de recovery, pooler ni proyecto de producción.

    $env:PGHOST = 'ep-raspy-frost-b49icksh.c-6.us-east-2.aws.neon.tech'
    $env:PGPORT = '5432'
    $env:PGDATABASE = 'neondb'
    $env:PGUSER = 'neondb_owner'
    $env:PGSSLMODE = 'require'

Preflight (es de solo lectura). Ejecutar con los parámetros anteriores y -W; revisar manualmente todo el resultado y abortar si cualquier valor no coincide:

    psql -X -W -v ON_ERROR_STOP=1 -c 'SELECT current_database() AS db, current_user AS role, (SELECT count(*) FROM public."__EFMigrationsHistory") AS migration_count, (SELECT "MigrationId" FROM public."__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 1) AS latest, to_regclass(''public.employee_schedules'') AS new_schedules, to_regclass(''public."EmployeeSchedules"'') AS old_schedules, EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema=''public'' AND table_name=''customers'' AND column_name=''privacy_accepted_at_utc'') AS privacy_column_present'

Esperado antes de migrar: db=neondb, role=neondb_owner, migration_count=35, latest=20261022110000_EnforceNotificationIdempotency, new_schedules=NULL, old_schedules presente y privacy_column_present=false. También verificar visualmente que PGHOST sea el host exacto de staging escrito arriba.

Ejecución, no ejecutar ahora. El valor PGHOST/PGDATABASE/PGUSER/PGPORT/PGSSLMODE debe haberse fijado en esa misma PowerShell, y la contraseña se solicita sin eco. El advisory lock serializa migradores; lock_timeout/statement_timeout acotan esperas. Los -c/-f de psql se procesan secuencialmente en una sesión; el archivo mantiene sus BEGIN/COMMIT propios. No añadir --single-transaction porque el archivo ya controla su transacción explícitamente.

    $sql = 'C:\Users\SENA\Desktop\SaborExpress\SaborExpressBack\docs\evidence\sql\HU-12-AuditCorrections20261030-up.sql'
    psql -X -W -v ON_ERROR_STOP=1 -c "SET lock_timeout='10s'; SET statement_timeout='15min'; SELECT pg_advisory_lock(hashtextextended('SaborExpress:HU12:STAGING', 0));" -f $sql -c "SELECT pg_advisory_unlock(hashtextextended('SaborExpress:HU12:STAGING', 0));"
    if ($LASTEXITCODE -ne 0) { throw 'Migración fallida o indeterminada: mantener mantenimiento; no reintentar hasta consultar el historial en staging.' }

El bloqueo advisory solo protege contra otro operador que use la misma llave de aplicación; no reemplaza el apagado de API/workers ni evita escrituras de otras credenciales. Si se pierde la conexión, dejar el servicio en mantenimiento y comprobar 35/36 con conexión nueva antes de actuar.

## 5. Validación post-migración y reapertura controlada

Antes de admitir tráfico, ejecutar SELECT de solo lectura y exigir:

- Historial EF 36/36 y última migración 20261030124239_AuditCorrections20261030.
- Las 5 tablas snake_case presentes y las 5 PascalCase anteriores ausentes; 2 columnas privacy de clientes presentes.
- 7/7 CHECK nuevas validadas y cero violaciones; 10 FK recreadas validadas; total esperado 49/49 FK en public y 6/6 en neon_auth según el rehearsal; 5 PK y el índice único parcial de notifications.idempotency_key correctos.
- Las 11 columnas monetarias en numeric(14,2), conteos de orders, order_details, payments, invoices, users, refresh_tokens, customers, notifications, employee_schedules y email_confirmation_codes iguales al baseline, cero referencias huérfanas y cero duplicados no nulos de idempotency_key.

Solo después, reanudar el Backend de staging en el mismo SHA d2dc0199ced9374c108090bbfcae5ee01dd676c3, manteniendo migración automática apagada. Eliminar Maintenance Mode en una ventana controlada o limitar acceso a un tester; verificar /health HTTP 200 y /health/ready HTTP 200, login con cliente sintético de staging (sin registrar credenciales/tokens), logs sin 42703/42P01 y lectura/escritura del proceso de horarios con un registro sintético. Verificar que recordatorios, enlaces de cocina y expiración de roles no creen efectos externos duplicados. Después revisar y conciliar webhooks Wompi pendientes antes de volver a habilitar tráfico normal.

Si el esquema quedó en 36 pero el smoke falla, mantener clientes en pausa. No correr Down. La recuperación sería restaurar el nuevo punto pre-migración en una rama separada y, con autorización independiente, reconfigurar únicamente Render Staging. El recovery branch actual no debe usarse como garantía de los últimos datos porque su parent LSN quedó atrás del WAL actual.

## 6. Matriz final

| Control | Estado | Evidencia / bloqueo |
|---|---|---|
| Proyecto, branch, endpoint y DB target | PASS de lectura | Neon consultado explícitamente: odd-sound-44850362 / br-ancient-violet-b45js5mp / ep-raspy-frost-b49icksh / neondb. No se consultó la base real de producción. |
| Diagnóstico 35/36 y objetos faltantes | PASS de lectura | Ambas ramas revisadas conservan 35/36 y el esquema anterior; coincide con logs reportados. Login no se ha probado después del cambio. |
| Render SHA y DB branch | PASS según el propietario; parcial independiente | SHA d2dc... coincide con origin/staging leído y migration blob. No se accedió al panel de Render para revisar la conexión, la variable activa ni el despliegue live. |
| SQL y hash aprobado | PASS local | Hash exacto confirmado; no ejecutado contra staging original. |
| Ensayo aislado | PASS documentado | No repetido. No equivale a migración en staging. |
| Recovery branch actual | BLOCKED como punto final | Direct child y conteos iguales, pero parent LSN 0/2129D28 < current staging WAL 0/2151788. Crear y verificar un punto fresco tras congelar escritores. |
| Auto-migración apagada | BLOCKED | Verificar en Render que Database__ApplyMigrationsOnStartup está ausente o false. No activarlo. |
| Drenaje de API, workers, servicios y jobs | BLOCKED operativo | Maintenance Mode bloquea tráfico público pero deja proceso activo. Suspender el Web Service después del drenaje y comprobar workers/jobs separados. |
| Aislamiento/reconciliación Wompi | BLOCKED operativo | Confirmar sandbox y eventos pendientes; mantener 503 durante ventana breve, revisar reintentos/eventos después. |
| Build/CI del SHA live | BLOCKED | GitHub devolvió GitGuardian, security-gate y production-release-gate fallidos en la ejecución PR más reciente de d2dc...; verify y scan exitosos. GitGuardian reporta 4 hallazgos genéricos en PasswordResetPostgresIntegrationTests.cs. Clasificar/remediar y obtener checks requeridos verdes; no repetir suites locales en este preflight. |
| DDL en staging | NO EJECUTADO | Ninguna escritura realizada. |
| Producción | SIN CAMBIOS | Proyecto fancy-night-67370126 y branch br-lingering-forest-b78lblvh no se consultaron ni alteraron en esta revisión. |

## 7. Autorización exacta

No ejecutar ahora: falta actualizar/verificar el punto de recuperación tras freeze, confirmar el ajuste de auto-migración y acordar la suspensión de los procesos y la reconciliación Wompi. Por separado, la promoción/release del SHA d2dc... queda bloqueada por los checks de seguridad fallidos hasta que los cuatro findings se clasifiquen/remedien y el gate requerido quede verde. Esto no cambia el hash del SQL, pero tampoco permite declarar CI aprobado.

Cuando los bloqueos anteriores tengan evidencia y el preflight final vuelva a mostrar 35/36 en el mismo target, la autorización debe ser explícita y limitarse a esta acción:

> Autorizo ejecutar una sola vez el archivo HU-12-AuditCorrections20261030-up.sql con SHA-256 5083FBEC84B8E6367A7CC29EB36617C6F204B98257D75D5DC6E082825D18DB1A, exclusivamente en Neon proyecto odd-sound-44850362, branch br-ancient-violet-b45js5mp, endpoint ep-raspy-frost-b49icksh y base neondb, después de confirmar el freeze de staging y verificar un punto de recuperación restaurable creado desde el estado congelado. No autorizo cambios en producción.

La aprobación de staging no autoriza migrar fancy-night-67370126, cambiar variables/credenciales de Render, desplegar otra versión, ni ejecutar merges. El plan de producción será separado y requerirá autorización independiente después del PASS completo de staging.

## Referencias

- Ensayo y auditoría ya existentes: HU-12-AUDITORIA-MIGRACION-2026-11-04.md.
- SQL revisado: sql/HU-12-AuditCorrections20261030-up.sql.
- Procedimiento de smoke del repositorio: ../release/HU-STG-02-smoke-migraciones.md.
- [Render Maintenance Mode](https://render.com/docs/maintenance-mode): todos los paths públicos responden 503, pero el servicio sigue activo.
- [Wompi eventos](https://docs.wompi.co/docs/colombia/eventos-pagos-a-terceros/): Wompi reintenta hasta 3 veces adicionales si el endpoint no devuelve 2xx.
- [PostgreSQL psql](https://www.postgresql.org/docs/current/app-psql.html): las opciones -c y -f se procesan en secuencia; no envolver de nuevo una migración que ya contiene BEGIN/COMMIT.
- [Checks del commit d2dc0199](https://github.com/yersonct/SaborExpressBack/commit/d2dc0199ced9374c108090bbfcae5ee01dd676c3/checks): estado remoto consultado para documentar el bloqueo de CI, sin abrir ni copiar anotaciones con valores detectados.


## Addendum final de preflight — seguimiento del 2026-11-04

Esta nota actualiza el estado operativo descrito arriba; conserva el registro de las verificaciones anteriores. Solo se consultaron metadatos de Neon y el artefacto SQL local. No se abrió una conexión SQL, no se ejecutó SQL, no se modificaron Render ni Neon y no se consultó ni tocó producción.

### Identidad, recuperación y estado reportado

- La consulta de metadatos de Neon confirma el proyecto odd-sound-44850362 y el branch objetivo br-ancient-violet-b45js5mp. Neon lo etiqueta production y lo marca primary/default; para SaborExpress corresponde al branch de STAGING según la asignación explícita del propietario. Su endpoint directo actual es ep-raspy-frost-b49icksh, asociado a ese branch. El compute figuró idle/suspendido al consultar metadatos.
- La rama de recuperación vigente es br-steep-sun-b46ajpoe. Neon la muestra ready, hija de br-ancient-violet-b45js5mp, creada a las 21:06:10 UTC desde el punto de las 21:06:08 UTC. Su endpoint ep-sweet-tooth-b48zyj2y figuró idle/suspendido. Los conteos principales coincidentes, 35/36 migraciones y cero conexiones son datos confirmados por el propietario; no se repitieron consultas SQL.
- Render Staging suspendido y ausencia de pagos Wompi Sandbox en proceso son confirmaciones del propietario; no se accedió a esos paneles. No hay evidencia de que la rama de recuperación vieja br-wandering-darkness-b4fyyyao sea el respaldo vigente; queda preservada, pero la nueva es br-steep-sun-b46ajpoe.
- Producción real sigue siendo otro proyecto: fancy-night-67370126. No se consultó ni modificó.

### Artefacto e integridad de ejecución

- Archivo: docs/evidence/sql/HU-12-AuditCorrections20261030-up.sql.
- SHA-256 comprobado localmente: 5083FBEC84B8E6367A7CC29EB36617C6F204B98257D75D5DC6E082825D18DB1A; coincide exactamente con el hash aprobado. Tamaño: 9,662 bytes.
- Contiene START TRANSACTION y COMMIT explícitos, 117 sentencias de migración y no contiene DROP TABLE, DELETE FROM, TRUNCATE ni un ROLLBACK explícito. No incluye lock_timeout, statement_timeout ni advisory lock; esos controles están en el procedimiento psql ya preparado en la sección 4.
- La invocación documentada fija lock_timeout a 10 segundos y statement_timeout a 15 minutos en esa sesión; el timeout de sentencia se aplica a cada sentencia, no es un límite total para todo el archivo. Adquiere un advisory lock de sesión antes de ejecutar el archivo y lo libera después. ON_ERROR_STOP detiene psql ante error. Si la conexión termina antes de COMMIT, PostgreSQL revierte la transacción abierta y libera el advisory lock al cerrar la sesión. El advisory lock solo coordina operadores que usan la misma llave: no sustituye mantener API, workers y demás escritores detenidos.
- Mantener el SQL sin cambios para conservar el hash. No añadir --single-transaction, porque el archivo ya controla BEGIN/COMMIT. Ante resultado ambiguo o timeout, no volver a ejecutarlo: consultar primero, en modo lectura y en el target exacto, si el historial sigue en 35 o llegó a 36. Si está en 36 pero falla el smoke, mantener Render suspendido y no usar Down; cualquier recuperación debe partir de la copia conservada y requerir aprobación separada.

### Veredicto actualizado

GO CONDICIONADO para la operación de staging: la identidad del target y del endpoint quedó confirmada por metadatos de Neon; existe una rama de recuperación fresca y lista, y el hash del SQL coincide. La migración no se ejecutó y esta nota no autoriza ejecutarla.

Antes de ejecutar se debe obtener autorización explícita y, en esa misma ventana, volver a comprobar solo los controles sensibles al tiempo: target project/branch/endpoint/database, 35/36, cero clientes conectados, branch de recuperación todavía listo, Render/otros escritores detenidos y ausencia de eventos Wompi pendientes. Ejecutar únicamente con la invocación psql protegida de la sección 4 y el archivo cuyo SHA consta arriba.

Bloqueos para reactivar o promover el backend, separados de la ejecución SQL: el estado actual de Database__ApplyMigrationsOnStartup en Render no fue verificado y debe confirmarse ausente o false antes de reanudar; el informe anterior registra GitGuardian, security-gate y production-release-gate fallidos en el SHA d2dc0199. La corrección local de las contraseñas de prueba no se ha publicado y no aporta todavía nuevos checks remotos. No reanudar ni promover hasta cerrar estos controles.

### Autorización que se necesitaría para ejecutar

Para autorizar una ejecución única y limitada, confirma expresamente:

> Autorizo ejecutar una sola vez HU-12-AuditCorrections20261030-up.sql con SHA-256 5083FBEC84B8E6367A7CC29EB36617C6F204B98257D75D5DC6E082825D18DB1A, exclusivamente en el proyecto Neon odd-sound-44850362, branch br-ancient-violet-b45js5mp, endpoint ep-raspy-frost-b49icksh y base neondb, usando los timeouts y advisory lock documentados, solo tras superar el preflight inmediato de 35/36 y cero escritores. No autorizo cambios en producción.

## Addendum de ejecución — bloqueo por conexión cliente (2026-11-04)

**Resultado: NO-GO; la migración no se ejecutó.** El guard `ON_ERROR_STOP` abortó antes de abrir el archivo de migración. El historial permanece en 35/36 (`20261022110000_EnforceNotificationIdempotency`). La sesión de comprobación terminó y el advisory lock de sesión se liberó al cerrar la conexión.

- Hash local del SQL: `5083FBEC84B8E6367A7CC29EB36617C6F204B98257D75D5DC6E082825D18DB1A` (coincide).
- Destino confirmado por metadatos y lectura: proyecto `odd-sound-44850362`, branch `br-ancient-violet-b45js5mp`, endpoint directo `ep-raspy-frost-b49icksh`, base `neondb`. La rama de recuperación `br-steep-sun-b46ajpoe` continúa `ready` y derivada de staging.
- En la conexión `psql` de preflight se observó un cliente adicional que no cumplía la excepción: `application_name=pgbouncer`, `client_addr=127.0.0.1/32`, `state=idle`, `backend_type=client backend`, rol `neondb_owner`, sin transacción abierta. El guard registró `compute_ctl=0`, `unexpected=1` y abortó antes de `-f HU-12-AuditCorrections20261030-up.sql`.
- Una consulta de solo lectura posterior confirmó 35 migraciones, cero clientes adicionales, cero transacciones abiertas y cero transacciones preparadas. Por su carácter transitorio, la sesión `pgbouncer` no persistía al hacer esa consulta.
- El propietario había observado previamente `compute_ctl` en `127.0.0.1`, `idle`. El intento que falló anteriormente solo guardó un conteo de clientes, no PID ni `application_name`; por eso no se puede asociar retrospectivamente aquella fila a `compute_ctl`. En el nuevo preflight sí se probó una excepción precisa: como máximo una sesión, únicamente `application_name=compute_ctl`, `client_addr=127.0.0.1`, `state=idle`, sin transacción. Cualquier otro cliente, estado, dirección o transacción aborta. Esa excepción no coincide con la fila `pgbouncer` observada y no se aplicó a ella.
- La referencia del código de Neon explica que `compute_ctl` y otros monitores consultan estadísticas de PostgreSQL ([`compute_tools/src/monitor.rs`](https://github.com/neondatabase/neon/blob/main/compute_tools/src/monitor.rs)); esto respalda la clasificación del nombre de proceso, pero no identifica el PID de una sesión histórica ni justifica ignorar `pgbouncer`.
- Render Staging permanece suspendido según la captura reportada por el propietario. Producción no se consultó ni modificó. No se cambiaron credenciales, respaldos o variables; no hubo commit, push ni merge.

**Bloqueo para otra ejecución:** obtener un preflight directo sin clientes desconocidos. La excepción queda limitada a la sesión `compute_ctl` descrita; `pgbouncer` sigue siendo bloqueante bajo el criterio autorizado. No repetir el SQL mientras el guard detecte cualquier cliente no permitido. Si el proveedor requiere que una sesión `pgbouncer` permanezca conectada, se necesita una nueva decisión operativa que la identifique y autorice expresamente antes de cambiar el guard.

## Addendum — espera acotada de conexiones y estado actual (2026-11-04)

Se preparó `sql/HU-12-staging-quiescence-preflight.sql` como guard de solo lectura para usar en la misma sesión `psql` inmediatamente antes del SQL aprobado. No se añadió ninguna excepción por `application_name`, dirección IP, rol o estado idle. Cada conexión cliente adicional —incluido `pgbouncer`— bloquea y aborta inmediatamente; no se espera a que desaparezca. El guard exige dos lecturas consecutivas de cero clientes, separadas por una espera de un segundo, con límite estricto de 30 segundos; al vencer, falla cerrado. Durante esa ventana, el ciclo solo consulta `pg_stat_activity` y duerme. Verifica antes de iniciar el ciclo el destino lógico, la historia 35/36, el esquema pre-migración, la ausencia de transacciones preparadas y que el rol pueda inspeccionar la actividad completa.

La migración autorizada no se modificó. Su SHA-256 local se volvió a comprobar y coincide con el valor autorizado: `5083FBEC84B8E6367A7CC29EB36617C6F204B98257D75D5DC6E082825D18DB1A`. `git diff --check` no reportó errores. No se ejecutó el guard ni se abrió una conexión SQL en esta intervención.

### Controles con evidencia disponible

| Control | Resultado de esta intervención |
|---|---|
| Proyecto/branch Neon objetivo | **PASS por metadatos de control plane:** proyecto `odd-sound-44850362`, branch `br-ancient-violet-b45js5mp`, endpoint lógico staging; el branch aparece con nombre Neon `production`, según el mapeo operativo documentado. No se conectó a producción real. |
| Rama de recuperación | **Parcial:** `br-steep-sun-b46ajpoe` está `ready` y declara como padre a la rama objetivo, con punto padre `2026-11-04T21:06:08Z`. La metadata de staging tiene `updated_at=2026-11-04T23:31:31Z` y el tamaño lógico informado difiere del de la copia; sin comparación de datos actual no se acredita que ese punto incluya el estado más reciente. |
| SHA del SQL autorizado | **PASS:** coincidencia local exacta, sin imprimir ni manejar credenciales. |
| Espera estricta de dos comprobaciones limpias | **Preparada, no ejecutada:** estática y fail-closed; ninguna conexión adicional se exceptúa. |
| Migraciones 35/36 y cero clientes actuales | **No revalidados en esta intervención:** la última cifra 35/36 procede de evidencia anterior del propietario. No se abrió una sesión adicional porque los controles externos obligatorios no podían confirmarse. |
| Render Staging suspendido y demás escritores detenidos | **BLOCKED:** no hay conector/lectura de Render disponible en esta sesión. La evidencia previa del propietario no acredita el estado actual ni descarta otros clientes o webhooks. |
| Respaldo reciente con datos actuales | **BLOCKED:** la rama de recuperación está lista, pero el punto padre es anterior a la última metadata `updated_at` del objetivo; falta comprobar que los datos actuales siguen coincidiendo o crear un punto nuevo bajo el freeze. |
| Migración / producción | **No ejecutada / intacta.** |

### Procedimiento preparado para una ventana aprobada

Antes de abrir la única sesión de `psql`, verificar en Render que el servicio y todos sus workers están suspendidos, confirmar que webhooks/jobs no pueden escribir y confirmar en Neon que el respaldo restaurable corresponde al estado congelado. Preparar, sin mostrar ni guardar la contraseña, `PGHOST=ep-raspy-frost-b49icksh.c-6.us-east-2.aws.neon.tech`, `PGPORT=5432`, `PGDATABASE=neondb`, `PGUSER=neondb_owner` y `PGSSLMODE=require`. Verificar `PGHOST`, el hash autorizado y la metadata de proyecto/branch. Después, ejecutar todo en una sola sesión; el guard de conexiones debe ir inmediatamente antes del SQL:

```powershell
$hu12Sql = 'C:\Users\SENA\Desktop\SaborExpress\SaborExpressBack\docs\evidence\sql\HU-12-AuditCorrections20261030-up.sql'
$hu12Guard = 'C:\Users\SENA\Desktop\SaborExpress\SaborExpressBack\docs\evidence\sql\HU-12-staging-quiescence-preflight.sql'
$hu12ExpectedSha = '5083FBEC84B8E6367A7CC29EB36617C6F204B98257D75D5DC6E082825D18DB1A'
if ($env:PGHOST -ne 'ep-raspy-frost-b49icksh.c-6.us-east-2.aws.neon.tech' -or $env:PGPORT -ne '5432' -or $env:PGDATABASE -ne 'neondb' -or $env:PGUSER -ne 'neondb_owner' -or $env:PGSSLMODE -ne 'require') { throw 'HU-12 destino incorrecto; abortado.' }
if ((Get-FileHash -LiteralPath $hu12Sql -Algorithm SHA256).Hash -ne $hu12ExpectedSha) { throw 'HU-12 SHA incorrecto; abortado.' }
psql -X -W -v ON_ERROR_STOP=1 -c "SET lock_timeout='10s'" -c "SET statement_timeout='30s'" -c "SELECT pg_advisory_lock(hashtextextended('SaborExpress:HU12:STAGING', 0))" -f $hu12Guard -c "SET statement_timeout='15min'" -f $hu12Sql -c "SELECT pg_advisory_unlock(hashtextextended('SaborExpress:HU12:STAGING', 0))"
if ($LASTEXITCODE -ne 0) { throw 'HU-12 abortada o resultado indeterminado: mantener el freeze y consultar primero el historial EF; no reenviar el SQL.' }
```

La sesión aplica 30 segundos como `statement_timeout` al guard completo; tras aprobarlo lo restablece a 15 minutos para el archivo de migración. Un resultado distinto de dos comprobaciones limpias termina `psql` antes de leer el archivo de migración. Si la salida de ejecución fuera ambigua, no repetir; abrir una conexión de solo lectura únicamente después, comprobar primero `__EFMigrationsHistory` y decidir con esa evidencia.

**Veredicto actual: NO-GO.** Aunque el guard acotado está preparado y el hash coincide, no se puede acreditar en vivo que Render siga suspendido ni que no haya otros procesos escritores; además, el punto de recuperación requiere confirmar actualidad frente al objetivo. No se inició el preflight final ni se usó la autorización de escritura. Staging debe permanecer suspendido y producción no fue tocada.

## Addendum — continuación solicitada (2026-11-04)

- El propietario declara que sus capturas actuales de Render muestran `saborexpressback-staging` suspendido y el backend de producción como servicio independiente. Las capturas no se adjuntaron en este turno; no existe conexión de Render disponible en esta sesión para inspeccionar servicios, workers o tareas programadas en vivo.
- Metadatos de Neon, lectura de control plane: proyecto `odd-sound-44850362`; la rama destino `br-ancient-violet-b45js5mp` está `ready` y lleva el nombre de Neon `production`; la recuperación `br-steep-sun-b46ajpoe` está `ready` y tiene como padre la rama destino. No se repitió la comparación de tablas/filas; se conserva como evidencia aportada previamente por el propietario (34/34 tablas y 140/140 registros con huellas coincidentes).
- Neon informa lista vacía de funciones y de triggers gestionados en la rama consultada. Esto no descarta otros servicios de Render, tareas externas, webhooks ni clientes con acceso directo a la cadena de conexión.
- La API de control de Neon lista un único endpoint read/write para esta rama, `ep-raspy-frost-b49icksh`, con estado `idle`/suspendido; no lista otro compute en esa rama. Esto tampoco identifica servicios externos que puedan conectarse después.
- El SHA-256 local del SQL autorizado vuelve a coincidir exactamente con el autorizado. El archivo de guard existe y no contiene excepciones de conexión.
- No hay `psql` disponible en el entorno y no hay variables `PGHOST`, `PGPORT`, `PGDATABASE`, `PGUSER` o `PGSSLMODE` configuradas. Por ello no se puede ejecutar el guard inmediato de una sola sesión ni revalidar SQL 35/36 sin cambiar de procedimiento o abrir otra conexión de diagnóstico. No se abrió ninguna conexión SQL.
- No hubo ejecución, commit, push, merge, cambio en Render ni acceso a producción. La cifra 35/36 y cero conexiones se conserva como evidencia reciente aportada por el propietario, no como lectura de este agente.

**Resultado de esta continuación: NO-GO.** Causas concretas: no se puede completar el inventario actual de procesos externos escritores y falta el cliente/conexión directa para correr el guard y la migración en la misma sesión segura. La autorización previa no se consumió.

## Addendum — clasificación de sesiones y guard candidato (2026-11-04)

Se ejecutaron únicamente consultas `SELECT` en `neondb`, proyecto `odd-sound-44850362`, rama `br-ancient-violet-b45js5mp`, con rol de lectura de control. Ninguna consulta cambió datos ni lanzó la migración.

| Comprobación de solo lectura | Resultado observado |
|---|---|
| Historial EF | 35 migraciones; última `20261022110000_EnforceNotificationIdempotency`. |
| Tablas públicas | 34. |
| Clientes en `neondb`, excluyendo el PID de la consulta | 1: rol `neon_auth`, `application_name=pgbouncer`, loopback `127.0.0.1`, `idle`, sin `xact_start`, `backend_xid` ni `backend_xmin`. |
| Identidad/propiedades de Neon Auth | `usesysid` de la sesión coincide con el OID del rol; `neon_auth` es dueño del esquema `neon_auth`; no es superusuario; no puede crear en `public`; la cadena recursiva de membresías no llega a superusuario ni a privilegios `INSERT`, `UPDATE`, `DELETE` o `TRUNCATE` sobre las 34 tablas, incluyendo privilegios de columna. |
| Funciones `SECURITY DEFINER` ejecutables por Neon Auth en esquemas de usuario | Ninguna en la consulta de solo lectura. |
| SHA del SQL de migración | No se modificó el archivo; hash local recalculado al cierre: coincide exactamente con el autorizado. |

El propietario había informado cuatro conexiones `cloud_admin` en la base `postgres`. El guard ahora filtra `pg_stat_activity.datname = current_database()` después de verificar que `current_database()` sea `neondb`; por eso no confunde sesiones de supervisión conectadas a otra base con escritores de las tablas SaborExpress.

### Cambio exacto propuesto en el guard

El archivo [HU-12-staging-quiescence-preflight.sql](sql/HU-12-staging-quiescence-preflight.sql) quedó actualizado localmente como **candidato no ejecutado**:

1. Cuenta sesiones cliente solo en la base validada `neondb`, excluye el PID de `psql` y mantiene el rechazo para cualquier conexión no clasificada.
2. Permite como máximo una sesión adicional, y solo si coinciden el `usesysid` del rol Neon Auth existente, esquema `neon_auth` propiedad de ese rol, `application_name=pgbouncer`, dirección loopback, `backend_type=client backend`, `state=idle`, sin transacción ni `backend_xid`/`backend_xmin`.
3. Comprueba la cadena recursiva de membresías del rol; rechaza superusuario, `CREATE` en `public`, DML/TRUNCATE a nivel tabla o columna en cualquiera de las 34 tablas y funciones `SECURITY DEFINER` ejecutables en esquemas de usuario.
4. Conserva la historia 35/36, el esquema pre-migración, la visibilidad completa de sesiones, cero transacciones preparadas, límite de 30 segundos y dos observaciones limpias consecutivas. Si hay más sesiones, una sesión desconocida o la sesión Auth deja de cumplir cualquier condición, aborta.
5. No se modificó `HU-12-AuditCorrections20261030-up.sql` ni el procedimiento externo de SHA/destino/respaldo/timeouts/advisory lock.

**Capacidad de clasificación:** con las ACL y la sesión observadas, el candidato distingue las cuatro sesiones de supervisión en otra base, admite la única sesión `neon_auth` probada como inactiva y sin vías de escritura comprobadas, y sigue bloqueando otros clientes y escritores. Esto no sustituye el freeze operativo de Render ni el uso del guard y el SQL en una única sesión directa.

**No ejecutado:** el guard candidato no fue ejecutado y no se aplicó migración. Esta edición del criterio queda pendiente de autorización explícita antes de utilizarse. No se tocó producción, Render, credenciales ni ramas Git.

## Addendum — ejecutor enlazado al guard corregido (2026-11-04)

- `sql/EJECUTAR-HU12-STAGING.ps1` ahora resuelve sus archivos desde `$PSScriptRoot`, apunta exclusivamente a `sql/HU-12-staging-quiescence-preflight.sql` y monta el directorio SQL como solo lectura en el contenedor temporal `postgres:16-alpine` bajo `/hu12`.
- La invocación `-PreflightOnly` crea una ruta separada de `psql`: fija `lock_timeout=10s` y `statement_timeout=30s`, adquiere el advisory lock, carga solo `/hu12/HU-12-staging-quiescence-preflight.sql` y libera el lock. No pasa a `psql` el archivo de migración ni el postcheck. La ruta de migración solo añade esos dos archivos después de que el guard termina correctamente.
- La revisión estática del guard pasó: los dos sondeos de `pg_stat_activity` filtran `datname = current_database()`; la excepción única exige rol/OID Neon Auth verificado, propiedad del esquema, `pgbouncer` en loopback, estado idle, sin transacción/XID/XMIN y ausencia de privilegios DML/TRUNCATE, CREATE, superusuario y ejecución de funciones `SECURITY DEFINER`. Clientes adicionales o identidad/estado distinto abortan. Se conservan el estado 35/36, esquema previo, 34 tablas, visibilidad de actividad, transacciones preparadas, espera de dos observaciones y timeout.
- El parser de PowerShell no reportó errores. La ruta `-PreflightOnly` se ejercitó con una función Docker simulada: el comando resultante incluyó el guard corregido y no incluyó el SQL de migración ni el postcheck. Esta prueba valida el cableado y el aislamiento del modo; **no** ejecutó `psql` contra Neon ni acredita el preflight vivo.
- El SHA-256 del archivo de migración se mantiene en `5083FBEC84B8E6367A7CC29EB36617C6F204B98257D75D5DC6E082825D18DB1A`. No se ejecutó migración, se abrió conexión a la base, ni se modificó Render, producción o credenciales.
- La ejecución directa del `.ps1` en esta sesión de Windows quedó bloqueada por la política local de ejecución de scripts; además, aunque Docker CLI está instalado, el daemon no está disponible. Para ejecutar después solo el preflight en una PowerShell interactiva, la política puede habilitarse solo para el proceso y la ventana debe cerrarse al terminar: `Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass`; luego `& '.\docs\evidence\sql\EJECUTAR-HU12-STAGING.ps1' -PreflightOnly`. El script solicita el marcador `PREFLIGHT-HU12-STAGING` y la contraseña de Neon; no se almacena la contraseña.

**Resultado de integración:** PASS para el cableado local y el modo de prueba solo preflight. Antes de autorizar cualquier migración siguen siendo obligatorias la ejecución real de `-PreflightOnly` y las verificaciones operativas externas sensibles al tiempo. La migración no se ejecutó.

## Addendum — diagnóstico de conexiones del guard (2026-11-04)

- El guard ahora emite un `NOTICE` de diagnóstico si hay más de un cliente adicional o si la única conexión no cumple la excepción Neon Auth. Incluye únicamente PID, usuario, aplicación, base, estado, dirección, indicador de transacción abierta y clasificación del candidato Neon Auth. Normaliza caracteres de control, limita longitudes y reemplaza nombres de aplicación que parezcan contener secretos. No selecciona ni imprime `pg_stat_activity.query`, contraseñas ni secretos. No amplía excepciones ni termina sesiones.
- El ejecutor sigue apuntando a este mismo guard actualizado. La migración no se modificó; el hash local sigue siendo `5083FBEC84B8E6367A7CC29EB36617C6F204B98257D75D5DC6E082825D18DB1A`.
- Se intentó `EJECUTAR-HU12-STAGING.ps1 -PreflightOnly` con política de ejecución limitada al proceso. El script se detuvo antes de conectar porque el entorno sandbox denegó acceso a `C:\Users\SENA\.docker\config.json`; por tanto, el contenedor y el modo del ejecutor no alcanzaron `psql`.
- Como diagnóstico de solo lectura, se consultó después la rama exacta de staging. En esa observación había un único cliente adicional: PID 692, usuario `neon_auth`, aplicación `pgbouncer`, base `neondb`, estado `idle`, dirección `127.0.0.1/32`, sin transacción abierta. No se observaron otros clientes bloqueantes. La sentencia `DO` del guard actualizado (incluidos los cambios de diagnóstico y redacción) se ejecutó otra vez como consulta de solo lectura mediante Neon y terminó sin error; esto confirma que el estado observado satisfizo los controles del guard en ese instante. Esa ejecución no usó el contenedor, el advisory lock ni los timeouts de la ruta PowerShell.
- Las conexiones múltiples de la ejecución anterior no pueden identificarse retroactivamente: aquella versión abortó sin enumerarlas y ya no estaban en la observación actual. Si reaparecen, la nueva salida del guard las identificará sin exponer consultas SQL.
- No se ejecutó `-f HU-12-AuditCorrections20261030-up.sql`; no hubo escrituras de migración, cambios en producción/Render ni terminación de sesiones.

**Resultado:** PASS para el diagnóstico del guard actualizado en la observación actual; **pendiente** validar el modo `-PreflightOnly` del ejecutor porque Docker no pudo leer su configuración local. El bloqueo anterior no se reprodujo y no hay una conexión desconocida actual que resolver.

## Addendum — intento del ejecutor solicitado por el propietario (2026-11-05)

- Se recalculó el SHA-256 del SQL local; coincidió con el SHA autorizado.
- Se intentó iniciar `EJECUTAR-HU12-STAGING.ps1` en modo de migración según la autorización previa. El proceso se detuvo en `docker info`, antes de mostrar el destino, solicitar el marcador o abrir `psql`, porque el sandbox no pudo leer `C:\Users\SENA\.docker\config.json`.
- No se abrió conexión a Neon, no se ejecutó el guard ni el SQL de migración, y no se modificaron Staging, Producción, Render o credenciales. El estado 35/36 no se reconsultó en este intento.
- El siguiente paso requiere ejecutar el script desde una PowerShell local que sí tenga acceso a Docker Desktop; introducir allí el marcador solicitado y la contraseña de Neon, sin compartirla en el chat. Si el guard devuelve NO-GO, detenerse y enviar únicamente su diagnóstico redactado.

**Resultado del intento:** NO-GO local por acceso del sandbox al archivo de configuración de Docker; no hubo operación de base de datos.
