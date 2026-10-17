# Estructura limpia de backend

Esta carpeta representa la estructura objetivo para la capa de aplicación, sin tocar la base actual del proyecto.

## Bloque objetivo

SaborExpressBack/
├── src/
│ ├── Presentation/
│ │ └── Controllers/
│ ├── Application/
│ │ └── Features/
│ │ ├── Orders/
│ │ ├── Products/
│ │ ├── Employees/
│ │ ├── Notifications/
│ │ ├── UserPreferences/
│ │ └── ...
│ ├── Domain/
│ │ ├── Entities/
│ │ ├── Enums/
│ │ └── ValueObjects/
│ └── Infrastructure/
│ ├── Data/
│ ├── Auth/
│ ├── Realtime/
│ └── ExternalServices/

## Mapeo con el proyecto actual

- [SaborExpressBack/Controllers](../../SaborExpressBack/Controllers) -> src/Presentation/Controllers
- [SaborExpressBack/Modules](../../SaborExpressBack/Modules) -> src/Application/Features
- [SaborExpressBack/Data](../../SaborExpressBack/Data) -> src/Infrastructure/Data
- [SaborExpressBack/Shared](../../SaborExpressBack/Shared) -> src/Infrastructure/Common y Shared transversal

## Principios

- Una feature = una carpeta con su responsabilidad.
- Controller solo recibe y responde.
- Service orquesta la lógica de negocio.
- Repositorio/DbContext accede a los datos.
- WebSocket/SignalR queda aislado en Infrastructure/Realtime.
- No se rompe la lógica actual; solo se deja la estructura objetivo lista para migrar por bloques.
