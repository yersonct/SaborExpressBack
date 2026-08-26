# SaborExpress — Plan de pruebas: Permisos operativos + Turnos (Employees, Orders, Tables, Payments, Deliveries, DailyMenu)

Este plan cubre el bloque completo que se construyó en esta sesión: el
catálogo de permisos (`Permission`), su conexión real en 6 módulos, y el
sistema de turnos (`EmployeeSchedule` con `RoleId`) que determina si un rol
operativo puede o no ejercer sus permisos en este momento.

---

## Parte 1 — Preparación: crear los datos base

Antes de poder probar nada de esto, necesitas empleados con roles operativos
y turnos activos. Sin esto, **todas** las pruebas de Mesero/Cocinero/Cajero/
Repartidor van a fallar con "no tienes permiso", sin importar que el código
esté bien.

### 1.1 Crear un empleado por cada rol operativo

Usa `POST /api/Employees` (como Gerente/Administrador), una vez por cada rol:

| Empleado de prueba | Rol (`RoleIds`) |
|---|---|
| Mesero de prueba | Mesero |
| Cocinero de prueba | Cocinero |
| Cajero de prueba | Cajero |
| Repartidor de prueba | Repartidor |

Recuerda que cada uno recibirá un código de activación por correo — actívalos
con `POST /api/Auth/activate-account` antes de poder loguearlos.

### 1.2 Asignarle un turno activo AHORA MISMO a cada uno

```json
POST /api/employee-schedules
{
  "employeeId": 10,
  "branchId": 2,
  "shiftDate": "2026-08-25",
  "startTime": "00:00",
  "endTime": "23:59",
  "roleId": 5
}
```

**Tip práctico:** usa `startTime: "00:00"` y `endTime: "23:59"` mientras
pruebas, así no dependes de la hora exacta del día. Ajusta a horarios reales
una vez que confirmes que todo funciona.

Repite esto para los 4 roles operativos, cada uno con su propio `roleId`.

---

## Parte 2 — Casos de prueba por módulo

### Employees

| Caso | Quién | Resultado esperado |
|---|---|---|
| Crear empleado | Gerente/Administrador con `CrearEmpleado` | 201 |
| Crear empleado | Un rol sin ese permiso | Bloqueado |
| Dar de baja empleado | Gerente/Administrador con `EliminarEmpleado` | 204/200 |

### Orders

| Caso | Quién | Resultado esperado |
|---|---|---|
| Editar pedido propio | Mesero con `EditarPedidoPropio` + turno activo, dueño del pedido | 200 |
| Editar pedido de otro Mesero | Mesero con `EditarPedidoPropio`, pero no es el dueño | Bloqueado |
| Editar cualquier pedido | Gerente/Administrador | 200 sin importar el dueño |
| Cambiar estado del pedido | Cocinero con `ActualizarEstadoPedido` + turno activo | 200 |
| Cambiar estado del pedido | Cocinero sin turno activo ahora mismo | Bloqueado |
| Cancelar pedido | Mesero con `CancelarPedido` + turno activo | 200 |
| Cancelar pedido | Cocinero (no tiene `CancelarPedido` en el seeder) | Bloqueado |

### Tables

| Caso | Quién | Resultado esperado |
|---|---|---|
| Cambiar estado de mesa (Ocupada/Libre) | Mesero con `CambiarEstadoMesa` + turno activo | 200 |
| Cambiar estado de mesa | Mesero de otra sede | Bloqueado |
| Crear/editar/borrar mesa | Mesero (no tiene ese permiso) | 403 (bloqueado por rol en el controller) |

### Payments

| Caso | Quién | Resultado esperado |
|---|---|---|
| Registrar pago | Cajero con `RegistrarPago` + turno activo | 201 |
| Reembolsar pago | Cajero con `ReembolsarPago` + turno activo | 200 |
| Registrar pago | Mesero (sin `RegistrarPago`) | Bloqueado |

### Deliveries

| Caso | Quién | Resultado esperado |
|---|---|---|
| Ver pedidos disponibles | Repartidor | 200 |
| Tomar un pedido | Repartidor con `TomarEntrega` + turno activo | 201 |
| Tomar un pedido | Repartidor sin turno activo | Bloqueado |
| Actualizar estado de entrega (la suya) | Repartidor dueño de la entrega | 200 |
| Actualizar estado de entrega (de otro) | Repartidor que no es el dueño | Bloqueado |
| Actualizar estado de entrega | Administrador (respaldo, sin necesitar el permiso) | 200 |
| Actualizar estado de entrega | Gerente (NO tiene esta excepción, según diseño) | Bloqueado |

### DailyMenu

| Caso | Quién | Resultado esperado |
|---|---|---|
| Crear/editar/planear menú | Gerente/Administrador | 200/201 |
| Crear/editar/planear menú | Cocinero | 403 (bloqueado por rol en el controller, ni siquiera llega al permiso) |
| Prender/apagar un plato (toggle) | Cocinero con `ActivarDesactivarPlato` + turno activo | 200 |
| Prender/apagar un plato | Cocinero sin turno activo | Bloqueado |
| Prender/apagar un plato | Cocinero de otra sede | Bloqueado |

---

## Parte 3 — Rendimiento esperado

Ninguno de estos endpoints tiene correo ni archivos de por medio, así que
todos deberían responder rápido:

| Endpoint | Tiempo esperado | Motivo |
|---|---|---|
| Cualquier endpoint con `CanPerformActionAsync` | < 250ms | Hace 2-3 consultas extra (empleado, turno activo, permisos del rol) comparado con un endpoint simple |
| `GetCurrentShiftAsync` | < 150ms | Consulta directa por fecha/empleado |
| El resto (listados, creación simple) | < 200ms | Ya validado en módulos anteriores |

Si algún endpoint con permisos sale notablemente más lento que estos rangos,
el sospechoso más probable es `AuthorizationService.CanPerformActionAsync` —
revisa si `GetByRoleIdAsync` (dentro de `RolePermissionService`) hace
consultas repetidas por cada permiso en vez de traer todos los permisos del
rol de una sola vez.

---

## Tabla para ir llenando resultados

| Módulo | Caso | Rol de prueba | Resultado esperado | Resultado real | Tiempo |
|---|---|---|---|---|---|
| Orders | Editar pedido propio | Mesero | 200 | | |
| Orders | Editar pedido de otro | Mesero | Bloqueado | | |
| Orders | Cambiar estado | Cocinero | 200 | | |
| Orders | Cancelar | Mesero | 200 | | |
| Tables | Cambiar estado mesa | Mesero | 200 | | |
| Payments | Registrar pago | Cajero | 201 | | |
| Payments | Reembolsar pago | Cajero | 200 | | |
| Deliveries | Tomar entrega | Repartidor | 201 | | |
| Deliveries | Actualizar estado (dueño) | Repartidor | 200 | | |
| Deliveries | Actualizar estado (Admin respaldo) | Administrador | 200 | | |
| DailyMenu | Toggle plato | Cocinero | 200 | | |
| DailyMenu | Crear/editar menú | Cocinero | 403 | | |

---

## Por dónde empezar

Empieza por **Orders → "Editar pedido propio" con el Mesero** — es el caso
más representativo, porque valida en una sola prueba: (1) que el permiso
existe y está bien nombrado, (2) que el turno activo se está detectando
correctamente, y (3) que la comparación de "dueño del pedido" funciona. Si
este caso sale bien, es una señal fuerte de que todo el mecanismo de
`CanPerformActionAsync` está funcionando como se diseñó.