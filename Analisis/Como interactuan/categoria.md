# SaborExpress — Guía de front: cómo interactúan todos los roles con "Categories"

Este módulo es de los más simples del sistema en cuanto a permisos — solo
hay dos niveles: **cualquiera puede consultar**, y **solo Gerente/Administrador
puede modificar**. No hay reglas de sede, turno, ni permisos operativos finos
(no usa `CanPerformActionAsync`, a diferencia de Orders/Tables/Payments).

---

## Matriz rápida de quién puede qué

| Acción | Cliente | Mesero | Cocinero | Cajero | Repartidor | Administrador | Gerente |
|---|---|---|---|---|---|---|---|
| Ver categorías (lista y detalle) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Crear categoría | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |
| Editar categoría | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |
| Eliminar categoría | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |

**Importante:** aquí Administrador y Gerente tienen **exactamente los mismos
permisos** — a diferencia de Branches o Employees, no hay restricción de
"solo tu sede", porque las categorías son globales para toda la cadena, no
pertenecen a una sede en particular.

---

## Vista de todos los roles operativos y el Cliente: solo consulta

### Qué pasa al cargar el menú (cualquier rol)

```
Cualquier usuario autenticado (Cliente, Mesero, Cocinero, Cajero,
Repartidor, Administrador, Gerente) abre la app
        ↓
Front llama: GET /api/Categories
        ↓
Backend responde con TODAS las categorías, sin ningún filtro por rol
```

### Ejemplo de respuesta (igual para todos los roles)

```json
[
  { "id": 1, "name": "Entradas", "description": "Platos para picar", "status": true, "createdAt": "2026-01-10T00:00:00Z" },
  { "id": 2, "name": "Platos Fuertes", "description": "El plato principal", "status": true, "createdAt": "2026-01-10T00:00:00Z" },
  { "id": 3, "name": "Bebidas", "description": "Jugos, gaseosas, cervezas", "status": true, "createdAt": "2026-01-10T00:00:00Z" },
  { "id": 4, "name": "Postres", "description": "Para el final", "status": true, "createdAt": "2026-01-10T00:00:00Z" }
]
```

### Dónde se usa esto en cada rol

| Rol | Para qué consulta las categorías |
|---|---|
| **Cliente** | Ve las categorías como pestañas/secciones al navegar el menú para pedir domicilio |
| **Mesero** | Ve las categorías al tomar un pedido en mesa, para ubicar rápido el plato que el cliente pidió |
| **Cocinero** | Las usa de forma indirecta al revisar `DailyMenu`, organizadas por categoría |
| **Cajero** | Rara vez las consulta directo — normalmente ve el pedido ya armado, no arma el pedido él mismo |
| **Repartidor** | No las usa — su flujo empieza en `Deliveries`, no toca el menú |
| **Administrador / Gerente** | Las consultan también al gestionar el catálogo de productos, para saber en qué categoría meter cada plato nuevo |

### Ver el detalle de una categoría puntual (cualquier rol)

```
GET /api/Categories/3
        ↓
{ "id": 3, "name": "Bebidas", "description": "Jugos, gaseosas, cervezas", "status": true, "createdAt": "..." }
```

Uso típico: el front pide el detalle de una categoría puntual cuando el
usuario toca sobre ella en el menú, o cuando el Administrador abre el
formulario de edición.

---

## Vista de Gerente/Administrador: gestión del catálogo

### Crear una categoría nueva

```
Gerente/Administrador entra a "Categorías" en el panel
        ↓
Toca "+ Nueva categoría"
        ↓
Front llama: POST /api/Categories
{
  "name": "Vegano",
  "description": "Opciones sin ingredientes de origen animal"
}
        ↓
Backend valida: nombre obligatorio, máx. 100 caracteres,
                descripción máx. 500 caracteres, nombre único
        ↓
201 Created
```

### Casos que el front debe manejar

| Resultado | Qué mostrar |
|---|---|
| `201` | Toast "Categoría creada" + refrescar la lista |
| `400` nombre duplicado | "Ya existe una categoría con ese nombre" bajo el campo Nombre |
| `400` nombre vacío/muy largo | Mensaje bajo el campo Nombre |
| `400` descripción muy larga | Mensaje bajo el campo Descripción |

### Editar una categoría existente

```
Administrador toca "Editar" en "Bebidas"
        ↓
Front precarga el formulario con los datos actuales (GET previo, o los
que ya tenía en la tabla)
        ↓
Administrador cambia la descripción
        ↓
Front llama: PUT /api/Categories/3
{
  "name": "Bebidas",
  "description": "Jugos naturales, gaseosas, cervezas y cocteles",
  "status": true
}
        ↓
204 No Content → el front refresca la fila con los nuevos datos
```

**Nota para el front:** este `PUT` no usa `multipart/form-data` como
Employees — es JSON normal, porque Categories no maneja archivos.

### Desactivar una categoría (vía `status`, no borrado)

No hay un endpoint separado de "desactivar" — se hace mandando `status: false`
en el mismo `PUT`. El front puede ofrecer esto como un toggle/switch en la
tabla en vez de obligar a abrir el formulario completo:

```
Administrador apaga el switch de "Postres" en la tabla
        ↓
Front arma el PUT con los mismos datos que ya tenía, solo cambiando status a false
        ↓
204 No Content → la fila se pinta en gris/inactiva
```

### Eliminar una categoría

```
Gerente/Administrador toca "Eliminar" en "Postres"
        ↓
Front muestra modal de confirmación:
"¿Seguro que quieres eliminar 'Postres'? Esta acción no se puede deshacer."
        ↓
Confirma
        ↓
Front llama: DELETE /api/Categories/4
```

### Los dos resultados posibles, y cómo debe reaccionar el front

| Resultado | Qué mostrar |
|---|---|
| `204 No Content` | Toast "Categoría eliminada" + quitarla de la lista |
| `400`/`409` tiene productos activos | Alerta: **"No se puede eliminar esta categoría porque tiene productos activos. Puedes desactivarla en su lugar."** + ofrecer el botón directo de "Desactivar" (que dispara el `PUT` con `status: false`) |

Igual que hicimos notar en `Branches`, esta segunda opción es una buena
oportunidad de UX — el backend te da el motivo exacto del bloqueo, aprovéchalo
para ofrecer la alternativa en el mismo momento en vez de solo mostrar un
error seco.

---

## Resumen: reglas para el front de este módulo

1. **La lista de categorías se pide igual para todos los roles** — no hay que armar vistas distintas según quién esté logueado, a diferencia de Branches o Employees.
2. **Solo Gerente/Administrador ven los botones de Crear/Editar/Eliminar** — el resto de roles (incluido Cliente) solo navegan la lista en modo lectura.
3. **No hay distinción entre Gerente y Administrador aquí** — a diferencia de otros módulos, ambos tienen exactamente los mismos permisos sobre Categories (no hay restricción de sede).
4. **El "desactivar" es solo un `PUT` con `status: false`**, no un endpoint aparte — el front puede modelarlo como un switch/toggle en la UI en vez de un botón separado.
5. **El error de "tiene productos activos" al eliminar** es la misma oportunidad de UX que en Branches: ofrece "Desactivar en su lugar" en el mismo mensaje.

---

## Próximo paso sugerido

Con esto puedes armar un `categoriesApi` simple con 5 funciones (`getAll`,
`getById`, `create`, `update`, `delete`), y una sola pantalla de tabla que
muestra u oculta los botones de acción según si el usuario logueado tiene
rol Gerente/Administrador — sin necesidad de pantallas separadas por rol,
como sí hicimos con Branches.