# Métricas de latencia de SaborExpress

La API exporta métricas Prometheus en `GET /metrics`. Prometheus conserva las series y Grafana provisiona el tablero `SaborExpress API — latencia y errores`.

## Iniciar localmente

1. Inicia la API en el puerto 8080 (`dotnet run --urls http://0.0.0.0:8080`).
2. En PowerShell, define una clave local para Grafana y levanta el stack:

   ```powershell
   New-Item -ItemType Directory -Force .\observability\secrets | Out-Null
   $secretPath = Join-Path (Resolve-Path .\observability\secrets) 'grafana_admin_password'
   if (-not (Test-Path -LiteralPath $secretPath)) {
     $secretBytes = [byte[]]::new(32)
     [System.Security.Cryptography.RandomNumberGenerator]::Fill($secretBytes)
     [System.IO.File]::WriteAllText($secretPath, [Convert]::ToBase64String($secretBytes))
   }
   docker compose -f observability/docker-compose.yml up -d
   ```

3. Abre Grafana en `http://localhost:3001` e inicia sesión con el usuario `admin` y el contenido del archivo local `observability/secrets/grafana_admin_password`. El tablero aparece en la carpeta **SaborExpress**.
4. Prometheus está disponible en `http://localhost:9090`. Su objetivo local apunta a `host.docker.internal:8080`; cambia `observability/prometheus/prometheus.yml` si el backend escucha en otro host o puerto.

## Qué mide

- `sabor_express_http_server_request_duration_milliseconds`: duración del procesamiento de API, por método, plantilla de ruta y código HTTP. Excluye `/metrics` y el endpoint de recepción de telemetría para no sesgar sus propias medidas.
- `sabor_express_http_server_requests_total`: volumen y errores por los mismos atributos. Las rutas son plantillas de ASP.NET Core; nunca se usan URL completas, query strings ni IDs.
- `sabor_express_http_client_network_duration_milliseconds`: tiempo observado por el navegador menos la duración de servidor recibida en `Server-Timing`. Incluye red, transferencia y espera del navegador; no representa una medición de red unidireccional.
- `sabor_express_provider_request_duration_milliseconds`: duración de solicitudes salientes a Wompi, Nominatim, Brevo y Expo Push. Solo expone un nombre fijo de proveedor y el código HTTP; no guarda URL, cabeceras ni cuerpos.

La telemetría del navegador se envía en lotes autenticados, se valida contra las rutas registradas en el backend y se descarta si excede tamaño o duración permitidos. Si el envío falla, no afecta la operación del panel.

## Despliegue

Configura el scrape target de Prometheus para la URL interna/privada del backend, conservando `/metrics`. El endpoint contiene agregados de latencia, volumen y códigos HTTP, sin datos personales; limita su acceso en el proxy o firewall de producción a la red de scraping. Los datos se guardan en Prometheus/Grafana, no en la base de negocio.

El tablero calcula p95/p99 a partir de histogramas acumulativos y `rate()`; deja acumular tráfico durante al menos cinco minutos antes de interpretar esos percentiles. Para comparar despliegues con varias instancias, Prometheus debe descubrir y scrapeear cada instancia.
