# SaborExpress — Guía de front: cómo funciona "Notifications" con cada rol

Este módulo es distinto a los demás — no es solo un CRUD, tiene una parte de
**tiempo real** (SignalR) que conecta con TODOS los módulos del sistema.
Cualquier usuario (Cliente, Mesero, Cocinero, Cajero, Repartidor,
Administrador, Gerente) recibe notificaciones, pero el contenido de cada una
depende de su rol y de lo que esté pasando en ese momento.

---

## El concepto clave: dos caminos que llegan al mismo lugar

```
Camino 1 — Se guarda siempre (buzón)
   Algo pasa (pedido listo, entrega asignada, etc.)
        ↓
   Se guarda en la base de datos como Notification
        ↓
   El usuario la ve la próxima vez que abra su lista de notificaciones,
   aunque no haya tenido la app abierta en ese momento

Camino 2 — Llega al instante (solo si está conectado)
   Al mismo tiempo que se guarda,
        ↓
   Si el usuario tiene la app abierta y conectada al hub,
   le llega la notificación EN VIVO, sin que tenga que refrescar nada
```

Esto significa que el front necesita **dos piezas** trabajando juntas: la
API normal (para el historial/buzón) y una conexión de SignalR (para lo que
llega mientras la app está abierta).

---

## Cómo se conecta el front al tiempo real

```
1. El usuario hace login, obtiene su accessToken

2. El front abre una conexión a:
   wss://tuservidor/hubs/notifications?access_token={el mismo accessToken}

3. Mientras esa conexión esté abierta, cualquier notificación nueva
   que le corresponda a este usuario llega automáticamente
   (el evento se llama "ReceiveNotification")

4. El front debe reconectar automáticamente si la conexión se cae
   (esto lo maneja normalmente la librería de cliente de SignalR sola,
   con reconexión automática configurada)
```

**Importante:** el token va en la URL de conexión (`?access_token=...`), no
en un header — es la única forma en que los navegadores permiten mandar
credenciales en una conexión de este tipo. Esto ya está resuelto en el
backend, el front solo necesita construir la URL con el token ahí.

---

## Qué notificación recibe cada rol, y por qué

### Cliente

| Notificación | Cuándo llega |
|---|---|
| "Tu pedido va en camino" | Cuando un Repartidor toma su domicilio (`Deliveries`) |
| "Tu pedido está listo" | Cuando el Cocinero marca el pedido como `Ready` (`Orders`) |
| "Tu pago fue confirmado" | Cuando se completa un pago (`Payments`) |

El Cliente es quien más se beneficia de la parte en tiempo real — es la
pieza que le da la sensación de "seguimiento en vivo" de su pedido, similar
a cualquier app de domicilios que ya conozca.

### Mesero

| Notificación | Cuándo llega |
|---|---|
| "Mesa 5 - pedido listo para servir" | Cuando el Cocinero marca `Ready` un pedido de esa mesa |
| Avisos manuales del Gerente/Administrador | Cuando se los envían con `POST /api/Notifications` |

### Cocinero

| Notificación | Cuándo llega |
|---|---|
| "Nuevo pedido confirmado" | Cuando entra un pedido nuevo a preparar |
| Avisos manuales | Igual que los demás roles |

### Cajero

| Notificación | Cuándo llega |
|---|---|
| Avisos manuales del Gerente/Administrador | Anuncios generales, cambios de turno, etc. |

### Repartidor

| Notificación | Cuándo llega |
|---|---|
| "Nuevo pedido disponible para domicilio" | Cuando un pedido queda listo y sin repartidor asignado |
| Avisos manuales | Igual que los demás |

### Administrador / Gerente

| Notificación | Cuándo llega |
|---|---|
| Avisos que ellos mismos envían no les llegan a sí mismos (van a otros) | — |
| Reciben las mismas notificaciones manuales que envíen otros Gerentes/Administradores, si se las dirigen a ellos | — |

**Nota:** hoy el tipo `ShiftEndingSoon` existe en el catálogo de tipos de
notificación, pensado para avisar a un empleado operativo que su turno está
por terminar — es una buena función a futuro para Mesero/Cocinero/Cajero/
Repartidor, pero conviene confirmar si ya está conectada a algo o si sigue
pendiente de implementarse en el flujo de `EmployeeSchedules`.

---

## Vista de todos los roles: la campanita de notificaciones

```
Cualquier usuario entra a la app
        ↓
Front llama: GET /api/Notifications/user/{miPropioUserId}/unread-count
        ↓
Muestra el número en el ícono de campana (ej. 🔔 3)
```

### Al tocar la campana

```
GET /api/Notifications/user/{miPropioUserId}?unreadOnly=false
```

Muestra la lista completa (leídas y no leídas), ordenadas de más reciente a
más antigua. Cada una trae `title`, `message`, `type`, y opcionalmente
`relatedEntityType` + `relatedEntityId` — esto último es clave para la
navegación.

### Navegación al tocar una notificación puntual

```
El usuario toca "Tu pedido está listo" (relatedEntityType: "Order", relatedEntityId: 42)
        ↓
El front interpreta ese tipo y navega directo al detalle:
GET /api/Orders/42
        ↓
Se abre la pantalla del pedido, sin que el usuario tenga que buscarlo manualmente
```

El front debe tener un pequeño "mapa" de qué pantalla abrir según el
`relatedEntityType` ("Order" → detalle de pedido, "Delivery" → detalle de
entrega, "Payment" → detalle de pago).

### Marcar como leída

```
Al tocar una notificación individual → PATCH /api/Notifications/{id}/read
Al tocar "Marcar todo como leído"    → PATCH /api/Notifications/user/{miUserId}/read-all
```

### Eliminar una notificación vieja

```
DELETE /api/Notifications/{id}
```

---

## Vista de Gerente/Administrador: enviar avisos manuales

```
POST /api/Notifications
{
  "userId": 15,          ← a una persona puntual
  "title": "Reunión de personal",
  "message": "Hoy a las 3pm en la sala de descanso",
  "type": "Manual"
}
```

o, para avisar a toda una sede de una vez:

```
POST /api/Notifications
{
  "branchId": 2,          ← a todos los empleados activos de esa sede
  "title": "Cambio de horario",
  "message": "El restaurante cierra 1 hora antes hoy",
  "type": "Manual"
}
```

**Nota:** debe venir **uno de los dos** (`userId` o `branchId`), nunca los
dos ni ninguno — el front debe validar esto antes de mandar el request, para
no depender solo del error del backend.

---

## Resumen: reglas para el front de este módulo

1. **Conecta el hub apenas el usuario hace login**, y mantenlo abierto mientras la sesión esté activa — es lo que le da la sensación de "tiempo real" a toda la app.

2. **Nunca dependas solo del hub** — siempre debe existir la ruta de "consultar el buzón" (`GET /api/Notifications/user/{id}`) para cuando el usuario abre la app después de haber estado desconectado; el hub es un extra, no el único camino.

3. **Usa `relatedEntityType` + `relatedEntityId` para la navegación directa** — es lo que hace que tocar una notificación se sienta útil, en vez de solo mostrar texto.

4. **El contador de no leídas debe refrescarse tanto por polling normal como por el evento en vivo** — cuando llega una notificación nueva por el hub, incrementa el contador localmente sin esperar a volver a pedir el número completo al backend.

5. **El formulario de "enviar aviso manual"** (solo Gerente/Administrador) debe forzar elegir entre destinatario puntual o toda una sede, nunca ambos.

---

## Próximo paso sugerido

Con esto puedes armar un `notificationsApi` (REST normal) y un
`notificationsSocket` (conexión SignalR) por separado, pero que compartan el
mismo estado en el front — por ejemplo, un contexto/store global de
"notificaciones" que ambos actualicen, así cualquier pantalla de la app
puede mostrar el contador actualizado sin importar si vino del REST o del
hub en vivo.