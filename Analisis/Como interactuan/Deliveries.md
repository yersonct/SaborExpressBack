# SaborExpress — Guía de front: cómo interactúan los roles con "Payments" (Pagos)

Este módulo maneja dinero real, así que el front debe ser especialmente
cuidadoso con la confirmación de montos y con mostrar el estado exacto del
pago. Además, ahora tiene dos caminos de pago: **presencial** (efectivo,
tarjeta física, transferencia) y **Wompi** (pago digital vía widget).

---

## Matriz rápida de quién puede qué

| Acción | Cliente | Mesero | Cocinero | Cajero | Repartidor | Administrador | Gerente |
|---|---|---|---|---|---|---|---|
| Registrar un pago (efectivo/tarjeta/transferencia) | ❌ | ❌ | ❌ | ✅ (turno activo) | ❌ | ✅ | ✅ |
| Iniciar un pago vía Wompi | 🔶 (desde su propio checkout) | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Ver pagos de un pedido | ❌ | 🔶 | ❌ | ✅ | ❌ | ✅ | ✅ |
| Reembolsar un pago | ❌ | ❌ | ❌ | ✅ (permiso `ReembolsarPago`, turno activo) | ❌ | ✅ | ✅ |
| Ver reportes de pagos (todos, por sede/fecha) | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |

---

## Flujo 1: Pago presencial (Cajero cobra en mostrador)

```
El Cajero abre el pedido de la Mesa 5, ve el total: $45.000
        ↓
Front llama: POST /api/Payments
{
  "orderId": 12,
  "method": "Cash",
  "amount": 45000
}
        ↓
Backend valida: (1) el Cajero tiene el permiso + turno activo,
                (2) el pedido no está Cancelado ni Pendiente,
                (3) el monto no excede lo que falta por pagar
        ↓
201 Created
        ↓
Si ese pago completó el TOTAL del pedido → la mesa se libera SOLA (ver guía de Tables)
```

### Casos de error que el front debe anticipar

| Resultado | Qué mostrar |
|---|---|
| `400` pedido cancelado | "No se puede registrar un pago sobre un pedido cancelado" |
| `400` pedido aún Pendiente | "Este pedido todavía no ha sido confirmado" |
| `400` ya pagado completo | "Este pedido ya fue pagado en su totalidad" |
| `400` monto excede lo pendiente | "El monto excede lo pendiente por cobrar. Falta: $X" — el front debería mostrar ese saldo pendiente ANTES de que el Cajero intente cobrar, no solo como reacción al error |
| Sin turno activo | "No tienes permiso para registrar pagos ahora mismo" |

### Recomendación de UX importante

Antes de dejar que el Cajero escriba un monto libremente, el front debería:
1. Llamar primero a `GET /api/Payments/order/{orderId}` para ver si ya hay pagos parciales
2. Calcular el saldo pendiente (`Total del pedido - suma de pagos completados`)
3. Mostrar ese saldo como sugerencia o límite en el campo de monto, en vez de dejar que el Cajero adivine y se encuentre con el error después

---

## Flujo 2: Pago con múltiples métodos (pago dividido)

Como `Payment` permite varios registros por pedido, un cliente puede pagar
parte en efectivo y parte con tarjeta:

```
1. POST /api/Payments { orderId: 12, method: "Cash", amount: 20000 }
   → queda un saldo pendiente de $25.000

2. POST /api/Payments { orderId: 12, method: "Card", amount: 25000 }
   → con este segundo pago se completa el total → la mesa se libera sola
```

El front debe permitir hacer varias llamadas seguidas a `POST /api/Payments`
para el mismo pedido, actualizando el saldo pendiente después de cada una.

---

## Flujo 3: Pago digital vía Wompi (el Cliente paga desde su celular)

Este es un flujo de varios pasos, distinto al presencial:

```
1. Cliente en su checkout, elige "Pagar con Wompi"
        ↓
   Front llama: POST /api/Payments/wompi/init  (o el endpoint que definiste)
   { "orderId": 12, "amount": 45000 }
        ↓
   Backend responde con los datos para armar el widget:
   { paymentId, reference, amountInCents, currency, publicKey, integritySignature }

2. El front usa esos datos para abrir el WIDGET DE WOMPI
   (esto es una librería/iframe de Wompi, no una pantalla propia)

3. El cliente paga dentro del widget con su tarjeta/PSE/etc.

4. Wompi le avisa al BACKEND directamente (webhook), no al front
   → el front NO se entera al instante de si el pago pasó o no

5. El front debe "preguntar" (polling) o esperar una notificación
   para saber si el pago quedó aprobado
```

### Punto crítico para el front: el pago NO es instantáneo desde su perspectiva

A diferencia del pago presencial (donde el Cajero sabe al toque si se
registró), con Wompi hay un tiempo de espera entre que el cliente completa
el widget y que el backend confirma el pago (porque depende de que Wompi
mande su webhook). El front debe:

1. Mostrar una pantalla de "Procesando tu pago..." después de que el cliente cierre el widget
2. Consultar periódicamente `GET /api/Payments/{paymentId}` (polling cada pocos segundos), o mejor, escuchar una notificación en tiempo real si la conectas a `Notifications`
3. Cuando el `Status` cambie a `Completed` → mostrar "¡Pago confirmado!"
4. Si cambia a `Failed` → mostrar "El pago no pudo procesarse, intenta de nuevo"
5. Si se queda mucho tiempo en `Pending` → ofrecer un mensaje de "Esto está tardando más de lo normal" con opción de contactar soporte

---

## Vista del Cajero/Administrador/Gerente: ver pagos de un pedido

```
GET /api/Payments/order/{orderId}
```

Muestra la lista de todos los pagos de ese pedido (útil si fue dividido en
varios métodos), cada uno con su `Method`, `Amount`, `Status`, y quién lo
registró (`CashierName`, que puede salir `null` si fue un pago Wompi sin
cajero de por medio).

---

## Vista de Administrador/Gerente: reembolsos

```
PATCH /api/Payments/{id}/refund
{ "reason": "Cliente reportó plato en mal estado" }
```

### Detalle importante para el front

Al reembolsar, **la mesa asociada NO vuelve a marcarse como "Ocupada"
automáticamente** — si el cliente todavía está en el restaurante y hay que
"reabrir" la mesa por algún motivo, eso se hace manualmente desde el módulo
de Tables, no es parte de este flujo.

---

## Vista de Administrador/Gerente: reportes

```
GET /api/Payments?branchId=X&fromDate=Y&toDate=Z
```

Todos los filtros son opcionales — el front puede ofrecer un dashboard con
selector de rango de fechas y sede (recordando que el Administrador, según
la regla ya vista en otros módulos como Orders, debería quedar limitado a
su propia sede — confirma si `PaymentsController` ya aplica esa misma
restricción, o si es un pendiente para revisar).

---

## Resumen: reglas para el front de este módulo

1. **Muestra siempre el saldo pendiente antes de cobrar** — no dejes que el Cajero adivine el monto, especialmente en pagos divididos.

2. **El pago con Wompi no es síncrono** — el front necesita una pantalla de espera/confirmación, no puede asumir que el pago pasó solo porque el widget se cerró.

3. **La liberación de mesa es automática y silenciosa** — el front del Cajero no necesita hacer nada extra para eso; ocurre en el backend tras completar el pago.

4. **El reembolso no reabre la mesa** — si hace falta, es una acción manual aparte en Tables.

5. **`CashierName` puede venir vacío** — un pago hecho vía Wompi por el cliente mismo no tiene cajero asociado; el front debe manejar ese caso (mostrar "Pago en línea" en vez de un nombre vacío, por ejemplo).

---

## Próximo paso sugerido# SaborExpress — Cómo interactúa cada rol con "Deliveries" (domicilios)

Este módulo funciona con un modelo de **auto-asignación**: nadie le asigna
un pedido a un Repartidor — el propio Repartidor elige, de una lista de
pedidos disponibles de su sede, cuál va a llevar. Es distinto a como
funciona `EmployeeSchedule` (donde el Administrador sí asigna los turnos).

---

## Repartidor — el protagonista de este módulo

**Dónde lo usa:** pantalla "Domicilios disponibles" (una especie de bandeja
de pedidos para tomar) + "Mis entregas" (lo que ya tomó).

### 1. Ver pedidos disponibles

Entra a la pantalla y ve una lista de pedidos tipo Delivery que están
`Confirmado` o `Listo`, **sin repartidor asignado todavía**, y **solo de su
propia sede** — nunca ve pedidos de otras sucursales, aunque estén cerca.

### 2. Tomar un pedido

Toca "Tomar este pedido" en uno de la lista, indica a qué dirección del
cliente va (elige entre las direcciones guardadas del cliente, nunca
inventa una nueva). El sistema:
- Lo asigna a sí mismo automáticamente (no puede tomar un pedido "a nombre de otro" — el backend usa su propia identidad, no algo que se pueda mandar en el formulario).
- Si otro repartidor lo tomó un segundo antes, el sistema rechaza el intento con un mensaje claro ("este pedido ya fue tomado").

### 3. Actualizar el estado de su entrega

A medida que avanza, cambia el estado en 2 pasos:
- **Asignado → En camino** (cuando sale del restaurante)
- **En camino → Entregado** (cuando confirma la entrega en la puerta del cliente)

No puede saltarse pasos (por ejemplo, marcar "Entregado" directo desde "Asignado") ni retroceder un estado ya avanzado.

### Qué NO puede hacer

- Ver ni tomar pedidos de otra sede.
- Ver las entregas de otro repartidor.
- Actualizar el estado de una entrega que no es suya.

---

## Administrador — supervisa su sede, y es el respaldo si el repartidor falla

**Dónde lo usa:** pantalla "Domicilios de mi sede" — ve todos los repartos
en curso y ya entregados de todos los repartidores de su sucursal.

**Qué puede hacer:**
- Consultar todos los domicilios de su propia sede (no de otras).
- Ver las entregas de un repartidor puntual, siempre que sea de su misma sede.
- **Confirmar la entrega en nombre del repartidor** — esto es una excepción operativa, pensada para el caso real de "el repartidor tuvo un problema con el celular y no puede confirmar él mismo, así que el Administrador lo hace por él desde el sistema".

**Qué NO puede hacer:**
- Ver o gestionar domicilios de otra sede.
- Tomar un pedido para sí mismo (eso es exclusivo del rol Repartidor).

---

## Gerente — visión completa, sin el respaldo de confirmación

**Dónde lo usa:** mismo tipo de pantalla que el Administrador, pero sin
límite de sede — puede consultar domicilios de cualquier sucursal del
restaurante.

**Qué puede hacer:**
- Ver todos los domicilios de cualquier sede (filtrando la que le interese).
- Ver las entregas de cualquier repartidor, sin restricción de sede.

**Qué NO puede hacer — y esta es una diferencia importante a comunicar bien en el front:**
El Gerente **no tiene** la excepción de "confirmar entrega en nombre del repartidor" que sí tiene el Administrador. Esto es intencional: la confirmación de entrega es una operación de campo, del día a día de una sede puntual — el Administrador está más cerca de esa operación. Si el front muestra un botón de "Confirmar entrega" en la pantalla del Gerente, hay que asegurarse de que no aparezca, o que aparezca deshabilitado con una explicación, para no generar confusión de "¿por qué no me deja?".

---

## Cliente — sigue su propio pedido, sin interactuar con la asignación

**Dónde lo usa:** pantalla de "Seguimiento de mi pedido" (si pidió a domicilio).

**Qué puede hacer:**
- Ver el estado de su entrega (Asignado / En camino / Entregado) para saber en qué va su pedido.

**Qué NO puede hacer:**
- Elegir o cambiar el repartidor.
- Modificar la dirección de entrega una vez que el repartidor ya tomó el pedido (si necesita cambiar la dirección, debería ser antes de que alguien lo tome, a través del propio pedido, no de este módulo).

---

## Mesero / Cajero / Cocinero — no interactúan con este módulo

Estos roles preparan y gestionan el pedido en sí (`Order`, `OrderDetail`),
pero una vez que el pedido pasa a `Confirmado`/`Listo` y es tipo Delivery,
el relevo lo toma exclusivamente un Repartidor. No hay ninguna pantalla ni
acción de este módulo pensada para estos roles.

---

## Resumen visual

```
Pedido tipo Delivery llega a estado "Confirmado" o "Listo"
              │
              ▼
    Aparece en "Disponibles" — SOLO para Repartidores de esa misma sede
              │
    Un Repartidor lo toma (se auto-asigna, no lo asigna nadie más)
              │
              ▼
    Asignado ──► En camino ──► Entregado
    (el propio Repartidor cambia estos estados, paso a paso, sin saltos)
              │
    Si el Repartidor no puede confirmar → el Administrador de esa
    sede lo hace en su lugar (excepción — el Gerente NO tiene esta opción)
              │
              ▼
    Cliente ve el progreso en "Seguimiento de mi pedido" (solo lectura)
    Administrador ve todo lo de su sede
    Gerente ve todo, de cualquier sede, sin poder confirmar por otro
```

Arma un `paymentsApi` con `create`, `getByOrder`, `getById`, `refund`,
`getAll`, `initWompiPayment`. Para el flujo de Wompi específicamente, separa
la lógica de "abrir el widget" de la de "confirmar el resultado" — son dos
responsabilidades distintas y el front debe manejarlas con paciencia, ya que
la confirmación depende de un webhook externo, no de la respuesta inmediata
de tu propia API.