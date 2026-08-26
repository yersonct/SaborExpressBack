# SaborExpress — Guía de front (React): módulo UserRoles

Este módulo es exclusivamente administrativo — no tiene vista para el propio
empleado, solo lo usan Gerente y Administrador para gestionar qué rol(es)
tiene cada trabajador. No confundir con `Employees` (que crea la persona) ni
con `Roles` (que crea el catálogo de roles disponibles) — `UserRoles` es la
tabla puente que conecta ambos.

---

## El servicio de API (`userRolesApi.js`)

```javascript
const API_BASE = "http://localhost:5050/api/user-roles";

function getAuthHeaders() {
  const token = localStorage.getItem("token");
  return {
    "Content-Type": "application/json",
    "Authorization": `Bearer ${token}`
  };
}

export const userRolesApi = {
  // Asigna un rol a un usuario
  async assign(userId, roleId) {
    const res = await fetch(API_BASE, {
      method: "POST",
      headers: getAuthHeaders(),
      body: JSON.stringify({ userId, roleId })
    });
    if (!res.ok) throw await res.json();
    return res.json();
  },

  // Quita un rol de un usuario
  async remove(userId, roleId) {
    const res = await fetch(`${API_BASE}/${userId}/${roleId}`, {
      method: "DELETE",
      headers: getAuthHeaders()
    });
    if (!res.ok) throw await res.json();
  },

  // Lista todas las asignaciones (para un panel general, poco usado en la práctica)
  async getAll() {
    const res = await fetch(API_BASE, { headers: getAuthHeaders() });
    if (!res.ok) throw await res.json();
    return res.json();
  },

  // Lista los roles de UN usuario puntual (el más usado, va dentro del
  // detalle de empleado)
  async getByUser(userId) {
    const res = await fetch(`${API_BASE}/user/${userId}`, { headers: getAuthHeaders() });
    if (!res.ok) throw await res.json();
    return res.json();
  }
};
```

---

## Dónde vive esto en la interfaz: dentro del detalle de un empleado

**Importante:** `UserRoles` casi nunca es una pantalla propia — vive **dentro**
de la pantalla de detalle/edición de un empleado (`EmployeeDetail.jsx`), como
una sección más, junto a nombre, sede, teléfono, etc.

```
┌─────────────────────────────────────┐
│  Luis Gómez — Detalle de empleado    │
│                                       │
│  Documento: 1023456789               │
│  Sede: Sede Centro                   │
│                                       │
│  Roles asignados:                    │
│  ┌─────────────┐  ┌───────────┐      │
│  │ Mesero   ✕  │  │ Cajero  ✕ │      │
│  └─────────────┘  └───────────┘      │
│                                       │
│  [ + Agregar rol ▾ ]                 │
└─────────────────────────────────────┘
```

Cada "chip" de rol tiene su botón ✕ para quitarlo (llama a `remove`), y el
botón "+ Agregar rol" abre un selector con los roles que el empleado **todavía
no tiene** (llama a `assign`).

---

## Componente: `EmployeeRolesSection.jsx`

```jsx
import { useState, useEffect } from "react";
import { userRolesApi } from "../api/userRolesApi";
import { rolesApi } from "../api/rolesApi"; // el catalogo de roles (modulo Roles)

export function EmployeeRolesSection({ userId }) {
  const [assignedRoles, setAssignedRoles] = useState([]);
  const [allRoles, setAllRoles] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [showAddMenu, setShowAddMenu] = useState(false);

  useEffect(() => {
    loadData();
  }, [userId]);

  async function loadData() {
    setLoading(true);
    try {
      const [assigned, catalog] = await Promise.all([
        userRolesApi.getByUser(userId),
        rolesApi.getAll()
      ]);
      setAssignedRoles(assigned);
      setAllRoles(catalog);
    } catch (err) {
      setError("No se pudieron cargar los roles.");
    } finally {
      setLoading(false);
    }
  }

  async function handleAssign(roleId) {
    setError(null);
    try {
      await userRolesApi.assign(userId, roleId);
      setShowAddMenu(false);
      await loadData(); // refresca la lista tras asignar
    } catch (err) {
      // El backend devuelve mensajes claros de negocio, ej:
      // "Esta sede ya tiene un Administrador asignado."
      setError(err.error || "No se pudo asignar el rol.");
    }
  }

  async function handleRemove(roleId) {
    setError(null);
    try {
      await userRolesApi.remove(userId, roleId);
      await loadData();
    } catch (err) {
      // Ej: "No puedes quitar el rol de Administrador porque es el unico de esta sede."
      setError(err.error || "No se pudo quitar el rol.");
    }
  }

  if (loading) return <p>Cargando roles...</p>;

  const roleIdsAssigned = new Set(assignedRoles.map(r => r.roleId));
  const availableToAdd = allRoles.filter(r => !roleIdsAssigned.has(r.id));

  return (
    <div>
      <h3>Roles asignados</h3>

      {error && <div className="error-banner">{error}</div>}

      <div className="role-chips">
        {assignedRoles.map(r => (
          <span key={r.roleId} className="role-chip">
            {r.roleName}
            <button onClick={() => handleRemove(r.roleId)} title="Quitar rol">✕</button>
          </span>
        ))}
        {assignedRoles.length === 0 && <p>Este empleado no tiene roles asignados.</p>}
      </div>

      <div className="add-role">
        <button onClick={() => setShowAddMenu(!showAddMenu)}>+ Agregar rol</button>

        {showAddMenu && (
          <ul className="role-dropdown">
            {availableToAdd.map(r => (
              <li key={r.id} onClick={() => handleAssign(r.id)}>
                {r.name}
              </li>
            ))}
            {availableToAdd.length === 0 && <li>Ya tiene todos los roles disponibles</li>}
          </ul>
        )}
      </div>
    </div>
  );
}
```

---

## Los mensajes de error que el backend ya te resuelve — no los repitas

El backend valida bastantes reglas de negocio y devuelve el mensaje exacto en
`{ "error": "..." }`. El front **no necesita reimplementar esa lógica** —
solo mostrar el mensaje tal cual llega. Ejemplos reales que vas a ver:

| Situación | Mensaje que devuelve el backend |
|---|---|
| El usuario ya tiene ese rol | `"El usuario 'x@mail.com' ya tiene asignado el rol 'Mesero'."` |
| Un Administrador intenta asignar rol de Gerente | `"Un Administrador no puede asignar el rol de Administrador ni Gerente. Solo un Gerente puede hacerlo."` |
| La sede ya tiene un Administrador | `"Esta sede ya tiene un Administrador asignado."` |
| Se intenta quitar el único Administrador de una sede | `"No puedes quitar el rol de Administrador porque es el único de esta sede. Asigna otro Administrador antes de quitar este."` |
| Un Administrador intenta gestionar roles de otra sede | `"Solo puedes asignar roles a empleados de tu misma sede."` |

Por eso el componente de arriba usa `err.error` directo en el mensaje de
error — es la forma correcta de aprovechar esas validaciones sin duplicar
reglas de negocio en el front.

---

## Nota importante: `UserId` vs `EmployeeId`

Ojo con esto, es una confusión común: los endpoints de `UserRoles` trabajan
con **`UserId`** (el `Id` de la cuenta, tabla `User`), **no** con `EmployeeId`
(el `Id` del perfil de empleado). Si tu pantalla de detalle de empleado
obtuvo los datos desde `GET /api/Employees/{id}`, fijate que la respuesta
trae `userId` como campo — es ese el que hay que pasarle a este componente,
no el `id` del empleado.

```jsx
// Ejemplo de uso desde la pantalla de detalle
<EmployeeRolesSection userId={employee.userId} />
```

---

## Resumen

1. `UserRoles` no es una pantalla propia — es una sección dentro del detalle de un empleado.
2. Usa siempre `userId`, no `employeeId`.
3. El backend ya valida todas las reglas de negocio (sede, Administrador único, duplicados) — el front solo muestra el mensaje de error tal cual llega.
4. Después de asignar o quitar un rol, siempre recargar la lista (`getByUser`) para reflejar el estado real.