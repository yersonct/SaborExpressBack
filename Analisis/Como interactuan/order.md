# SaborExpress — Guía de front: cómo interactúan los roles con "Orders" (Pedidos)

Este es el módulo central del sistema. Un mismo pedido pasa por varias manos
a lo largo de su vida, y además ahora tiene reglas de **sede** (como ya vimos
en Branches/Employees/Tables): el Administrador solo ve lo de su propia sede,
el Gerente ve todo.

---

## Concepto clave 1: el pedido puede nacer "sin dueño"

```
Si el pedido lo crea un EMPLEADO (Mesero, Cajero, Gerente, Administrador)
   → nace YA con ese empleado como responsable

Si el pedido lo crea un CLIENTE desde la app
   → nace SIN ningún empleado responsable todavía
   → alguien se lo "apropia" después (un Repartidor lo toma para domicilio)
```

## Concepto clave 2: la visibilidad por sede

```
Gerente       → ve pedidos de TODAS las sedes, puede filtrar por la que quiera
Administrador → ve SOLO los pedidos de su propia sede, sin importar qué
                 sede intente pedir en la URL — el backend lo fuerza solo
```

Esto significa que, a diferencia de antes, el front del Administrador **no
necesita** (ni debe) dejarlo elegir sede en un filtro — directamente no
existe esa decisión para él, siempre ve la suya.

---

## Flujo 1: El Cliente pide domicilio desde la app

```
Ana arma su pedido en la app
        ↓
Front llama: POST /api/Orders
(sin mandar ningún dato de empleado — ni siquiera existe ese campo para ella)
        ↓
Backend crea el pedido SIN empleado asignado, Channel: "App"
        ↓
"¡Tu pedido fue recibido! Un repartidor lo tomará pronto."
```

Ana pasa a una pantalla de seguimiento, conectada con `Notifications` para
avisos en tiempo real de cambios de estado.

---

## Flujo 2: El Mesero toma un pedido en mesa

```
Un cliente llega al restaurante, se sienta en la Mesa 5
        ↓
El Mesero abre "Nuevo pedido", elige la mesa
        ↓
Front llama: POST /api/Orders (el Mesero va identificado por su sesión)
        ↓
El pedido nace YA con el Mesero como responsable
```

---

## Flujo 3: Un domicilio "huérfano" encuentra a su repartidor

```
El pedido de Ana aparece en el pool de domicilios disponibles
        ↓
Un Repartidor lo ve y lo toma (módulo Deliveries)
        ↓
Recién ahí el pedido de Ana queda vinculado a un empleado real
```

---

## Vista por rol

### Cliente

```
GET /api/Orders/customer/{customerId}
```
Solo ve **sus propios** pedidos — su historial. Desde un pedido entregado
puede dejar una reseña.

### Mesero

```
GET /api/Orders/table/{tableId}
```
Ve el pedido activo de la mesa que atiende. Con turno activo puede:
- Editar el pedido, si es el suyo
- Cancelarlo con motivo

### Cocinero

Trabaja sobre la lista general de pedidos de su sede, buscando los que
están `Confirmed` o `InPreparation`. Con turno activo puede:
- Cambiar el estado del pedido conforme avanza en cocina

### Cajero

Consulta el detalle del pedido antes de cobrarlo (se cruza con `Payments`).

### Repartidor

No entra directo a `Orders` — su puerta de entrada es `Deliveries`, y desde
ahí "hereda" el pedido que toma.

### Administrador

```
GET /api/Orders/branch/{miPropiaSede}
GET /api/Orders                         (siempre se le filtra a su sede sola)
```
Ve únicamente los pedidos de su propia sede. Si el front intentara mandarle
una sede distinta a la suya, el backend lo bloquea — así que el front ni
siquiera debe ofrecerle esa opción de cambiar de sede.

### Gerente

```
GET /api/Orders?branchId=X    (cualquier sede que elija)
GET /api/Orders                (sin filtro = todas juntas)
GET /api/Orders/branch/{cualquierId}
```
Ve el panorama completo de la cadena. El front puede ofrecerle un selector
de sede (o "todas") sin restricción.

---

## La máquina de estados — para que el front no ofrezca botones inválidos

```
Pending ──────► Confirmed ──────► InPreparation ──────► Ready ──────► Delivered
   │                │                    │                  │
   └────────────────┴────────────────────┴──────────────────┴──────► Cancelled
```

Cada estado solo avanza al siguiente en la línea, o salta a `Cancelled` —
nunca retrocede, nunca se salta un paso. El front debería pintar solo las
transiciones válidas según el estado actual.

---

## Resumen: reglas para el front de este módulo

1. **El formulario de crear pedido es distinto según quién esté logueado** — Cliente no ve campo de empleado; Mesero/Cajero tampoco lo piden, se resuelve solo con la sesión.

2. **Un pedido de Cliente puede mostrar "Pendiente de asignar" en el nombre del empleado** hasta que alguien lo tome — nunca dejar el campo vacío feo en la interfaz.

3. **El Administrador nunca elige sede** — el front no debe darle selector de sede en la vista de pedidos, porque el backend siempre lo fuerza a la suya. Ofrecerle ese selector sería confuso, ya que cualquier otra sede que intente ver le fallaría.

4. **El Gerente sí puede elegir sede o ver todas** — aquí sí tiene sentido un selector/filtro en el front.

5. **Mesero y Cocinero necesitan turno activo** para editar, cancelar o cambiar estado — un error de "no tienes permiso ahora mismo" probablemente significa que no tienen turno asignado en este momento, no que el pedido esté mal.

6. **Respeta la máquina de estados en la interfaz** — no ofrezcas botones para saltos inválidos.

7. **El flujo de domicilio conecta dos módulos** (`Orders` + `Deliveries`) — para el Cliente debe sentirse como una sola experiencia continua, aunque técnicamente sean dos llamadas distintas.

---

## Próximo paso sugerido

Arma un `ordersApi` con `create`, `getById`, `getAll`, `getByTable`,
`getByCustomer`, `getByBranch`, `update`, `updateStatus`, `cancel`. Construye
dos experiencias separadas: la del **Cliente** (historial + seguimiento,
sin selector de sede porque ni le aplica), y la **operativa** para el resto
de roles, donde el Administrador ve fija su sede y el Gerente tiene libertad
de elegir cualquiera.