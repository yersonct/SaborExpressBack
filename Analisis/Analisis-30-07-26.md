# Análisis del Modelo Entidad–Relación de SaborExpress

## ¿Cómo interpreto el modelo?

El diagrama representa el **módulo de autenticación, autorización y gestión de usuarios** de un sistema para un restaurante llamado **SaborExpress**. Su propósito es controlar quién puede acceder al sistema, qué permisos posee cada usuario y cómo se relacionan empleados, clientes y sucursales.

## Entidades

| Entidad | Descripción |
|----------|-------------|
| **USERS** | Almacena las cuentas de acceso al sistema. |
| **EMPLOYEES** | Contiene la información laboral de un usuario que trabaja en el restaurante. |
| **CUSTOMERS** | Guarda la información de los clientes registrados. |
| **BRANCHES** | Representa las sedes o sucursales del restaurante. |
| **ROLES** | Define los roles del sistema (Administrador, Cajero, Mesero, Cocinero, Gerente, etc.). |
| **PERMISSIONS** | Contiene los permisos específicos que pueden asignarse a un rol. |
| **USER_ROLES** | Tabla puente que relaciona usuarios con roles (Muchos a Muchos). |
| **ROLE_PERMISSIONS** | Tabla puente que relaciona roles con permisos (Muchos a Muchos). |
| **PASSWORD_RESET_CODES** | Almacena los códigos utilizados para recuperar la contraseña de un usuario. |

---

# Relaciones

## 1. USERS → EMPLOYEES

```text
USERS (1) -------- (0..1) EMPLOYEES
```

Cada empleado tiene un usuario.

### Ejemplo

```text
Usuario
-------
id = 5
email = juan@sabor.com

Empleado
---------
id = 2
user_id = 5
document = 10203040
```

### ✔ Pros

- Muy buena separación entre autenticación y datos laborales.
- El usuario sirve únicamente para autenticación.
- El empleado almacena únicamente información laboral.
- Permite agregar información del empleado sin llenar la tabla de usuarios.
- Hace el sistema más organizado y escalable.

### ❌ Contras<- separacion de roles 

No todos los usuarios necesariamente son empleados.

Ejemplos:

- Cliente
- Superadministrador
- API

Esto realmente no representa un problema porque la relación es opcional.

> **Calificación:** ⭐⭐⭐⭐⭐ (10/10)

---

## 2. USERS → CUSTOMERS

```text
USERS (1) -------- (0..1) CUSTOMERS
```

Un usuario puede ser un cliente.

### Ejemplo

```text
Usuario
   │
   ▼
Cliente
```

### ✔ Pros

- Excelente diseño.
- Existe un único sistema de autenticación.
- No es necesario crear una tabla `LOGIN_CLIENTES`.
- Evita duplicar información.

### ❌ Contras<- tabla pedido dependiendo del rol(cajero o mesero)

Puede existir un caso donde una misma persona sea empleado y cliente.

Ejemplo:

```text
Juan

│

├── Empleado

└── Cliente
```

El modelo sí lo permite.

Esto no está mal; de hecho muchas empresas funcionan de esta manera.

---

## 3. BRANCHES → EMPLOYEES

```text
Sucursal

│

└── Muchos empleados
```

Cada empleado pertenece a una sede.

### Ejemplo

```text
Sucursal Norte

• Juan
• Carlos
• Ana
```

### ✔ Pros

- Muy correcto.
- Representa exactamente cómo funciona un restaurante.
- Facilita la organización del personal por sucursal.

### ❌ Contras <- opcional si el dia de la mañana quiere un empleado o lo pone en otra sede 

Si un empleado trabaja en dos sedes, el modelo actual no lo permite.

Actualmente solo existe:

```text
EMPLOYEES

branch_id
```

Lo que significa:

```text
Empleado → Una sola sucursal
```

Sin embargo, muchos restaurantes manejan empleados rotativos entre varias sedes.

Una mejor solución sería crear una tabla puente:

```text
EMPLOYEE_BRANCHES

employee_id
branch_id
```

Con esto se obtiene una relación **Muchos a Muchos** entre empleados y sucursales.

---

## 4. USERS ↔ ROLES

Se utiliza una tabla puente:

```text
USER_ROLES
```

Esto permite que un usuario tenga varios roles.

### Ejemplo

```text
Usuario

│

├── Administrador

├── Cajero

└── Mesero
```

### ✔ Pros

- Excelente decisión de diseño.
- Muy escalable.
- Un usuario puede desempeñar múltiples funciones.
- Evita duplicar usuarios.

Ejemplo:

```text
Administrador

+

Cajero

+

Supervisor
```

### ❌ Contras<- Analisar 

Si el sistema nunca va a manejar múltiples roles por usuario, la tabla puente sería innecesaria.

Podría simplificarse a:

```text
USERS

role_id
```

Sin embargo, mantener la tabla puente es una mejor práctica porque facilita futuras ampliaciones.

---

## 5. ROLES ↔ PERMISSIONS

También se utiliza una tabla puente:

```text
ROLE_PERMISSIONS
```

Este diseño implementa el modelo **RBAC (Role-Based Access Control)**.

### Ejemplo

```text
Administrador

│

├── Crear usuarios

├── Eliminar pedidos

└── Cambiar precios
```

Mientras que:

```text
Mesero

│

├── Crear pedidos

└── Ver mesas
```

### ✔ Pros

- Excelente diseño.
- Muy profesional.
- Fácil de administrar.
- Escalable.
- Permite modificar permisos sin afectar directamente a los usuarios.

Es el modelo utilizado por frameworks como:

- Laravel
- Django
- Spring Security
- ASP.NET Identity

### ❌ Contras

No presenta desventajas importantes.

---

## 6. PASSWORD_RESET_CODES

Relación:

```text
Usuario

│

└── Muchos códigos de recuperación
```

Cada vez que un usuario solicita recuperar su contraseña se genera un nuevo código.

### ✔ Pros

- Correcto.
- Permite múltiples solicitudes de recuperación.
- Facilita invalidar códigos anteriores.

### ❌ Contras

Sería recomendable agregar algunos campos adicionales para mejorar la seguridad:

```text
code
expires_at
used_at
created_at
ip
attempts
```

Con estos campos se podría:

- Definir una fecha de expiración.
- Evitar reutilizar un código.
- Registrar cuándo fue utilizado.
- Guardar la IP desde donde se solicitó.
- Limitar la cantidad de intentos.

---

# Lo que me gusta del modelo

- ✔ Separa correctamente la autenticación de la lógica del negocio.
- ✔ Roles y permisos completamente independientes.
- ✔ Excelente uso de tablas puente para relaciones Muchos a Muchos.
- ✔ Clientes separados de empleados.
- ✔ Usuarios centralizados en una única tabla.
- ✔ Modelo limpio, modular y fácil de mantener.
- ✔ Muy escalable para agregar nuevos módulos como pedidos, inventario o facturación.
- ✔ Sigue buenas prácticas de diseño utilizadas en sistemas empresariales modernos.

![Diagrama ER de SaborExpress](relacion%2030%20jul%202026,%2009_58_34%20p.m..png)