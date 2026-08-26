# SaborExpress — Plan de pruebas funcionales + rendimiento: Módulo Branches

Como aquí no hay envío de correo de por medio (a diferencia de Auth), las
expectativas de tiempo son distintas — todo debería ser rápido (milisegundos),
así que cualquier cosa que se demore más de ~300-500ms ya sería sospechosa.

---

## Orden de prueba sugerido

| # | Endpoint | Requiere antes |
|---|---|---|
| 1 | `POST /api/Branches` | Login como Gerente |
| 2 | `GET /api/Branches` | Paso 1 |
| 3 | `GET /api/Branches/{id}` | Paso 1 |
| 4 | `GET /api/Branches/mine` | Login como Administrador |
| 5 | `PUT /api/Branches/{id}` | Paso 1 |
| 6 | `DELETE /api/Branches/{id}` | Una sede sin empleados |

---

## Casos de prueba y qué anotar

### 1. `POST /api/Branches` (Gerente)
- ✅ Caso feliz: nombre/dirección/teléfono válidos → `201`
- ❌ Nombre duplicado → `400`
- ❌ Teléfono inválido (ej. `123`) → `400`
- **Tiempo esperado:** < 200ms (solo un `INSERT` + una consulta de nombre único)

### 2. `GET /api/Branches` (Gerente)
- ✅ Caso feliz → `200` con la lista completa
- **Tiempo esperado:** debería notarse la mejora vs. antes — ya no trae la lista completa de empleados, solo el conteo. Compara el tiempo con el que tenías antes de este cambio si lo alcanzaste a medir.

### 3. `GET /api/Branches/{id}` (Gerente)
- ✅ Id existente → `200`
- ❌ Id inexistente → `404`
- **Tiempo esperado:** < 100ms

### 4. `GET /api/Branches/mine` (Administrador)
- ✅ Caso feliz → `200` con su propia sede
- ❌ Con token de un usuario sin `Employee` asociado (ej. un Cliente) → error controlado, no un 500
- **Tiempo esperado:** < 150ms — aquí hay una consulta extra (`GetByIdWithRelationsAsync` del usuario actual dentro del `BranchAccessGuard`), así que puede ser un poco más que el resto, pero no debería dispararse

### 5. `PUT /api/Branches/{id}` (Gerente y Administrador)
- ✅ Gerente editando cualquier sede → `200`
- ✅ Administrador editando su propia sede → `200`
- ❌ Administrador intentando editar otra sede → error controlado (`400`/`409`, el mensaje "Solo puedes operar sobre la sede a la que perteneces")
- **Tiempo esperado:** < 200ms

### 6. `DELETE /api/Branches/{id}` (Gerente)
- ✅ Sede sin empleados → `204`
- ❌ Sede con empleados asignados → `400` con el mensaje de que tiene empleados
- **Tiempo esperado:** < 150ms

---

## Tabla para ir llenando

| Endpoint | Caso | Status esperado | Status real | Tiempo | Resultado |
|---|---|---|---|---|---|
| `POST /api/Branches` | Crear sede válida | 201 | | | |
| `POST /api/Branches` | Nombre duplicado | 400 | | | |
| `POST /api/Branches` | Teléfono inválido | 400 | | | |
| `GET /api/Branches` | Listar todas | 200 | | | |
| `GET /api/Branches/{id}` | Sede existente | 200 | | | |
| `GET /api/Branches/{id}` | Sede inexistente | 404 | | | |
| `GET /api/Branches/mine` | Administrador ve la suya | 200 | | | |
| `GET /api/Branches/mine` | Usuario sin Employee | 400/409 | | | |
| `PUT /api/Branches/{id}` | Gerente edita cualquiera | 200 | | | |
| `PUT /api/Branches/{id}` | Admin edita la suya | 200 | | | |
| `PUT /api/Branches/{id}` | Admin intenta otra sede | 400/409 | | | |
| `DELETE /api/Branches/{id}` | Sin empleados | 204 | | | |
| `DELETE /api/Branches/{id}` | Con empleados | 400 | | | |

---

## Por dónde empezar

Empieza por el **`GET /api/Branches`** (punto 2) — es el que más debería
notarse la mejora de rendimiento por el cambio de la proyección SQL (conteo
de empleados calculado en base de datos en vez de traer la lista completa).
Anota ese tiempo primero y seguimos con los demás casos en orden.