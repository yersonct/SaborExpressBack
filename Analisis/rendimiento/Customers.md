# SaborExpress — Plan de pruebas funcionales + rendimiento: Módulo Customers

Este módulo tiene un caso especial: `POST /api/Customers/register` **sí dispara
un correo** (confirmación de cuenta), así que ese endpoint puntual debe tratarse
con la misma expectativa que vimos en Auth (forgot-password, resend-code) —
si no está usando la cola en background que ya construimos, va a salir lento.//
Los demás endpoints (`me`, listar, ver por Id) no tienen correo de por medio,
así que deberían ser rápidos (milisegundos).

---

## Orden de prueba sugerido

| # | Endpoint | Requiere antes |
|---|---|---|
| 1 | `POST /api/Customers/register` | Nada |
| 2 | `GET /api/Customers/me` | Login como Cliente (después de confirmar el correo) |
| 3 | `PUT /api/Customers/me` | Paso 2 |
| 4 | `GET /api/Customers` | Login como Gerente/Administrador |
| 5 | `GET /api/Customers/{id}` | Paso 4 |

---

## Casos de prueba y qué anotar

### 1. `POST /api/Customers/register`
- ✅ Caso feliz: datos válidos → `201`, dispara el correo de confirmación
- ❌ Email ya registrado → `400`
- ❌ Password débil (si hay validación de fortaleza) → `400`
- ❌ Campos obligatorios faltantes (nombre, email, etc.) → `400`
- **Tiempo esperado:** si ya está usando `IBackgroundEmailQueue` (la misma cola que armamos en Auth) → **milisegundos**. Si no la está usando todavía → probablemente salga en **varios segundos**, igual que nos pasó con `forgot-password` antes de arreglarlo. Este es el primer candidato a revisar en el código.

### 2. `GET /api/Customers/me`
- ✅ Caso feliz: token de Cliente válido → `200` con sus propios datos
- ❌ Sin token → `401`
- ❌ Token de un Empleado (no Cliente) intentando entrar → `403`
- **Tiempo esperado:** < 100ms

### 3. `PUT /api/Customers/me`
- ✅ Caso feliz: actualiza su propio teléfono/dirección → `200`
- ❌ Intenta mandar un email que ya usa otro cliente → `400` (si hay esa validación)
- ❌ Campos inválidos (ej. teléfono con letras) → `400`
- **Tiempo esperado:** < 150ms

### 4. `GET /api/Customers` (Gerente/Administrador)
- ✅ Caso feliz → `200` con la lista completa de clientes
- ❌ Un Cliente intentando ver la lista de todos → `403`
- **Tiempo esperado:** ⚠️ **este es el que más hay que vigilar.** Si la tabla `Customer` ya tiene muchos registros y el endpoint trae relaciones de más (por ejemplo, todos sus `Order` u `Address` incluidos sin necesitarlos para un listado simple), el tiempo puede dispararse igual que nos pasó con `Branches` antes de aplicar la proyección SQL. Anota el tiempo con atención — si hay pocos clientes de prueba puede parecer rápido igual, aunque el problema exista.

### 5. `GET /api/Customers/{id}` (Gerente/Administrador)
- ✅ Id existente → `200`
- ❌ Id inexistente → `404`
- **Tiempo esperado:** < 100ms

---

## Tabla para ir llenando

| Endpoint | Caso | Status esperado | Status real | Tiempo | Resultado |
|---|---|---|---|---|---|
| `POST /api/Customers/register` | Registro válido | 201 | | | |
| `POST /api/Customers/register` | Email duplicado | 400 | | | |
| `POST /api/Customers/register` | Campos faltantes | 400 | | | |
| `GET /api/Customers/me` | Cliente ve su perfil | 200 | | | |
| `GET /api/Customers/me` | Sin token | 401 | | | |
| `GET /api/Customers/me` | Token de Empleado | 403 | | | |
| `PUT /api/Customers/me` | Actualización válida | 200 | | | |
| `PUT /api/Customers/me` | Datos inválidos | 400 | | | |
| `GET /api/Customers` | Gerente ve la lista | 200 | | | |
| `GET /api/Customers` | Cliente intenta ver la lista | 403 | | | |
| `GET /api/Customers/{id}` | Id existente | 200 | | | |
| `GET /api/Customers/{id}` | Id inexistente | 404 | | | |

---

## Por dónde empezar

Empieza por **`POST /api/Customers/register`** — es el más urgente de revisar
porque es el único con correo de por medio, y ya sabemos por experiencia
(forgot-password, resend-confirmation-code) que este patrón suele estar lento
si no se aplicó la cola en background todavía. Anota el tiempo exacto y lo
revisamos juntos antes de seguir con los demás.