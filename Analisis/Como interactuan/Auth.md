# SaborExpress — Guía de endpoints: para qué sirve cada uno y cómo interactúan las personas

Este documento explica, módulo por módulo, **qué hace cada endpoint** y **un
ejemplo real de cómo lo usaría una persona** (cliente, mesero, cajero, cocinero,
repartidor, administrador o gerente) dentro del flujo normal de un restaurante.

La idea es que sirva como referencia rápida antes de conectar el front: cuando
tengas dudas de "¿para qué llamo este endpoint y en qué momento?", lo buscas aquí.

---

## 1. Auth — Iniciar sesión y gestionar la cuenta

Todo el mundo (cliente o empleado) pasa por aquí antes de hacer cualquier otra cosa.

| Endpoint | Para qué sirve | Ejemplo de interacción |
|---|---|---|
| `POST /api/Auth/login` | Iniciar sesión y obtener los tokens de acceso | María abre la app, escribe su correo y contraseña, y entra a ver el menú |
| `POST /api/Auth/refresh-token` | Renovar la sesión sin pedirle la contraseña de nuevo | El token de María expiró después de 20 min; la app renueva en silencio y ella ni se entera |
| `POST /api/Auth/logout` | Cerrar sesión de forma segura en todos los dispositivos | María termina su turno de mesera y le da "Cerrar sesión" en la tablet del restaurante |
| `POST /api/Auth/forgot-password` | Iniciar recuperación de contraseña | Carlos olvidó su clave, pone su correo y le llega un código de 6 dígitos |
| `POST /api/Auth/verify-reset-code` | Confirmar que el código recibido es válido | Carlos escribe el código que le llegó; el sistema le da un token temporal para cambiar la clave |
| `POST /api/Auth/reset-password` | Definir la nueva contraseña | Carlos escribe su nueva clave usando ese token temporal |
| `POST /api/Auth/confirm-email` | Confirmar el correo al registrarse | Después de registrarse, Ana recibe un código y lo confirma antes de poder loguear |
| `POST /api/Auth/resend-confirmation-code` | Reenviar el código si no llegó o expiró | Ana no vio el correo a tiempo y pide que se lo reenvíen |
| `POST /api/Auth/activate-account` | El empleado nuevo define su propia contraseña | El Gerente crea a Luis como cocinero; Luis recibe un código y con eso pone su clave la primera vez que entra |

---

## 2. Branches — Las sucursales del restaurante

| Endpoint | Para qué sirve | Ejemplo de interacción |
|---|---|---|
| `GET /api/Branches` | Ver todas las sucursales de la cadena | El Gerente entra al panel y ve la lista completa: Sede Norte, Sede Sur, Sede Centro |
| `POST /api/Branches` | Abrir una sucursal nueva en el sistema | El Gerente registra "Sede Poblado" antes de que empiece a operar |
| `GET /api/Branches/mine` | Ver la sede que le corresponde al usuario logueado | El Administrador de la Sede Norte entra y automáticamente ve solo su sucursal |
| `GET /api/Branches/{id}` | Ver el detalle de una sucursal puntual | El Gerente revisa la dirección y datos de "Sede Sur" |
| `PUT /api/Branches/{id}` | Editar los datos de una sucursal | Cambia de dirección la Sede Centro y el Administrador la actualiza |
| `DELETE /api/Branches/{id}` | Dar de baja una sucursal | El Gerente cierra "Sede Antigua" — el sistema no deja si todavía tiene empleados asignados |

---

## 3. Customers — Los clientes de la app

| Endpoint | Para qué sirve | Ejemplo de interacción |
|---|---|---|
| `POST /api/Customers/register` | Crear una cuenta nueva de cliente | Ana descarga la app y se registra con su nombre, correo y clave |
| `GET /api/Customers/me` | Ver su propio perfil | Ana entra a "Mi cuenta" y ve sus datos |
| `PUT /api/Customers/me` | Editar su propio perfil | Ana actualiza su número de teléfono |
| `GET /api/Customers` | Ver la lista de todos los clientes | El Gerente revisa cuántos clientes están registrados |
| `GET /api/Customers/{id}` | Ver el detalle de un cliente puntual | El Administrador busca a un cliente específico por una queja |

---

## 4. Employees — Los trabajadores del restaurante

| Endpoint | Para qué sirve | Ejemplo de interacción |
|---|---|---|
| `GET /api/Employees` | Ver todos los empleados | El Gerente revisa el listado completo de personal |
| `GET /api/Employees/{id}` | Ver el detalle de un empleado | El Administrador consulta el perfil de Luis, el cocinero |
| `PUT /api/Employees/{id}` | Editar datos de un empleado | Se actualiza el cargo de Luis de "Ayudante" a "Cocinero" |
| `GET /api/Employees/{id}/cv` | Descargar/ver la hoja de vida del empleado | RRHH revisa el CV de Luis al momento de una promoción |
| `POST /api/Employees` | Contratar/crear un empleado nuevo | El Gerente registra a Luis como nuevo cocinero de la Sede Norte |
| `DELETE /api/Employees/{id}` | Dar de baja a un empleado (sin borrar su historial) | Luis renuncia; se marca como "Retirado" pero sus pedidos pasados quedan intactos |

---

## 5. Permissions y RolePermissions — El catálogo de permisos

| Endpoint | Para qué sirve | Ejemplo de interacción |
|---|---|---|
| `GET /api/Permissions` | Ver todos los permisos que existen en el sistema | El Gerente revisa qué acciones existen: `ORDERS_EDIT`, `ORDERS_CANCEL`, etc. |
| `POST /api/Permissions` | Crear un permiso nuevo | Se agrega el permiso `INVOICE_VOID` para poder anular facturas |
| `GET/PUT/DELETE /api/Permissions/{id}` | Ver, editar o borrar un permiso puntual | El Gerente corrige el nombre de un permiso mal escrito |
| `POST /api/role-permissions` | Asignar un permiso a un rol | El rol "Cajero" recibe el permiso `PAYMENTS_CREATE` |
| `GET /api/role-permissions` / `GET /api/role-permissions/role/{roleId}` | Ver qué permisos tiene cada rol | El Gerente revisa qué puede hacer un "Mesero" por defecto |
| `DELETE /api/role-permissions/{roleId}/{permissionId}` | Quitar un permiso de un rol | Se le retira `ORDERS_CANCEL` al rol "Mesero" |

---

## 6. Roles y UserRoles — Quién es quién en el sistema

| Endpoint | Para qué sirve | Ejemplo de interacción |
|---|---|---|
| `GET/POST/PUT/DELETE /api/Roles` | Gestionar el catálogo de roles (Mesero, Cocinero, Cajero, Gerente, Administrador, Cliente) | El Gerente crea el rol "Repartidor" cuando el restaurante empieza a hacer domicilios |
| `POST /api/user-roles` | Asignarle un rol a un usuario | El Administrador convierte a Ana (antes solo Cliente) en Mesera de la Sede Norte |
| `GET /api/user-roles` / `GET /api/user-roles/user/{userId}` | Ver qué roles tiene un usuario | Se revisa que Ana tenga tanto el rol Cliente como Mesero (puede tener ambos) |
| `DELETE /api/user-roles/{userId}/{roleId}` | Quitarle un rol a un usuario | Se le retira el rol Mesero a Ana cuando deja ese cargo |

---

## 7. Categories y Products — El menú

| Endpoint | Para qué sirve | Ejemplo de interacción |
|---|---|---|
| `GET /api/Categories` / `GET /api/Categories/{id}` | Ver las categorías del menú | El cliente abre la app y ve "Entradas", "Platos Fuertes", "Bebidas", "Postres" |
| `POST/PUT/DELETE /api/Categories` | Crear, editar o eliminar categorías | El Gerente agrega la categoría "Vegano" al menú |
| `GET /api/Products` / `GET /api/Products/{id}` | Ver los productos disponibles | El cliente toca "Platos Fuertes" y ve la Bandeja Paisa, el Sancocho, etc. |
| `GET /api/Products/category/{categoryId}` | Ver los productos de una categoría específica | El cliente filtra solo "Bebidas" |
| `POST /api/Products` | Agregar un plato nuevo al menú | El Gerente sube "Ajiaco Santafereño" con su precio y descripción |
| `PUT /api/Products/{id}` | Editar un producto existente | Se sube el precio de la Bandeja Paisa |
| `DELETE /api/Products/{id}` | Eliminar un producto (si no tiene pedidos asociados) | Se retira del catálogo un plato que ya no se prepara |
| `PATCH /api/Products/{id}/status` | Activar/desactivar un producto rápidamente | Se agota el pescado del día y el cocinero lo marca como "no disponible" sin borrarlo |

---

## 8. Orders, OrderDetails y sus historiales — El corazón del sistema

| Endpoint | Para qué sirve | Ejemplo de interacción |
|---|---|---|
| `POST /api/Orders` | Crear un pedido nuevo | El mesero toma el pedido de la Mesa 5: Ana pidió Bandeja Paisa + jugo de mora |
| `GET /api/Orders/{id}` | Ver el detalle de un pedido | El cajero abre el pedido de la Mesa 5 para cobrar |
| `GET /api/Orders` | Ver todos los pedidos (con filtros) | El Gerente filtra los pedidos "Pendientes" del día |
| `GET /api/Orders/table/{tableId}` | Ver el pedido activo de una mesa | El mesero revisa qué tiene pendiente la Mesa 5 |
| `GET /api/Orders/customer/{customerId}` | Ver el historial de pedidos de un cliente | Ana revisa en la app sus pedidos anteriores |
| `GET /api/Orders/branch/{branchId}` | Ver todos los pedidos de una sucursal | El Administrador revisa la actividad del día en su sede |
| `PUT /api/Orders/{id}` | Editar un pedido antes de que llegue a un estado final | Se agrega una gaseosa extra al pedido de la Mesa 5 |
| `PATCH /api/Orders/{id}/status` | Cambiar el estado del pedido (Pendiente → En preparación → Listo → Entregado) | El cocinero marca el pedido como "Listo" cuando termina de prepararlo |
| `PATCH /api/Orders/{id}/cancel` | Cancelar un pedido (con motivo obligatorio, nunca se borra) | Un cliente se va sin querer el pedido; el mesero lo cancela indicando el motivo |
| `POST /api/orders/{orderId}/details` | Agregar una línea (producto) a un pedido | Se agrega "1 postre de flan" al pedido de la Mesa 5 después de que ya se había creado |
| `GET /api/orders/{orderId}/details` | Ver todas las líneas de un pedido | El cajero revisa exactamente qué se pidió antes de cobrar |
| `PUT /api/order-details/{id}` | Editar una línea del pedido | Se cambia la cantidad de "jugo de mora" de 1 a 2 |
| `PATCH /api/order-details/{id}/void` | Anular solo una línea del pedido (no todo el pedido) | El cliente se arrepiente del postre y el mesero anula solo esa línea |
| `GET /api/order-details/{orderDetailId}/history` | Ver el historial de cambios de una línea | El Gerente revisa quién y cuándo modificó una línea sospechosa |
| `GET /api/orders/{orderId}/status-history` | Ver el historial de estados de un pedido completo | Se audita cuánto tiempo pasó entre "Pendiente" y "Entregado" |

---

## 9. Payments — Cobrar el pedido

| Endpoint | Para qué sirve | Ejemplo de interacción |
|---|---|---|
| `POST /api/Payments` | Registrar el cobro de un pedido | El cajero cobra $45.000 en efectivo por el pedido de la Mesa 5 |
| `GET /api/Payments/order/{orderId}` | Ver los pagos hechos a un pedido | Se revisa si un pedido ya fue pagado o le falta saldo |
| `GET /api/Payments/{id}` | Ver el detalle de un pago puntual | El Administrador revisa un pago específico por una reclamación |
| `PATCH /api/Payments/{id}/refund` | Reembolsar un pago | El cliente se queja de un plato mal preparado y se le reembolsa |
| `GET /api/Payments` | Ver todos los pagos (con filtro por sede/fecha) | El Gerente revisa cuánto se facturó ayer en la Sede Norte |

---

## 10. Tables — Las mesas físicas

| Endpoint | Para qué sirve | Ejemplo de interacción |
|---|---|---|
| `GET /api/Tables/branch/{branchId}` | Ver todas las mesas de una sede | El mesero abre la app y ve el mapa de mesas de su sucursal |
| `GET /api/Tables/{id}` | Ver el detalle de una mesa | Se revisa el estado actual de la Mesa 5 |
| `POST /api/Tables` | Registrar una mesa nueva | Se agrega la "Mesa 12" cuando el restaurante crece |
| `PUT /api/Tables/{id}` | Editar una mesa (ej. capacidad) | Se actualiza la Mesa 5 de 4 a 6 puestos |
| `PATCH /api/Tables/{id}/status` | Cambiar el estado de la mesa (Libre, Ocupada, Reservada) | El mesero marca la Mesa 5 como "Ocupada" cuando llegan los clientes |
| `DELETE /api/Tables/{id}` | Dar de baja una mesa | Se retira una mesa dañada (bloqueado si está ocupada/reservada) |

---

## 11. Deliveries — Domicilios (modelo de auto-asignación)

| Endpoint | Para qué sirve | Ejemplo de interacción |
|---|---|---|
| `GET /api/Deliveries/available` | Ver el "pool" de pedidos disponibles para domicilio | Pedro, el repartidor, abre la app y ve 3 pedidos listos esperando repartidor |
| `POST /api/Deliveries` | Tomar un pedido para sí mismo | Pedro toca "Tomar pedido" en el pedido de Ana; queda asignado a él, nadie más lo puede tomar |
| `GET /api/Deliveries/order/{orderId}` | Ver los datos de entrega de un pedido | Ana revisa en la app quién le está llevando su pedido |
| `GET /api/Deliveries/employee/{deliveryPersonId}` | Ver las entregas de un repartidor | Pedro revisa sus propias entregas del día; el Administrador ve las de todos en su sede |
| `GET /api/Deliveries/branch/{branchId}` | Ver todas las entregas de una sucursal | El Gerente revisa cuántos domicilios salieron hoy en la Sede Sur |
| `PATCH /api/Deliveries/{id}/status` | Actualizar el estado de la entrega (Asignado → En camino → Entregado) | Pedro marca "En camino" al salir, y "Entregado" cuando llega donde Ana |

---

## 12. Reviews — Calificaciones del cliente

| Endpoint | Para qué sirve | Ejemplo de interacción |
|---|---|---|
| `POST /api/Reviews` | Dejar una calificación de un pedido ya entregado (solo si lo pidió desde la app) | Ana califica con 5 estrellas su pedido y escribe "Excelente sabor" |
| `PUT /api/Reviews/{id}` | Editar una reseña propia | Ana corrige su comentario un día después |
| `GET /api/Reviews/order/{orderId}` | Ver la reseña de un pedido puntual | El Gerente revisa qué calificó un pedido específico |
| `GET /api/Reviews/customer/{customerId}` | Ver todas las reseñas de un cliente | Ana revisa su historial de calificaciones |
| `GET /api/Reviews/branch/{branchId}` | Ver las reseñas de una sede | El Administrador revisa las quejas/elogios de su sucursal |
| `GET /api/Reviews/branch/{branchId}/summary` | Ver el promedio y total de reseñas de una sede | El Gerente compara el promedio de calificación entre sedes |
| `DELETE /api/Reviews/{id}` | Eliminar una reseña | Se borra una reseña con lenguaje inapropiado |

---

## 13. Addresses — Direcciones para domicilio

| Endpoint | Para qué sirve | Ejemplo de interacción |
|---|---|---|
| `GET /api/Addresses/customer/{customerId}` | Ver las direcciones guardadas de un cliente | Ana ve "Casa" y "Oficina" guardadas al momento de pedir domicilio |
| `GET /api/Addresses/{id}` | Ver el detalle de una dirección | Se revisa la dirección exacta antes de asignar un repartidor |
| `POST /api/Addresses` | Guardar una dirección nueva | Ana agrega "Casa de mis papás" como nueva dirección |
| `PUT /api/Addresses/{id}` | Editar una dirección guardada | Ana corrige el número de apartamento |
| `PATCH /api/Addresses/{id}/default` | Marcar una dirección como predeterminada | Ana marca "Casa" como su dirección principal |
| `DELETE /api/Addresses/{id}` | Eliminar una dirección guardada | Ana borra "Oficina antigua" tras cambiar de trabajo |

---

## 14. UserPreferences — Preferencias personales

| Endpoint | Para qué sirve | Ejemplo de interacción |
|---|---|---|
| `GET/POST/PUT/DELETE /api/user-preferences` | Guardar cómo cada usuario quiere ver la app | Ana activa el modo oscuro y desactiva las notificaciones por sonido |
| `GET/PUT /api/user-preferences/language` | Ver/cambiar el idioma preferido | Un cliente extranjero cambia la app a inglés |

---

## 15. Configurations — Parámetros del sistema

| Endpoint | Para qué sirve | Ejemplo de interacción |
|---|---|---|
| `GET /api/Configurations` | Ver todos los parámetros configurables | El sistema consulta el `TAX_RATE` (IVA) para calcular el total de un pedido |
| `GET /api/Configurations/branch/{branchId}` | Ver los parámetros de una sede (+ los globales) | Se consulta el horario de apertura específico de la Sede Norte |
| `GET /api/Configurations/key/{key}` | Buscar un parámetro puntual por su clave | Se busca directamente `TAX_RATE` sin traer todo lo demás |
| `POST/PUT/DELETE /api/Configurations` | Crear, editar o borrar un parámetro | El Gerente sube el IVA del 19% al 20% para toda la cadena |

---

## 16. Notifications — Avisos en tiempo real

| Endpoint | Para qué sirve | Ejemplo de interacción |
|---|---|---|
| `GET /api/Notifications/user/{userId}` | Ver las notificaciones de un usuario | Ana abre la campanita y ve "Tu pedido va en camino" |
| `GET /api/Notifications/user/{userId}/unread-count` | Ver cuántas notificaciones no leídas tiene | La app muestra un "3" en rojo sobre el ícono de campana |
| `POST /api/Notifications` | Enviar una notificación manual | El Gerente avisa a toda una sede: "Reunión de personal a las 3pm" |
| `PATCH /api/Notifications/{id}/read` | Marcar una notificación como leída | Ana toca la notificación y esta se marca como leída |
| `PATCH /api/Notifications/user/{userId}/read-all` | Marcar todas como leídas de una vez | Ana toca "Marcar todo como leído" |
| `DELETE /api/Notifications/{id}` | Eliminar una notificación | Ana borra un aviso viejo de su bandeja |

**Extra:** el `NotificationHub` (SignalR) hace que estas notificaciones lleguen **al instante** si el cliente tiene la app abierta — no tiene que refrescar para verlas.

---

## 17. EmployeePermissions — Excepciones puntuales de permisos

| Endpoint | Para qué sirve | Ejemplo de interacción |
|---|---|---|
| `GET /api/employee-permissions/employee/{employeeId}` | Ver qué excepciones tiene un empleado, más allá de su rol | Se revisa que Ana, mesera, tiene el permiso extra `ORDERS_CANCEL` aunque las demás meseras no lo tengan |
| `GET /api/employee-permissions/effective/{employeeId}` | Ver el resultado final de permisos (rol + excepciones) | El sistema calcula si Ana puede o no cancelar un pedido en este momento |
| `POST /api/employee-permissions` | Otorgar/revocar un permiso puntual a un empleado | El Administrador le da a Ana el permiso especial de cancelar pedidos |
| `PUT/DELETE /api/employee-permissions/{id}` | Editar o quitar esa excepción | Se le retira el permiso especial a Ana cuando ya no lo necesita |

---

## 18. EmployeeSchedules — Turnos de trabajo

| Endpoint | Para qué sirve | Ejemplo de interacción |
|---|---|---|
| `GET /api/employee-schedules/employee/{employeeId}` | Ver los turnos de un empleado | Ana revisa cuándo le toca trabajar esta semana |
| `GET /api/employee-schedules/branch/{branchId}` | Ver todos los turnos de una sede | El Administrador arma el horario semanal completo |
| `GET /api/employee-schedules/branch/{branchId}/today` | Ver quién debería estar trabajando hoy | El Gerente revisa si falta alguien en el turno de la mañana |
| `POST /api/employee-schedules` | Asignar un turno nuevo | Se le asigna a Ana el turno de mañana del jueves (valida que no se cruce con otro turno) |
| `PUT /api/employee-schedules/{id}` | Editar un turno ya asignado | Se cambia el turno de Ana de mañana a tarde |
| `DELETE /api/employee-schedules/{id}` | Quitar un turno | Se elimina un turno que ya no aplica porque Ana pidió el día libre |

**Para qué se conecta con Permissions:** la idea es que un empleado solo pueda ejercer sus permisos (ej. `ORDERS_EDIT`) **si está dentro de su turno asignado** en ese momento — evita que alguien opere fuera de su horario.

---

## 19. DailyMenu — El menú del día

| Endpoint | Para qué sirve | Ejemplo de interacción |
|---|---|---|
| `GET /api/daily-menu/branch/{branchId}/today` | Ver qué hay disponible hoy (lo que ve el cliente/mesero al pedir) | Ana abre la app y solo ve los platos que sí están disponibles hoy en su sede |
| `GET /api/daily-menu/branch/{branchId}/date/{date}` | Planear el menú de una fecha futura | El Gerente arma desde hoy el menú del almuerzo de mañana |
| `GET /api/daily-menu/{id}` | Ver el detalle de una entrada puntual del menú | Se revisa si "Ajiaco" está disponible para la franja de almuerzo |
| `POST /api/daily-menu` | Agregar un producto puntual al menú de un día | Se agrega "Sancocho" al almuerzo de hoy |
| `POST /api/daily-menu/bulk` | Reemplazar de una vez todo el menú del día | El Gerente carga los 8 platos del almuerzo de hoy en un solo request |
| `PUT /api/daily-menu/{id}` | Editar una entrada del menú del día | Se cambia la franja de un plato de "Almuerzo" a "Cena" |
| `PATCH /api/daily-menu/{id}/toggle` | Prender/apagar rápido un plato a media jornada | Se acabó el plato del día y el cocinero lo apaga sin borrar el registro |
| `DELETE /api/daily-menu/{id}` | Quitar un producto del menú de ese día | Se retira un plato que se programó por error |

---

## Ejemplo de flujo completo: un pedido de principio a fin

Para que veas cómo se conectan varios módulos en un solo caso real:

```
1. Ana abre la app → GET /api/daily-menu/branch/1/today
   (ve qué platos hay disponibles hoy)

2. Ana hace su pedido → POST /api/Orders
   (se crea el pedido con su primera línea)

3. Ana agrega un postre → POST /api/orders/{orderId}/details
   (se recalcula el total del pedido automáticamente)

4. El cocinero marca "En preparación" → PATCH /api/Orders/{id}/status

5. El cocinero termina y marca "Listo" → PATCH /api/Orders/{id}/status
   → esto dispara una notificación automática a Ana (módulo Notifications)

6. Pedro, el repartidor, ve el pedido disponible → GET /api/Deliveries/available
   y lo toma → POST /api/Deliveries

7. Pedro marca "En camino" → PATCH /api/Deliveries/{id}/status
   → Ana recibe otra notificación en tiempo real

8. Pedro entrega y marca "Entregado" → PATCH /api/Deliveries/{id}/status

9. Se registra el cobro → POST /api/Payments

10. Ana califica su experiencia → POST /api/Reviews
```

Este flujo es el que más vale la pena probar de punta a punta una vez que cada endpoint individual ya esté validado — porque es donde se ven los problemas de integración entre módulos que las pruebas aisladas no detectan.