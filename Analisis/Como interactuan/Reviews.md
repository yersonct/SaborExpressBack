## 18. Reviews (`Review`)

**Qué es:** calificación que deja el cliente sobre un pedido ya entregado,
**solo si lo pidió él mismo desde la app** (no aplica a pedidos tomados por
mesero/cajero en mostrador — se distingue con el campo `Order.Channel`).

**Se relaciona con:** `Order` (1 a 1, protegido con índice único en `OrderId`
a nivel de base de datos), `Customer` (muchos a 1).

| Método | Endpoint | Quién | Estado |
|---|---|---|---|
| POST | `/api/Reviews` | Cliente (dueño del pedido) | 🟢 Completo — valida pedido `Delivered` + `Channel=App`, pertenece al cliente, no duplicado |
| PUT | `/api/Reviews/{id}` | Cliente (dueño de la reseña) | 🟢 **Nuevo** — antes el código existía suelto (DTO + Validator) pero nunca se conectó a un endpoint real |
| GET | `/api/Reviews/order/{orderId}` | Cualquier autenticado | 🟢 Completo |
| GET | `/api/Reviews/customer/{customerId}` | Cliente (solo lo suyo) / Gerente / Administrador (cualquiera) | 🟢 Completo — restricción de ownership aplicada en el Controller |
| GET | `/api/Reviews/branch/{branchId}` | Gerente (cualquier sede) / Administrador (solo su sede) | 🟢 Completo — filtro de sede aplicado en el Service |
| GET | `/api/Reviews/branch/{branchId}/summary` | Gerente | 🟢 **Nuevo** — el cálculo de promedio ya existía en el Repository, nunca se exponía |
| DELETE | `/api/Reviews/{id}` | Dueño / Gerente (cualquiera) / Administrador (solo su sede) | 🟢 **Corregido** — antes cualquier usuario autenticado podía borrar la reseña de cualquier otro cliente; el `ValidateDelete` existía pero nunca se llamaba |

**Correcciones aplicadas en esta revisión:**
- `[Authorize]` agregado a todo el Controller (antes no tenía ninguna restricción de autenticación).
- `GetCurrentCustomerId()`/`GetCurrentUserId()` reemplazados por las extensiones centralizadas de `ControllerBaseExtensions` (antes había un método privado que devolvía `0` en silencio si el claim faltaba).
- `DeleteAsync` ahora valida ownership/rol antes de borrar — hueco de seguridad cerrado.
- `GetByBranchIdAsync` ahora restringe al Administrador a su propia sede (antes no filtraba nada).

**Pendiente de confirmar:** correr en Swagger un caso de prueba real por rol (Cliente dueño, Cliente ajeno, Gerente, Administrador de otra sede) para confirmar que las 4 combinaciones responden como se espera antes de dar el módulo por 100% cerrado.