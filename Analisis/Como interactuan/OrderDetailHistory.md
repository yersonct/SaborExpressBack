# OrderDetailHistory — Guía para Frontend

## Qué es esto

Es el **historial de cambios de una línea de pedido** — no del pedido
completo, sino de cada producto individual dentro de un pedido. Cada vez que
una línea se crea, se edita (cambia cantidad/notas) o se anula, queda una
fila de auditoría con quién lo hizo y cuándo.

Piensa en un pedido con 3 platos: si el mesero cambia la cantidad de uno de
esos platos, ese cambio queda registrado por separado — no afecta el
historial de los otros 2 platos del mismo pedido.

---

## Para qué le sirve al frontend

Es información de **solo lectura**, pensada para trazabilidad — no hay
manera de crear ni editar estas filas directamente. Se generan solas cada
vez que el backend procesa un cambio en una línea de pedido.

Casos de uso típicos:

- Un Gerente o Administrador quiere ver **quién** cambió la cantidad de un
  plato en un pedido, y **por qué** (si fue anulado, con qué motivo).
- Resolver un reclamo: "el cliente dice que pidió 2 platos y le llegó 1" —
  el historial muestra si alguien lo modificó después de creado.
- Auditoría interna: detectar si un empleado anula líneas con frecuencia
  sospechosa.

---

## Endpoint disponible

| Método | Endpoint | Qué devuelve |
|---|---|---|
| GET | `/api/order-details/{orderDetailId}/history` | Lista de cambios de esa línea puntual, del más reciente al más antiguo |

**Requiere estar autenticado** (`[Authorize]`), pero no está restringido por
rol — cualquier usuario logueado puede consultarlo. No hay filtro por
sucursal tampoco: si tienes el `orderDetailId`, puedes ver su historial sin
importar de qué sede sea.

---

## Qué trae cada registro del historial

| Campo | Qué es |
|---|---|
| `Action` | Tipo de cambio: `Created`, `Updated`, `Cancelled`, `Voided` |
| `OldValue` | Cómo estaba la línea antes del cambio (texto libre, ej. "Qty: 2, Notes: sin cebolla") |
| `NewValue` | Cómo quedó después del cambio |
| `Reason` | Motivo, solo se llena cuando la acción es anular (`Voided`) |
| `ChangedByEmployeeId` / `ChangedByEmployeeName` | Quién hizo el cambio |
| `ChangedAt` | Cuándo |

---

## Cómo se integra con el flujo del frontend

Este endpoint **no se consulta solo** — normalmente aparece como un detalle
secundario dentro de la pantalla de un pedido, no como su propia sección de
menú. El flujo típico sería:

1. El usuario está viendo el detalle de un pedido (`GET /api/Orders/{id}`),
   que ya trae la lista de líneas (`OrderDetails`) con su `id` de cada una.
2. Si el usuario quiere ver el historial de una línea puntual (por ejemplo,
   tocando un ícono de "ver cambios" junto a esa línea), ahí se llama a
   `GET /api/order-details/{orderDetailId}/history` usando el `id` de esa
   línea específica — no el `id` del pedido completo.
3. Se muestra como una lista cronológica (más reciente arriba), tipo
   "timeline" — cada entrada con la acción, quién la hizo y cuándo.

**No hace falta traer este historial al cargar el pedido completo** — es
información que solo se pide bajo demanda cuando alguien quiere
investigar una línea puntual, para no sobrecargar la carga inicial del
pedido con datos que la mayoría de las veces nadie va a mirar.

---

## Qué pasa si el `orderDetailId` no existe

El backend responde con error (`ArgumentException` → probablemente un 400)
si el `orderDetailId` no corresponde a ninguna línea real. El frontend
debería tratar esto como un caso de "la línea ya no existe o el id es
inválido", no reintentar la petición.