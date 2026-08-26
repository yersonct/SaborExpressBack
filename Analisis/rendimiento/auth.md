# SaborExpress — Plan de pruebas funcionales + rendimiento: Módulo Auth

## 1. Qué vamos a medir en cada endpoint

| Métrica | Qué significa | Dónde se ve en Postman |
|---|---|---|
| **Tiempo de respuesta** | Cuánto tarda esa petición puntual en responder | Abajo a la derecha, ej. `184 ms` |
| **Status code** | Si respondió como se esperaba | Junto al tiempo, ej. `200 OK` / `401 Unauthorized` |
| **Tasa de errores** | De todos los intentos (éxito + error a propósito), cuántos fallaron *sin que fuera intencional* | Se calcula manual: (errores no esperados / total de pruebas) x 100 |
| **Throughput** | Cuántas peticiones aguanta por segundo | Se mide con Postman Runner o Collection Runner, no en una sola llamada |
| **Uso de CPU / RAM** | Cuánto consume el backend mientras se prueba | Administrador de tareas (Windows) o `dotnet-counters` si querés algo más fino |
| **Disponibilidad** | Que el endpoint responda de forma consistente en varias corridas seguidas | Repetir la misma prueba 3-5 veces y comparar |

**Regla simple para esta primera ronda:** por cada endpoint, mínimo 1 caso feliz + 1 caso de error, anotando status code y tiempo de respuesta. El throughput y CPU/RAM los dejamos para una segunda pasada cuando ya todos los endpoints individuales estén validados — no tiene sentido medir throughput de algo que todavía no sabemos si funciona bien.

---

## 2. Orden de prueba (por dependencias)

Como ya tenés un cliente registrado y confirmado, arrancamos directo en el paso 4.

| # | Endpoint | Método | Requiere antes | Estado |
|---|---|---|---|---|
| 1 | `/api/Customers/register` | POST | Nada | 🔲 |
| 2 | `/api/Auth/confirm-email` | POST | Paso 1 | 🔲 |
| 3 | `/api/Auth/resend-confirmation-code` | POST | Paso 1 | 🔲 |
| 4 | `/api/Auth/login` | POST | Paso 2 | 🔲 **Empezar aquí** |
| 5 | `/api/Auth/refresh-token` | POST | Paso 4 | 🔲 |
| 6 | `/api/Auth/logout` | POST | Paso 4 | 🔲 |
| 7 | `/api/Auth/forgot-password` | POST | Paso 1 | 🔲 |
| 8 | `/api/Auth/verify-reset-code` | POST | Paso 7 | 🔲 |
| 9 | `/api/Auth/reset-password` | POST | Paso 8 | 🔲 |
| 10 | `/api/Auth/activate-account` | POST | Empleado creado | 🔲 |

---

## 3. Casos de prueba sugeridos por endpoint

### 4. `POST /api/Auth/login`
- ✅ Caso feliz: email + password correctos → `200 OK` + `accessToken` + `refreshToken`
- ❌ Password incorrecta → `401`
- ❌ Email que no existe → `401` (mismo mensaje que password incorrecta, no debe revelar cuál falló)
- ❌ Email vacío/mal formado → `400`
- ❌ 5-6 intentos fallidos seguidos → verificar que se activa el bloqueo por intentos

### 5. `POST /api/Auth/refresh-token`
- ✅ Caso feliz: `refreshToken` válido → `200 OK` + tokens nuevos (verificar que el `refreshToken` viejo ya no sirve, por la rotación)
- ❌ `refreshToken` inválido/inventado → `401`
- ❌ `refreshToken` ya usado (reintentar el mismo dos veces) → `401`

### 6. `POST /api/Auth/logout`
- ✅ Caso feliz: con `accessToken` válido en el header → `200 OK`
- ❌ Intentar usar el mismo `refreshToken` después del logout → debe fallar (quedó revocado)
- ❌ Sin header de autorización → `401`

### 7. `POST /api/Auth/forgot-password`
- ✅ Caso feliz: email existente → `200 OK` (mensaje genérico)
- ✅ Caso "trampa": email que NO existe → también `200 OK` con el mismo mensaje (así se valida que no filtra info)
- ❌ Repetir la petición muy seguido (4ta vez en menos de 15 min) → debe bloquear por rate-limit

### 8. `POST /api/Auth/verify-reset-code`
- ✅ Caso feliz: código correcto → `200 OK` + `resetToken`
- ❌ Código incorrecto → `400`
- ❌ Código correcto pero repetido 6 veces (superando el máximo de 5 intentos) → debe bloquear

### 9. `POST /api/Auth/reset-password`
- ✅ Caso feliz: `resetToken` válido + password nueva → `200 OK`
- ❌ Reutilizar el mismo `resetToken` dos veces → debe fallar la segunda vez
- ❌ `resetToken` expirado (si podés simular esperando o cambiando la hora) → `400`

### 10. `POST /api/Auth/activate-account`
- ✅ Caso feliz: código del empleado + password nueva → `200 OK`, `User.Status = true`
- ❌ Código incorrecto → `400`
- ❌ Código correcto pero después de 30 min (expirado) → `400`

---

## 4. Tabla para ir llenando resultados

> Copia esta tabla en Postman/Excel/Notion y ve llenándola a medida que pruebes. Un ✅ o ❌ rápido en "Resultado" te sirve para ver de un vistazo qué falta.

| Endpoint | Caso | Status esperado | Status real | Tiempo | Resultado |
|---|---|---|---|---|---|
| `POST /api/Auth/login` | Login correcto | 200 | | | |
| `POST /api/Auth/login` | Password incorrecta | 401 | | | |
| `POST /api/Auth/login` | Email faltante | 400 | | | |
| `POST /api/Auth/login` | Bloqueo por intentos | 401/423 | | | |
| `POST /api/Auth/refresh-token` | Refresh válido | 200 | | | |
| `POST /api/Auth/refresh-token` | Token viejo reutilizado | 401 | | | |
| `POST /api/Auth/logout` | Logout correcto | 200 | | | |
| `POST /api/Auth/logout` | Sin token | 401 | | | |
| `POST /api/Auth/forgot-password` | Email existente | 200 | | | |
| `POST /api/Auth/forgot-password` | Email no existente | 200 | | | |
| `POST /api/Auth/verify-reset-code` | Código correcto | 200 | | | |
| `POST /api/Auth/verify-reset-code` | Código incorrecto | 400 | | | |
| `POST /api/Auth/reset-password` | Reset correcto | 200 | | | |
| `POST /api/Auth/reset-password` | Token reutilizado | 400/401 | | | |
| `POST /api/Auth/activate-account` | Activación correcta | 200 | | | |
| `POST /api/Auth/activate-account` | Código expirado | 400 | | | |

---

## 5. Notas para la segunda pasada (throughput, CPU, RAM)

Cuando ya todos los casos de arriba estén en ✅, esto es lo que sigue:

- **Throughput:** en Postman, usar el **Collection Runner** o **Postman Runtime (Newman)** para lanzar la misma colección 50-100 veces seguidas y ver cuántas por segundo aguanta, sobre todo en `/login` (es el endpoint más golpeado en producción real).
- **CPU/RAM:** dejar el backend corriendo local y observar el Administrador de Tareas (o `dotnet-counters monitor -p <PID>` si querés algo más preciso) mientras corre el Runner del punto anterior.
- **Disponibilidad:** correr la colección completa 3 veces en momentos distintos del día y comparar si los tiempos se mantienen estables o se disparan.

Esto lo dejamos para después de cerrar Auth — no tiene sentido medir carga de algo que todavía no confirmamos que funciona bien en el caso normal.