# Módulos del backend

Esta carpeta representa la capa funcional del backend organizada por feature.

## Principios

- Cada feature encapsula su dominio: modelos, DTOs, servicios, repositorios y validadores.
- Las interfaces de servicio y repositorio definen el contrato de acceso.
- Los controllers reciben las peticiones y delegan la lógica a servicios del módulo.
- El acceso a datos y la capa de infraestructura quedan separada del flujo de negocio.
- La migración de estructura se hace por feature sin tocar la lógica operativa actual.

## Estructura sugerida por módulo

- DTOs/
- Interfaces/
- Services/
- Repositories/
- Validators/
- Mappings/
- Models/
- Enums/
- Hubs/ (cuando aplica)
- BackgroundServices/ (cuando aplica)

## Mapeo actual

- `Modules` -> dominio funcional por feature
- `Controllers` -> entrada HTTP
- `Data` -> acceso a base de datos
- `Shared` -> utilidades y soporte transversal
- `Middleware` -> infraestructura de request/response

La estrategia sigue siendo migrar en bloques, sin cambiar el comportamiento existente.
