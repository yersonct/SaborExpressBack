# Reporte — HU-STG-01

> Estado inicial: **pendiente de ejecutar en staging aislado**. No hay resultados de carga adjuntos a esta rama.

## Identificación de la corrida

| Campo | Valor |
|---|---|
| Fecha/hora UTC | Pendiente |
| Commit backend probado | Pendiente |
| Commit web probado | Pendiente |
| Build móvil probado | Pendiente |
| Ambiente y región | Pendiente |
| Región del generador | Pendiente |
| Tamaño de datos sintéticos | Pendiente |
| Perfil de usuarios | Pendiente |
| Calentamiento | 120 s (ajustar al perfil aprobado) |
| Concurrencia máxima | 15 VUs (ajustar al perfil aprobado) |
| Ramp-up / carga sostenida / descenso | 120 s / 600 s / 30 s (ajustar al perfil aprobado) |
| Duración efectiva medida | Pendiente |
| Versión de k6 | Pendiente |

## API: duración del servidor

| Método | Plantilla de ruta | Estado | Solicitudes | p50 ms | p95 ms | p99 ms | Errores % | ¿p95 ≤ 300 ms? |
|---|---|---:|---:|---:|---:|---:|---:|---|
| — | — | — | — | — | — | — | — | Pendiente |

## API: cliente/generador, incluye red

| Endpoint lógico | p50 ms | p95 ms | p99 ms | Errores % |
|---|---:|---:|---:|---:|
| — | — | — | — | Pendiente |

## Web

| Página/flujo | Tipo de medida | p50 ms | p95 ms | p99 ms | Errores % | Limitaciones |
|---|---|---:|---:|---:|---:|---|
| Documento inicial `/login` | HTTP desde generador; no mide JS/renderizado | — | — | — | — | Pendiente |

## Móvil físico

| Flujo | Dispositivo / Android | Red y región | Build/commit | p50 cliente ms | p95 cliente ms | p99 cliente ms | p95 servidor ms | Errores % |
|---|---|---|---|---:|---:|---:|---:|---:|
| Inicio frío | — | — | — | — | — | — | N/A | — |
| Inicio caliente | — | — | — | — | — | — | N/A | — |
| Carga de carta | — | — | — | — | — | — | — | — |
| Consulta de pedidos | — | — | — | — | — | — | — | — |

## Separación de componentes

| Componente | p50 ms | p95 ms | p99 ms | Fuente |
|---|---:|---:|---:|---|
| Tiempo servidor API | — | — | — | `Server-Timing` / Prometheus |
| Red y transferencia API | — | — | — | cliente total menos servidor |
| Web documento inicial | — | — | — | k6 HTTP |
| Arranque móvil frío/caliente | — | — | — | Android Studio/Perfetto |
| Esperas externas (Wompi, correo, geocoding, push) | — | — | — | métrica de proveedor separada; no sumar a API p95 |

## Resultado y defectos

- Objetivo servidor API p95 ≤ 300 ms: **pendiente**.
- Tasa de errores: **pendiente**.
- Defectos observados: **pendiente**.
- Limitaciones y desviaciones del perfil acordado: **pendiente**.
- HU de optimización requerida: **pendiente de resultados**.

## Artefactos

- Metadatos de ejecución: pendiente.
- Resumen k6 API: pendiente.
- Resumen k6 web: pendiente.
- Evidencias Android: pendiente.
