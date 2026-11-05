# HU-CIERRE-12 â€” AuditorÃ­a de `AuditCorrections20261030`

**Fecha:** 2026-11-04
**Repositorio:** `yersonct/SaborExpressBack`
**Rama de trabajo local:** `audit/hu-12-pending-migration`
**SHA auditado:** `caa4f0336a4f96f529c5da10d37128e381822b9c` (base `origin/develop`)
**MigraciÃ³n:** `20261030124239_AuditCorrections20261030`
**Veredicto para staging:** **NO-GO por ahora**. La migraciÃ³n fue aplicada y restaurada en PostgreSQL desechable local, pero falta un build reproducible del SHA auditado y todavÃ­a no existe backup/restore probado de staging ni preflight independiente de esa base. No se consultÃ³ ni modificÃ³ staging o producciÃ³n.

## Resumen de resultados

| Ãrea | Estado | Evidencia / lÃ­mite |
|---|---|---|
| RevisiÃ³n de migraciÃ³n y esquema EF | PASS con salvedad | ComparaciÃ³n del esquema anterior (`20261022110000_EnforceNotificationIdempotency`) con el snapshot/modelo posterior. EF informÃ³ â€œNo changes have been made to the model since the last migrationâ€ usando el ensamblado local preexistente; no sustituye un build fresco del SHA auditado. |
| SQL `Up` generado y revisado | PASS | Script adjunto, SHA-256 `5083FBEC84B8E6367A7CC29EB36617C6F204B98257D75D5DC6E082825D18DB1A`, 9,662 bytes. Un `START TRANSACTION`/`COMMIT`; no borra tablas/columnas ni contiene `UPDATE`/`DELETE`. |
| MigraciÃ³n en PostgreSQL local | PASS | PostgreSQL 16.15, contenedor local `saborexpress-hu12-pgtest`, destino verificado `127.0.0.1:55433`, base/rol `hu12_test`. Historial pasÃ³ de 35 a 36 migraciones. |
| Fallo inducido y rollback | PASS | Un producto sintÃ©tico con precio `-1` hizo fallar la nueva restricciÃ³n `ck_products_price` (SQLSTATE `23514`). La transacciÃ³n revirtiÃ³; el historial siguiÃ³ en 35 y se conservaron tabla, filas y nombre anteriores. Eliminado ese Ãºnico dato sintÃ©tico, el segundo intento terminÃ³ con 36/36. |
| Integridad posterior | PASS | 7 checks aÃ±adidos validados; 49 FK validadas; Ã­ndice Ãºnico filtrado de idempotencia presente; 11 columnas monetarias en `numeric(14,2)`; las 2 columnas nuevas de privacidad existen. |
| ConservaciÃ³n de datos sintÃ©ticos | PASS | Una fila antes y despuÃ©s en cada una de las tablas verificadas: pedidos, detalles, pagos, facturas, notificaciones, refresh tokens, empleados y clientes. Las FK aplicadas tambiÃ©n validan las referencias del resto del esquema. |
| Backup y restore local | PASS | Dump custom de 106,696 bytes; `pg_restore` a una base nueva `hu12_restore`; 36 migraciones y las filas sintÃ©ticas verificadas. Backup 0.33 s, restore 0.92 s en este contenedor pequeÃ±o; no son estimaciones de staging. SHA-256 del dump: `8c27a2997fc1c5691b513b560fa6b67751673b10e4ba2c1e3a985ba8252c572f`. |
| Smoke de aplicaciÃ³n | PASS limitado | El binario preexistente local (`SaborExpress.dll`, SHA-256 `A4F8CEE68F94328291DDD5112609A06816831118B34699B826C9251AEB5B64B8`) se iniciÃ³ con configuraciÃ³n sintÃ©tica y esa base local: `/health` 200; `/health/ready` 200 con `database=connected`, `migrations=applied`; `GET /api/Categories` 200 y leyÃ³ la fila sintÃ©tica. No demuestra que el cÃ³digo fuente actual compile ni cubre flujos funcionales completos. |
| Build fresco del SHA auditado | BLOCKED | `dotnet build .\SaborExpress.csproj --no-restore` terminÃ³ con 4 errores `NU1301`: no se pudo obtener la firma de `https://api.nuget.org/v3-index/repository-signatures/5.0.0/index.json` por fallo SSL/autenticaciÃ³n del paquete de seguridad. No es un error de compilaciÃ³n C# demostrado. |
| Suites automatizadas del repositorio | BLOCKED / no ejecutadas | No se ejecutaron con esta fuente: el build actual no puede resolver/verificar paquetes NuGet. Deben ejecutarse desde CI o un entorno con NuGet operativo antes de promover. |
| Staging/producciÃ³n | NO EJECUTADO | No se conectÃ³ a Neon ni se hicieron backups, consultas, cambios de variables, migraciones o despliegues en bases alojadas. El reporte de cero infracciones de CHECK en esas bases es informaciÃ³n aportada por el propietario, no una lectura realizada para esta auditorÃ­a. |

Los tiempos corresponden solo al `pg_dump`/`pg_restore` del dataset sintÃ©tico pequeÃ±o. El tiempo exacto de aplicaciÃ³n de la migraciÃ³n no se capturÃ³ en la ejecuciÃ³n original; no se estima aquÃ­.

## Alcance y destino de prueba

Se creÃ³ una instancia desechable independiente para esta validaciÃ³n. Antes de aplicar EF, se comprobÃ³ su identidad: `current_database() = hu12_test`, `current_user = hu12_test`, conexiÃ³n local `127.0.0.1:55433` (puerto interno del contenedor 5432), PostgreSQL 16.15. La cadena usada no contiene contraseÃ±a. Ninguna operaciÃ³n apuntÃ³ a Neon, Render, staging o producciÃ³n.

El esquema inicial se creÃ³ ejecutando las migraciones hasta `20261022110000_EnforceNotificationIdempotency`; el historial tenÃ­a 35 entradas. Se insertaron filas sintÃ©ticas relacionadas, con identificadores de prueba 990001â€“990019 y correos bajo `.invalid`. No se copiaron datos personales ni credenciales reales.

Al terminar, el contenedor local quedÃ³ detenido para cerrar el puerto. No se borrÃ³; contiene solo las bases y filas sintÃ©ticas de esta prueba, incluido el dump local bajo `/tmp/hu12-test.dump`. No se incluyÃ³ un backup de base en el repositorio.

## AuditorÃ­a del cambio y comparaciÃ³n del modelo

Conteo inspeccionado en `Up`:

| OperaciÃ³n | Cantidad | ObservaciÃ³n |
|---|---:|---|
| Renombres de tablas | 5 | `Notifications`, `Invoices`, `RefreshTokens`, `EmployeeSchedules`, `EmailConfirmationCodes` a sus nombres `snake_case`. |
| Renombres de columnas | 49 | Renombra propiedades y claves a `snake_case`; se inspeccionaron en especial columnas FK, importes y `Notifications.IdempotencyKey`. |
| Alteraciones numÃ©ricas | 11 | AmplÃ­a importes de precisiÃ³n 10,2/12,2 a 14,2. No estrecha valores en `Up`. |
| Columnas nuevas | 2 | `customers.privacy_accepted_at_utc` y `customers.privacy_policy_version`, ambas nullable y sin valor por defecto. |
| Restricciones CHECK nuevas | 7 | Rating de reviews y deliveries; precio; monto; importes de pedido y detalle; cantidad positiva. |
| FK recreadas | 10 | Se eliminan y recrean para las cinco tablas renombradas. En PostgreSQL todas quedaron validadas. |
| Ãndice nuevo/recreado | 1 | Ãšnico filtrado de `notifications.idempotency_key` donde el valor no es NULL. |
| PK recreadas | 5 | Se recrean para las tablas renombradas. |

El diseÃ±ador de la migraciÃ³n anterior representa las cinco tablas con nombres PascalCase; el diseÃ±ador de `AuditCorrections20261030` y `AppDbContextModelSnapshot.cs` usan los nombres y columnas `snake_case`. La intenciÃ³n es consistente con el SQL generado y el esquema post-migraciÃ³n ensayado. El esquema local posterior contiene las tablas nuevas y no las cinco tablas PascalCase anteriores. EF no reportÃ³ diferencias de modelo pendientes al comparar el ensamblado local disponible con el snapshot.

### SQL revisado

El SQL `Up` completo estÃ¡ en artefacto SQL ejecutado excluido del PR; SHA-256 registrado en HU-12-RESULTADO-STAGING-2026-11-05.md. Fue generado con EF CLI 8.0.18 y runtime EF 8.0.29; la advertencia de desfase de herramientas queda pendiente de corregir para el build repetible. Es un script dirigido de la migraciÃ³n 35 a la 36, no es idempotente ni debe volver a ejecutarse manualmente tras una aplicaciÃ³n; la ruta prevista del backend llama `Database.MigrateAsync()` al iniciar.

El script usa una transacciÃ³n PostgreSQL. Las operaciones de renombre no borran datos y las ampliaciones numÃ©ricas no reducen precisiÃ³n. El fallo inducido en un CHECK confirma que PostgreSQL revierte este DDL transaccional como se espera. No se adjunta un script `Down` ejecutable: su revisiÃ³n mostrÃ³ que elimina las dos columnas nuevas y reduce 11 columnas numÃ©ricas; despuÃ©s de aceptar nuevas escrituras, ese downgrade podrÃ­a perder datos o fallar. Ante un incidente posterior al commit, preferir correcciÃ³n hacia adelante o restauraciÃ³n aprobada a una base nueva, no ejecutar `Down` a ciegas.

## VerificaciÃ³n de esquema y datos local

Tras la aplicaciÃ³n exitosa:

- `__EFMigrationsHistory`: 36 filas, incluida la migraciÃ³n objetivo.
- Los 7 checks `ck_*` de esta migraciÃ³n estÃ¡n validados. En total se observaron 9 checks validados, contando dos checks de dominios PostgreSQL preexistentes.
- Las 49 FK del esquema estÃ¡n validadas.
- `IX_Notifications_IdempotencyKey` existe como Ã­ndice Ãºnico parcial de `idempotency_key IS NOT NULL`.
- Las 11 columnas monetarias revisadas tienen precisiÃ³n/escala 14,2.
- Las dos columnas de privacidad de clientes existen.
- Conteos sintÃ©ticos antes/despuÃ©s: `orders=1`, `order_details=1`, `payments=1`, `invoices=1`, `notifications=1`, `refresh_tokens=1`, `employees=1`, `customers=1`.
- La restauraciÃ³n a `hu12_restore` conservÃ³ 36/36 migraciones y los registros sintÃ©ticos comprobados de pedidos, facturas, pagos, refresh tokens y notificaciones.

La Ãºnica fila invÃ¡lida utilizada para inducir el fallo fue borrada de la base local despuÃ©s de verificar el rollback. No se modificaron otras bases.

## Bloqueos, incompatibilidad y riesgos operativos

1. **Build reproducible:** resolver acceso de NuGet en el entorno de trabajo y repetir restore/build/test sobre el SHA auditado. El SQL se pudo generar con el ensamblado ya disponible, pero eso no reemplaza la compilaciÃ³n de fuente actual.
2. **Compatibilidad de versiÃ³n:** las tablas y columnas cambian de PascalCase a `snake_case`. La versiÃ³n anterior del backend no debe seguir atendiendo trÃ¡fico contra el esquema renombrado. Coordinar una ventana de mantenimiento o un mecanismo de despliegue que impida solapamiento entre instancias viejas y nuevas. Revisar ademÃ¡s `Program.cs`: la aplicaciÃ³n invoca `Database.MigrateAsync()` al iniciar; evitar que varias instancias desplieguen simultÃ¡neamente y no ejecutar manualmente este SQL ademÃ¡s del arranque de la aplicaciÃ³n.
3. **Locks y duraciÃ³n:** validar checks e Ã­ndices puede recorrer tablas y las operaciones de PK/FK requieren locks. El ensayo con pocas filas no mide duraciÃ³n ni bloqueos con el volumen real. Obtener tamaÃ±o/volumen, ventana y umbral de espera antes de staging.
4. **Deriva de esquema:** si staging no coincide exactamente con la migraciÃ³n 35, cualquier constraint/tabla/columna esperada ausente puede hacer fallar el despliegue. Revisar nombres de FK, PK, Ã­ndices y tablas con consultas de solo lectura antes de autorizar.
5. **Unique index:** comprobar duplicados de `idempotency_key` no nulos y existencia/definiciÃ³n del Ã­ndice actual. La migraciÃ³n lo elimina y vuelve a crear.
6. **CHECK preflight:** antes de staging contar violaciones de las siete expresiones exactas. El resultado cero informado para staging/producciÃ³n por el propietario debe archivarse con fecha, SHA de consulta y base identificada; no fue verificado por esta ejecuciÃ³n.
7. **Rollback de aplicaciÃ³n:** fallo dentro de la transacciÃ³n deja la versiÃ³n anterior del esquema en el ensayo. DespuÃ©s de confirmar la migraciÃ³n y admitir escrituras, no usar `Down`: reduce precisiones y elimina columnas de privacidad.

## Procedimiento de backup/restore para staging (preparado, no ejecutado)

No usar este documento para conectarse ahora a staging. El procedimiento requiere autorizaciÃ³n operativa y credenciales que se suministran por un almacÃ©n local seguro o `PGPASSFILE` con permisos restringidos; nunca pegar la URL o contraseÃ±a en terminal, historial, issue o informe.

1. Confirmar por canal operativo el proyecto, branch, host y base de **staging**; obtener aprobaciÃ³n explÃ­cita para ventana y backup. Rechazar cualquier destino con nombre/host de producciÃ³n. Congelar o pausar escrituras y despliegues concurrentes durante el cambio.
2. Hacer preflight de solo lectura: confirmar las 35 migraciones, las tablas/columnas/constraints/indexes que espera el predecesor, ausencia de violaciones CHECK, duplicados de idempotencia y referencias huÃ©rfanas. Guardar salida anonimizada con fecha y SHA de consulta.
3. Desde un equipo/runner autorizado con acceso directo al endpoint de staging, usar un `PGPASSFILE` o servicio PostgreSQL protegido fuera del repo. Ejecutar `pg_dump --format=custom --no-owner --no-acl --file=<archivo-de-backup> --host=<host-staging> --port=<puerto> --username=<usuario> --dbname=<base-staging>`. No usar pooler para el backup si el proveedor requiere conexiÃ³n directa.
4. Calcular y archivar SHA-256, tamaÃ±o y hora del dump en almacenamiento privado cifrado, fuera del repo. Probar `pg_restore --list <archivo-de-backup>` y restaurar en **una base aislada nueva**, nunca sobre staging. Verificar historial, conteos, constraints y que la aplicaciÃ³n pueda iniciar sobre esa restauraciÃ³n. El backup solo se considera verificado despuÃ©s del restore exitoso.
5. Con respaldo verificado y autorizaciÃ³n, desplegar un Ãºnico candidato aprobado por el flujo `HU â†’ develop â†’ qa â†’ staging â†’ main`. Como el backend ejecuta migraciones al iniciar, controlar las instancias para que no haya dos arranques migrando a la vez ni versiÃ³n antigua atendiendo despuÃ©s de los renombres. No aplicar tambiÃ©n el SQL manualmente.
6. Validar SHA desplegado, historial 36/36, `/health` 200, `/health/ready` 200, logs sin excepciones y smoke funcional de login, lectura de pedidos/facturas, pagos, notificaciones y refresh token. Mantener la ventana y monitoreo acordados.
7. Si falla antes del commit, comprobar automÃ¡ticamente que el historial continÃºa en 35, corregir causa y no dejar que una instancia nueva siga atendiendo. Si el cambio ya se confirmÃ³, preferir forward-fix. Si es necesario restaurar, hacerlo en **una base nueva** desde el dump verificado; el cambio de endpoint/corte de trÃ¡fico requiere aprobaciÃ³n separada. No restaurar ni borrar la base real automÃ¡ticamente.

## DecisiÃ³n para staging

**NO-GO actualmente.** Para cambiar a GO deben cumplirse todas estas condiciones:

- build y suites requeridas pasan desde el SHA exacto que se promoverÃ¡, con CI verde;
- evidencia de preflight de staging confirma versiÃ³n 35, esquema esperado, cero violaciones CHECK/huÃ©rfanos y sin duplicados del Ã­ndice;
- backup de staging se restaura con Ã©xito en una base aislada y se archivan checksum/conteos;
- ventana aprobada, plan de compatibilidad que evita mezcla entre binarios antiguos/nuevos, control de una sola instancia migradora y reversiÃ³n aprobada;
- autorizaciÃ³n explÃ­cita antes de cualquier operaciÃ³n en una base desplegada.

Esta auditorÃ­a no autoriza migrar staging, no cierra HU-12 institucionalmente y no acredita producciÃ³n.

## Addendum â€” Ensayo Neon autorizado, 2026-11-04

El propietario autorizÃ³ aplicar la migraciÃ³n Ãºnicamente en `br-broad-cell-b4z6hyen`. Neon confirmÃ³ el proyecto `odd-sound-44850362`, base `neondb`, rol de conexiÃ³n `neondb_owner` y endpoint de ensayo `ep-muddy-pine-b4bcp3fu`. No se recuperÃ³ ni usÃ³ ninguna cadena de conexiÃ³n. Todas las consultas y la transacciÃ³n llevaron `project_id`, `branch_id` y `compute_id` explÃ­citos. La rama original `br-ancient-violet-b45js5mp` no recibiÃ³ consultas SQL ni escrituras; Neon la identifica como `production`, primaria y default.

El SHA de cÃ³digo revisado fue `caa4f0336a4f96f529c5da10d37128e381822b9c`; el blob de la migraciÃ³n coincide con el archivo local (`733c95d09bcc6ac6657798ae24ebec56d3c609af`). El SQL aplicado fue el artefacto de 9,662 bytes cuyo SHA-256 es `5083FBEC84B8E6367A7CC29EB36617C6F204B98257D75D5DC6E082825D18DB1A`. Se enviaron sus 117 sentencias `Up` como una sola transacciÃ³n al endpoint de ensayo. El resultado de Neon no informÃ³ errores.

### Conteos y preflight

| Dato | Antes | DespuÃ©s |
|---|---:|---:|
| Migraciones EF | 35 | 36 |
| Tablas base en `public` | 34 | 34 |
| Usuarios | 4 | 4 |
| Pedidos / detalles | 1 / 1 | 1 / 1 |
| Pagos / facturas | 1 / 1 | 1 / 1 |
| Refresh tokens | 12 | 12 |
| Clientes | 1 | 1 |
| Notificaciones / horarios / cÃ³digos de confirmaciÃ³n | 0 / 0 / 0 | 0 / 0 / 0 |

Antes de ejecutar se verificaron las siete expresiones CHECK, duplicados de claves de idempotencia, tablas anteriores y nuevas, las cinco PK de tablas que se renombran, las diez FK que se recrean y los tipos numÃ©ricos de origen. No hubo infracciones CHECK, duplicados ni huÃ©rfanos; el Ã­ndice Ãºnico filtrado de notificaciones estaba presente. Las dos columnas de privacidad todavÃ­a no existÃ­an.

### Resultado posterior

- Historial 36/36; Ãºltima migraciÃ³n `20261030124239_AuditCorrections20261030`.
- Las cinco tablas antiguas dejaron de existir con su nombre anterior y estÃ¡n presentes con nombre `snake_case`; no se aÃ±adieron ni quitaron tablas.
- Conteos de pedidos, detalles, pagos, facturas, usuarios, refresh tokens, clientes, notificaciones, horarios y cÃ³digos de confirmaciÃ³n iguales al baseline.
- Las siete restricciones CHECK nuevas quedaron validadas y las siete expresiones siguen con cero infracciones.
- Las 10 FK de las tablas renombradas quedaron validadas. En `public` hay 49/49 FK validadas; en `neon_auth` hay 6/6, total 55/55.
- Las cinco PK, el Ã­ndice Ãºnico filtrado de `idempotency_key`, las dos columnas nuevas de clientes y las 11 columnas `numeric(14,2)` esperadas estÃ¡n presentes.
- Cero duplicados no nulos de idempotencia y cero huÃ©rfanos en las diez relaciones revisadas.
- No se desplegÃ³ ni probÃ³ el backend contra Neon, Render o producciÃ³n. La compatibilidad de aplicaciÃ³n se limita al smoke test local previamente documentado.

### Veredicto actualizado

**Ensayo en la rama de snapshot: GO.** La migraciÃ³n aplicada conserva los conteos examinados y las restricciones quedaron vÃ¡lidas en una copia restaurada.

**AplicaciÃ³n a staging: NO-GO por ahora.** En el proyecto Neon consultado, la Ãºnica otra rama observada es la primaria llamada `production`; `br-broad-cell-b4z6hyen` es la rama de ensayo. No queda identificada una rama separada de staging a la cual promover. AdemÃ¡s, sigue pendiente resolver el build reproducible/CI bloqueado por NuGet y ejecutar las suites automatizadas antes de una promociÃ³n. No modificar `br-ancient-violet-b45js5mp` ni Render.
