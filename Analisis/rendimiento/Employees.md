# SaborExpress — Plan de pruebas funcionales + rendimiento: Módulo Employees

Este módulo tiene un caso especial: `POST /api/Employees` **dispara un correo**
(código de activación de cuenta), así que ese endpoint puntual debe tratarse
con la misma expectativa que vimos en Auth y Customers — si el envío no corre
en background, ese endpoint va a salir lento.

También es el módulo donde acabamos de aplicar 3 mejoras de código:
proyección liviana en los listados (para no traer el PDF del CV al listar),
control de sede en `GetById`, y eliminación del N+1 al validar roles. Este plan
sirve para confirmar que esas mejoras funcionan como se espera.

---

## Orden de prueba sugerido

| # | Endpoint | Requiere antes |
|---|---|---|
| 1 | `POST /api/Employees` | Login como Gerente/Administrador |
| 2 | `GET /api/Employees` | Paso 1 |
| 3 | `GET /api/Employees/{id}` | Paso 1 |
| 4 | `PUT /api/Employees/{id}` | Paso 1 |
| 5 | `GET /api/Employees/{id}/cv` | Empleado con CV cargado |
| 6 | `DELETE /api/Employees/{id}` | Paso 1 |

---

## Casos de prueba y qué anotar

### 1. `POST /api/Employees`
- ✅ Caso feliz: datos válidos + al menos 1 rol → `201`, dispara el correo con el código de activación
- ❌ Documento ya registrado → `400`
- ❌ Email ya registrado → `400`
- ❌ Sin roles asignados (`RoleIds` vacío) → `400`
- ❌ Un rol inexistente en `RoleIds` (ej. `999`) → `400`
- ❌ Sucursal inexistente en `BranchId` → `400`
- ❌ CV que no es PDF, o pesa más de 5MB → `400`
- ❌ Usuario sin permiso `CrearEmpleado` → `403`
- **Tiempo esperado:** si el envío de correo corre en background → milisegundos aparte del guardado en base de datos. Si no → puede tardar varios segundos, igual que pasaba antes con `forgot-password`. Primer candidato a revisar si sale lento.

### 2. `GET /api/Employees`
- ✅ Caso feliz (Gerente): trae empleados de todas las sedes → `200`
- ✅ Caso feliz (Administrador): trae solo empleados de su propia sede → `200`
- ✅ Filtro `?estado=retirado` → solo trae los dados de baja
- ✅ Filtro `?estado=todos` → trae activos y retirados
- ❌ Un Mesero/Cocinero (sin rol Gerente/Administrador) intentando entrar → `403`
- **Tiempo esperado:** < 200ms, incluso con varios empleados con CV cargado. Este es el endpoint que arreglamos con la proyección liviana — antes traía el PDF completo de cada empleado solo para calcular un booleano (`HasCv`). Si el tiempo sigue siendo alto con muchos CVs cargados, revisar que el repositorio esté usando `GetAllLightAsync`/`GetAllByBranchLightAsync` y no los métodos viejos.

### 3. `GET /api/Employees/{id}`
- ✅ Id existente, misma sede (o Gerente viendo cualquier sede) → `200`
- ❌ Id inexistente → `404`
- ❌ **Administrador de la Sede A intentando ver un empleado de la Sede B** → `403` (este caso es el que arreglamos: antes no existía este control y era una fuga de datos entre sedes)
- **Tiempo esperado:** < 100ms

### 4. `PUT /api/Employees/{id}`
- ✅ Caso feliz: actualiza datos y roles → `200`
- ❌ Sin roles asignados → `400`
- ❌ Email ya usado por otro usuario → `400`
- ❌ Administrador intentando editar un empleado de otra sede → `400` (mensaje: "Solo puedes gestionar empleados de tu propia sede")
- **Tiempo esperado:** < 200ms

### 5. `GET /api/Employees/{id}/cv`
- ✅ Empleado con CV cargado → `200`, descarga el PDF
- ❌ Empleado sin CV → `404`
- **Tiempo esperado:** depende del tamaño del PDF (hasta 5MB), no aplica el límite de 200ms acá — es una descarga de archivo, no una consulta de datos.

### 6. `DELETE /api/Employees/{id}` (soft-delete)
- ✅ Caso feliz: pasa a `Status = "Retirado"` → `204`
- ❌ Id inexistente → `404`
- ❌ Usuario sin permiso `EliminarEmpleado` → `403`
- **Tiempo esperado:** < 150ms

---

## Tabla para ir llenando

| Endpoint | Caso | Status esperado | Status real | Tiempo | Resultado |
|---|---|---|---|---|---|
| `POST /api/Employees` | Creación válida | 201 | | | |
| `POST /api/Employees` | Documento duplicado | 400 | | | |
| `POST /api/Employees` | Sin roles | 400 | | | |
| `POST /api/Employees` | Rol inexistente | 400 | | | |
| `POST /api/Employees` | CV no-PDF | 400 | | | |
| `POST /api/Employees` | Sin permiso | 403 | | | |
| `GET /api/Employees` | Gerente ve todas las sedes | 200 | | | |
| `GET /api/Employees` | Administrador ve solo su sede | 200 | | | |
| `GET /api/Employees` | Rol sin permiso | 403 | | | |
| `GET /api/Employees/{id}` | Id existente, misma sede | 200 | | | |
| `GET /api/Employees/{id}` | Id inexistente | 404 | | | |
| `GET /api/Employees/{id}` | Administrador de otra sede | 403 | | | |
| `PUT /api/Employees/{id}` | Actualización válida | 200 | | | |
| `PUT /api/Employees/{id}` | Otra sede (Administrador) | 400 | | | |
| `GET /api/Employees/{id}/cv` | Con CV | 200 | | | |
| `GET /api/Employees/{id}/cv` | Sin CV | 404 | | | |
| `DELETE /api/Employees/{id}` | Soft-delete válido | 204 | | | |
| `DELETE /api/Employees/{id}` | Sin permiso | 403 | | | |

---

## Por dónde empezar

Empieza por **`GET /api/Employees`** — es el endpoint que más cambió en esta
revisión (proyección liviana) y el más fácil de comparar: si tenés empleados
de prueba con CV cargado, fijate que el tiempo de respuesta sea notablemente
bajo (< 200ms) a pesar de eso. Después seguí con `GET /api/Employees/{id}`
probando específicamente el caso de un Administrador intentando ver un
empleado de otra sede — ese `403` es la prueba de que la brecha de seguridad
quedó cerrada.