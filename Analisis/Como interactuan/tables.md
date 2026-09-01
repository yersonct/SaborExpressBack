# SaborExpress — Guía de front: "Tables" (con liberación automática al pagar)

Este módulo combina acciones de gestión (poco frecuentes, solo Gerente/
Administrador) con el cambio de estado del día a día (Mesero), y ahora tiene
una pieza **automática**: la mesa se libera sola cuando el pedido queda
pagado del todo — sin que el Mesero tenga que acordarse de hacerlo.

---

## Matriz rápida de quién puede qué

| Acción | Cliente | Mesero | Cocinero | Cajero | Repartidor | Administrador | Gerente |
|---|---|---|---|---|---|---|---|
| Ver mesas de una sede | ❌ | ✅ | ✅ | ✅ | ❌ | ✅ | ✅ |
| Ver detalle de una mesa | ❌ | ✅ | ✅ | ✅ | ❌ | ✅ | ✅ |
| Crear mesa nueva | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ (su sede) | ✅ |
| Editar número de mesa | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ (su sede) | ✅ |
| Cambiar estado manualmente (Ocupar/Reservar) | ❌ | ✅ (su sede, turno activo) | ❌ | ❌ | ❌ | ✅ (su sede) | ✅ |
| Dar de baja mesa | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ (su sede) | ✅ |

---

## El ciclo completo de una mesa, ahora con el paso automático

```
1. Mesero sienta al cliente → marca la Mesa 5 como "Ocupada" (manual)

2. Se toma el pedido, se cocina, se sirve (esto vive en Orders)

3. El Cajero cobra el pedido → POST /api/Payments

4. Si ese pago completa el TOTAL del pedido:
   → el backend AUTOMÁTICAMENTE marca la Mesa 5 como "Disponible"
   → el Mesero NO tiene que hacer nada extra

5. Si el pago fue PARCIAL (no cubrió todo):
   → la mesa sigue "Ocupada", nada cambia todavía
```

### Qué significa esto para el front

**Ya no hace falta que el Mesero libere la mesa manualmente** en el flujo
normal — solo necesita hacerlo a mano en casos excepcionales (ej. el cliente
se va sin pagar del todo por algún motivo especial, o se cancela el pedido
sin cobro). El botón de "Marcar como Disponible" sigue existiendo para esos
casos raros, pero en el día a día normal el mapa se actualiza solo.

### Ejemplo de cómo se ve en pantalla

```
El Cajero cobra $45.000 (el total exacto del pedido de la Mesa 5)
        ↓
POST /api/Payments se completa con 201
        ↓
El front, si tiene el mapa de mesas abierto (Mesero en otra pantalla),
ve la Mesa 5 cambiar de 🔴 Ocupada a 🟢 Disponible SIN que nadie la haya
tocado directamente — esto se nota mejor si el mapa está conectado a
Notifications en tiempo real
```

**Importante para el front:** si tienes el mapa de mesas y la pantalla de
cobro abiertos en dispositivos distintos (el Cajero cobra desde una tablet,
el Mesero ve el mapa desde otra), el cambio de estado de la mesa **no llega
solo** a menos que refresques la lista o estés escuchando actualizaciones en
tiempo real. Vale la pena conectar esto a `Notifications`/SignalR para que
el mapa se sienta "vivo" también en este caso.

---

## Vista de Mesero, Cocinero, Cajero: consulta del mapa

```
GET /api/Tables/branch/{suPropiaSede}
```

Pinta el mapa con colores por estado. Con el cambio nuevo, conviene mostrar
un pequeño indicador quieto de que "esta mesa se liberó automáticamente al
pagar" — por ejemplo, si el estado cambió sin que el propio Mesero lo haya
tocado, podría aparecer un pequeño ícono de "🔄 auto" la primera vez que lo
ve, solo para que entienda por qué cambió sin que él hiciera nada.

---

## Vista del Mesero: cambio manual (para los casos que no son automáticos)

```
PATCH /api/Tables/{id}/status
{ "status": "Occupied" }    (sentar a un cliente nuevo)

PATCH /api/Tables/{id}/status
{ "status": "Available" }   (liberar manualmente, en un caso excepcional)
```

El Mesero sigue necesitando el permiso `CambiarEstadoMesa` + turno activo
para esto — la liberación automática por pago es un camino aparte que no
pasa por este endpoint ni exige el mismo permiso (lo hace el sistema
internamente cuando se registra el pago).

---

## Vista de Administrador/Gerente: gestión (sin cambios respecto a antes)

```
POST /api/Tables       → crear (su sede / cualquiera)
PUT /api/Tables/{id}   → editar número
DELETE /api/Tables/{id} → dar de baja (bloqueado si está Ocupada/Reservada)
```

---

## Resumen: reglas nuevas para el front de este módulo

1. **El Mesero ya no necesita liberar la mesa manualmente en el flujo normal** — pasa solo cuando el `Payment` completa el total del pedido.

2. **La liberación automática solo ocurre si la mesa estaba "Ocupada"** — si estaba en otro estado (Reservada, por ejemplo), el pago no la toca.

3. **Un reembolso NO vuelve a poner la mesa como "Ocupada"** — si necesitas ese caso, hay que gestionarlo manualmente desde el front (el Mesero la marca de nuevo si el cliente sigue ahí).

4. **El mapa de mesas se beneficia de estar conectado a tiempo real** — porque ahora un cambio de estado puede originarse desde una pantalla completamente distinta (la de Pagos), no solo desde el propio módulo de Mesas.

---

## Próximo paso sugerido

Si construyes el flujo de cobro y el mapa de mesas en pantallas separadas
(típico: Cajero cobra desde su propia vista, Mesero ve el mapa desde la
suya), conecta ambas a `Notifications` para que el cambio de estado se vea
reflejado sin que nadie tenga que refrescar manualmente.