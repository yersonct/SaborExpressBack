# Features del backend

Esta carpeta representa la capa funcional del sistema, organizada por feature y alineada con el patrón de arquitectura limpia.

## Organización objetivo

- Orders
- Products
- Employees
- Notifications
- UserPreferences
- Branches
- Payments
- Tables
- Reviews
- Deliveries

## Regla de flujo

Request -> Controller -> Application Service -> Repository/DbContext -> Response

Esto mantiene una sola vía de entrada para cada acción y evita duplicación y lógica mezclada.

## Realtime y la capa de infraestructura

La lógica de SignalR y conexión en tiempo real permanece en la capa de infraestructura, no dentro de controladores ni pantallas.
