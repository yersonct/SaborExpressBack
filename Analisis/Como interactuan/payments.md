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

## Próximo paso sugerido

Arma un `paymentsApi` con `create`, `getByOrder`, `getById`, `refund`,
`getAll`, `initWompiPayment`. Para el flujo de Wompi específicamente, separa
la lógica de "abrir el widget" de la de "confirmar el resultado" — son dos
responsabilidades distintas y el front debe manejarlas con paciencia, ya que
la confirmación depende de un webhook externo, no de la respuesta inmediata
de tu propia API.