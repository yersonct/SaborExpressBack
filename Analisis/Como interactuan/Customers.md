# SaborExpress — Guía de front: cómo interactúa el Cliente y el personal con "Customers"

Este módulo tiene dos audiencias completamente distintas usando los mismos
datos: el **propio Cliente** (que solo ve y edita su perfil) y el
**Gerente/Administrador** (que consulta la lista completa para soporte,
reportes o atención de quejas).

---

## Vista del Cliente: "Mi cuenta"

### 1. Registro (antes de que exista sesión)

```
Ana descarga la app y toca "Crear cuenta"
        ↓
Front muestra formulario: Nombre, Apellido, Email, Password, Teléfono, Dirección
        ↓
Ana llena y da "Registrarme"
        ↓
Front llama: POST /api/Customers/register
        ↓
Backend crea User + Customer, le asigna el rol CLIENTE,
y dispara el correo de confirmación (en background, sin que Ana espere)
        ↓
201 Created
```

### Ejemplo de request

```json
POST /api/Customers/register
{
  "name": "Ana",
  "lastName": "Torres",
  "email": "ana@gmail.com",
  "password": "Ana12345.",
  "phone": "3009876543",
  "address": "Calle 10 # 5-20"
}
```

### Qué le muestra el front

```
"¡Listo! Revisa tu correo para confirmar tu cuenta antes de iniciar sesión."
```

Y la redirige a la pantalla de **"Ingresa tu código de confirmación"** —
mismo flujo que ya armamos para Auth (`confirm-email` / `resend-confirmation-code`).

---

### 2. Ver mi perfil

```
Ana ya inició sesión → entra a "Mi cuenta"
        ↓
Front llama: GET /api/Customers/me
(con su Authorization: Bearer {token})
        ↓
Backend identifica a Ana por el token, sin que ella mande su propio Id
        ↓
Responde con sus datos
```

### Ejemplo de respuesta

```json
{
  "id": 2,
  "name": "Ana",
  "lastName": "Torres",
  "email": "ana@gmail.com",
  "phone": "3009876543",
  "address": "Calle 10 # 5-20"
}
```

### Cómo se pinta

Una pantalla simple de perfil, con los datos precargados y un botón "Editar":

```
┌─────────────────────────────┐
│  👤 Ana Torres               │
│  ✉️ ana@gmail.com             │
│  📞 300 987 6543             │
│  📍 Calle 10 # 5-20          │
│                              │
│  [ ✏️ Editar mis datos ]      │
└─────────────────────────────┘
```

---

### 3. Editar mi perfil

```
Ana toca "Editar mis datos"
        ↓
Front abre el formulario precargado (no hace falta otro GET)
        ↓
Ana cambia su teléfono
        ↓
Front llama: PUT /api/Customers/me
        ↓
200 OK → toast "Datos actualizados correctamente"
```

**Importante para el front:** al igual que `GET /me`, este endpoint **no
recibe el Id de Ana en la URL** — el backend siempre identifica "quién soy yo"
a partir del token, nunca hay que mandarlo manualmente. Esto evita que alguien
intente editar el perfil de otro cliente cambiando un número en la URL.

---

## Vista del Gerente/Administrador: "Clientes"

### 1. Listar todos los clientes

```
Gerente entra a "Clientes" en el panel
        ↓
Front llama: GET /api/Customers
        ↓
Backend responde con la lista completa
```

### Ejemplo de respuesta

```json
[
  { "id": 1, "name": "Carlos", "lastName": "Pérez", "email": "carlos@gmail.com", "phone": "3001112222" },
  { "id": 2, "name": "Ana", "lastName": "Torres", "email": "ana@gmail.com", "phone": "3009876543" }
]
```

### Cómo se pinta

Una tabla simple, con buscador por nombre/email si la lista crece mucho:

| Nombre | Email | Teléfono | Acciones |
|---|---|---|---|
| Carlos Pérez | carlos@gmail.com | 300 111 2222 | 👁️ Ver detalle |
| Ana Torres | ana@gmail.com | 300 987 6543 | 👁️ Ver detalle |

**Nota importante:** a diferencia de `Employees`, aquí **no hay acciones de
editar/desactivar** desde el panel — el Gerente/Administrador solo puede
**consultar**, no modificar los datos de un cliente. Si más adelante necesitas
que el personal pueda editar datos de un cliente (por ejemplo, corregir una
dirección mal escrita en una llamada de soporte), eso sería un endpoint nuevo
que hoy no existe (`PUT /api/Customers/{id}` administrado, distinto del
`PUT /api/Customers/me` que es solo para el propio cliente).

---

### 2. Ver el detalle de un cliente puntual

```
Gerente toca "Ver detalle" en la fila de Ana
        ↓
Front llama: GET /api/Customers/2
        ↓
Responde con los mismos datos que Ana vería en "Mi cuenta"
```

Esta pantalla es útil, por ejemplo, cuando llega una queja de un cliente y el
Administrador necesita confirmar sus datos de contacto rápidamente.

---

## Resumen: reglas para el front de este módulo

1. **`me` nunca lleva Id en la URL** — el backend siempre sabe quién eres por el token. Nunca construyas la URL como `/api/Customers/me/2`, siempre es literal `/api/Customers/me`.
2. **El Cliente nunca ve ni edita a otros clientes** — solo tiene acceso a su propio perfil.
3. **El Gerente/Administrador solo puede consultar (GET), no editar clientes desde el panel** — si necesitas esa función, es un endpoint nuevo por construir.
4. **El registro dispara un correo** — el front debe manejar el estado de "pendiente de confirmación" antes de permitir el login, igual que ya hicimos con empleados nuevos.

---

## Próximo paso sugerido

Cuando construyas el front, un solo `customersApi` con estas funciones cubre
todo el módulo: `register()`, `getMe()`, `updateMe()`, `getAll()` (solo
Gerente/Administrador), `getById()` (solo Gerente/Administrador). La pantalla
de "Mi cuenta" y la de "Clientes" (panel admin) consumen ese mismo servicio,
cada una llamando solo a las funciones que su rol tiene permitido usar.