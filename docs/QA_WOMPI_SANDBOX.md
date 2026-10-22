# HU-QA-02 — Validación de Wompi en sandbox

**Rama:** `test/HU-QA-02-wompi-sandbox`  
**Ambiente:** Sandbox de Wompi; ninguna evidencia de este procedimiento representa un cobro real.

## Ejecución automatizada reproducible

Desde `SaborExpressBack`:

```powershell
dotnet test tests/SaborExpressBack.QA.Tests/SaborExpressBack.QA.Tests.csproj --configuration Release --logger "trx;LogFileName=HU-QA-02-WOMPI.trx" --results-directory TestResults
```

El workflow `.github/workflows/qa-wompi-sandbox.yml` ejecuta la misma suite al subir esta rama, al abrir/actualizar un PR hacia `qa` o manualmente. El artefacto se etiqueta `SANDBOX-SIMULATED`: estas pruebas usan datos sintéticos y dobles HTTP/repositorio; no contactan Wompi ni generan transacciones. Sirven para validar el contrato, la idempotencia y la recuperación sin credenciales ni datos productivos.

Los casos automatizados cubren aprobación, rechazo (`DECLINED`, `ERROR`, `VOIDED`), pendiente, respuesta `EXPIRED` no reconocida como final, repetición de webhook, checksum, coincidencia de referencia/monto/moneda, cálculo del total desde el pedido y fallas recuperables al confirmar el pedido, notificar y liberar mesa.

## Preparar el sandbox real

1. Configura el servicio de **QA** para usar exclusivamente `https://sandbox.wompi.co/v1/` y credenciales con prefijos `pub_test_`, `prv_test_`, `test_integrity_` y `test_events_`. La URL base y prefijos de los ambientes están descritos en la [documentación oficial de ambientes y llaves](https://docs.wompi.co/docs/colombia/ambientes-y-llaves/).
2. Guarda las credenciales en el gestor de secretos del entorno de QA (por ejemplo, variables protegidas del servicio de despliegue o un GitHub Environment `wompi-sandbox`). Para configuración .NET se usan `Wompi__BaseUrl`, `Wompi__PublicKey`, `Wompi__PrivateKey`, `Wompi__IntegritySecret` y `Wompi__EventsSecret`. **No las pongas en `.env` versionado, fixtures, logs, capturas ni artefactos.** No se requieren para la suite automatizada.
3. En el dashboard Wompi, activa el modo Sandbox y registra para ese ambiente la URL HTTPS pública del webhook de QA. Wompi mantiene URLs de eventos separadas entre sandbox y producción; valida el checksum usando el secreto de eventos de sandbox. Consulta [eventos y checksum](https://docs.wompi.co/docs/colombia/eventos/).
4. Usa solo pedidos y clientes sintéticos en el backend de QA. Comprueba que el widget recibe `COP`, el monto calculado por el backend y una referencia de prueba propia del pedido.

## Matriz de ejecución manual en el widget sandbox

Wompi documenta `4242 4242 4242 4242` para aprobación y `4111 1111 1111 1111` para declinación. Usa fecha futura y CVC de tres dígitos; no captures ni guardes esos datos en el informe. Ver [datos de prueba oficiales](https://docs.wompi.co/docs/colombia/datos-de-prueba-en-sandbox/).

| Caso | Acción en QA/Sandbox | Resultado que se debe registrar |
| --- | --- | --- |
| Aprobado | Completar checkout con tarjeta de prueba aprobada. | API autoritativa confirma mismo ID, referencia, monto y `COP`; pago completado una sola vez; pedido confirmado y efectos posteriores ejecutados. |
| Rechazado | Completar checkout con tarjeta de prueba declinada. | Pago fallido; pedido no se confirma ni se libera su mesa por este pago. |
| Pendiente | Iniciar un método asíncrono y dejarlo sin concluir. | Permanece pendiente; no se confirma pedido, no se notifica aprobación ni se libera mesa. |
| Expirado | Si el método asíncrono del sandbox lleva la transacción a un estado final, registrar exactamente el estado devuelto por Wompi. | No tratar `PENDING` como pago aprobado. La lista oficial de estados de transacción incluye `PENDING`, `APPROVED`, `DECLINED`, `VOIDED` y `ERROR`, pero no define `EXPIRED`; por eso una respuesta `EXPIRED` desconocida queda no final y no confirma el pedido. Ver [estados oficiales](https://docs.wompi.co/docs/colombia/transacciones/). |
| Webhook repetido | Reintentar la entrega del mismo evento de prueba desde el mecanismo disponible en el dashboard o entorno controlado. | Una transición de pago; sin notificación/confirmación/liberación duplicada. La repetición también está cubierta automáticamente con datos sintéticos. |
| Monto/moneda/referencia alterados | Ejecutar el caso sintético automatizado con valores que no coinciden con la consulta autoritativa. | No se modifica el pago ni se confirma el pedido. No alterar/capturar información de un evento productivo. |
| Notificación falla después de aprobar | Suite automatizada: primer intento falla, reintento posterior completa. | El pago sigue completado; el reclamo se libera; el efecto queda elegible para reintento y termina una sola vez por clave idempotente. |
| Confirmación del pedido falla después de aprobar | Suite automatizada: primer guardado falla y el reintento vuelve a intentar la transición. | El pago sigue completado; el pedido vuelve a ser elegible para la confirmación persistente y termina confirmado. |
| Liberación de mesa falla después de aprobar | Suite automatizada: primer intento falla y el reintento recupera el paso. | Pago y pedido no se revierten; no se marca el posprocesamiento completo hasta completar la liberación. |

La documentación actual de Wompi no ofrece un estado transaccional `EXPIRED`. Si una operación asíncrona no llega a un estado final, registra `PENDING` y su comportamiento observado; no informes un caso como expirado sin evidencia de un estado final real.

## Registro de evidencia

Completar después de la corrida manual real. La evidencia debe conservar el marcador `WOMPI SANDBOX`, fecha/hora con zona horaria, commit, versión desplegada, método de pago, escenario y resultado. Enmascara referencias/IDs si el repositorio de evidencias no es privado. No adjuntes llaves, datos de tarjeta, cuerpos completos de eventos ni tokens.

| Fecha/hora | Commit/versión | Ambiente | Caso | Resultado/defecto |
| --- | --- | --- | --- | --- |
| Pendiente de ejecutar con la cuenta sandbox del responsable | — | `SANDBOX` | Ciclo real del widget y webhook | No se declara ejecutado en este cambio: el entorno de trabajo no tiene credenciales de sandbox ni una URL QA pública configurada. |

La corrida automatizada de CI queda identificada por el artefacto `HU-QA-02-WOMPI-SANDBOX-SIMULATED-<run>`. Debe conservarse separada de la evidencia del sandbox real.
