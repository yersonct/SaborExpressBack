# SaborExpress — Guía de front: cómo interactúan Gerente y Administrador con "Sedes" (Branches)

Este documento es el mapa de pantallas + llamadas al API para construir la
sección de sedes. A diferencia de Employees, aquí el **Gerente** ve/gestiona
**todas** las sedes de la cadena, mientras que el **Administrador** solo ve y
edita **su propia sede** — no puede crearlas ni borrarlas, ni ver las demás.

---

## Diferencia clave entre roles en este módulo

| Acción | Gerente | Administrador |
|---|---|---|
| Ver todas las sedes | ✅ `GET /api/Branches` | ❌ No tiene acceso a este endpoint |
| Ver mi propia sede | ✅ `GET /api/Branches/mine` | ✅ `GET /api/Branches/mine` |
| Ver el detalle de una sede puntual | ✅ `GET /api/Branches/{id}` | ❌ No tiene acceso |
| Crear una sede nueva | ✅ `POST /api/Branches` | ❌ No puede |
| Editar una sede | ✅ Cualquiera | ✅ Solo la suya |
| Eliminar una sede | ✅ | ❌ No puede |

Esto significa que **el front debe mostrar pantallas distintas según el rol**,
no la misma pantalla con botones ocultos — porque el Administrador ni siquiera
tiene acceso a la lista completa (`GET /api/Branches` le devolvería `403`).

---

## Vista del Gerente: "Todas las Sedes"

### Qué pasa al cargar la pantalla

```
Gerente entra a "Sedes" en el panel
        ↓
Front llama: GET /api/Branches
        ↓
Backend responde con TODAS las sedes de la cadena
```

### Ejemplo de respuesta

```json
[
  {
    "id": 1,
    "name": "SEDE NORTE",
    "address": "Cra 10 # 20-30",
    "phone": "3001234567",
    "status": true,
    "employeeCount": 12
  },
  {
    "id": 2,
    "name": "SEDE SUR",
    "address": "Calle 45 # 12-08",
    "phone": "3109876543",
    "status": true,
    "employeeCount": 8
  }
]
```

### Cómo se pinta

Una tabla o tarjetas, una por sede:

| Sede | Dirección | Teléfono | Empleados | Estado | Acciones |
|---|---|---|---|---|---|
| Sede Norte | Cra 10 # 20-30 | 300 123 4567 | 12 | 🟢 Activa | ✏️ Editar · 🗑️ Eliminar |
| Sede Sur | Calle 45 # 12-08 | 310 987 6543 | 8 | 🟢 Activa | ✏️ Editar · 🗑️ Eliminar |

Un botón arriba de la tabla: **"+ Nueva sede"** → abre el formulario de creación.

---

## Vista del Administrador: "Mi Sede"

### Qué pasa al cargar la pantalla

```
Administrador entra a "Mi Sede" (no ve un listado, entra directo al detalle)
        ↓
Front llama: GET /api/Branches/mine
        ↓
Backend resuelve solo con el token: "este Administrador pertenece a Sede Norte"
        ↓
Responde con los datos de esa sede puntual
```

### Ejemplo de respuesta

```json
{
  "id": 1,
  "name": "SEDE NORTE",
  "address": "Cra 10 # 20-30",
  "phone": "3001234567",
  "status": true,
  "employeeCount": 12
}
```

### Cómo se pinta

No es una tabla — es más bien una tarjeta de perfil de la sede, con un botón de **"Editar"** (nunca de "Eliminar", porque el Administrador no tiene ese permiso):

```
┌─────────────────────────────┐
│  SEDE NORTE                 │
│  Cra 10 # 20-30              │
│  📞 300 123 4567             │
│  👥 12 empleados             │
│  🟢 Activa                   │
│                              │
│  [ ✏️ Editar información ]   │
└─────────────────────────────┘
```

**Importante para el front:** como el Administrador nunca ve otras sedes, no
necesitas construir ni un selector de sede ni una tabla — es directo al grano.

---

## Crear una sede nueva (solo Gerente)

```
Gerente toca "+ Nueva sede"
        ↓
Front abre formulario: Nombre, Dirección, Teléfono
        ↓
Gerente llena y da "Guardar"
        ↓
Front llama: POST /api/Branches
        ↓
Backend valida: nombre único, dirección (5-150 caracteres),
                teléfono (7 u 10 dígitos)
```

### Ejemplo de request

```json
POST /api/Branches
{
  "name": "Sede Poblado",
  "address": "Cra 43A # 5-15",
  "phone": "3201234567"
}
```

### Casos que el front debe manejar

| Resultado | Qué mostrar |
|---|---|
| `201 Created` | Toast "Sede creada correctamente" + refrescar la lista |
| `400` nombre duplicado | "Ya existe una sede con ese nombre" bajo el campo Nombre |
| `400` teléfono inválido | "El teléfono debe tener 7 u 10 dígitos, sin espacios" bajo el campo Teléfono |
| `400` dirección muy corta/larga | Mensaje bajo el campo Dirección |

---

## Editar una sede

### Caso Gerente (cualquier sede)

```
Gerente entra al detalle de "Sede Sur" → toca "Editar"
        ↓
Front llama: PUT /api/Branches/2
        ↓
200 OK → se actualiza sin restricciones
```

### Caso Administrador (solo la suya)

```
Administrador entra a "Mi Sede" → toca "Editar información"
        ↓
Front llama: PUT /api/Branches/1
(el 1 es SU propia sede, resuelto antes con /mine)
        ↓
Backend valida: ¿esta sede (1) es la misma del Administrador? Sí → 200 OK
```

Si por algún motivo el front tuviera un bug y mandara el Id equivocado, el
backend respondería con `400`/`409`: **"Solo puedes editar la sede a la que
perteneces"** — pero en el flujo normal el Administrador nunca ve ni maneja
el Id de otra sede, así que este error prácticamente no debería aparecer.

### Ejemplo de request (igual para ambos roles)

```json
PUT /api/Branches/1
{
  "name": "Sede Norte",
  "address": "Cra 10 # 20-30, nueva dirección",
  "phone": "3001234567",
  "status": true
}
```

**Nota:** a diferencia de Employees (que ahora solo devuelve un mensaje),
`Branches` sigue devolviendo el objeto actualizado completo — así que el front
puede refrescar la tarjeta/fila directamente con la respuesta, sin otro `GET`.

---

## Eliminar una sede (solo Gerente)

```
Gerente toca "Eliminar" en "Sede Antigua"
        ↓
Front muestra modal de confirmación:
"¿Seguro que quieres eliminar Sede Antigua? Esta acción no se puede deshacer."
        ↓
Gerente confirma
        ↓
Front llama: DELETE /api/Branches/{id}
```

### Dos resultados posibles

| Resultado | Qué mostrar |
|---|---|
| `204 No Content` | Toast "Sede eliminada" + quitarla de la lista |
| `400` tiene empleados asignados | Modal/alerta: **"No se puede eliminar esta sede porque tiene empleados asignados. Puedes desactivarla en su lugar."** + ofrecer un botón directo "Desactivar en su lugar" que dispare un `PUT` cambiando solo `status: false` |

Esta segunda opción es importante de construir bien en el front — el backend
te da el motivo exacto del bloqueo, así que el front debería aprovecharlo para
ofrecer la alternativa (desactivar) en el mismo momento, en vez de solo mostrar
un error genérico.

---

## Resumen: reglas para el front de este módulo

1. **El Administrador NUNCA ve un listado de sedes** — su experiencia entra directo a `/mine`, sin selector ni tabla.
2. **El Gerente sí ve el listado completo**, con acceso a crear/editar/eliminar cualquiera.
3. **El botón "Eliminar" solo debe existir en la vista del Gerente** — ni siquiera lo agregues (oculto o deshabilitado) en la vista del Administrador, porque ese endpoint le devolvería `403` directamente.
4. **`Create` y `Update` sí devuelven el objeto completo** (a diferencia de Employees) — aprovecha eso para no tener que recargar con un `GET` extra después de cada acción.
5. **El error de "tiene empleados asignados" al eliminar** es una oportunidad de UX — ofrece la alternativa de desactivar en el mismo mensaje, no solo un error plano.

---

## Próximo paso sugerido

Con esto puedes armar un `branchesApi` con 5 funciones (`getAll`, `getMine`,
`getById`, `create`, `update`, `delete`) y construir **dos pantallas distintas**
según el rol logueado — no una sola pantalla con lógica condicional compleja,
porque el propio backend ya separa claramente qué puede ver cada uno.