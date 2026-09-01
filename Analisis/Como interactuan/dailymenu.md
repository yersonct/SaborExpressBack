# SaborExpress — Guía de front: cómo interactúan los roles con "DailyMenu" (Menú del día)

Este módulo distingue entre **planear el menú** (una tarea de gestión, poco
frecuente) y **prender/apagar un plato a media jornada** (una acción rápida,
del día a día, que ahora también puede hacer el Cocinero).

---

## Matriz rápida de quién puede qué

| Acción | Cliente | Mesero | Cocinero | Cajero | Repartidor | Administrador | Gerente |
|---|---|---|---|---|---|---|---|
| Ver el menú disponible de hoy | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | ✅ |
| Ver/planear el menú de una fecha específica | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ (su sede) | ✅ |
| Ver el detalle de una entrada puntual | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |
| Agregar un producto puntual al menú | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ (su sede) | ✅ |
| Reemplazar todo el menú de un día (bulk) | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ (su sede) | ✅ |
| Editar una entrada (fecha/franja/disponibilidad) | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ (su sede) | ✅ |
| **Prender/apagar un plato (toggle)** | ❌ | ❌ | ✅ (su sede, turno activo) | ❌ | ❌ | ✅ (su sede) | ✅ |
| Quitar un producto del menú de un día | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ (su sede) | ✅ |

**El Cocinero solo tiene acceso a una acción: el toggle** — no puede planear
el menú completo, ni agregar/quitar productos, ni editar fechas. Solo puede
apagar (o volver a prender) un plato puntual que ya existe en el menú.

---

## Vista de Cliente, Mesero, Cocinero, Cajero: ver el menú de hoy

```
GET /api/daily-menu/branch/{suSede}/today?period=Almuerzo
```

Este es el endpoint más consultado de todo el sistema — cada vez que
alguien abre la app para pedir, se llama aquí. Filtra automáticamente por
la fecha de hoy y **solo muestra los platos marcados como disponibles**
(los apagados no aparecen en absoluto, ni siquiera tachados).

### Ejemplo de respuesta

```json
[
  {
    "id": 15,
    "branchId": 2,
    "branchName": "Sede Norte",
    "productId": 8,
    "productName": "Bandeja Paisa",
    "categoryName": "Platos Fuertes",
    "productPrice": 28000,
    "date": "2026-08-29",
    "mealPeriod": "Almuerzo",
    "isAvailable": true,
    "createdAt": "..."
  }
]
```

### Cómo se pinta en pantalla

El menú del Cliente/Mesero se arma agrupando por `categoryName` (Entradas,
Platos Fuertes, Bebidas, Postres), mostrando `productName` y `productPrice`
de cada uno — es la vista que ya conocías del módulo `Products`, pero
filtrada solo a lo que está disponible **hoy** en esa sede.

---

## Vista del Cocinero: apagar un plato agotado (su única acción aquí)

```
Se acabó el pescado del día a mitad del almuerzo
        ↓
El Cocinero busca el plato en su lista de hoy, toca "Marcar agotado"
        ↓
Front llama: PATCH /api/daily-menu/{id}/toggle
        ↓
Backend valida: (1) misma sede, (2) permiso ActivarDesactivarPlato,
                (3) turno activo ahora mismo
        ↓
200 OK → el plato desaparece del menú visible para Cliente/Mesero de inmediato
```

**Detalle importante:** el `toggle` invierte el estado actual (`IsAvailable`),
así que el mismo botón sirve tanto para apagar como para volver a prender el
plato si al final sí quedó producto — el front no necesita dos botones
distintos, solo mostrar el estado actual y dejar que el toggle lo invierta.

### Casos de error para el Cocinero

| Resultado | Qué mostrar |
|---|---|
| Sin turno activo | "No tienes permiso para activar/desactivar platos ahora mismo" |
| Plato de otra sede (no debería poder verlo siquiera) | No debería ocurrir en la práctica |

---

## Vista de Administrador/Gerente: planear el menú

### Ver o planear el menú de una fecha específica

```
GET /api/daily-menu/branch/{branchId}/date/2026-08-30?period=Almuerzo
```

Útil para armar desde hoy el menú de mañana. El Administrador solo puede
consultar/planear el de **su propia sede** — el backend lo fuerza, así que
el front no debería ofrecerle un selector de otra sede.

### Reemplazar todo el menú de un día de una sola vez (el caso de uso real)

```
Gerente arma el almuerzo de mañana: selecciona 8 platos de una lista
        ↓
Front llama: POST /api/daily-menu/bulk
{
  "branchId": 2,
  "date": "2026-08-30",
  "mealPeriod": "Almuerzo",
  "productIds": [3, 8, 12, 15, 20, 22, 25, 30]
}
        ↓
Backend BORRA todo lo que había para esa sede+fecha+franja,
y crea de cero las 8 entradas nuevas, todas disponibles por defecto
```

**Advertencia para el front:** este endpoint es "todo o nada" — si el
Gerente ya tenía 10 platos configurados para ese almuerzo y manda una lista
de 8, los 10 anteriores se **eliminan por completo**, no se combinan. El
front debería mostrar una confirmación clara antes de ejecutar esto,
especialmente si detecta que ya existía un menú previo para esa fecha
("Esto reemplazará los 10 platos que ya tenías configurados para este
almuerzo. ¿Continuar?").

### Agregar un producto puntual (sin reemplazar todo)

```
POST /api/daily-menu
{
  "branchId": 2,
  "productId": 33,
  "date": "2026-08-30",
  "mealPeriod": "Almuerzo",
  "isAvailable": true
}
```

Útil cuando solo quieres sumar un plato más sin tocar los que ya estaban.

### Editar o quitar una entrada puntual

```
PUT /api/daily-menu/{id}      → cambia fecha/franja/disponibilidad
DELETE /api/daily-menu/{id}   → la quita del menú de ese día
```

---

## Resumen: reglas para el front de este módulo

1. **El endpoint de "hoy" es el más importante de optimizar en el front** — se consulta constantemente, así que conviene cachear la respuesta un rato corto (unos minutos) en vez de pedirla en cada interacción menor.

2. **El Cocinero solo ve el botón de toggle** — nunca los de planear, agregar en bulk, editar o eliminar. Esos son exclusivos de Administrador/Gerente.

3. **El toggle es un interruptor, no dos botones separados** — "apagar" y "volver a prender" son la misma acción, solo cambia el estado actual que se muestra.

4. **El `bulk` reemplaza todo, no combina** — el front debe advertir claramente antes de ejecutar esta acción si ya existía un menú previo para esa fecha/franja.

5. **El Administrador nunca elige otra sede** — igual que en Orders, Reviews y Tables, el front no debería ofrecerle esa opción porque el backend lo bloquea de todos modos.

---

## Próximo paso sugerido

Arma un `dailyMenuApi` con `getToday`, `getByDate`, `getById`, `create`,
`bulkSet`, `update`, `toggle`, `delete`. La pantalla de "Menú de hoy" (para
Cliente/Mesero/Cocinero/Cajero) solo necesita `getToday` + `toggle` (esta
última solo visible para Cocinero); la pantalla de "Planear menú"
(Administrador/Gerente) usa el resto de funciones completas.