# OrderStatusHistory — Guía para Frontend

## Qué es esto

Es el **historial de cambios de estado de un pedido completo** — no de una
línea puntual (eso es `OrderDetailHistory`), sino del pedido en su conjunto:
cuándo pasó de `Pending` a `Confirmed`, de ahí a `InPreparation`, luego
`Ready`, `Delivered`, o si en algún punto se `Cancelled`.

Cada vez que el estado del pedido cambia (vía `PATCH /Orders/{id}/status` o
`PATCH /Orders/{id}/cancel`), se guarda automáticamente una fila nueva con
quién hizo el cambio y cuándo.

---

## Para qué le sirve al frontend

Es de **solo lectura** — no se crea ni edita manualmente, se genera sola.

Casos de uso típicos:

- Mostrar una **línea de tiempo del pedido** al cliente o al staff: "Pedido
  confirmado a las 12:05, en preparación a las 12:10, listo a las 12:25,
  entregado a las 12:30" — ideal para una pantalla de seguimiento de pedido.
- Auditoría: saber quién canceló un pedido y por qué (el campo `Notes` trae
  el motivo cuando la acción es cancelación).
- Medir tiempos de cocina: la diferencia entre el timestamp de
  `InPreparation` y `Ready` da el tiempo real que tardó el plato.

---

## Endpoint disponible

| Método | Endpoint | Qué devuelve |
|---|---|---|
| GET | `/api/orders/{orderId}/status-history` | Lista de cambios de estado de ese pedido, del más reciente al más antiguo |

Requiere estar autenticado (`[Authorize]`), sin restricción de rol ni de
sede — cualquier usuario logueado puede consultar el historial de cualquier
pedido si tiene su `orderId`.

---

## Qué trae cada registro del historial

| Campo | Qué es |
|---|---|
| `Status` | El estado al que pasó el pedido en ese momento (`Pending`, `Confirmed`, `InPreparation`, `Ready`, `Delivered`, `Cancelled`) |
| `Notes` | Notas del cambio — en cancelaciones, aquí va el motivo obligatorio |
| `ChangedByEmployeeId` / `ChangedByEmployeeName` | Quién hizo el cambio |
| `ChangedAt` | Cuándo |

---

## Cómo se integra con el flujo del frontend

A diferencia de `OrderDetailHistory` (que es un detalle secundario que casi
nadie consulta), este historial tiene un uso mucho más visible: es la base
natural de una **pantalla de "seguimiento de pedido"**, tanto para el
cliente que pidió por la app como para el staff que necesita ver en qué
punto va cada pedido.

Flujo típico:

1. El usuario abre el detalle de un pedido (`GET /api/Orders/{id}`).
2. Con el `id` del pedido, se llama a
   `GET /api/orders/{orderId}/status-history`.
3. Se renderiza como una línea de tiempo vertical, en orden cronológico
   (idealmente invirtiendo el orden que trae el backend — que viene del más
   reciente al más antiguo — si se quiere mostrar de arriba hacia abajo en
   el orden en que realmente ocurrieron).
4. Si el pedido está `Delivered` o `Cancelled`, la línea de tiempo está
   "completa" y puede mostrarse como cerrada/final. Si sigue en un estado
   intermedio, se puede mostrar el siguiente paso esperado como "pendiente"
   (esto es lógica de presentación, el backend no lo indica explícitamente).

**A diferencia del historial de líneas**, este sí conviene cargarlo junto
con el detalle del pedido si la pantalla principal ya está pensada como
"seguimiento en tiempo real" — no es tan "bajo demanda" como el de líneas,
porque su caso de uso principal (mostrarle al cliente dónde va su pedido) es
central, no un detalle secundario de investigación.

---

## Qué pasa si el `orderId` no existe

El backend responde con error si el pedido no existe. El frontend debería
tratarlo como "el pedido no existe o el id es inválido", igual que con
`OrderDetailHistory`.