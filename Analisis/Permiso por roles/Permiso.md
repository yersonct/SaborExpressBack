# SaborExpress — Matriz de Roles, Módulos y Permisos

Este documento es la referencia central para saber **qué puede hacer cada rol
en cada módulo**, y qué permisos granulares (`Permission`) hacen falta crear
en el catálogo para cubrir toda la operación del restaurante.

Los 7 roles del sistema: **Cliente, Mesero, Cocinero, Cajero, Repartidor,
Administrador, Gerente.**

---

## 1. Los dos niveles de control que ya tienes construidos

Es importante distinguir esto antes de la matriz, porque no todo se controla igual:

| Nivel | Cómo se aplica | Ejemplo |
|---|---|---|
| **Rol (grueso)** | `[Authorize(Roles = "...")]` en el controller — decide si puedes entrar al endpoint siquiera | Solo Gerente/Administrador pueden entrar a `POST /api/Employees` |
| **Permiso (fino)** | `AuthorizationService.CanPerformActionAsync(employeeId, "NombrePermiso")` — decide una acción puntual, y además valida turno activo | Un Mesero con turno activo puede editar un pedido si tiene `ORDERS_EDIT`; otro Mesero sin ese permiso puntual, no |

El rol dice **"a qué módulo entras"**; el permiso dice **"qué tan lejos llegas dentro de ese módulo, y solo mientras estás en tu turno"**. Gerente y Administrador quedan exentos del chequeo de turno (como ya definimos), el resto de roles operativos sí lo necesita.

---

## 2. Rol de cada uno en el restaurante (resumen operativo)

| Rol | Qué hace en la vida real |
|---|---|
| **Cliente** | Pide domicilios/pedidos desde la app, gestiona su perfil, sus direcciones, deja reseñas |
| **Mesero** | Toma pedidos en mesa, los edita, marca mesas ocupadas/libres |
| **Cocinero** | Ve los pedidos pendientes, actualiza el estado de preparación, gestiona el menú del día |
| **Cajero** | Cobra pedidos, procesa pagos y reembolsos |
| **Repartidor** | Toma domicilios disponibles, actualiza el estado de la entrega |
| **Administrador** | Gestiona su propia sede: empleados, mesas, turnos, ve reportes de su sede |
| **Gerente** | Control total de la cadena: todas las sedes, catálogo de roles/permisos, configuración global |

---

## 3. Matriz completa por módulo

Leyenda: ✅ Sí puede · ❌ No puede · 🔶 Parcial/condicionado (se explica en notas)

### Auth

| Acción | Cliente | Mesero | Cocinero | Cajero | Repartidor | Admin | Gerente |
|---|---|---|---|---|---|---|---|
| Login / logout / refresh-token | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Recuperar contraseña | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Activar cuenta nueva (empleado) | ❌ | ✅ (una vez) | ✅ (una vez) | ✅ (una vez) | ✅ (una vez) | ✅ (una vez) | N/A |

*Todos los roles pasan por Auth por igual — no hay diferenciación aquí, es la puerta de entrada común.*

---

### Branches (Sedes)

| Acción | Cliente | Mesero | Cocinero | Cajero | Repartidor | Admin | Gerente |
|---|---|---|---|---|---|---|---|
| Ver todas las sedes | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |
| Ver mi propia sede | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |
| Crear sede | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |
| Editar sede | ❌ | ❌ | ❌ | ❌ | ❌ | 🔶 solo la suya | ✅ |
| Eliminar sede | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |

---

### Customers

| Acción | Cliente | Mesero | Cocinero | Cajero | Repartidor | Admin | Gerente |
|---|---|---|---|---|---|---|---|
| Registrarse | ✅ (público) | — | — | — | — | — | — |
| Ver/editar mi propio perfil | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Ver lista de clientes | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |
| Ver detalle de un cliente | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |

---

### Employees

| Acción | Cliente | Mesero | Cocinero | Cajero | Repartidor | Admin | Gerente |
|---|---|---|---|---|---|---|---|
| Ver listado de empleados | ❌ | ❌ | ❌ | ❌ | ❌ | 🔶 solo su sede | ✅ todas |
| Ver detalle de un empleado | ❌ | ❌ | ❌ | ❌ | ❌ | 🔶 solo su sede | ✅ |
| Crear empleado | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ (permiso `CrearEmpleado`) | ✅ |
| Editar empleado | ❌ | ❌ | ❌ | ❌ | ❌ | 🔶 solo su sede | ✅ |
| Dar de baja empleado | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ (permiso `EliminarEmpleado`) | ✅ |
| Ver CV | ❌ | ❌ | ❌ | ❌ | ❌ | 🔶 solo su sede | ✅ |

---

### Roles / Permissions / RolePermissions / UserRoles

| Acción | Cliente | Mesero | Cocinero | Cajero | Repartidor | Admin | Gerente |
|---|---|---|---|---|---|---|---|
| Ver catálogo de roles/permisos | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |
| Crear/editar/borrar roles o permisos | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |
| Asignar rol a un usuario | ❌ | ❌ | ❌ | ❌ | ❌ | 🔶 solo su sede, no puede dar Admin/Gerente | ✅ sin restricción |

---

### Categories / Products (Menú general)

| Acción | Cliente | Mesero | Cocinero | Cajero | Repartidor | Admin | Gerente |
|---|---|---|---|---|---|---|---|
| Ver categorías/productos | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Crear/editar/borrar categoría o producto | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |
| Activar/desactivar producto | ❌ | ❌ | 🔶 recomendado | ❌ | ❌ | ✅ | ✅ |

**Nota:** hoy `Products` solo deja Gerente/Administrador. Vale la pena considerar
si el Cocinero debería poder desactivar un plato agotado sin llamar al Administrador
(`PATCH /api/Products/{id}/status`) — es un caso de uso real ("se acabó el pescado del día").

---

### DailyMenu (Menú del día)

| Acción | Cliente | Mesero | Cocinero | Cajero | Repartidor | Admin | Gerente |
|---|---|---|---|---|---|---|---|
| Ver el menú de hoy | ✅ | ✅ | ✅ | ✅ | — | ✅ | ✅ |
| Planear/crear el menú de una fecha | ❌ | ❌ | 🔶 recomendado | ❌ | ❌ | ✅ | ✅ |
| Prender/apagar un plato a media jornada | ❌ | ❌ | ✅ recomendado | ❌ | ❌ | ✅ | ✅ |

**Nota:** este es el módulo donde el Cocinero debería tener más participación —
hoy el diseño solo contempla Gerente/Administrador, pero operativamente quien
mejor sabe si "se acabó el plato del día" es el propio cocinero en el momento.

---

### Orders / OrderDetails y sus historiales

| Acción | Cliente | Mesero | Cocinero | Cajero | Repartidor | Admin | Gerente |
|---|---|---|---|---|---|---|---|
| Crear pedido | 🔶 desde la app (domicilio) | ✅ en mesa | ❌ | ❌ | ❌ | ✅ | ✅ |
| Ver pedido propio / de mi mesa | ✅ (solo mío) | ✅ | 🔶 solo para preparar | ✅ | 🔶 solo el que entrego | ✅ | ✅ |
| Ver todos los pedidos de una sede | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |
| Editar pedido (agregar/quitar líneas) | ❌ | ✅ (permiso `ORDERS_EDIT`) | ❌ | ❌ | ❌ | ✅ (`ORDERS_EDIT_ALL`) | ✅ |
| Cambiar estado del pedido | ❌ | 🔶 a "Confirmado" | ✅ a "En preparación"/"Listo" | 🔶 a "Entregado" en mostrador | ❌ (usa Deliveries) | ✅ | ✅ |
| Cancelar pedido | ❌ | ✅ (permiso `ORDERS_CANCEL`) | ❌ | ❌ | ❌ | ✅ | ✅ |
| Ver historial de cambios | ❌ | 🔶 | 🔶 | 🔶 | ❌ | ✅ | ✅ |

**Permiso granular sugerido nuevo:** `ORDERS_STATUS_UPDATE` — específico para que
el Cocinero pueda mover el pedido entre estados de preparación sin necesitar el
permiso completo de `ORDERS_EDIT` (que permite modificar líneas/productos).

---

### Payments

| Acción | Cliente | Mesero | Cocinero | Cajero | Repartidor | Admin | Gerente |
|---|---|---|---|---|---|---|---|
| Registrar un pago | ❌ | ❌ | ❌ | ✅ (permiso `PAYMENTS_CREATE`) | ❌ | ✅ | ✅ |
| Ver pagos de un pedido | ❌ | 🔶 | ❌ | ✅ | ❌ | ✅ | ✅ |
| Reembolsar un pago | ❌ | ❌ | ❌ | 🔶 (permiso `PAYMENTS_REFUND`, si se le otorga) | ❌ | ✅ | ✅ |
| Ver todos los pagos (reportes) | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |

**Permisos granulares sugeridos:** `PAYMENTS_CREATE` y `PAYMENTS_REFUND` — hoy
no aparecen en tu catálogo (`CrearSede`, `EditarSede`, ..., `CrearEmpleado`,
`EliminarEmpleado`), pero son necesarios si quieres controlar finamente quién
de los cajeros puede hacer reembolsos (una acción más sensible que cobrar).

---

### Tables

| Acción | Cliente | Mesero | Cocinero | Cajero | Repartidor | Admin | Gerente |
|---|---|---|---|---|---|---|---|
| Ver mesas de una sede | ❌ | ✅ | ✅ | ✅ | ❌ | ✅ | ✅ |
| Crear/editar/borrar mesa | ❌ | ❌ | ❌ | ❌ | ❌ | 🔶 solo su sede | ✅ |
| Cambiar estado (Libre/Ocupada/Reservada) | ❌ | ✅ recomendado | ❌ | ❌ | ❌ | ✅ | ✅ |

**Nota:** hoy `PATCH /api/Tables/{id}/status` solo permite Gerente/Administrador
— en la práctica, el Mesero es quien más necesita marcar una mesa como
"Ocupada" al sentar clientes. Vale la pena habilitarle este permiso puntual
(`TABLES_UPDATE_STATUS`).

---

### Deliveries

| Acción | Cliente | Mesero | Cocinero | Cajero | Repartidor | Admin | Gerente |
|---|---|---|---|---|---|---|---|
| Ver pedidos disponibles para domicilio | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ |
| Tomar un pedido | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ |
| Ver mis propias entregas | ❌ | ❌ | ❌ | ❌ | ✅ (solo lo suyo) | 🔶 solo su sede | ✅ |
| Ver todas las entregas de una sede | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |
| Actualizar estado de entrega | ❌ | ❌ | ❌ | ❌ | ✅ (la suya) | 🔶 como respaldo | ❌ (Gerente NO tiene esta excepción, según tu diseño) |
| Ver estado de mi propio domicilio | ✅ | — | — | — | — | — | — |

---

### Reviews

| Acción | Cliente | Mesero | Cocinero | Cajero | Repartidor | Admin | Gerente |
|---|---|---|---|---|---|---|---|
| Dejar una reseña (pedido propio, desde la app) | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Editar mi propia reseña | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Ver reseñas de un pedido | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |
| Ver reseñas de una sede | ❌ | ❌ | ❌ | ❌ | ❌ | 🔶 solo su sede | ✅ cualquiera |
| Eliminar una reseña | 🔶 la propia | ❌ | ❌ | ❌ | ❌ | 🔶 solo su sede | ✅ cualquiera |

---

### Addresses

| Acción | Cliente | Mesero | Cocinero | Cajero | Repartidor | Admin | Gerente |
|---|---|---|---|---|---|---|---|
| Ver/crear/editar/borrar mis direcciones | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Ver direcciones de un cliente (para entrega) | ❌ | ❌ | ❌ | ❌ | 🔶 solo la del pedido que lleva | ❌ | ❌ |

---

### UserPreferences

| Acción | Cliente | Mesero | Cocinero | Cajero | Repartidor | Admin | Gerente |
|---|---|---|---|---|---|---|---|
| Ver/editar mis propias preferencias | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

*Es 100% personal para todos los roles por igual, nadie ve las de otro (según tu diseño actual).*

---

### Configurations

| Acción | Cliente | Mesero | Cocinero | Cajero | Repartidor | Admin | Gerente |
|---|---|---|---|---|---|---|---|
| Ver configuración (idioma, tema, IVA, etc.) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Crear/editar/borrar configuración | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |

---

### Notifications

| Acción | Cliente | Mesero | Cocinero | Cajero | Repartidor | Admin | Gerente |
|---|---|---|---|---|---|---|---|
| Ver mis propias notificaciones | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Enviar notificación manual | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |

---

### EmployeePermissions / EmployeeSchedules

| Acción | Cliente | Mesero | Cocinero | Cajero | Repartidor | Admin | Gerente |
|---|---|---|---|---|---|---|---|
| Ver mis propios turnos/permisos | ❌ | ✅ (mis turnos) | ✅ | ✅ | ✅ | ✅ | ✅ |
| Asignar turno / otorgar permiso puntual | ❌ | ❌ | ❌ | ❌ | ❌ | 🔶 solo su sede | ✅ |

---

## 4. Catálogo de permisos granulares — lo que ya existe vs. lo que falta

### Ya existen (confirmado por tu consulta a `role-permissions`)

```
CrearSede, EditarSede, EliminarSede, GestionarEmpleados, AprobarRegistros,
VerReportes, GestionarMenu, CrearEmpleado, EliminarEmpleado
```

### Permisos nuevos sugeridos, para cubrir los roles operativos (Mesero, Cocinero, Cajero, Repartidor)

| Permiso sugerido | Para qué rol | Para qué acción |
|---|---|---|
| `ORDERS_CREATE` | Mesero | Crear un pedido nuevo en mesa |
| `ORDERS_EDIT` | Mesero | Editar líneas de un pedido propio *(ya lo tienes mencionado en el doc maestro)* |
| `ORDERS_EDIT_ALL` | Administrador/Gerente | Editar cualquier pedido, no solo el propio *(ya existe)* |
| `ORDERS_CANCEL` | Mesero | Cancelar un pedido *(ya lo tienes mencionado)* |
| `ORDERS_STATUS_UPDATE` | Cocinero | Mover el pedido entre estados de preparación, sin editar líneas |
| `TABLES_UPDATE_STATUS` | Mesero | Marcar mesa Ocupada/Libre/Reservada |
| `PAYMENTS_CREATE` | Cajero | Registrar un cobro |
| `PAYMENTS_REFUND` | Cajero (opcional, más sensible) | Procesar un reembolso |
| `MENU_TOGGLE` | Cocinero | Prender/apagar un plato del menú del día |
| `DELIVERY_TAKE` | Repartidor | Tomar un pedido disponible del pool |
| `DELIVERY_UPDATE_STATUS` | Repartidor | Actualizar el estado de su entrega |

---

## 5. Cómo se conecta esto con lo que ya diseñamos de turnos

Recuerda el pendiente que dejamos abierto: agregar `RoleId` a `EmployeeSchedule`,
para que el turno determine **con qué rol** trabaja el empleado ese día. Esta
matriz es justo la base para definir, cuando lleguemos a ese punto, **qué
permisos se activan automáticamente según el rol del turno activo** — por
ejemplo, si hoy Camilo tiene turno de Cocinero, el sistema solo le habilita
`ORDERS_STATUS_UPDATE` y `MENU_TOGGLE`, no `PAYMENTS_CREATE` aunque también
tenga el rol Cajero asignado de forma permanente.

---

## Próximo paso sugerido

Esta matriz es la base para ir creando, en orden, los permisos que faltan
(sección 4) y aplicarlos a los endpoints correspondientes con el mismo patrón
que ya usamos en Employees (`_authorizationService.CanPerformActionAsync(...)`).
Sugiero empezar por **Orders**, ya que es el módulo más usado del día a día y
el que más roles operativos toca (Mesero, Cocinero, y de forma indirecta
Cajero y Repartidor).