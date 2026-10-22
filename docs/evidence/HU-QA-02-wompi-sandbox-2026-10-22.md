# Evidencia HU-QA-02 — Wompi sandbox

- **Fecha:** 2026-10-22
- **Versión probada:** `357b339` (`test/HU-QA-02-wompi-sandbox`)
- **Ambiente:** `SANDBOX_SIMULATED` — datos sintéticos y HTTP/repositories simulados; sin llamadas a Wompi y sin cobros reales.
- **Comando:** `dotnet test tests/SaborExpressBack.QA.Tests/SaborExpressBack.QA.Tests.csproj --configuration Release --no-restore --logger "trx;LogFileName=HU-QA-02-WOMPI.trx" --results-directory TestResults`
- **Resultado:** 61 aprobadas, 0 fallidas, 0 omitidas.
- **Artefacto local:** `SaborExpressBack/TestResults/HU-QA-02-WOMPI.trx` (ignorado por Git).
- **Incidencias encontradas y corregidas:** no se conciliaba la moneda COP en todos los caminos de actualización; un fallo al guardar la confirmación podía dejar el pedido como confirmado solo en memoria e impedir el reintento.
- **Pendiente de evidencia externa:** la ejecución real en la cuenta Sandbox y el webhook HTTPS de QA requiere configurar las credenciales en el gestor de secretos del entorno y ejecutar la matriz manual de `docs/QA_WOMPI_SANDBOX.md`.
- **Nota de estado:** la documentación oficial consultada no incluye `EXPIRED` entre los estados de una transacción. Un estado desconocido se conserva como no final; no se confirma ni se libera el pedido con esa respuesta.

La restauración local mostró `NU1900` porque no fue posible consultar el servicio de avisos de vulnerabilidades de NuGet en esta red. Restauración, compilación y ejecución terminaron correctamente; esa advertencia no corresponde a una falla de prueba.
