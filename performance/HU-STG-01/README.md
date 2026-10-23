# HU-STG-01 — Carga y percentiles

Este directorio contiene un arnés reproducible para medir por separado el tiempo del servidor API, la duración observada desde el generador de carga y la entrega del documento web. El script API consume `Server-Timing: app;dur=...` ya emitido por el backend y etiqueta las series con nombres de endpoint estables. k6 deshabilita la etiqueta automática `url` y no incorpora identificadores en nombres de métricas; los extras validan `routeTemplate` sin IDs numéricos.

## Estado de la HU

**Preparación implementada; evidencia de staging y medición móvil físico pendiente.** No se ejecutó carga en un servicio remoto: esta rama no tiene una URL de staging aislado, tamaño de datos ni perfil de carga aprobado. No se deben interpretar valores inventados o pruebas de desarrollo como resultado de staging.

## Perfil inicial propuesto

Debe acordarse con el responsable de staging antes de ejecutar:

- Ambiente: staging aislado, misma región que el servicio medido; registrar ubicación exacta del generador.
- Datos: semilla sintética; registrar cantidad de sedes, productos, pedidos y facturas. No usar credenciales o datos de clientes reales.
- Calentamiento: 120 s a 3 usuarios virtuales.
- Carga: subir durante 120 s hasta 15 usuarios virtuales y sostener 600 s; descenso de 30 s.
- Perfil: lectura de carta pública (sede, categorías y productos); opcionalmente añadir endpoints `GET` de lectura con un token sintético temporal por rol.
- Pausa: 1 s entre ciclos del flujo.
- Objetivo: p95 de tiempo del servidor API ≤ 300 ms; errores < 1% como señal de ejecución válida. La duración observada desde el cliente se informa por separado y no se compara con el presupuesto del servidor.

Los valores son una propuesta inicial configurable, no un acuerdo ya aprobado ni una afirmación de carga representativa. Ajusta `-Vus`, `-WarmupSeconds`, `-RampSeconds`, `-SteadySeconds`, `-ThinkTimeSeconds`, `-Region`, `-DatasetSize` y `-UserProfile` a lo que se apruebe.

## Requisitos

- k6 instalado en la máquina generadora.
- API y, opcionalmente, web desplegados en staging aislado y accesibles por HTTPS.
- Generador ubicado en la región anotada, o indicar claramente la distancia/red del generador.
- Datos sintéticos y tokens de usuarios de prueba; nunca pases tokens como argumentos ni los guardes en archivos de resultados.
- Para rutas autenticadas, obtener tokens desde un gestor de secretos en variables temporales del proceso. Añadir solo endpoints `GET` sin efectos laterales.

## Ejecutar API y web

Desde PowerShell:

```powershell
$env:PERF_CASHIER_TOKEN = '<token sintético temporal>'
$env:PERF_EXTRA_READ_ENDPOINTS = '[{"key":"orders-branch-read","path":"/api/Orders/branch/123","routeTemplate":"/api/Orders/branch/{branchId}","tokenEnv":"PERF_CASHIER_TOKEN","expectedStatus":200}]'

.\performance\HU-STG-01\run.ps1 `
  -Environment staging `
  -ApiUrl 'https://<api-staging-host>' `
  -WebUrl 'https://<web-staging-host>' `
  -Region '<región del generador y servicios>' `
  -DatasetSize '<conteos de datos sintéticos>' `
  -UserProfile '<mezcla de usuarios acordada>' `
  -Vus 15 -WarmupSeconds 120 -RampSeconds 120 -SteadySeconds 600 -ThinkTimeSeconds 1 `
  -ConfirmStagingTarget -ConfirmLoadProfile

Remove-Item Env:PERF_CASHIER_TOKEN, Env:PERF_EXTRA_READ_ENDPOINTS -ErrorAction SilentlyContinue
```

El ejemplo de ruta usa el ID ficticio `123`; sustitúyelo por un fixture sintético del ambiente. `routeTemplate` permanece como etiqueta estable para evitar IDs en los percentiles. La ejecución falla si falta HTTPS, no se confirma staging/perfil o coincide con los hosts productivos conocidos. El runner guarda resultados JSON y metadatos sin tokens en `%TEMP%\SaborExpress-load-reports\<run-id>`.

La API incluye por defecto `GET /health`, `GET /api/Branches/public`, `GET /api/Categories` y `GET /api/Products`. Los extras deben ser exclusivamente consultas GET; el script no envía pedidos, pagos, cierres de caja ni webhooks. Para medir escrituras se requiere un ambiente desechable con datos y recuperación controlados, y una HU de carga específica.

## Interpretación

- `server_duration_ms`: duración de aplicación reportada en `Server-Timing`; sirve para decidir el objetivo p95 de 300 ms bajo control del backend.
- `api_client_duration_ms`: tiempo total observado por k6 para el request API, que incluye red y transferencia.
- `web_document_duration_ms`: descarga del documento inicial (por defecto `/login`). No mide hidratación JavaScript, renderizado ni interacción real del navegador.
- `api_request_error_rate` y `web_document_error_rate`: solicitudes que no recibieron el estado esperado.
- `server_timing_missing_rate`: debe ser cero para poder atribuir duración del servidor. Si no, la corrida no permite evaluar el objetivo.

El resumen de k6 incluye mediana (p50), p95 y p99 para cada serie. La definición de escenarios y sus etiquetas permite aislar calentamiento y medición; las métricas etiquetadas pueden usar thresholds por endpoint según la documentación oficial de k6. Conserva `metadata.json`, `api-summary.json`, `web-summary.json`, el reporte completado y el commit medido. Anonimiza los valores de IDs en cualquier captura o log adjunto.

## Medición móvil física

k6 no representa radio móvil, latencia del operador, estado de batería, inicio en frío ni ejecución React Native. Ejecuta la misma APK/development build aprobada en al menos un dispositivo Android físico; registra modelo, Android, versión/número de build, tipo de red, región y commit de API. Separa:

1. Inicio frío y caliente de la app, medidos con Android Studio/Perfetto.
2. Duración cliente de cada flujo móvil principal con Android Studio Network Inspector o una traza de red equivalente.
3. Duración de servidor por ruta desde `Server-Timing`/Prometheus.
4. Tiempo de red del cliente (cliente total menos servidor) y espera de proveedor externo, en columnas distintas.

Usa usuario y datos sintéticos, realiza al menos 20 repeticiones por flujo y no actives operaciones que cobren dinero o alteren datos compartidos. Si el inspector no puede capturar el tráfico de la APK, registra esa limitación; no sustituyas la medida física por k6.

## Evidencia

Completa `REPORT_TEMPLATE.md` después de ejecutar. Si un endpoint supera 300 ms p95, anota el endpoint/plantilla y crea una HU de optimización con trazas y perfilado de consultas. No marques la HU aprobada mientras falten carga representativa, percentiles, tasa de errores, commit y mediciones móviles documentadas.
