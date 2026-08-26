# SaborExpress — Guía de front (React): módulo RolePermissions

Este módulo es exclusivamente administrativo, y **solo lo usa el Gerente**
(no Administrador, no Cliente) — define qué permisos tiene cada rol del
catálogo. No confundir con `UserRoles` (que asigna roles a personas) — este
módulo define **qué puede hacer** cada rol, no quién lo tiene.

**Recordatorio del concepto:** `RolePermission` es la regla general ("todos
los Meseros pueden editar pedidos"), a diferencia de excepciones puntuales
por persona (que ya no existen en el sistema, se decidió simplificar).

---

## El servicio de API (`rolePermissionsApi.js`)

```javascript
const API_BASE = "http://localhost:5050/api/role-permissions";

function getAuthHeaders() {
  const token = localStorage.getItem("token");
  return {
    "Content-Type": "application/json",
    "Authorization": `Bearer ${token}`
  };
}

export const rolePermissionsApi = {
  // Asigna un permiso a un rol
  async assign(roleId, permissionId) {
    const res = await fetch(API_BASE, {
      method: "POST",
      headers: getAuthHeaders(),
      body: JSON.stringify({ roleId, permissionId })
    });
    if (!res.ok) throw await res.json();
    return res.json();
  },

  // Quita un permiso de un rol
  async remove(roleId, permissionId) {
    const res = await fetch(`${API_BASE}/${roleId}/${permissionId}`, {
      method: "DELETE",
      headers: getAuthHeaders()
    });
    if (!res.ok) throw await res.json();
  },

  // Lista todas las asignaciones (poco usado directo, sirve para reportes)
  async getAll() {
    const res = await fetch(API_BASE, { headers: getAuthHeaders() });
    if (!res.ok) throw await res.json();
    return res.json();
  },

  // Lista los permisos de UN rol puntual (el más usado, va en la pantalla
  // de "editar rol")
  async getByRole(roleId) {
    const res = await fetch(`${API_BASE}/role/${roleId}`, { headers: getAuthHeaders() });
    if (!res.ok) throw await res.json();
    return res.json();
  }
};
```

---

## Dónde vive esto en la interfaz: dentro del detalle de un rol

Igual que `UserRoles` vivía dentro del detalle de un **empleado**, este
módulo vive dentro del detalle de un **rol** (`RoleDetail.jsx`), en la
pantalla de "Roles" del panel de Gerente — no es una pantalla propia.

```
┌───────────────────────────────────────────┐
│  Mesero — Detalle de rol                   │
│                                             │
│  Nombre: Mesero                            │
│  Descripción: Atiende mesas y toma pedidos │
│                                             │
│  Permisos asignados:                       │
│  ┌──────────────┐  ┌───────────────┐       │
│  │ ORDERS_VIEW ✕│  │ ORDERS_EDIT ✕ │       │
│  └──────────────┘  └───────────────┘       │
│                                             │
│  [ + Agregar permiso ▾ ]                   │
└───────────────────────────────────────────┘
```

---

## Componente: `RolePermissionsSection.jsx`

```jsx
import { useState, useEffect } from "react";
import { rolePermissionsApi } from "../api/rolePermissionsApi";
import { permissionsApi } from "../api/permissionsApi"; // el catalogo de permisos (modulo Permissions)

export function RolePermissionsSection({ roleId }) {
  const [assignedPermissions, setAssignedPermissions] = useState([]);
  const [allPermissions, setAllPermissions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [showAddMenu, setShowAddMenu] = useState(false);

  useEffect(() => {
    loadData();
  }, [roleId]);

  async function loadData() {
    setLoading(true);
    try {
      const [assigned, catalog] = await Promise.all([
        rolePermissionsApi.getByRole(roleId),
        permissionsApi.getAll()
      ]);
      setAssignedPermissions(assigned);
      setAllPermissions(catalog);
    } catch (err) {
      setError("No se pudieron cargar los permisos.");
    } finally {
      setLoading(false);
    }
  }

  async function handleAssign(permissionId) {
    setError(null);
    try {
      await rolePermissionsApi.assign(roleId, permissionId);
      setShowAddMenu(false);
      await loadData();
    } catch (err) {
      setError(err.error || "No se pudo asignar el permiso.");
    }
  }

  async function handleRemove(permissionId) {
    setError(null);
    try {
      await rolePermissionsApi.remove(roleId, permissionId);
      await loadData();
    } catch (err) {
      setError(err.error || "No se pudo quitar el permiso.");
    }
  }

  if (loading) return <p>Cargando permisos...</p>;

  const permissionIdsAssigned = new Set(assignedPermissions.map(p => p.permissionId));
  const availableToAdd = allPermissions.filter(p => !permissionIdsAssigned.has(p.id));

  return (
    <div>
      <h3>Permisos asignados</h3>

      {error && <div className="error-banner">{error}</div>}

      <div className="permission-chips">
        {assignedPermissions.map(p => (
          <span key={p.permissionId} className="permission-chip">
            {p.permissionName}
            <button onClick={() => handleRemove(p.permissionId)} title="Quitar permiso">✕</button>
          </span>
        ))}
        {assignedPermissions.length === 0 && <p>Este rol no tiene permisos asignados.</p>}
      </div>

      <div className="add-permission">
        <button onClick={() => setShowAddMenu(!showAddMenu)}>+ Agregar permiso</button>

        {showAddMenu && (
          <ul className="permission-dropdown">
            {availableToAdd.map(p => (
              <li key={p.id} onClick={() => handleAssign(p.id)}>
                {p.name}
                {p.description && <small> — {p.description}</small>}
              </li>
            ))}
            {availableToAdd.length === 0 && <li>Ya tiene todos los permisos disponibles</li>}
          </ul>
        )}
      </div>
    </div>
  );
}
```

---

## Diferencia clave con `UserRoles` (para no confundirlos en el front)

| | `UserRoles` | `RolePermissions` |
|---|---|---|
| Conecta | Un `User` con un `Role` | Un `Role` con un `Permission` |
| Lo usa | Gerente y Administrador | Solo Gerente |
| Vive dentro de | Detalle de un **empleado** | Detalle de un **rol** |
| Pregunta que responde | "¿qué roles tiene Juan?" | "¿qué puede hacer un Mesero?" |
| IDs que usa | `userId` + `roleId` | `roleId` + `permissionId` |

**Por qué esto importa para el front:** si editás el rol "Mesero" acá y le
quitás el permiso `ORDERS_EDIT`, el cambio aplica **a todos los Meseros del
sistema al instante** — no es una configuración por persona. Vale la pena que
la pantalla lo deje claro con algún texto de advertencia, algo como *"Los
cambios aquí afectan a todos los empleados con este rol."*

---

## El efecto sobre los turnos (contexto para entender el impacto real)

Este módulo se conecta con el sistema de turnos que armamos: cuando un
empleado tiene un turno activo con rol "Mesero", sus permisos reales en ese
momento son **exactamente** los que estén configurados acá para el rol
Mesero — no hay ninguna excepción individual posible. Si el Gerente quita un
permiso de un rol, el efecto es inmediato para todos los que estén trabajando
con ese rol en ese momento.

---

## Resumen

1. `RolePermissions` no es una pantalla propia — vive dentro del detalle de un rol.
2. Usa `roleId` + `permissionId`, no `userId`.
3. Solo el Gerente debería poder acceder a esta sección (el Administrador no gestiona el catálogo de roles/permisos).
4. Los cambios acá afectan a **todos** los empleados con ese rol al instante — conviene un aviso visual claro en la interfaz.
5. Después de asignar o quitar un permiso, siempre recargar la lista (`getByRole`).