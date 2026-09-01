# SaborExpress — Cómo interactúa cada rol con "OrderDetails" (líneas de pedido)

Este módulo no tiene pantalla propia — vive **dentro** de la pantalla de
detalle de un pedido (`Order`). Cada rol ve y puede hacer cosas distintas
sobre las líneas de ese pedido, según su rol y su turno activo.

---

## Mesero — arma el pedido en la mesa

**Dónde lo usa:** pantalla "Pedido — Mesa 5", mientras el pedido está abierto.

**Qué puede hacer:**
- **Agregar un plato** al pedido (elige producto del menú del día, cantidad, notas como "sin cebolla") → aparece una línea nueva en la cuenta, y el total del pedido se actualiza solo.
- **Editar una línea** que todavía no llegó a cocina (cambiar cantidad o la nota) — por ejemplo, el cliente pidió 2 Hamburguesas y ahora quiere 3.
- **Anular una línea** si el cliente se arrepintió antes de que se prepare — tiene que escribir el motivo ("cliente cambió de opinión").

**Qué NO puede hacer:**
- Editar o anular una línea que ya está `Delivered` (entregada) — el sistema lo bloquea.
- Tocar líneas de un pedido ya cerrado (`Delivered`/`Cancelled`) — ni para agregar ni para editar nada.
- Ver o modificar pedidos de otra sede.

**Lo que ve en pantalla:** la lista de líneas con su estado (Pendiente, En preparación, Lista, Entregada), y el total recalculado automáticamente cada vez que agrega/edita/anula algo.

---

## Cocinero — prepara lo que llega

**Dónde lo usa:** pantalla de "Cocina" — una vista tipo tablero con las líneas pendientes de todos los pedidos activos de su sede.

**Qué puede hacer:**
- Ver las líneas en estado `Pending`/`InPreparation` que le corresponden preparar.
- (Si el flujo lo contempla) marcar una línea como lista — esto normalmente pasa por el cambio de estado del pedido completo, no línea por línea, pero puede variar según cómo armes la pantalla de cocina.

**Qué NO puede hacer:**
- Agregar productos nuevos al pedido — eso es tarea del Mesero/Cajero, no de Cocina.
- Editar cantidades o notas de una línea ya creada.
- Anular líneas — si algo no se puede preparar, avisa al Mesero para que la anule él.

**Por qué:** el Cocinero solo ejecuta lo que ya se pidió — no tiene el permiso operativo para tocar el contenido del pedido, solo para prepararlo.

---

## Cajero — cobra y a veces también toma pedidos

**Dónde lo usa:** mismas pantallas que el Mesero, si su rol también tiene el permiso — típicamente en el mostrador, cuando el cliente pide y paga directo (sin pasar por mesa).

**Qué puede hacer:** igual que el Mesero (agregar, editar, anular líneas), siempre y cuando el pedido siga abierto.

**Nota importante:** lo que el Cajero puede hacer en este módulo depende del rol que tenga **activo en su turno ahora mismo**, no de todos los roles que tenga guardados en su perfil. Si hoy está fichado como Cajero, tiene los permisos de Cajero — si mañana lo programan como Mesero, tiene los de Mesero. El front no necesita preguntar "qué roles tiene" en general, sino confiar en lo que el backend ya resuelve según el turno activo.

---

## Repartidor — no interactúa con este módulo

El Repartidor trabaja con el pedido ya armado y cerrado (`Delivery`), no con
sus líneas — para cuando un pedido llega a sus manos, las líneas ya están
fijas. No tiene ninguna pantalla ni permiso relacionado a `OrderDetails`.

---

## Administrador — supervisa su sede

**Dónde lo usa:** puede entrar al detalle de cualquier pedido de su propia
sede, con capacidad de edición si tiene el permiso amplio configurado (ej.
para resolver un reclamo o corregir un error de un mesero).

**Qué puede hacer:**
- Ver todas las líneas de cualquier pedido de su sede.
- Editar/anular líneas de pedidos que **no tomó él mismo** — esto depende de si su rol tiene el permiso "editar cualquier pedido" o solo "editar pedido propio" (son 2 permisos distintos en el sistema).

**Qué NO puede hacer:**
- Tocar pedidos de otra sede — queda fuera de su alcance por diseño.

---

## Gerente — supervisión total

**Dónde lo usa:** igual que el Administrador, pero sin la restricción de sede — puede entrar al detalle de cualquier pedido de cualquier sucursal.

**Qué puede hacer:** todo lo que puede un Administrador, mismo alcance de edición, pero sin límite de sede.

---

## Cliente — solo consulta, no edita

**Dónde lo usa:** en "Mi pedido" o "Seguimiento de pedido", cuando pidió desde la app.

**Qué puede hacer:**
- Ver las líneas de su propio pedido y su estado (para saber si ya está en preparación, listo, etc.)

**Qué NO puede hacer:**
- Agregar, editar, o anular ninguna línea — una vez que el pedido está creado, el Cliente no tiene forma de modificarlo desde este módulo. Si quiere cambiar algo, tiene que comunicarse con el restaurante (llamada, chat, o cancelar el pedido completo si el estado todavía lo permite).

---

## Regla transversal que aplica a todos los roles con permiso de edición

Ningún rol —ni siquiera Gerente o Administrador— puede tocar las líneas de
un pedido que ya está en estado **Entregado** o **Cancelado**. Esto no es una
restricción de permisos, es una regla de negocio fija: un pedido cerrado
queda congelado para siempre, como comprobante histórico. La única forma de
"corregir" algo después de cerrado sería a través de otro proceso (ej. una
nota de crédito o ajuste manual), no editando la línea original.

---

## Resumen visual

```
Mesero/Cajero (con turno activo)
   │
   ├── Agrega líneas ─────► Cocinero las prepara (solo lectura)
   ├── Edita líneas (antes de Delivered)
   └── Anula líneas (con motivo)
              │
              ▼
   Administrador/Gerente pueden supervisar y corregir según su sede/alcance
              │
              ▼
   Cliente solo mira el resultado final, sin poder tocar nada
              │
              ▼
   Repartidor nunca interactúa con las líneas, solo con el pedido ya cerrado
```