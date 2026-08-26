# SaborExpress — Documento maestro de entidades y endpoints (completo)

Este documento cubre **todas las entidades reales que existen en tu `AppDbContext`**,
más los 2 módulos nuevos que salieron en esta conversación (`EmployeePermissions`,
`EmployeeSchedules`). Por cada una: qué es, con qué se relaciona, y la tabla
**completa** de endpoints (los que ya existen y los que faltan).

## ⚠️ Aviso importante antes de empezar

Conté los `DbSet` reales de tu `AppDbContext.cs` y son exactamente **23**:

```
PasswordResetCode, User, Role, Permission, RolePermission, UserRole,
Customer, Employee, Branch, Category, Product, Order, OrderDetail,
OrderDetailHistory, OrderStatusHistory, Payment, Table, Delivery,
Review, Address, UserPreference, Configuration (BranchSetting),
EmailConfirmationCode
```

**`Notification` NO aparece como `DbSet` en tu `AppDbContext`.** Tu documento
original trae una sección 20 completa para Notifications, pero si no hay una
tabla `Notifications` creada todavía, ese módulo no puede funcionar — sería
necesario agregar `DbSet<Notification> Notifications` + su modelo + migración
antes de construir el controller. Te lo marco como pendiente de confirmar más
abajo, en su propia sección.

También noté que `PasswordResetCode` y `EmailConfirmationCode` sí son tablas
propias en tu base de datos, pero en el documento original solo aparecen
mencionadas dentro de Auth, sin sus propios endpoints — las agrego como
sub-secciones de Auth para que no se pierdan del conteo.

---

## 1. Auth (tabla base: `User`, + `PasswordResetCode`, `EmailConfirmationCode`, `RefreshToken`, `EmployeeActivationCode`)

**Qué es:** `User` es la cuenta base de cualquier persona que inicia sesión —
cliente o empleado. `PasswordResetCode`, `EmailConfirmationCode`, `RefreshToken`
y `EmployeeActivationCode` son tablas de apoyo con códigos/tokens temporales de
seguridad.

**Se relaciona con:** `UserRole` (roles del usuario), `Customer` (1 a 1),
`Employee` (1 a 1), `RefreshToken` (1 a muchos), `EmployeeActivationCode` (1 a muchos).

| Método | Endpoint | Estado |
|---|---|---|
| POST | `/api/Auth/login` | 🟢 Revisado — bloqueo por intentos fallidos, JWT con roles, `LoginValidator` completo, ahora también emite refresh-token |
| POST | `/api/Auth/forgot-password` | 🟢 Revisado — no revela si el usuario existe, invalida códigos anteriores, rate-limit (3 intentos/15 min) |
| POST | `/api/Auth/verify-reset-code` | 🟢 Revisado — valida código + expiración + máximo de intentos (5), genera `ResetToken` de un solo uso |
| POST | `/api/Auth/reset-password` | 🟢 Revisado — el repositorio ya filtra token completado/expirado en la consulta, no hace falta el fix que se había propuesto |
| POST | `/api/Auth/logout` | 🟢 Revisado — usa `[Authorize]`, saca el `userId` del claim correctamente, ahora también revoca todos los refresh-tokens del usuario |
| POST | `/api/Auth/confirm-email` | 🟢 Revisado — mismo patrón sólido que reset de contraseña |
| POST | `/api/Auth/resend-confirmation-code` | 🟢 Revisado — bonus, no estaba en el documento original pero ya existe y funciona |
| POST | `/api/Auth/refresh-token` | 🟢 Diseñado e implementado — access token corto + refresh-token de 12h con rotación |
| POST | `/api/Auth/activate-account` | 🟢 **Nuevo** — el empleado usa el código enviado al crearse la cuenta para definir su propia contraseña; valida código (máx. 5 intentos, expira en 30 min), activa `User.Status = true` |



---

## 2. Branches (`Branch`)

**Qué es:** cada sucursal física del restaurante.

**Se relaciona con:** `Employee`, `Table`, `Order`, `Configuration` (parámetros
propios de la sucursal), `EmployeeSchedule` (nuevo).

| Método | Endpoint | Estado |
|---|---|---|
| GET | `/api/Branches` | ✅ Existe — restringido solo a Gerente |
| POST | `/api/Branches` | ✅ Existe — restringido solo a Gerente, valida nombre único |
| GET | `/api/Branches/mine` | ✅ Existe — Gerente/Admin, devuelve la sede del usuario logueado |
| GET | `/api/Branches/{id}` | ✅ Existe — restringido solo a Gerente |
| PUT | `/api/Branches/{id}` | ✅ Existe — Gerente/Admin, pero Admin solo puede editar su propia sede |
| DELETE | `/api/Branches/{id}` | ✅ Existe — bloquea el borrado si la sede tiene empleados asignados |

---

## 3. Customers (`Customer`)

**Qué es:** el perfil de cliente de la app.

**Se relaciona con:** `User` (1 a 1), `Order` (1 a muchos), `Address` (1 a muchos),
`Review` (1 a muchos).

| Método | Endpoint | Estado |
|---|---|---|
| POST | `/api/Customers/register` | 🟢 Crea User+Customer, asigna rol, dispara confirmación de correo |
| GET | `/api/Customers/me` | 🟢 Protegido por rol Cliente |
| PUT | `/api/Customers/me` | 🟢 Protegido por rol Cliente |
| GET | `/api/Customers` | 🟢 Protegido por rol Gerente/Administrador |
| GET | `/api/Customers/{id}` | 🟢 Protegido por rol Gerente/Administrador |

**Pendiente menor sin resolver:** borrar `CreateCustomerDto.cs` (código muerto).

---

## 4. Employees (`Employee`) — ✅ COMPLETADO

**Qué es:** cualquier trabajador del restaurante (mesero, cocinero, cajero,
repartidor, gerente, admin). El rol específico vive en `UserRole`.

**Se relaciona con:** `User` (1 a 1), `Branch` (muchos a 1), `Order`, `OrderDetail`,
`OrderDetailHistory`, `OrderStatusHistory` (como responsable), `EmployeePermission`
(nuevo), `EmployeeSchedule` (nuevo).

| Método | Endpoint | Estado |
|---|---|---|
| GET | `/api/Employees` | ✅ |
| GET | `/api/Employees/{id}` | ✅ |
| PUT | `/api/Employees/{id}` | ✅ |
| GET | `/api/Employees/{id}/cv` | ✅ |
| POST | `/api/Employees` | ✅ Crea `User` + `Employee` juntos, con candado `CrearEmpleado` |
| DELETE | `/api/Employees/{id}` | ✅ Soft-delete (`Status = "Retirado"`), con candado `EliminarEmpleado` |

---

## 5. Permissions (`Permission`)

**Qué es:** catálogo de acciones posibles del sistema (`ORDERS_EDIT`, `ORDERS_CANCEL`...).

**Se relaciona con:** `RolePermission`.

| Método | Endpoint | Estado |
|---|---|---|
| GET | `/api/Permissions` | 🟢 Revisado — restringido a Gerente/Administrador |
| POST | `/api/Permissions` | 🟢 Revisado — nombre único, validado contra el enum `PermissionAction` |
| GET | `/api/Permissions/{id}` | 🟢 Revisado |
| PUT | `/api/Permissions/{id}` | 🟢 Revisado — mismas validaciones que create |
| DELETE | `/api/Permissions/{id}` | 🟢 Revisado — `HasAssignedRolesAsync` bloquea el borrado si el permiso está en uso (mejora opcional: envolver en try-catch por si en el futuro otra entidad además de `RolePermission` llega a referenciarlo) |
---

## 6. RolePermissions (`RolePermission`)

**Qué es:** tabla puente — qué permisos tiene cada rol.

**Se relaciona con:** `Role` (muchos a 1), `Permission` (muchos a 1).

| Método | Endpoint | Estado |
|---|---|---|
| POST | `/api/role-permissions` | 🟢 Revisado — valida que RoleId y PermissionId existan antes de crear, y que no esté duplicado |
| GET | `/api/role-permissions` | 🟢 Revisado |
| DELETE | `/api/role-permissions/{roleId}/{permissionId}` | 🟢 Revisado — valida que la relación exista |
| GET | `/api/role-permissions/role/{roleId}` | 🟢 Revisado |
---

## 7. Roles (`Role`)

**Qué es:** catálogo de roles (Mesero, Cocinero, Cajero, Gerente, Admin, Cliente...).

**Se relaciona con:** `UserRole`, `RolePermission`.

| Método | Endpoint | Estado |
|---|---|---|
| GET | `/api/Roles` | 🟢 Revisado — restringido a Gerente/Administrador |
| POST | `/api/Roles` | 🟢 Revisado — nombre único, validado contra el enum `RoleName` (incluye `Cliente`) |
| GET | `/api/Roles/{id}` | 🟢 Revisado |
| PUT | `/api/Roles/{id}` | 🟢 Revisado — mismas validaciones que create, `Cliente` se puede editar sin problema |
| DELETE | `/api/Roles/{id}` | 🟢 Revisado — `HasAssignedUsersAsync` bloquea el borrado si hay usuarios (empleados o clientes) con ese rol |
---

## 8. UserRoles (`UserRole`)

**Qué es:** tabla puente — qué roles tiene cada usuario (puede tener varios a la vez).

**Se relaciona con:** `User` (muchos a 1), `Role` (muchos a 1).

| Método | Endpoint | Estado |
|---|---|---|
| POST | `/api/user-roles` | 🟢 Revisado — reglas de negocio completas: valida existencia, no permite duplicados, un Administrador no puede asignar Admin/Gerente, solo puede operar dentro de su propia sede, máximo 1 Administrador por sede |
| GET | `/api/user-roles` | 🟢 Revisado |
| DELETE | `/api/user-roles/{userId}/{roleId}` | 🟢 Revisado — ahora tiene las mismas restricciones que `POST` (antes solo `AssignAsync` las tenía, era asimétrico); bloquea quitar el rol Administrador si es el único de la sede |
| GET | `/api/user-roles/user/{userId}` | 🟢 Revisado |

---
## 3. Categories (`Category`)

**Qué es:** categorías del menú (ej: Bebidas, Entradas, Postres).

**Se relaciona con:** `Product` (1-N — una categoría tiene muchos productos).

| Método | Endpoint | Estado |
|---|---|---|
| GET | `/api/Categories` | ✅ Existe — cualquier usuario autenticado (sin restricción de rol) |
| GET | `/api/Categories/{id}` | ✅ Existe — cualquier usuario autenticado (sin restricción de rol) |
| POST | `/api/Categories` | ✅ Existe — Gerente/Administrador, valida nombre único |
| PUT | `/api/Categories/{id}` | ✅ Existe — Gerente/Administrador, valida nombre único (excluyendo el propio id) |
| DELETE | `/api/Categories/{id}` | ✅ Existe — Gerente/Administrador, bloquea el borrado si la categoría tiene productos activos |

---

| 12 | **Products** | ✅ Completo — CRUD funcional, `[Authorize]` agregado en POST/PUT/DELETE/status (Gerente/Administrador), DELETE valida `OrderDetail` asociados. |

## 10. Products (`Product`)

**Qué es:** cada plato/bebida del menú.

**Se relaciona con:** `Category` (muchos a 1), `OrderDetail` (1 a muchos).

| Método | Endpoint | Estado |
|---|---|---|
| GET | `/api/Products` | ✅ Cualquier autenticado |
| GET | `/api/Products/{id}` | ✅ Cualquier autenticado |
| POST | `/api/Products` | ✅ Gerente/Administrador, validaciones completas |
| PUT | `/api/Products/{id}` | ✅ Gerente/Administrador |
| DELETE | `/api/Products/{id}` | ✅ Gerente/Administrador, bloquea el borrado si tiene `OrderDetail` asociados |
| GET | `/api/Products/category/{categoryId}` | ✅ Cualquier autenticado |
| PATCH | `/api/Products/{id}/status` | ✅ Gerente/Administrador |

---

| 14 | **Orders** | ✅ Completo — `[Authorize]` agregado, candado `ORDERS_EDIT`/`ORDERS_EDIT_ALL` aplicado consistentemente en PUT/status/cancel, `EmployeeExistsAsync` ya se valida en create. Pendiente confirmar `GetCurrentEmployeeId()`. |

## 11. Orders (`Order`)

**Qué es:** el pedido — documento central del sistema.

**Se relaciona con:** `Customer` (opcional), `Employee`, `Table` (opcional),
`Branch`, `OrderDetail` (1 a muchos), `OrderStatusHistory` (1 a muchos), `Payment`
(1 a muchos), `Delivery` (0 o 1), `Review` (0 o 1).

| Método | Endpoint | Estado |
|---|---|---|
| POST | `/api/Orders` | ✅ `[Authorize]`, ahora valida que el empleado exista (`EmployeeExistsAsync`) |
| GET | `/api/Orders/{id}` | ✅ `[Authorize]` (cualquier autenticado) |
| GET | `/api/Orders` | ✅ `[Authorize]`, con filtro branch/status |
| GET | `/api/Orders/table/{tableId}` | ✅ `[Authorize]` |
| GET | `/api/Orders/customer/{customerId}` | ✅ `[Authorize]` |
| GET | `/api/Orders/branch/{branchId}` | ✅ `[Authorize]` |
| PUT | `/api/Orders/{id}` | ✅ Bloquea estados finales; candado `ORDERS_EDIT`/`ORDERS_EDIT_ALL` distingue pedido propio vs. cualquiera |
| PATCH | `/api/Orders/{id}/status` | ✅ Máquina de estados + historial automático + candado de permisos aplicado |
| PATCH | `/api/Orders/{id}/cancel` | ✅ Nunca borra, exige motivo, con historial + candado de permisos aplicado |

**Pendiente:** confirmar que `GetCurrentEmployeeId()` (en `Shared.Extensions`) resuelve el `EmployeeId` real desde el JWT y no un valor mockeado.
---

## 12. OrderDetails (`OrderDetail`)

**Qué es:** cada línea de un pedido (1 producto + cantidad + notas).

**Se relaciona con:** `Order` (muchos a 1), `Product` (muchos a 1), `Employee`
(quién la modificó), `OrderDetailHistory` (1 a muchos).

| Método | Endpoint | Estado |
|---|---|---|
| GET | `/api/orders/{orderId}/details` | 🟢 |
| POST | `/api/orders/{orderId}/details` | 🟢 `UnitPrice` del catálogo. Recálculo de `Order.Total` resuelto. `[Authorize]` aplicado. `GetCurrentEmployeeId()` centralizado sin fallback silencioso |
| PUT | `/api/order-details/{id}` | 🟢 Ahora valida que el pedido padre no esté `Delivered`/`Cancelled` antes de editar la línea |
| PATCH | `/api/order-details/{id}/void` | 🟢 Misma validación de estado del pedido padre aplicada antes de anular |

**Pendiente de confirmar:** que el proyecto compile sin errores tras los cambios
en `OrderDetailValidator.cs` (métodos `ValidateUpdate`/`ValidateVoid` pasaron de
`void` a `async Task`) y `OrderDetailService.cs` (se agregaron los `await`
correspondientes).

---

## 13. OrderDetailHistory (`OrderDetailHistory`)

**Qué es:** auditoría de cambios sobre una línea de pedido (creada, editada, anulada).

**Se relaciona con:** `OrderDetail` (muchos a 1), `Employee` (quién cambió).

| Método | Endpoint | Estado |
|---|---|---|
| GET | `/api/order-details/{orderDetailId}/history` | 🟢 Ruta correcta, orden descendente, valida existencia, solo lectura, con `[Authorize]` |
---

## 14. OrderStatusHistory (`OrderStatusHistory`)

**Qué es:** auditoría de cambios de estado del pedido completo.

**Se relaciona con:** `Order` (muchos a 1), `Employee` (quién cambió).

| Método | Endpoint | Estado |
|---|---|---|
| GET | `/api/orders/{orderId}/status-history` | 🟢 Ruta correcta, orden descendente, valida existencia, solo lectura, con `[Authorize]` |

## 15. Payments (`Payment`)

**Qué es:** el registro de cobro de un pedido.

**Se relaciona con:** `Order` (muchos a 1, puede haber más de un pago por pedido).

| Método | Endpoint | Estado |
|---|---|---|
| POST | `/api/Payments` | 🟢 `PaymentMethod` confirmado, valida monto contra saldo pendiente del pedido, bloquea pedidos `Pending`/`Cancelled`, con `[Authorize]` y `GetCurrentEmployeeId()` corregido |
| GET | `/api/Payments/order/{orderId}` | 🟢 |
| GET | `/api/Payments/{id}` | 🟢 |
| PATCH | `/api/Payments/{id}/refund` | 🟢 Valida estado antes de reembolsar |
| GET | `/api/Payments` | 🟢 Filtro branch/fecha |

---

| 13 | **Tables** | ✅ Completo — `[Authorize]` agregado, Administrador restringido a su propia sede (mismo patrón que `Branches`). |

## 16. Tables (`Table`)

**Qué es:** las mesas físicas de cada sucursal.

**Se relaciona con:** `Branch` (muchos a 1), `Order` (1 a muchos).

| Método | Endpoint | Estado |
|---|---|---|
| GET | `/api/Tables/branch/{branchId}` | ✅ Cualquier autenticado |
| GET | `/api/Tables/{id}` | ✅ Cualquier autenticado |
| POST | `/api/Tables` | ✅ Gerente/Administrador (Admin solo su sede), valida sucursal existe y número duplicado |
| PUT | `/api/Tables/{id}` | ✅ Gerente/Administrador (Admin solo su sede) |
| PATCH | `/api/Tables/{id}/status` | ✅ Gerente/Administrador (Admin solo su sede) |
| DELETE | `/api/Tables/{id}` | ✅ Gerente/Administrador (Admin solo su sede), soft-delete, bloquea `Occupied`/`Reserved` |

---

## 17. Deliveries (`Delivery`)

**Qué es:** el modelo es de "auto-asignación" — el repartidor ve los pedidos
disponibles y los toma él mismo (no se los asigna un tercero). Cada repartidor
solo ve sus propias entregas. La confirmación de entrega la hace el propio
repartidor; el Administrador queda como respaldo si el repartidor no puede
confirmar (Gerente NO tiene esta excepción).

**Se relaciona con:** `Order` (1 a 1, protegido con índice único en `OrderId`
a nivel de base de datos), `Employee` (repartidor, muchos a 1), `Address`
(destino, validado contra el cliente del pedido).

| Método | Endpoint | Quién | Estado |
|---|---|---|---|
| GET | `/api/Deliveries/available` | Repartidor | 🟢 **Nuevo** — lista pedidos tipo Delivery en estado `Ready`/`Confirmed` que aún no tienen repartidor asignado. Es el "pool" del que el repartidor escoge |
| POST | `/api/Deliveries` | Repartidor | 🟢 Revisado y corregido — el repartidor **toma** el pedido para sí mismo; `DeliveryPersonId` ya no viene en el body, se toma del JWT (evita que alguien tome un pedido a nombre de otro). Valida pedido tipo Delivery, estado correcto, dirección perteneciente al cliente, y que el pedido no haya sido tomado ya por otro repartidor (protegido además con índice único en BD) |
| GET | `/api/Deliveries/order/{orderId}` | Cualquier autenticado | 🟢 Revisado — consulta de solo lectura, sin cambios de lógica |
| GET | `/api/Deliveries/employee/{deliveryPersonId}` | Repartidor (solo lo suyo) / Administrador (solo su sede) / Gerente (cualquiera) | 🟢 Revisado y corregido — antes cualquiera podía consultar las entregas de cualquier repartidor; ahora un repartidor solo ve las suyas, y un Administrador solo ve repartidores de su propia sede |
| GET | `/api/Deliveries/branch/{branchId}` | Administrador (su sede) / Gerente (cualquier sede) | 🟢 **Nuevo** — ver todas las entregas (en curso y entregadas) de todos los repartidores de una sede |
| PATCH | `/api/Deliveries/{id}/status` | El repartidor asignado, o Administrador como respaldo | 🟢 Revisado y corregido — máquina de estados `Assigned → InTransit → Delivered` sin saltos ni retrocesos; solo el repartidor dueño de la entrega puede confirmarla, salvo excepción del Administrador |

**Pendiente de confirmar antes de dar por cerrado el módulo:**
- Confirmar `Shared/Constants/RoleNames.cs` (`Repartidor`, `Administrador`, `Gerente` exactos).
- Confirmar `Employee.BranchId` y `Order.BranchId`/`CreatedAt`/`Total`/`CustomerId`.
- Correr `dotnet ef migrations list` para verificar si el índice único de `OrderId` ya está aplicado en la base de datos real (el código ya lo tiene desde antes).

---
## 18. Reviews (`Review`)

**Qué es:** calificación que deja el cliente sobre un pedido ya entregado,
**solo si lo pidió él mismo desde la app** (no aplica a pedidos tomados por
mesero/cajero en mostrador — se distingue con el nuevo campo `Order.Channel`).

**Se relaciona con:** `Order` (1 a 1), `Customer` (muchos a 1).

| Método | Endpoint | Quién | Estado |
|---|---|---|---|
| POST | `/api/Reviews` | Cliente (dueño del pedido) | 🟡 Código listo, **pendiente de compilar** — depende de que `Order.Channel` esté bien seteado en la creación del pedido |
| PUT | `/api/Reviews/{id}` | Cliente (dueño de la reseña) | 🟡 Nuevo, código listo, pendiente de compilar |
| GET | `/api/Reviews/order/{orderId}` | Cualquier autenticado | 🟡 Código listo, pendiente de compilar |
| GET | `/api/Reviews/customer/{customerId}` | Cliente (solo lo suyo) / Gerente / Administrador (cualquiera) | 🟡 Código listo, pendiente de compilar |
| GET | `/api/Reviews/branch/{branchId}` | Administrador (solo su sede) | 🟡 Nuevo, código listo, pendiente de compilar |
| GET | `/api/Reviews/branch/{branchId}/summary` | Gerente (cualquier sede, promedio + total) | 🟡 Nuevo, código listo, pendiente de compilar |
| DELETE | `/api/Reviews/{id}` | Dueño / Gerente (cualquiera) / Administrador (solo su sede) | 🟡 Código listo, pendiente de compilar |

---

## 19. Addresses (`Address`)

**Qué es:** direcciones guardadas de un cliente (casa, oficina) para domicilio.

**Se relaciona con:** `Customer` (muchos a 1), `Delivery` (indirecto, vía `Order`).

| Método | Endpoint | Estado |
|---|---|---|
| GET | `/api/Addresses/customer/{customerId}` | 🟡 Código con `[Authorize]` entregado, falta confirmar compilación |
| GET | `/api/Addresses/{id}` | 🟡 Mismo caso |
| POST | `/api/Addresses` | 🟡 Fix de `GetCurrentCustomerId()` entregado, falta confirmar: (1) compilación, (2) que el JWT ya traiga el claim `CustomerId` tras el cambio en `JwtTokenGenerator.cs` |
| PUT | `/api/Addresses/{id}` | 🟡 Mismo caso |
| PATCH | `/api/Addresses/{id}/default` | 🟡 Mismo caso |
| DELETE | `/api/Addresses/{id}` | 🟡 Mismo caso |

**Pendiente crítico de confirmar:** que después de aplicar los 4 archivos
(`AuthRepository.cs`, `JwtTokenGenerator.cs`, `AddressesController.cs`,
`ControllerBaseExtensions.cs`), un login real genere un token con el claim
`CustomerId`, y que ese claim llegue correctamente a los endpoints de Addresses.

## 20. UserPreferences (`UserPreference`)

**Qué es:** preferencias personales de cada usuario (tema, sonido, vista por defecto, idioma).

**Se relaciona con:** `User` (muchos a 1).

| Método | Endpoint | Estado |
|---|---|---|
| GET | `/api/user-preferences/user/{userId}` | 🟢 Revisado y migrado |
| POST | `/api/user-preferences` | 🟢 Revisado y migrado |
| PUT | `/api/user-preferences/{id}` | 🟢 Revisado y migrado |
| DELETE | `/api/user-preferences/{id}` | 🟢 Revisado y migrado |
| GET | `/api/user-preferences/language` | 🟢 Nuevo, migrado |
| PUT | `/api/user-preferences/language` | 🟢 Nuevo, migrado |

**Pendiente de confirmar antes de pasar a 🟢:**
- Probar en Swagger con un token real (cliente y empleado) que el claim `UserId` llega correctamente y que `GetCurrentUserId()` no truena.
- Decidir si Gerente/Administrador deberían poder consultar preferencias de cualquier usuario (hoy es 100% personal, ni el Admin ve las de otro).
- Mover físicamente `UserPreferenceRepository.cs` a la carpeta `Repositories/` si aún queda algún rastro en `Mappings/` (ya se corrigió el duplicado que rompía el build).

---

## 21. Configurations (`BranchSetting`)

**Qué es:** parámetros configurables del sistema, por sucursal o globales
(`TAX_RATE`, horario de apertura, etc.). Solo el Gerente puede crear/editar/borrar;
cualquier usuario autenticado puede consultar.

**Se relaciona con:** `Branch` (opcional, muchos a 1; nulo si es un parámetro global).

| Método | Endpoint | Estado |
|---|---|---|
| GET | `/api/Configurations` | 🟢 Cualquier autenticado |
| GET | `/api/Configurations/branch/{branchId}` | 🟢 Cualquier autenticado — trae las de esa sucursal + las globales |
| GET | `/api/Configurations/key/{key}` | 🟢 Acepta `?branchId=` opcional en query string |
| POST | `/api/Configurations` | 🟢 Solo Gerente |
| PUT | `/api/Configurations/{id}` | 🟢 Solo Gerente |
| DELETE | `/api/Configurations/{id}` | 🟢 Solo Gerente |

**Rediseño aplicado:** se agregó el campo `Key` y `BranchId` pasó a ser nullable
(soporta configuraciones globales), con índices únicos separados por sucursal y
entre globales. Se eliminó `ConfigurationConfiguration.cs` (código muerto del
modelo anterior, que entraba en conflicto con `BranchSettingConfiguration.cs`).

---

## 22. PasswordResetCode

**Qué es:** códigos temporales para el flujo de "olvidé mi contraseña".

**Se relaciona con:** `User` (muchos a 1).

**Endpoints:** no tiene endpoints propios — se maneja internamente a través de
`POST /api/Auth/forgot-password`, `verify-reset-code` y `reset-password` (ver
sección 1, Auth). No necesita CRUD expuesto directamente.

---

## 23. EmailConfirmationCode

**Qué es:** código temporal para confirmar el correo al registrarse.

**Se relaciona con:** `User` (muchos a 1).

**Endpoints:** tampoco tiene CRUD propio — se usa a través de
`POST /api/Auth/confirm-email` (sección 1, Auth).

---

## 24. EmployeeActivationCode — 🟢 nuevo, construido en esta conversación

**Qué es:** reemplaza el flujo anterior de "contraseña temporal por correo".
Cuando el Gerente/Administrador crea un empleado, ya no se le manda una
contraseña usable — se le manda un código temporal (30 min) para que el propio
empleado defina su contraseña real la primera vez que entra. Hasta que no

---

## 24. Notifications (`Notification`) — 🟢 tabla creada y migrada

**Qué es:** avisos que recibe un usuario ("Nuevo pedido en mesa 5", "Tu pedido
va en camino"), con referencia opcional a la entidad relacionada (ej. el
`Order` o `Delivery` que la originó) para que el cliente pueda navegar directo
a ese recurso al tocar la notificación.

**Se relaciona con:** `User` (muchos a 1), y opcionalmente con `Order`/`Delivery`/
`Payment` vía `RelatedEntityType` + `RelatedEntityId` (referencia polimórfica,
sin FK real en base de datos).

**Nota importante:** este módulo es distinto del `INotificationService` de
`Shared.Interfaces` (que solo envía correos). Para evitar el choque de nombres,
el servicio de este módulo se llama `IUserNotificationService` /
`UserNotificationService`.

| Método | Endpoint | Quién | Estado |
|---|---|---|---|
| GET | `/api/Notifications/user/{userId}` | El propio usuario (`GetCurrentUserId()` valida que coincida) | 🟢 Construido — soporta `?unreadOnly=true` |
| GET | `/api/Notifications/user/{userId}/unread-count` | El propio usuario | 🟢 Construido — para el badge/contador |
| POST | `/api/Notifications` | Gerente/Administrador | 🟢 Construido — manual, a un `UserId` puntual o a toda una `BranchId` |
| PATCH | `/api/Notifications/{id}/read` | El propio dueño de la notificación | 🟢 Construido |
| PATCH | `/api/Notifications/user/{userId}/read-all` | El propio usuario | 🟢 Construido — bonus, no estaba en el plan original |
| DELETE | `/api/Notifications/{id}` | El propio dueño de la notificación | 🟢 Construido |

**Tiempo real:** `NotificationHub` (SignalR) construido y mapeado en
`/hubs/notifications`, con agrupación por `UserId` (`ClaimTypes.NameIdentifier`
del JWT). Cada `Create`/`CreateBulk` dispara el push automáticamente además de
guardar en base de datos (funciona como buzón: se guarda siempre, y si el
usuario tiene la app abierta, también le llega al instante).

**Pendiente de confirmar antes de dar el módulo por 100% cerrado:**
- Falta agregar en `Configuration/JwtConfiguration.cs` (o como se llame el
  archivo con `AddJwtBearer`) el `OnMessageReceived` que permite que el JWT
  viaje por query string (`?access_token=...`) para la conexión al hub — sin
  esto, el front no va a poder autenticarse contra `/hubs/notifications`.
- Falta insertar las llamadas a `_userNotificationService.CreateAsync(...)`
  dentro de `OrderService.cs` (cuando un pedido pasa a `Ready`) y
  `DeliveryService.cs` (cuando pasa a `InTransit`/`Delivered`) — el CRUD y el
  hub ya funcionan, pero todavía nadie los está

## 25. EmployeePermissions (`EmployeePermission`) — nuevo, construido en esta conversación

**Qué es:** excepciones puntuales de permisos por empleado individual, por
encima de lo que da su rol. Ejemplo: dos meseros con el mismo rol, uno con
`ORDERS_EDIT` habilitado y el otro no.

**Se relaciona con:** `Employee` (muchos a 1), `Permission` (muchos a 1).
Complementa (no reemplaza) a `RolePermission`.

| Método | Endpoint | Estado |
|---|---|---|
| GET | `/api/employee-permissions/employee/{employeeId}` | 🟢 Construido — lista las excepciones puntuales de un empleado |
| GET | `/api/employee-permissions/effective/{employeeId}` | 🟢 Construido — calcula el resultado final (permisos del rol + excepciones otorgadas − excepciones revocadas) |
| POST | `/api/employee-permissions` | 🟢 Construido — valida existencia de empleado/permiso, no duplicados, Administrador restringido a su sede |
| PUT | `/api/employee-permissions/{id}` | 🟢 Construido — mismas restricciones de sede que create |
| DELETE | `/api/employee-permissions/{id}` | 🟢 Construido — mismas restricciones de sede |

**Pendiente:** pegar los archivos generados por el script, agregar `DbSet<EmployeePermission>` en `AppDbContext`, registrar las dependencias, compilar, correr la migración `AddEmployeePermissions`, y probar en Swagger.
---

## EmployeeSchedules (`EmployeeSchedule`) — nuevo ✅ COMPLETADO

**Qué es:** turno asignado a un empleado en una fecha específica (no recurrente,
cada turno es una fila con su propia fecha). Filtra los permisos: la idea es
que un empleado solo pueda ejercer sus permisos si está dentro de su turno
asignado para ese momento.

**Se relaciona con:** `Employee` (muchos a 1), `Branch` (muchos a 1).

| Método | Endpoint | Por qué | Estado |
|---|---|---|---|
| GET | `/api/employee-schedules/employee/{employeeId}` | Ver el historial/próximos turnos de un empleado puntual | ✅ |
| GET | `/api/employee-schedules/branch/{branchId}` | Ver todos los turnos de una sucursal | ✅ |
| GET | `/api/employee-schedules/branch/{branchId}/today` | Ver quién debería estar trabajando hoy | ✅ |
| POST | `/api/employee-schedules` | Asignar un turno nuevo a un empleado | ✅ Valida cruce de horario |
| PUT | `/api/employee-schedules/{id}` | Editar un turno ya asignado | ✅ |
| DELETE | `/api/employee-schedules/{id}` | Quitar un turno asignado | ✅ |

## 27. Invoice / Factura (`Invoice`) — nuevo

**Qué es:** el comprobante de cobro de un pedido, generado a partir de un
`Payment` ya confirmado (`Completed`). Factura física simple, con numeración
consecutiva por sucursal — sin integración DIAN por ahora.

**Se relaciona con:** `Payment` (1 a 1 — una factura por pago conf

---
## 26. DailyMenu (`DailyMenuItem`) — ✅ COMPLETADO

**Qué es:** el menú disponible por sucursal, fecha y franja (desayuno/almuerzo/cena). Permite que el Gerente/Administrador elija qué platos se ofrecen ese día, y que Cliente/Mesero solo vean lo disponible al tomar el pedido.

**Se relaciona con:** `Branch` (muchos a 1), `Product` (muchos a 1).

| Método | Endpoint | Quién | Para qué |
|---|---|---|---|
| GET | `/api/daily-menu/branch/{branchId}/today` | Cualquier autenticado (Cliente, Mesero, Gerente...) | Lo que Cliente/Mesero ven para tomar el pedido — filtra automáticamente por fecha de hoy, y opcionalmente `?period=Almuerzo` |
| GET | `/api/daily-menu/branch/{branchId}/date/{date}` | Gerente/Administrador | Ver o planear el menú de una fecha específica (ej. armar el de mañana desde hoy) |
| GET | `/api/daily-menu/{id}` | Gerente/Administrador | Ver el detalle de una entrada puntual |
| POST | `/api/daily-menu` | Gerente/Administrador | Agregar **un** producto puntual al menú de un día/franja |
| POST | `/api/daily-menu/bulk` | Gerente/Administrador | Reemplazar de una vez **todo** el menú de una sucursal+fecha+franja con una lista de productos (el caso de uso real: "el almuerzo de hoy son estos 8 platos") |
| PUT | `/api/daily-menu/{id}` | Gerente/Administrador | Editar fecha/franja/disponibilidad de una entrada |
| PATCH | `/api/daily-menu/{id}/toggle` | Gerente/Administrador | Prender/apagar rápido a media jornada (ej. "se acabó el plato del día", sin editar todo el registro) |
| DELETE | `/api/daily-menu/{id}` | Gerente/Administrador | Quitar un producto del menú de ese día |

**Estado:**

## Resumen final — las 23 entidades reales + 4 módulos nuevos/pendientes

| # | Entidad (`DbSet` real) | Estado general |
|---|---|---|
| 1 | User (+ PasswordResetCode, EmailConfirmationCode) | 🟡 Auth parcialmente revisado |
| 2 | Role | ⚪ No revisado |
| 3 | Permission | ⚪ No revisado |
| 4 | RolePermission | ⚪ No revisado |
| 5 | UserRole | ⚪ No revisado |
| 6 | Customer | ⬜ Falta la mayoría |
| 7 | Employee | 🟡 Falta POST/DELETE |
| 8 | Branch | ⚪ No revisado |
| 9 | Category | 🟢 Completo y revisado |
| 10 | Product | 🟢 Completo y revisado |
| 11 | Order | 🟡 Revisado, con pendientes de permisos |
| 12 | OrderDetail | 🟡 Revisado, falta validar estado del pedido padre |
| 13 | OrderDetailHistory | 🟢 Completo y revisado |
| 14 | OrderStatusHistory | ⚪ Existe, no revisado |
| 15 | Payment | ⚪ No revisado |
| 16 | Table | 🟢 Completo y revisado |
| 17 | Delivery | ⚪ No revisado |
| 18 | Review | ⚪ No revisado |
| 19 | Address | ⚪ No revisado |
| 20 | UserPreference | ⚪ No revisado |
| 21 | Configuration (BranchSetting) | ⚪ No revisado |
| — | Notification | ⚠️ Tabla no confirmada — falta verificar si existe |
| — | EmployeePermission | ⬜ Nuevo, en diseño |
| — | EmployeeSchedule | ⬜ Nuevo, en diseño |
| — | Invoice/Factura | ⬜ Nuevo, pendiente definir modelo |

---

## Próximo paso sugerido

Antes de seguir construyendo módulos nuevos, valdría la pena que confirmes si
`Notification` existe físicamente en tu base de datos (quizás con otro nombre
de tabla, o simplemente no se ha migrado todavía). Corre esto en PowerShell
para buscarlo en tu proyecto:

```powershell
Get-ChildItem -Path "Modules" -Filter "Notification*.cs" -Recurse | Select-Object Name, DirectoryName
```

Con eso confirmamos si hay que agregar el `DbSet` y la migración, o si ya
existe y solo faltó incluirlo en el `AppDbContext.cs` que revisamos.