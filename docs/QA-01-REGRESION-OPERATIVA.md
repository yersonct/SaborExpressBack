# HU-QA-01 — Regresión de pedidos, caja y domicilios

## Alcance

Esta HU agrega pruebas automatizadas de reglas del backend y una matriz de regresión para ejecutar los flujos integrados en un ambiente QA. Los casos deben usar una sede y usuarios sintéticos; no ejecutar cobros ni cierres en producción.

Las pruebas automatizadas cubren transiciones y cancelación de pedidos, validación de montos y estados de pago, asignación y avance del domicilio, autorización por rol/sede, emisión idempotente de factura y publicación de eventos SignalR. La corrida automatizada guarda un archivo TRX como artefacto de CI durante 30 días.

## Reproducir las pruebas automatizadas

Desde la raíz de `SaborExpressBack`:

```powershell
dotnet test tests/SaborExpressBack.QA.Tests/SaborExpressBack.QA.Tests.csproj `
  --configuration Release `
  --logger "trx;LogFileName=HU-QA-01.trx" `
  --results-directory TestResults
```

El proyecto de pruebas usa datos sintéticos en memoria/Moq y no lee `.env` ni se conecta a la base productiva. La integración continua ejecuta el mismo comando en cada push de esta rama y en pull requests dirigidos a `qa`.

## Matriz de regresión end to end en QA

### Preparación

1. Desplegar una versión identificable del backend y panel/app en QA. Registrar commit SHA, fecha, ambiente y responsable.
2. Crear una sede y usuarios sintéticos con roles GERENTE, ADMINISTRADOR, CAJERO, MESERO y REPARTIDOR. Configurar un turno activo para los roles que lo requieren.
3. Usar un identificador de corrida, por ejemplo `QA-HU-QA-01-<fecha>-<iniciales>`, en nombres/notas de los registros. Usar una mesa y una dirección de entrega de prueba.
4. Confirmar que Wompi está en sandbox o usar solo pagos de prueba habilitados en QA. Nunca incluir PAN, CVV, tokens o secretos en el informe.

### Casos

| ID | Flujo | Pasos y resultado esperado | Estado/observación |
| --- | --- | --- | --- |
| QA-01-01 | Crear y consultar pedido | MESERO crea un pedido de prueba (`POST /api/Orders`); consulta `GET /api/Orders/{id}` y verifica sede, tipo, productos y total. GERENTE lo puede consultar; MESERO de otra sede y REPARTIDOR no asignado reciben 403. | Pendiente de ejecutar en QA |
| QA-01-02 | Cancelar pedido | MESERO cancela un pedido permitido con motivo (`PATCH /api/Orders/{id}/cancel`); comprobar estado, motivo y liberación de mesa. Un pedido entregado/cancelado no debe poder cancelarse otra vez. Cliente solo usa `PATCH /api/Orders/{id}/cancel-mine` para su propio pedido. | Pendiente de ejecutar en QA |
| QA-01-03 | Cobrar y emitir recibo | CAJERO registra pago completo o parcial (`POST /api/Payments`); consulta el pago y crea recibo (`POST /api/Invoices`). Repetir la solicitud de recibo no crea una segunda factura. Un rol MESERO/REPARTIDOR que intente cobrar recibe 403. | Pendiente de ejecutar en QA |
| QA-01-04 | Cierre de caja | CAJERO consulta `GET /api/CashClosures/current`, cierra con conteo sintético (`POST /api/CashClosures`) y verifica el resumen. Repetir el cierre devuelve conflicto sin duplicar el cierre. CAJERO no accede al historial global; GERENTE/ADMINISTRADOR sí, limitado por sede según corresponda. | Pendiente de ejecutar en QA |
| QA-01-05 | Asignar y avanzar domicilio | GERENTE/ADMINISTRADOR crea el domicilio (`POST /api/Deliveries`) y lo consulta. REPARTIDOR asignado avanza `Assigned → InTransit → Delivered` (`PATCH /api/Deliveries/{id}/status`). Otro repartidor y un MESERO no pueden actualizarlo. | Pendiente de ejecutar en QA |
| QA-01-06 | Evento y reconexión SignalR | Cliente autenticado conecta a `/hubs/notifications`, escucha `OrdersChanged`; MESERO crea/actualiza el pedido y se verifica el evento. Interrumpir conexión, modificar un pedido y reconectar; el cliente vuelve a consultar `GET /api/Orders/{id}` y recupera el estado actual aunque haya perdido el evento. | Pendiente de ejecutar en QA |

Para cada caso registrar request/correlation ID no sensible, códigos HTTP, resultado esperado/obtenido, rol y sede sintéticos, duración, commit SHA y defecto asociado. No guardar credenciales, tokens ni cuerpos con información personal.

## Registro de ejecución de esta rama

| Campo | Registro |
| --- | --- |
| Historia / rama | HU-QA-01 / `test/HU-QA-01-flujos-operativos` |
| Fecha de preparación | 2026-10-22 |
| Commit base | `2b4d7cb` (rama `qa`) |
| Versión de runtime | .NET 8 (proyecto); SDK local detectado: 10.0.302 |
| Pruebas automatizadas | Ejecutadas localmente el 2026-10-22: 39 aprobadas, 0 fallidas, 0 omitidas; 86 ms. `dotnet test ... --configuration Release` compiló también el backend en Release. Se generó `TestResults/HU-QA-01.trx` (la CI publica el TRX como artefacto). |
| Advertencias de herramienta | NuGet mostró NU1900 al no poder consultar metadatos de auditoría de vulnerabilidades; la restauración, compilación y ejecución terminaron correctamente. |
| Flujos integrados | No ejecutados en este entorno: no hay endpoint/usuarios de QA suministrados ni motor Docker local para levantar dependencias. Los seis casos siguen pendientes y no se declaran aprobados. |
| Datos | Sintéticos; las pruebas unitarias no requieren base de datos. |

## Defectos identificados

| ID | Severidad | Hallazgo | Evidencia y seguimiento |
| --- | --- | --- | --- |
| QA-01-DEF-001 | Media | La descarga PDF de factura no está implementada y devuelve HTTP 501. | Observado en `InvoicesController.GetPdf`; el recibo JSON puede existir, pero la generación/descarga PDF debe abrirse como defecto separado. |
