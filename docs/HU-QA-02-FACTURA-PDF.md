# HU-QA-02 — Descargar factura en PDF

**Rama hija:** `feature/HU-QA-02-factura-pdf`  
**Base prevista del pull request:** `qa`  
**Prioridad:** P1

## Historia

Como cajero, gerente, administrador o usuario con acceso al pedido, quiero descargar el comprobante de factura en PDF para consultarlo, guardarlo o imprimirlo.

## Criterios de aceptación

- `GET /api/Invoices/{id}/pdf` devuelve un archivo PDF válido para una factura existente y autorizada; un identificador inexistente responde 404.
- Se mantienen las reglas de autorización de lectura de pedido y sede. Un usuario sin acceso recibe 403 y una solicitud sin sesión se bloquea por `[Authorize]`.
- El archivo incluye sede, número y fecha de emisión, cliente y documento/correo disponibles, dirección/referencia, tipo de pedido/mesa, conceptos, cantidades, precios, notas, impuestos, domicilio, total, método y monto pagado, referencia de pago, fecha de pago y cajero disponible. Si se anuló, incluye el motivo disponible.
- Las etiquetas se generan en español, inglés, portugués o francés a partir del parámetro `lang` o `Accept-Language`; si no se reconoce idioma, usa español.
- La descarga tiene nombre `factura-{id}.pdf`, `Content-Type: application/pdf`, y cabeceras `Cache-Control: private, no-store` y `X-Content-Type-Options: nosniff`.
- El formato es una representación descargable del comprobante. No afirma que el documento sea una factura electrónica autorizada o validada fiscalmente.
- La generación usa fuente empaquetada con su licencia para no depender de fuentes instaladas en el sistema operativo.

## Validación local

Desde la raíz de `SaborExpressBack`:

```powershell
dotnet test tests/SaborExpressBack.InvoicePdf.Tests/SaborExpressBack.InvoicePdf.Tests.csproj `
  --configuration Release `
  --logger "trx;LogFileName=HU-QA-02-invoice-pdf.trx" `
  --results-directory TestResults
```

El workflow ejecuta este comando en pushes a la rama y en pull requests dirigidos a `qa`, y conserva el TRX como artefacto por 30 días.

## Registro de ejecución

| Campo | Registro |
| --- | --- |
| Fecha | 2026-10-22 |
| Base | `origin/qa` (`2b4d7cb`) |
| Backend | .NET 8 |
| Pruebas locales | 11 aprobadas, 0 fallidas (`dotnet test`, Release, .NET 8), 2026-10-22. |
| Prueba en ambiente desplegado | Pendiente; validar que el contenedor despliega los recursos de fuente y permite la descarga con sesión autorizada. |
| Defectos conocidos | Ninguno en la ejecución local. La comprobación contra el ambiente desplegado sigue pendiente. |

