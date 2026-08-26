# SaborExpress — Backlog: Sistema de turnos, roles y permisos

## Contexto general

El concepto central del sistema de turnos: `EmployeeSchedule` no le da el rol
al empleado — el rol ya lo tiene de forma permanente en `UserRoles`. El turno
solo activa temporalmente cuál de esos roles está ejerciendo en ese momento.

**Regla cerrada:** si un `Employee` no tiene un `EmployeeSchedule` activo en
este momento (según fecha y hora), el sistema lo trata exactamente igual que
a un Cliente normal — sin turno activo = sin permisos operativos, pero con
acceso normal a su cuenta.

**Decisión tomada:** se elimina el módulo `EmployeePermission` (excepciones
puntuales de permisos por empleado individual). Los permisos ahora dependen
únicamente del rol (`RolePermission`), sin capa de excepciones por persona.

---

## Estado del backlog

| # | Tarea | Depende de | Estado |
|---|---|---|---|
| 0 | Migración de `EmployeePermissions` (limpieza tras eliminar el módulo) | — | ✅ Cerrado |
| 0.5 | `AuthorizationService.cs` confirmado correcto contra `IRolePermissionService` real | — | ✅ Cerrado |
| **1** | **Agregar `RoleId` a `EmployeeSchedule`** | — | 👉 Siguiente |
| 2 | Endpoint `GET /api/employee-schedules/employee/{id}/current` — rol activo según turno actual | Punto 1 | 🔲 Pendiente |
| 3 | `AuthorizationService` valida contra el rol del turno actual, no solo si "está en turno" | Puntos 1 y 2 | 🔲 Pendiente |
| 4 | `DeactivateAsync` en `EmployeeService` — quitar roles operativos al dar de baja, dejar solo Cliente | — | 🔲 Pendiente (sin migración) |
| 5 | Filtrar `GET /api/Employees` para no mostrar `Status = "Retirado"` por defecto (`?includeRetired=true` opcional) | — | 🔲 Pendiente (sin migración) |
| 7 | Horario de apertura/cierre por sede (`BranchSetting`) — evita pedidos con la sede cerrada, aplica igual a Clientes y a Empleados-actuando-como-Cliente | — | 🔲 Pendiente, nuevo |
| 6 | Notificación recordatorio cuando el turno está por terminar ("¿Quieres modificar el turno de hoy?") | Módulo Notifications (ya listo) | 🔲 Pendiente, no urgente |

---

## Detalle del punto 1 (siguiente a resolver)

**Objetivo:** cada turno (`EmployeeSchedule`) debe indicar explícitamente con
qué rol trabaja el empleado ese día puntual — así Juan puede ser Cocinero el
lunes y Mesero el martes, sin ambigüedad.

**Archivos del módulo `EmployeeSchedules` ya relevados** (contenido completo
disponible para editar):

- `DTOs/CreateEmployeeScheduleDto.cs`
- `DTOs/EmployeeScheduleResponseDto.cs`
- `DTOs/UpdateEmployeeScheduleDto.cs`
- `DTOs/UpdateScheduleStatusDto.cs`
- `Enum/ScheduleStatus.cs`
- `Interfaces/IEmployeeScheduleRepository.cs`
- `Interfaces/IEmployeeScheduleService.cs`
- `Mappings/EmployeeScheduleMappings.cs`
- `Models/EmployeeSchedule.cs`
- `Repositories/EmployeeScheduleRepository.cs`
- `Services/EmployeeScheduleService.cs`
- `Validators/EmployeeScheduleValidator.cs`
- `Controllers/EmployeeSchedulesController.cs`

**Cambio necesario:** agregar `RoleId` (int, FK a `Role`) al modelo
`EmployeeSchedule`, propagarlo a los DTOs de creación/edición/respuesta, al
mapper, y validar en el servicio que el rol elegido sea uno de los roles que
el empleado realmente tiene asignados en `UserRoles` (no debería poder
asignársele un turno con un rol que no le corresponde).

**Requiere migración:** sí — se agrega una columna nueva (`role_id`) a la
tabla `EmployeeSchedules`.

**Nota detectada en el código actual:** el `EmployeeScheduleValidator.cs`
tiene el mismo problema de encoding que ya resolvimos en Auth (`vÃ¡lido` en
vez de `válido`) — se puede corregir de paso al tocar este archivo.

---

## Próximo paso

Continuar con el punto 1: modificar el modelo `EmployeeSchedule` y los
archivos relacionados para incluir `RoleId`, generar la migración
correspondiente, y validar contra los roles reales del empleado antes de
avanzar al punto 2 (endpoint de "rol activo ahora mismo").