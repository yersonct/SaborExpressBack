# HU-STG-03 — Gate de seguridad y errores

## Qué bloquea

El workflow `Security release gate` corre pruebas negativas y escaneo de secretos en PR hacia `develop`, `qa`, `staging` y `main`. El job se llama `security-gate`. Para que bloquee una promoción, ese check debe quedar como requerido en las reglas de protección de `staging` y `main`; el repositorio debe conservar también GitGuardian como check requerido si esa integración está activa.

Al abrir PR hacia `staging`, el workflow ejecuta las pruebas de seguridad, pero no exige todavía el reporte de carga: la medición HU-STG-01 debe realizarse después de desplegar el candidato en staging. En una PR hacia `main`, sí exige `docs/release/security-gate-evidence.json` con mediciones reales. Así no se bloquea la instalación que se necesita para medir, y tampoco se promueve a producción sin la evidencia.

## Pruebas negativas

`tests/SaborExpressBack.Security.Tests` cubre:

- rol operativo sin acceso a otra sede y rol de cocina sin acceso financiero;
- acceso de gerente entre sedes;
- JWT expirado con tolerancia de reloj cero;
- webhook Wompi con firma inválida, respuesta HTTP 401 y sin consulta de transacción;
- CORS de producción: coincidencia exacta con orígenes configurados y rechazo de localhost, Dev Tunnels, subdominios engañosos y rutas;
- registros de correo sin destinatario ni respuesta del proveedor.

Ejecutar localmente:

```powershell
dotnet test tests/SaborExpressBack.Security.Tests/SaborExpressBack.Security.Tests.csproj --configuration Release
```

El escaneo local puede ejecutarse con Gitleaks instalado:

```powershell
gitleaks git --redact --exit-code 1
```

## Revisión de logs

Se revisaron autenticación/webhook, pagos Wompi, correo, push, turnos, entregas, reseñas, configuración y notificaciones en tiempo real. Los fallos ya no serializan objetos de excepción ni registran destinatarios, ubicaciones o identificadores de usuario/pedido en esos eventos. Los logs conservan el tipo de error, el estado HTTP o conteos agregados para diagnóstico; nunca contraseñas, tokens, firmas, cuerpos de webhook ni respuestas de proveedores.

La prueba de correo usa un destinatario sintético y un cuerpo de respuesta marcador, y verifica que ninguno aparezca en el log. La prueba Wompi verifica que el cuerpo y checksum marcadores no se registren.

## Evidencia y regla de línea base

Generar baseline y candidato con el mismo perfil aprobado de HU-STG-01, en `staging`, con las mismas rutas, método, región, tamaño de datos, concurrencia, duración y calentamiento. Registrar el SHA exacto probado. `serverP95Ms` es duración del servidor; no incluir red móvil, inicio frío ni Wompi/proveedores. `http5xxCount / requestCount` es el error rate del servidor.

Formato requerido:

```json
{
  "schemaVersion": 1,
  "environment": "staging",
  "baseline": {
    "capturedAtUtc": "<fecha ISO-8601 UTC de la ejecución real>",
    "commit": "<SHA de 40 caracteres medido>",
    "loadRunId": "<identificador de ejecución y artefacto>",
    "routes": [
      {
        "method": "GET",
        "route": "/api/example/{id}",
        "requestCount": null,
        "http5xxCount": null,
        "serverP95Ms": null
      }
    ]
  },
  "candidate": {
    "capturedAtUtc": "<fecha ISO-8601 UTC de la ejecución real>",
    "commit": "<SHA exacto medido; debe ser ancestro estricto del HEAD, al que solo se añade evidencia y metadatos de producción>",
    "loadRunId": "<identificador de ejecución y artefacto>",
    "routes": [
      {
        "method": "GET",
        "route": "/api/example/{id}",
        "requestCount": null,
        "http5xxCount": null,
        "serverP95Ms": null
      }
    ]
  }
}
```

Los valores `null` son marcadores estructurales y **no son una línea base válida ni evidencia**; sustitúyanse por datos reales. No subir un archivo de ejemplo al path que consume el gate. Cada ruta debe aparecer exactamente una vez en ambos conjuntos. El gate compara p95 y tasa 5xx del candidato con los valores medidos del baseline; no fija un SLA inventado. Debe mantenerse el mismo perfil de carga y commit medido como ancestro del commit promocionado. Después de medir, se requiere añadir o actualizar el archivo de evidencia de seguridad; también pueden cambiar exclusivamente `docs/release/production-release-candidate.json` y `docs/release/production-release-evidence.json` para preparar la promoción a `main`. Cualquier otro cambio, incluso en scripts o workflows, requiere una nueva medición.

PromQL para consultar la métrica del API existente:

```promql
histogram_quantile(0.95, sum by (le, method, route) (rate(sabor_express_http_server_request_duration_milliseconds_bucket[5m])))
```

```promql
sum by (method, route) (rate(sabor_express_http_server_requests_total{status_code=~"5.."}[5m]))
/
sum by (method, route) (rate(sabor_express_http_server_requests_total[5m]))
```

## Nota de publicación

Esta HU añade el workflow y el validador, pero no puede certificar ni producir evidencia real de staging desde el entorno local. Primero debe integrarse el código a través de `develop` y `qa`, y desplegarse en `staging`; allí el responsable ejecuta HU-STG-01 y registra el reporte. La evidencia se adjunta en la promoción posterior de `staging` a `main`.
