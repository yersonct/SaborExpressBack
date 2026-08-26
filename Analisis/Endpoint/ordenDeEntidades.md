# SaborExpress — Orden de construcción de módulos

Este documento define en qué orden conviene construir/cerrar cada entidad del
proyecto, pensando en que el sistema de permisos (rol → excepción individual
por empleado → turno) se pueda aplicar **a todos los módulos por igual**, en
vez de agregarse a la fuerza después.

---

## Idea central

Los pasos **6, 7 y 8** son el punto de quiebre del proyecto: ahí se construye
la maquinaria de autorización **una sola vez**, como pieza central reutilizable.
Todo lo que viene después (Orders en adelante) se construye **usando** esa
pieza desde el día uno, en vez de que cada módulo reinvente su propia
validación o haya que volver a tocar código ya hecho.

---

## Orden recomendado

| # | Entidad / Módulo | Por qué va en este punto |
|---|---|---|
| 1 | **Auth (User)** | Ya existe y funciona — es la base de todo, nadie puede hacer nada sin loguearse. |
| 2 | **Roles** | Catálogo de roles, necesario antes de poder asignarlos a nadie. |
| 3 | **Permissions** | Catálogo de acciones (`ORDERS_EDIT`, etc.) — necesario antes de poder otorgarlas. |
| 4 | **RolePermissions** | Conecta rol con permiso — capa 1 del sistema de autorización. |
| 5 | **UserRoles** | Conecta usuario con rol — sin esto, un usuario no tiene ningún rol asignado. |
| 6 | **EmployeePermissions** *(nuevo)* | Capa 2 — excepciones individuales por empleado. Se construye aquí, antes de Orders/Tables/etc., para no tener que retocarlos después. |
| 7 | **EmployeeSchedules** *(nuevo)* | Capa 3 — turnos por fecha específica. Va justo después de permisos porque ambos trabajan juntos en la misma validación final. |
| 8 | **🔧 Pieza central de autorización** | Servicio único (`CanPerformActionAsync`) que combina las 3 capas (rol → excepción → turno) en un solo lugar. De aquí en adelante, cada módulo nuevo nace con el candado puesto. |
| 9 | **Employees** | Se termina (falta POST/DELETE) ya con los permisos correctos desde el día uno. |
| 10 | **Branches** | Base física — sucursales. Casi todo lo demás depende de esto. |
| 11 | **Categories** | Ya completo — entra por orden lógico del menú. |
| 12 | **Products** | Ya completo — depende de Categories. |
| 13 | **Tables** | Ya completo — depende de Branches. |
| 14 | **Orders** | Módulo central — aquí se aplican por primera vez los permisos `ORDERS_EDIT` / `ORDERS_EDIT_ALL` usando la pieza del paso 8. |
| 15 | **OrderDetails** | Depende de Orders + Products. |
| 16 | **OrderDetailHistory** | Ya completo — depende de OrderDetails. |
| 17 | **OrderStatusHistory** | Depende de Orders — falta revisar su código. |
| 18 | **Payments** | Depende de Orders — necesita un pedido con total calculado antes de poder cobrarlo. |
| 19 | **Invoice / Factura** *(nuevo)* | Depende de Payments — la factura se genera sobre un pago ya confirmado. |
| 20 | **Customers** | Depende de Auth — resto del perfil del cliente. |
| 21 | **Addresses** | Depende de Customers. |
| 22 | **Deliveries** | Depende de Orders + Employees (repartidor) + Addresses (destino). |
| 23 | **Reviews** | Depende de Orders + Customers — solo se califica un pedido ya entregado. |
| 24 | **Notifications** | Depende de casi todo lo anterior — recibe avisos disparados por Orders, Deliveries, EmployeeSchedules, etc. **Pendiente confirmar si la tabla existe en la base de datos** (no se encontró `DbSet<Notification>` en el `AppDbContext`). |
| 25 | **Configurations** | Independiente en estructura, pero conviene resolverlo antes del final porque `TAX_RATE` ya está pendiente de sacar del hardcode en `OrderDetailService`. |
| 26 | **UserPreferences** | El más independiente de todos — solo depende de Auth. Va al final. |

---

## Alternativa (más pragmática, si necesitas avances rápidos)

Si por tiempo del proyecto necesitas mostrar resultados visibles pronto, existe
un camino alterno:

1. Cerrar primero lo que ya está casi terminado: **Employees, Branches,
   Orders/OrderDetails** (sin el candado de permisos todavía).
2. Dejar la maquinaria de permisos (pasos 6, 7 y 8) para el final.
3. Aplicarla de forma retroactiva sobre lo ya construido.

**Ventaja:** sistema funcional más rápido para mostrar avances.
**Desventaja:** hay que volver a tocar código ya terminado para agregarle el
candado de permisos después — más trabajo repetido a la larga.

---

## Decisión pendiente

☐ Seguir el orden recomendado (permisos primero, como base)
☐ Seguir la alternativa pragmática (cerrar lo casi listo, permisos al final)